 '------------------------------------------------------------------------------
' <copyright from='1997' to='2001' company='Microsoft Corporation'>           
'    Copyright (c) Microsoft Corporation. All Rights Reserved.                
'    Information Contained Herein is Proprietary and Confidential.            
' </copyright>                                                                
'------------------------------------------------------------------------------
' Copyright (C) 1997, 1998, 1999 Microsoft Corporation  All Rights Reserved

Imports System
Imports Microsoft.Win32
Imports Marshal = System.Runtime.InteropServices.Marshal

Namespace Microsoft.VisualStudio.Editors.AppDesInterop


' Users can make their classes implement this interface to get access to all
' the constants in the Win32 API easily.
    <System.Runtime.InteropServices.ComVisible(False)> _
Public Class win
'    Public Const APPCMD_CLIENTONLY As Integer = &H10
'    Public Const APPCMD_FILTERINITS As Integer = &H20
'    Public Const APPCMD_MASK As Integer = &HFF0
'    Public Const APPCLASS_STANDARD As Integer = &H0
'    Public Const APPCLASS_MASK As Integer = &HF
'    Public Const APPCLASS_MONITOR As Integer = &H1
'    Public Const ATTR_INPUT As Integer = &H0
'    Public Const ATTR_TARGET_CONVERTED As Integer = &H1
'    Public Const ATTR_CONVERTED As Integer = &H2
'    Public Const ATTR_TARGET_NOTCONVERTED As Integer = &H3
'    Public Const ATTR_INPUT_ERROR As Integer = &H4
'    Public Const AUXCAPS_CDAUDIO As Integer = 1
'    Public Const AUXCAPS_AUXIN As Integer = 2
'    Public Const AUXCAPS_VOLUME As Integer = &H1
'    Public Const AUXCAPS_LRVOLUME As Integer = &H2
    '    Public Const ASYNCH As Integer = &h80
    '    Public Const ASYNC_MODE_COMPATIBILITY As Integer = &H1
'    Public Const ASYNC_MODE_DEFAULT As Integer = &H0
'    Public Const ACTIVEOBJECT_STRONG As Integer = &H0
'    Public Const ACTIVEOBJECT_WEAK As Integer = &H1
'    Public Const ABM_NEW As Integer = &H0
'    Public Const ABM_REMOVE As Integer = &H1
'    Public Const ABM_QUERYPOS As Integer = &H2
'    Public Const ABM_SETPOS As Integer = &H3
'    Public Const ABM_GETSTATE As Integer = &H4
'    Public Const ABM_GETTASKBARPOS As Integer = &H5
'    Public Const ABM_ACTIVATE As Integer = &H6
'    Public Const ABM_GETAUTOHIDEBAR As Integer = &H7
'    Public Const ABM_SETAUTOHIDEBAR As Integer = &H8
'    Public Const ABM_WINDOWPOSCHANGED As Integer = &H9
'    Public Const ABN_STATECHANGE As Integer = &H0
'    Public Const ABN_POSCHANGED As Integer = &H1
'    Public Const ABN_FULLSCREENAPP As Integer = &H2
'    Public Const ABN_WINDOWARRANGE As Integer = &H3
'    Public Const ABS_AUTOHIDE As Integer = &H1
'    Public Const ABS_ALWAYSONTOP As Integer = &H2
'    Public Const ABE_LEFT As Integer = 0
'    Public Const ABE_TOP As Integer = 1
'    Public Const ABE_RIGHT As Integer = 2
'    Public Const ABE_BOTTOM As Integer = 3
'    Public Const AC_LINE_OFFLINE As Integer = &H0
'    Public Const AC_LINE_ONLINE As Integer = &H1
'    Public Const AC_LINE_BACKUP_POWER As Integer = &H2
'    Public Const AC_LINE_UNKNOWN As Integer = &HFF
'    Public Const ALG_CLASS_ANY As Integer = 0
'    Public Const AC_SRC_OVER As Integer = &H0
'    Public Const AC_SRC_ALPHA As Integer = &H1
'    Public Const ALG_CLASS_SIGNATURE As Integer = Machine.Shift.Left(1, 13)
'    Public Const ALG_CLASS_MSG_ENCRYPT As Integer = Machine.Shift.Left(2, 13)
'    Public Const ALG_CLASS_DATA_ENCRYPT As Integer = Machine.Shift.Left(3, 13)
'    Public Const ALG_CLASS_HASH As Integer = Machine.Shift.Left(4, 13)
'    Public Const ALG_CLASS_KEY_EXCHANGE As Integer = Machine.Shift.Left(5, 13)
'    Public Const ALG_TYPE_ANY As Integer = 0
'    Public Const ALG_TYPE_DSS As Integer = Machine.Shift.Left(1, 9)
'    Public Const ALG_TYPE_RSA As Integer = Machine.Shift.Left(2, 9)
'    Public Const ALG_TYPE_BLOCK As Integer = Machine.Shift.Left(3, 9)
'    Public Const ALG_TYPE_STREAM As Integer = Machine.Shift.Left(4, 9)
'    Public Const ALG_SID_ANY As Integer = 0
'    Public Const ALG_SID_RSA_ANY As Integer = 0
'    Public Const ALG_SID_RSA_PKCS As Integer = 1
'    Public Const ALG_SID_RSA_MSATWORK As Integer = 2
'    Public Const ALG_SID_RSA_ENTRUST As Integer = 3
'    Public Const ALG_SID_RSA_PGP As Integer = 4
'    Public Const ALG_SID_DSS_ANY As Integer = 0
'    Public Const ALG_SID_DSS_PKCS As Integer = 1
'    Public Const ALG_SID_DSS_DMS As Integer = 2
'    Public Const ALG_SID_DES As Integer = 1
'    Public Const ALG_SID_3DES As Integer = 3
'    Public Const ALG_SID_DESX As Integer = 4
'    Public Const ALG_SID_IDEA As Integer = 5
'    Public Const ALG_SID_CAST As Integer = 6
'    Public Const ALG_SID_SAFERSK64 As Integer = 7
'    Public Const ALD_SID_SAFERSK128 As Integer = 8
'    Public Const ALG_SID_RC2 As Integer = 2
'    Public Const ALG_SID_RC4 As Integer = 1
'    Public Const ALG_SID_SEAL As Integer = 2
'    Public Const ALG_SID_MD2 As Integer = 1
'    Public Const ALG_SID_MD4 As Integer = 2
'    Public Const ALG_SID_MD5 As Integer = 3
'    Public Const ALG_SID_SHA As Integer = 4
'    Public Const ALG_SID_MAC As Integer = 5
'    Public Const ALG_SID_RIPEMD As Integer = 6
'    Public Const ALG_SID_RIPEMD160 As Integer = 7
'    Public Const ALG_SID_SSL3SHAMD5 As Integer = 8
'    Public Const ALG_SID_EXAMPLE As Integer = 80
'    Public Const AT_KEYEXCHANGE As Integer = 1
'    Public Const AT_SIGNATURE As Integer = 2
'    Public Const ALTERNATE As Integer = 1
'    Public Const ASPECT_FILTERING As Integer = &H1
'    Public Const ABORTDOC As Integer = 2
'    Public Const ANTIALIASED_QUALITY As Integer = 4
'    Public Const ANSI_CHARSET As Integer = 0
'    Public Const ARABIC_CHARSET As Integer = 178
'    Public Const ABSOLUTE As Integer = 1
'    Public Const ANSI_FIXED_FONT As Integer = 11
'    Public Const ANSI_VAR_FONT As Integer = 12
'    Public Const AD_COUNTERCLOCKWISE As Integer = 1
'    Public Const AD_CLOCKWISE As Integer = 2
'    Public Const ASPECTX As Integer = 40
'    Public Const ASPECTY As Integer = 42
'    Public Const ASPECTXY As Integer = 44
'    Public Const ANYSIZE_ARRAY As Integer = 1
'    Public Const APPLICATION_ERROR_MASK As Integer = &H20000000
'    Public Const ACCESS_SYSTEM_SECURITY As Integer = &H1000000
'    Public Const ACL_REVISION As Integer = 2
'    Public Const ACL_REVISION1 As Integer = 1
'    Public Const ACL_REVISION2 As Integer = 2
'    Public Const ACL_REVISION3 As Integer = 3
'    Public Const ACCESS_ALLOWED_ACE_TYPE As Integer = &H0
'    Public Const ACCESS_DENIED_ACE_TYPE As Integer = &H1
'    Public Const ARW_BOTTOMLEFT As Integer = &H0
'    Public Const ARW_BOTTOMRIGHT As Integer = &H1
'    Public Const ARW_TOPLEFT As Integer = &H2
'    Public Const ARW_TOPRIGHT As Integer = &H3
'    Public Const ARW_STARTMASK As Integer = &H3
'    Public Const ARW_STARTRIGHT As Integer = &H1
'    Public Const ARW_STARTTOP As Integer = &H2
'    Public Const ARW_LEFT As Integer = &H0
'    Public Const ARW_RIGHT As Integer = &H0
'    Public Const ARW_UP As Integer = &H4
'    Public Const ARW_DOWN As Integer = &H4
'    Public Const ARW_HIDE As Integer = &H8
'    Public Const ARW_VALID As Integer = &HF
'    Public Const ATF_TIMEOUTON As Integer = &H1
'    Public Const ATF_ONOFFFEEDBACK As Integer = &H2
'    Public Const ACS_CENTER As Integer = &H1
'    Public Const ACS_TRANSPARENT As Integer = &H2
'    Public Const ACS_AUTOPLAY As Integer = &H4
'    Public Const ACS_TIMER As Integer = &H8
'    Public Const ACM_OPENA As Integer = &H400 + 100
'    Public Const ACM_OPENW As Integer = &H400 + 103
'    Public Const ACM_PLAY As Integer = &H400 + 101
'    Public Const ACM_STOP As Integer = &H400 + 102
'    Public Const ACN_START As Integer = 1
'    Public Const ACN_STOP As Integer = 2
'    Public Const ADVF_NODATA As Integer = 1
'    Public Const ADVF_ONLYONCE As Integer = 2
'    Public Const ADVF_PRIMEFIRST As Integer = 4
'    Public Const ADVFCACHE_NOHANDLER As Integer = 8
'    Public Const ADVFCACHE_FORCEBUILTIN As Integer = 16
'    Public Const ADVFCACHE_ONSAVE As Integer = 32
'    Public Const ADVFCACHE_DATAONSTOP As Integer = 64
'    Public Const AW_HOR_POSITIVE As Integer = &H1
'    Public Const AW_HOR_NEGATIVE As Integer = &H2
'    Public Const AW_VER_POSITIVE As Integer = &H4
'    Public Const AW_VER_NEGATIVE As Integer = &H8
'    Public Const AW_CENTER As Integer = &H10
'    Public Const AW_HIDE As Integer = &H10000
'    Public Const AW_ACTIVATE As Integer = &H20000
'    Public Const AW_SLIDE As Integer = &H40000
'    Public Const AW_BLEND As Integer = &H80000
'    ' NT5 begin 
'    ' NT5 end 
    
    
'    Public Const BOLD_FONTTYPE As Integer = &H100
'    Public Const BAUD_075 As Integer = &H1
'    Public Const BAUD_110 As Integer = &H2
'    Public Const BAUD_134_5 As Integer = &H4
'    Public Const BAUD_150 As Integer = &H8
'    Public Const BAUD_300 As Integer = &H10
'    Public Const BAUD_600 As Integer = &H20
'    Public Const BAUD_1200 As Integer = &H40
'    Public Const BAUD_1800 As Integer = &H80
'    Public Const BAUD_2400 As Integer = &H100
'    Public Const BAUD_4800 As Integer = &H200
'    Public Const BAUD_7200 As Integer = &H400
'    Public Const BAUD_9600 As Integer = &H800
'    Public Const BAUD_14400 As Integer = &H1000
'    Public Const BAUD_19200 As Integer = &H2000
'    Public Const BAUD_38400 As Integer = &H4000
'    Public Const BAUD_56K As Integer = &H8000
'    Public Const BAUD_128K As Integer = &H10000
'    Public Const BAUD_115200 As Integer = &H20000
'    Public Const BAUD_57600 As Integer = &H40000
'    Public Const BAUD_USER As Integer = &H10000000
'    Public Const BACKUP_INVALID As Integer = &H0
'    Public Const BACKUP_DATA As Integer = &H1
'    Public Const BACKUP_EA_DATA As Integer = &H2
'    Public Const BACKUP_SECURITY_DATA As Integer = &H3
'    Public Const BACKUP_ALTERNATE_DATA As Integer = &H4
'    Public Const BACKUP_LINK As Integer = &H5
'    Public Const BACKUP_PROPERTY_DATA As Integer = &H6
'    Public Const BATTERY_FLAG_HIGH As Integer = &H1
'    Public Const BATTERY_FLAG_LOW As Integer = &H2
'    Public Const BATTERY_FLAG_CRITICAL As Integer = &H4
'    Public Const BATTERY_FLAG_CHARGING As Integer = &H8
'    Public Const BATTERY_FLAG_NO_BATTERY As Integer = &h80
    '    Public Const BATTERY_FLAG_UNKNOWN As Integer = &HFF
'    Public Const BATTERY_PERCENTAGE_UNKNOWN As Integer = &HFF
    '    Public Const BATTERY_LIFE_UNKNOWN As Integer = &hFFFFFFFF
    '    Public Const BACKGROUND_BLUE As Integer = &H10
'    Public Const BACKGROUND_GREEN As Integer = &H20
'    Public Const BACKGROUND_RED As Integer = &H40
'    Public Const BACKGROUND_INTENSITY As Integer = &H80
'    Public Const BLACKONWHITE As Integer = 1
'    Public Const BANDINFO As Integer = 24
'    Public Const BEGIN_PATH As Integer = 4096
'    Public Const BI_RGB As Integer = 0
'    Public Const BI_RLE8 As Integer = 1
'    Public Const BI_RLE4 As Integer = 2
'    Public Const BI_BITFIELDS As Integer = 3
'    Public Const BALTIC_CHARSET As Integer = 186
'    Public Const BKMODE_LAST As Integer = 2
'    Public Const BLACK_BRUSH As Integer = 4
'    Public Const BLACK_PEN As Integer = 7
'    Public Const BS_SOLID As Integer = 0
'    Public Const BS_NULL As Integer = 1
'    Public Const BS_HOLLOW As Integer = 1
'    Public Const BS_HATCHED As Integer = 2
'    Public Const BS_PATTERN As Integer = 3
'    Public Const BS_INDEXED As Integer = 4
'    Public Const BS_DIBPATTERN As Integer = 5
'    Public Const BS_DIBPATTERNPT As Integer = 6
'    Public Const BS_PATTERN8X8 As Integer = 7
'    Public Const BS_DIBPATTERN8X8 As Integer = 8
'    Public Const BS_MONOPATTERN As Integer = 9
'    Public Const BITSPIXEL As Integer = 12
'    Public Const BLTALIGNMENT As Integer = 119
'    Public Const BDR_RAISEDOUTER As Integer = &H1
'    Public Const BDR_SUNKENOUTER As Integer = &H2
'    Public Const BDR_RAISEDINNER As Integer = &H4
'    Public Const BDR_SUNKENINNER As Integer = &H8
'    Public Const BDR_OUTER As Integer = &H3
'    Public Const BDR_INNER As Integer = &HC
'    Public Const BDR_RAISED As Integer = &H5
'    Public Const BDR_SUNKEN As Integer = &HA
'    Public Const BF_LEFT As Integer = &H1
'    Public Const BF_TOP As Integer = &H2
'    Public Const BF_RIGHT As Integer = &H4
'    Public Const BF_BOTTOM As Integer = &H8
'    Public Const BF_TOPLEFT As Integer = &H2 Or &H1
'    Public Const BF_TOPRIGHT As Integer = &H2 Or &H4
'    Public Const BF_BOTTOMLEFT As Integer = &H8 Or &H1
'    Public Const BF_BOTTOMRIGHT As Integer = &H8 Or &H4
'    Public Const BF_RECT As Integer = &H1 Or &H2 Or &H4 Or &H8
'    Public Const BF_DIAGONAL As Integer = &H10
'    Public Const BF_DIAGONAL_ENDTOPRIGHT As Integer = &H10 Or &H2 Or &H4
'    Public Const BF_DIAGONAL_ENDTOPLEFT As Integer = &H10 Or &H2 Or &H1
'    Public Const BF_DIAGONAL_ENDBOTTOMLEFT As Integer = &H10 Or &H8 Or &H1
'    Public Const BF_DIAGONAL_ENDBOTTOMRIGHT As Integer = &H10 Or &H8 Or &H4
'    Public Const BF_MIDDLE As Integer = &H800
'    Public Const BF_SOFT As Integer = &H1000
'    Public Const BF_ADJUST As Integer = &H2000
'    Public Const BF_FLAT As Integer = &H4000
    '    Public Const BF_MONO As Integer = &h8000
    '    Public Const BSM_ALLCOMPONENTS As Integer = &H0
'    Public Const BSM_VXDS As Integer = &H1
'    Public Const BSM_NETDRIVER As Integer = &H2
'    Public Const BSM_INSTALLABLEDRIVERS As Integer = &H4
'    Public Const BSM_APPLICATIONS As Integer = &H8
'    Public Const BSM_ALLDESKTOPS As Integer = &H10
'    Public Const BSF_QUERY As Integer = &H1
'    Public Const BSF_IGNORECURRENTTASK As Integer = &H2
'    Public Const BSF_FLUSHDISK As Integer = &H4
'    Public Const BSF_NOHANG As Integer = &H8
'    Public Const BSF_POSTMESSAGE As Integer = &H10
'    Public Const BSF_FORCEIFHUNG As Integer = &H20
'    Public Const BSF_NOTIMEOUTIFNOTHUNG As Integer = &H40
'    Public Const BROADCAST_QUERY_DENY As Integer = &H424D5144
'    Public Const BS_PUSHBUTTON As Integer = &H0
'    Public Const BS_DEFPUSHBUTTON As Integer = &H1
'    Public Const BS_CHECKBOX As Integer = &H2
'    Public Const BS_AUTOCHECKBOX As Integer = &H3
'    Public Const BS_RADIOBUTTON As Integer = &H4
'    Public Const BS_3STATE As Integer = &H5
'    Public Const BS_AUTO3STATE As Integer = &H6
'    Public Const BS_GROUPBOX As Integer = &H7
'    Public Const BS_USERBUTTON As Integer = &H8
'    Public Const BS_AUTORADIOBUTTON As Integer = &H9
'    Public Const BS_OWNERDRAW As Integer = &HB
'    Public Const BS_LEFTTEXT As Integer = &H20
'    Public Const BS_TEXT As Integer = &H0
'    Public Const BS_ICON As Integer = &H40
'    Public Const BS_BITMAP As Integer = &H80
'    Public Const BS_LEFT As Integer = &H100
'    Public Const BS_RIGHT As Integer = &H200
'    Public Const BS_CENTER As Integer = &H300
'    Public Const BS_TOP As Integer = &H400
'    Public Const BS_BOTTOM As Integer = &H800
'    Public Const BS_VCENTER As Integer = &HC00
'    Public Const BS_PUSHLIKE As Integer = &H1000
'    Public Const BS_MULTILINE As Integer = &H2000
'    Public Const BS_NOTIFY As Integer = &H4000
'    Public Const BS_FLAT As Integer = &H8000
'    Public Const BS_RIGHTBUTTON As Integer = &H20
'    Public Const BN_CLICKED As Integer = 0
'    Public Const BN_PAINT As Integer = 1
'    Public Const BN_HILITE As Integer = 2
'    Public Const BN_UNHILITE As Integer = 3
'    Public Const BN_DISABLE As Integer = 4
'    Public Const BN_DOUBLECLICKED As Integer = 5
'    Public Const BN_PUSHED As Integer = 2
'    Public Const BN_UNPUSHED As Integer = 3
'    Public Const BN_DBLCLK As Integer = 5
'    Public Const BN_SETFOCUS As Integer = 6
'    Public Const BN_KILLFOCUS As Integer = 7
'    Public Const BM_GETCHECK As Integer = &HF0
'    Public Const BM_SETCHECK As Integer = &HF1
'    Public Const BM_GETSTATE As Integer = &HF2
'    Public Const BM_SETSTATE As Integer = &HF3
'    Public Const BM_SETSTYLE As Integer = &HF4
'    Public Const BM_CLICK As Integer = &HF5
'    Public Const BM_GETIMAGE As Integer = &HF6
'    Public Const BM_SETIMAGE As Integer = &HF7
'    Public Const BST_UNCHECKED As Integer = &H0
'    Public Const BST_CHECKED As Integer = &H1
'    Public Const BST_INDETERMINATE As Integer = &H2
'    Public Const BST_PUSHED As Integer = &H4
'    Public Const BST_FOCUS As Integer = &H8
'    Public Const BLACKNESS As Integer = &H42
    
    
'    Public Const CDERR_DIALOGFAILURE As Integer = &HFFFF
'    Public Const CDERR_GENERALCODES As Integer = &H0
'    Public Const CDERR_STRUCTSIZE As Integer = &H1
'    Public Const CDERR_INITIALIZATION As Integer = &H2
'    Public Const CDERR_NOTEMPLATE As Integer = &H3
'    Public Const CDERR_NOHINSTANCE As Integer = &H4
'    Public Const CDERR_LOADSTRFAILURE As Integer = &H5
'    Public Const CDERR_FINDRESFAILURE As Integer = &H6
'    Public Const CDERR_LOADRESFAILURE As Integer = &H7
'    Public Const CDERR_LOCKRESFAILURE As Integer = &H8
'    Public Const CDERR_MEMALLOCFAILURE As Integer = &H9
'    Public Const CDERR_MEMLOCKFAILURE As Integer = &HA
'    Public Const CDERR_NOHOOK As Integer = &HB
'    Public Const CDERR_REGISTERMSGFAIL As Integer = &HC
'    Public Const CFERR_CHOOSEFONTCODES As Integer = &H2000
'    Public Const CFERR_NOFONTS As Integer = &H2001
'    Public Const CFERR_MAXLESSTHANMIN As Integer = &H2002
'    Public Const CCERR_CHOOSECOLORCODES As Integer = &H5000
'    Public Const CDN_FIRST As Integer = 0 - 601
'    Public Const CDN_LAST As Integer = 0 - 699
'    Public Const CDN_INITDONE As Integer = 0 - 601 - &H0
'    Public Const CDN_SELCHANGE As Integer = 0 - 601 - &H1
'    Public Const CDN_FOLDERCHANGE As Integer = 0 - 601 - &H2
'    Public Const CDN_SHAREVIOLATION As Integer = 0 - 601 - &H3
'    Public Const CDN_HELP As Integer = 0 - 601 - &H4
'    Public Const CDN_FILEOK As Integer = 0 - 601 - &H5
'    Public Const CDN_TYPECHANGE As Integer = 0 - 601 - &H6
'    Public Const CC_RGBINIT As Integer = &H1
'    Public Const CC_FULLOPEN As Integer = &H2
'    Public Const CC_PREVENTFULLOPEN As Integer = &H4
'    Public Const CC_SHOWHELP As Integer = &H8
'    Public Const CC_ENABLEHOOK As Integer = &H10
'    Public Const CC_ENABLETEMPLATE As Integer = &H20
'    Public Const CC_ENABLETEMPLATEHANDLE As Integer = &H40
'    Public Const CC_SOLIDCOLOR As Integer = &H80
'    Public Const CC_ANYCOLOR As Integer = &H100
'    Public Const CF_SCREENFONTS As Integer = &H1
'    Public Const CF_PRINTERFONTS As Integer = &H2
'    Public Const CF_BOTH As Integer = &H1 Or &H2
'    Public Const CF_SHOWHELP As Integer = &H4
'    Public Const CF_ENABLEHOOK As Integer = &H8
'    Public Const CF_ENABLETEMPLATE As Integer = &H10
'    Public Const CF_ENABLETEMPLATEHANDLE As Integer = &H20
'    Public Const CF_INITTOLOGFONTSTRUCT As Integer = &H40
'    Public Const CF_USESTYLE As Integer = &H80
'    Public Const CF_EFFECTS As Integer = &H100
'    Public Const CF_APPLY As Integer = &H200
'    Public Const CF_ANSIONLY As Integer = &H400
'    Public Const CF_SCRIPTSONLY As Integer = &H400
'    Public Const CF_NOVECTORFONTS As Integer = &H800
'    Public Const CF_NOOEMFONTS As Integer = &H800
'    Public Const CF_NOSIMULATIONS As Integer = &H1000
'    Public Const CF_LIMITSIZE As Integer = &H2000
'    Public Const CF_FIXEDPITCHONLY As Integer = &H4000
'    Public Const CF_WYSIWYG As Integer = &H8000
'    Public Const CF_FORCEFONTEXIST As Integer = &H10000
'    Public Const CF_SCALABLEONLY As Integer = &H20000
'    Public Const CF_TTONLY As Integer = &H40000
'    Public Const CF_NOFACESEL As Integer = &H80000
'    Public Const CF_NOSTYLESEL As Integer = &H100000
'    Public Const CF_NOSIZESEL As Integer = &H200000
'    Public Const CF_SELECTSCRIPT As Integer = &H400000
'    Public Const CF_NOSCRIPTSEL As Integer = &H800000
'    Public Const CF_NOVERTFONTS As Integer = &H1000000
'    Public Const CD_LBSELNOITEMS As Integer = - 1
'    Public Const CD_LBSELCHANGE As Integer = 0
'    Public Const CD_LBSELSUB As Integer = 1
'    Public Const CD_LBSELADD As Integer = 2
'    Public Const CADV_LATEACK As Integer = &HFFFF
'    Public Const CP_WINANSI As Integer = 1004
'    Public Const CP_WINUNICODE As Integer = 1200
'    ' CP_WINNEUTRAL = 1004;
'    Public Const CBF_FAIL_SELFCONNECTIONS As Integer = &H1000
'    Public Const CBF_FAIL_CONNECTIONS As Integer = &H2000
'    Public Const CBF_FAIL_ADVISES As Integer = &H4000
'    Public Const CBF_FAIL_EXECUTES As Integer = &H8000
'    Public Const CBF_FAIL_POKES As Integer = &H10000
'    Public Const CBF_FAIL_REQUESTS As Integer = &H20000
'    Public Const CBF_FAIL_ALLSVRXACTIONS As Integer = &H3F000
'    Public Const CBF_SKIP_CONNECT_CONFIRMS As Integer = &H40000
'    Public Const CBF_SKIP_REGISTRATIONS As Integer = &H80000
'    Public Const CBF_SKIP_UNREGISTRATIONS As Integer = &H100000
'    Public Const CBF_SKIP_DISCONNECTS As Integer = &H200000
'    Public Const CBF_SKIP_ALLNOTIFICATIONS As Integer = &H3C0000
'    Public Const ctlFirst As Integer = &H400
'    Public Const ctlLast As Integer = &H4FF
'    Public Const chx1 As Integer = &H410
'    Public Const chx2 As Integer = &H411
'    Public Const chx3 As Integer = &H412
'    Public Const chx4 As Integer = &H413
'    Public Const chx5 As Integer = &H414
'    Public Const chx6 As Integer = &H415
'    Public Const chx7 As Integer = &H416
'    Public Const chx8 As Integer = &H417
'    Public Const chx9 As Integer = &H418
'    Public Const chx10 As Integer = &H419
'    Public Const chx11 As Integer = &H41A
'    Public Const chx12 As Integer = &H41B
'    Public Const chx13 As Integer = &H41C
'    Public Const chx14 As Integer = &H41D
'    Public Const chx15 As Integer = &H41E
'    Public Const chx16 As Integer = &H41F
'    Public Const cmb1 As Integer = &H470
'    Public Const cmb2 As Integer = &H471
'    Public Const cmb3 As Integer = &H472
'    Public Const cmb4 As Integer = &H473
'    Public Const cmb5 As Integer = &H474
'    Public Const cmb6 As Integer = &H475
'    Public Const cmb7 As Integer = &H476
'    Public Const cmb8 As Integer = &H477
'    Public Const cmb9 As Integer = &H478
'    Public Const cmb10 As Integer = &H479
'    Public Const cmb11 As Integer = &H47A
'    Public Const cmb12 As Integer = &H47B
'    Public Const cmb13 As Integer = &H47C
'    Public Const cmb14 As Integer = &H47D
'    Public Const cmb15 As Integer = &H47E
'    Public Const cmb16 As Integer = &H47F
'    Public Const CPS_COMPLETE As Integer = &H1
'    Public Const CPS_CONVERT As Integer = &H2
'    Public Const CPS_REVERT As Integer = &H3
'    Public Const CPS_CANCEL As Integer = &H4
'    Public Const CS_INSERTCHAR As Integer = &H2000
'    Public Const CS_NOMOVECARET As Integer = &H4000
'    Public Const CFS_DEFAULT As Integer = &H0
'    Public Const CFS_RECT As Integer = &H1
'    Public Const CFS_POINT As Integer = &H2
'    Public Const CFS_FORCE_POSITION As Integer = &H20
'    Public Const CFS_CANDIDATEPOS As Integer = &H40
'    Public Const CFS_EXCLUDE As Integer = &H80
'    Public Const CALLBACK_TYPEMASK As Integer = &H70000
'    Public Const CALLBACK_NULL As Integer = &H0
'    Public Const CALLBACK_WINDOW As Integer = &H10000
'    Public Const CALLBACK_TASK As Integer = &H20000
'    Public Const CALLBACK_FUNCTION As Integer = &H30000
'    Public Const CALLBACK_THREAD As Integer = &H20000
'    Public Const CALLBACK_EVENT As Integer = &H50000
'    Public Const CFSEPCHAR As Char = "+"c
'    Public Const CALL_PENDING As Integer = &H2
'    Public Const CWCSTORAGENAME As Integer = 32
'    Public Const COM_RIGHTS_EXECUTE As Integer = 1
'    Public Const cbNDRContext As Integer = 20
'    Public Const CREATE_NEW As Integer = 1
'    Public Const CREATE_ALWAYS As Integer = 2
'    Public Const CALLBACK_CHUNK_FINISHED As Integer = &H0
'    Public Const CALLBACK_STREAM_SWITCH As Integer = &H1
'    Public Const COPY_FILE_FAIL_IF_EXISTS As Integer = &H1
'    Public Const COPY_FILE_RESTARTABLE As Integer = &H2
    '    Public Const COMMPROP_INITIALIZED As Integer = &hE73CF52E
    '    Public Const CREATE_SUSPENDED As Integer = &H4
'    Public Const CREATE_NEW_CONSOLE As Integer = &H10
'    Public Const CREATE_NEW_PROCESS_GROUP As Integer = &H200
'    Public Const CREATE_UNICODE_ENVIRONMENT As Integer = &H400
'    Public Const CREATE_SEPARATE_WOW_VDM As Integer = &H800
'    Public Const CREATE_SHARED_WOW_VDM As Integer = &H1000
'    Public Const CREATE_FORCEDOS As Integer = &H2000
'    Public Const CREATE_DEFAULT_ERROR_MODE As Integer = &H4000000
'    Public Const CREATE_NO_WINDOW As Integer = &H8000000
'    Public Const CREATE_THREAD_DEBUG_EVENT As Integer = 2
'    Public Const CREATE_PROCESS_DEBUG_EVENT As Integer = 3
'    Public Const CBR_110 As Integer = 110
'    Public Const CBR_300 As Integer = 300
'    Public Const CBR_600 As Integer = 600
'    Public Const CBR_1200 As Integer = 1200
'    Public Const CBR_2400 As Integer = 2400
'    Public Const CBR_4800 As Integer = 4800
'    Public Const CBR_9600 As Integer = 9600
'    Public Const CBR_14400 As Integer = 14400
'    Public Const CBR_19200 As Integer = 19200
'    Public Const CBR_38400 As Integer = 38400
'    Public Const CBR_56000 As Integer = 56000
'    Public Const CBR_57600 As Integer = 57600
'    Public Const CBR_115200 As Integer = 115200
'    Public Const CBR_128000 As Integer = 128000
'    Public Const CBR_256000 As Integer = 256000
'    Public Const CE_RXOVER As Integer = &H1
'    Public Const CE_OVERRUN As Integer = &H2
'    Public Const CE_RXPARITY As Integer = &H4
'    Public Const CE_FRAME As Integer = &H8
'    Public Const CE_BREAK As Integer = &H10
'    Public Const CE_TXFULL As Integer = &H100
'    Public Const CE_PTO As Integer = &H200
'    Public Const CE_IOE As Integer = &H400
'    Public Const CE_DNS As Integer = &H800
'    Public Const CE_OOP As Integer = &H1000
    '    Public Const CE_MODE As Integer = &h8000
    '    Public Const CLRRTS As Integer = 4
'    Public Const CLRDTR As Integer = 6
'    Public Const CLRBREAK As Integer = 9
'    Public Const CAPSLOCK_ON As Integer = &H80
'    Public Const CTRL_C_EVENT As Integer = 0
'    Public Const CTRL_BREAK_EVENT As Integer = 1
'    Public Const CTRL_CLOSE_EVENT As Integer = 2
'    Public Const CTRL_LOGOFF_EVENT As Integer = 5
'    Public Const CTRL_SHUTDOWN_EVENT As Integer = 6
'    Public Const CONSOLE_TEXTMODE_BUFFER As Integer = 1
'    Public Const CRYPT_MODE_CBCI As Integer = 6
'    Public Const CRYPT_MODE_CFBP As Integer = 7
'    Public Const CRYPT_MODE_OFBP As Integer = 8
'    Public Const CRYPT_MODE_CBCOFM As Integer = 9
'    Public Const CRYPT_MODE_CBCOFMI As Integer = 10
'    Public Const CALG_MD2 As Integer = Machine.Shift.Left(4, 13) Or 0 Or 1
'    Public Const CALG_MD4 As Integer = Machine.Shift.Left(4, 13) Or 0 Or 2
'    Public Const CALG_MD5 As Integer = Machine.Shift.Left(4, 13) Or 0 Or 3
'    Public Const CALG_SHA As Integer = Machine.Shift.Left(4, 13) Or 0 Or 4
'    Public Const CALG_MAC As Integer = Machine.Shift.Left(4, 13) Or 0 Or 5
'    Public Const CALG_RSA_SIGN As Integer = Machine.Shift.Left(1, 13) Or Machine.Shift.Left(2, 9) Or 0
'    Public Const CALG_DSS_SIGN As Integer = Machine.Shift.Left(1, 13) Or Machine.Shift.Left(1, 9) Or 0
'    Public Const CALG_RSA_KEYX As Integer = Machine.Shift.Left(5, 13) Or Machine.Shift.Left(2, 9) Or 0
'    Public Const CALG_DES As Integer = Machine.Shift.Left(3, 13) Or Machine.Shift.Left(3, 9) Or 1
'    Public Const CALG_RC2 As Integer = Machine.Shift.Left(3, 13) Or Machine.Shift.Left(3, 9) Or 2
'    Public Const CALG_RC4 As Integer = Machine.Shift.Left(3, 13) Or Machine.Shift.Left(4, 9) Or 1
'    Public Const CALG_SEAL As Integer = Machine.Shift.Left(3, 13) Or Machine.Shift.Left(4, 9) Or 2
'    Public Const CRYPT_VERIFYCONTEXT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                CALG_SEAL = ((3<<13)|(4<<9)|2),
'    '                                                                                                                                                            CRYPT_VERIFYCONTEXT = unchecked((int)0xF0000000),
'    '-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const CRYPT_NEWKEYSET As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                            CRYPT_VERIFYCONTEXT = unchecked((int)0xF0000000),
'    '        CRYPT_NEWKEYSET = unchecked((int)0x8),
'    '---------------------------^--- GenCode(token): unexpected token type
'    Public Const CRYPT_DELETEKEYSET As Integer = &H10
'    Public Const CRYPT_EXPORTABLE As Integer = &H1
'    Public Const CRYPT_USER_PROTECTED As Integer = &H2
'    Public Const CRYPT_CREATE_SALT As Integer = &H4
'    Public Const CRYPT_UPDATE_KEY As Integer = &H8
'    Public Const CRYPT_USERDATA As Integer = 1
'    Public Const CRYPT_MODE_CBC As Integer = 1
'    Public Const CRYPT_MODE_ECB As Integer = 2
'    Public Const CRYPT_MODE_OFB As Integer = 3
'    Public Const CRYPT_MODE_CFB As Integer = 4
'    Public Const CRYPT_MODE_CTS As Integer = 5
'    Public Const CRYPT_ENCRYPT As Integer = &H1
'    Public Const CRYPT_DECRYPT As Integer = &H2
'    Public Const CRYPT_READ As Integer = &H8
'    Public Const CRYPT_WRITE As Integer = &H10
'    Public Const CRYPT_MAC As Integer = &H20
'    Public Const CRYPT_FAILED As Boolean = False
'    Public Const CRYPT_SUCCEED As Boolean = True
'    Public Const CRYPT_FIRST As Integer = 1
'    Public Const CRYPT_NEXT As Integer = 2
'    Public Const CRYPT_IMPL_HARDWARE As Integer = 1
'    Public Const CRYPT_IMPL_SOFTWARE As Integer = 2
'    Public Const CRYPT_IMPL_MIXED As Integer = 3
'    Public Const CRYPT_IMPL_UNKNOWN As Integer = 4
'    Public Const CUR_BLOB_VERSION As Integer = 2
'    Public Const CO_E_INIT_TLS As Integer = Fix()
'    '
'    'Note:  Error processing original source shown below
'    '        CUR_BLOB_VERSION = 2,
'    '        CO_E_INIT_TLS = (int)unchecked((int)0x80004006),
'    '------------------------------^--- GenCode(token): unexpected token type
'    Public Const CO_E_INIT_SHARED_ALLOCATOR As Integer = Fix()
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_INIT_TLS = (int)unchecked((int)0x80004006),
'    '        CO_E_INIT_SHARED_ALLOCATOR = (int)unchecked((int)0x80004007),
'    '-------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const CO_E_INIT_MEMORY_ALLOCATOR As Integer = Fix()
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_INIT_SHARED_ALLOCATOR = (int)unchecked((int)0x80004007),
'    '        CO_E_INIT_MEMORY_ALLOCATOR = (int)unchecked((int)0x80004008),
'    '-------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const CO_E_INIT_CLASS_CACHE As Integer = Fix()
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_INIT_MEMORY_ALLOCATOR = (int)unchecked((int)0x80004008),
'    '        CO_E_INIT_CLASS_CACHE = (int)unchecked((int)0x80004009),
'    '--------------------------------------^--- GenCode(token): unexpected token type
'    Public Const CO_E_INIT_RPC_CHANNEL As Integer = Fix()
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_INIT_CLASS_CACHE = (int)unchecked((int)0x80004009),
'    '        CO_E_INIT_RPC_CHANNEL = (int)unchecked((int)0x8000400A),
'    '--------------------------------------^--- GenCode(token): unexpected token type
'    Public Const CO_E_INIT_TLS_SET_CHANNEL_CONTROL As Integer = Fix()
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_INIT_RPC_CHANNEL = (int)unchecked((int)0x8000400A),
'    '        CO_E_INIT_TLS_SET_CHANNEL_CONTROL = (int)unchecked((int)0x8000400B),
'    '--------------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const CO_E_INIT_TLS_CHANNEL_CONTROL As Integer = Fix()
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_INIT_TLS_SET_CHANNEL_CONTROL = (int)unchecked((int)0x8000400B),
'    '        CO_E_INIT_TLS_CHANNEL_CONTROL = (int)unchecked((int)0x8000400C),
'    '----------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const CO_E_INIT_UNACCEPTED_USER_ALLOCATOR As Integer = Fix()
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_INIT_TLS_CHANNEL_CONTROL = (int)unchecked((int)0x8000400C),
'    '        CO_E_INIT_UNACCEPTED_USER_ALLOCATOR = (int)unchecked((int)0x8000400D),
'    '----------------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const CO_E_INIT_SCM_MUTEX_EXISTS As Integer = Fix()
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_INIT_UNACCEPTED_USER_ALLOCATOR = (int)unchecked((int)0x8000400D),
'    '        CO_E_INIT_SCM_MUTEX_EXISTS = (int)unchecked((int)0x8000400E),
'    '-------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const CO_E_INIT_SCM_FILE_MAPPING_EXISTS As Integer = Fix()
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_INIT_SCM_MUTEX_EXISTS = (int)unchecked((int)0x8000400E),
'    '        CO_E_INIT_SCM_FILE_MAPPING_EXISTS = (int)unchecked((int)0x8000400F),
'    '--------------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const CO_E_INIT_SCM_MAP_VIEW_OF_FILE As Integer = Fix()
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_INIT_SCM_FILE_MAPPING_EXISTS = (int)unchecked((int)0x8000400F),
'    '        CO_E_INIT_SCM_MAP_VIEW_OF_FILE = (int)unchecked((int)0x80004010),
'    '-----------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const CO_E_INIT_SCM_EXEC_FAILURE As Integer = Fix()
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_INIT_SCM_MAP_VIEW_OF_FILE = (int)unchecked((int)0x80004010),
'    '        CO_E_INIT_SCM_EXEC_FAILURE = (int)unchecked((int)0x80004011),
'    '-------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const CO_E_INIT_ONLY_SINGLE_THREADED As Integer = Fix()
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_INIT_SCM_EXEC_FAILURE = (int)unchecked((int)0x80004011),
'    '        CO_E_INIT_ONLY_SINGLE_THREADED = (int)unchecked((int)0x80004012),
'    '-----------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const CO_E_CANT_REMOTE As Integer = Fix()
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_INIT_ONLY_SINGLE_THREADED = (int)unchecked((int)0x80004012),
'    '        CO_E_CANT_REMOTE = (int)unchecked((int)0x80004013),
'    '---------------------------------^--- GenCode(token): unexpected token type
'    Public Const CO_E_BAD_SERVER_NAME As Integer = Fix()
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_CANT_REMOTE = (int)unchecked((int)0x80004013),
'    '        CO_E_BAD_SERVER_NAME = (int)unchecked((int)0x80004014),
'    '-------------------------------------^--- GenCode(token): unexpected token type
'    Public Const CO_E_WRONG_SERVER_IDENTITY As Integer = Fix()
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_BAD_SERVER_NAME = (int)unchecked((int)0x80004014),
'    '        CO_E_WRONG_SERVER_IDENTITY = (int)unchecked((int)0x80004015),
'    '-------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const CO_E_OLE1DDE_DISABLED As Integer = Fix()
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_WRONG_SERVER_IDENTITY = (int)unchecked((int)0x80004015),
'    '        CO_E_OLE1DDE_DISABLED = (int)unchecked((int)0x80004016),
'    '--------------------------------------^--- GenCode(token): unexpected token type
'    Public Const CO_E_RUNAS_SYNTAX As Integer = Fix()
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_OLE1DDE_DISABLED = (int)unchecked((int)0x80004016),
'    '        CO_E_RUNAS_SYNTAX = (int)unchecked((int)0x80004017),
'    '----------------------------------^--- GenCode(token): unexpected token type
'    Public Const CO_E_CREATEPROCESS_FAILURE As Integer = Fix()
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_RUNAS_SYNTAX = (int)unchecked((int)0x80004017),
'    '        CO_E_CREATEPROCESS_FAILURE = (int)unchecked((int)0x80004018),
'    '-------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const CO_E_RUNAS_CREATEPROCESS_FAILURE As Integer = Fix()
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_CREATEPROCESS_FAILURE = (int)unchecked((int)0x80004018),
'    '        CO_E_RUNAS_CREATEPROCESS_FAILURE = (int)unchecked((int)0x80004019),
'    '-------------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const CO_E_RUNAS_LOGON_FAILURE As Integer = Fix()
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_RUNAS_CREATEPROCESS_FAILURE = (int)unchecked((int)0x80004019),
'    '        CO_E_RUNAS_LOGON_FAILURE = (int)unchecked((int)0x8000401A),
'    '-----------------------------------------^--- GenCode(token): unexpected token type
'    Public Const CO_E_LAUNCH_PERMSSION_DENIED As Integer = Fix()
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_RUNAS_LOGON_FAILURE = (int)unchecked((int)0x8000401A),
'    '        CO_E_LAUNCH_PERMSSION_DENIED = (int)unchecked((int)0x8000401B),
'    '---------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const CO_E_START_SERVICE_FAILURE As Integer = Fix()
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_LAUNCH_PERMSSION_DENIED = (int)unchecked((int)0x8000401B),
'    '        CO_E_START_SERVICE_FAILURE = (int)unchecked((int)0x8000401C),
'    '-------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const CO_E_REMOTE_COMMUNICATION_FAILURE As Integer = Fix()
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_START_SERVICE_FAILURE = (int)unchecked((int)0x8000401C),
'    '        CO_E_REMOTE_COMMUNICATION_FAILURE = (int)unchecked((int)0x8000401D),
'    '--------------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const CO_E_SERVER_START_TIMEOUT As Integer = Fix()
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_REMOTE_COMMUNICATION_FAILURE = (int)unchecked((int)0x8000401D),
'    '        CO_E_SERVER_START_TIMEOUT = (int)unchecked((int)0x8000401E),
'    '------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const CO_E_CLSREG_INCONSISTENT As Integer = Fix()
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_SERVER_START_TIMEOUT = (int)unchecked((int)0x8000401E),
'    '        CO_E_CLSREG_INCONSISTENT = (int)unchecked((int)0x8000401F),
'    '-----------------------------------------^--- GenCode(token): unexpected token type
'    Public Const CO_E_IIDREG_INCONSISTENT As Integer = Fix()
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_CLSREG_INCONSISTENT = (int)unchecked((int)0x8000401F),
'    '        CO_E_IIDREG_INCONSISTENT = (int)unchecked((int)0x80004020),
'    '-----------------------------------------^--- GenCode(token): unexpected token type
'    Public Const CO_E_NOT_SUPPORTED As Integer = Fix()
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_IIDREG_INCONSISTENT = (int)unchecked((int)0x80004020),
'    '        CO_E_NOT_SUPPORTED = (int)unchecked((int)0x80004021),
'    '-----------------------------------^--- GenCode(token): unexpected token type
'    Public Const CLASSFACTORY_E_FIRST As Integer = Fix()
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_NOT_SUPPORTED = (int)unchecked((int)0x80004021),
'    '        CLASSFACTORY_E_FIRST = (int)unchecked((int)0x80040110),
'    '-------------------------------------^--- GenCode(token): unexpected token type
'    Public Const CLASSFACTORY_E_LAST As Integer = Fix()
'    '
'    'Note:  Error processing original source shown below
'    '        CLASSFACTORY_E_FIRST = (int)unchecked((int)0x80040110),
'    '        CLASSFACTORY_E_LAST = (int)unchecked((int)0x8004011F),
'    '------------------------------------^--- GenCode(token): unexpected token type
'    Public Const CLASSFACTORY_S_FIRST As Integer = &H40110
'    Public Const CLASSFACTORY_S_LAST As Integer = &H4011F
'    Public Const CLASS_E_NOAGGREGATION As Integer = Fix()
'    '
'    'Note:  Error processing original source shown below
'    '        CLASSFACTORY_S_LAST = 0x0004011F,
'    '        CLASS_E_NOAGGREGATION = (int)unchecked((int)0x80040110),
'    '--------------------------------------^--- GenCode(token): unexpected token type
'    Public Const CLASS_E_CLASSNOTAVAILABLE As Integer = Fix()
'    '
'    'Note:  Error processing original source shown below
'    '        CLASS_E_NOAGGREGATION = (int)unchecked((int)0x80040110),
'    '        CLASS_E_CLASSNOTAVAILABLE = (int)unchecked((int)0x80040111),
'    '------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const CACHE_E_FIRST As Integer = Fix()
'    '
'    'Note:  Error processing original source shown below
'    '        CLASS_E_CLASSNOTAVAILABLE = (int)unchecked((int)0x80040111),
'    '        CACHE_E_FIRST = (int)unchecked((int)0x80040170),
'    '------------------------------^--- GenCode(token): unexpected token type
'    Public Const CACHE_E_LAST As Integer = Fix()
'    '
'    'Note:  Error processing original source shown below
'    '        CACHE_E_FIRST = (int)unchecked((int)0x80040170),
'    '        CACHE_E_LAST = (int)unchecked((int)0x8004017F),
'    '-----------------------------^--- GenCode(token): unexpected token type
'    Public Const CACHE_S_FIRST As Integer = &H40170
'    Public Const CACHE_S_LAST As Integer = &H4017F
'    Public Const CACHE_E_NOCACHE_UPDATED As Integer = Fix()
'    '
'    'Note:  Error processing original source shown below
'    '        CACHE_S_LAST = 0x0004017F,
'    '        CACHE_E_NOCACHE_UPDATED = (int)unchecked((int)0x80040170),
'    '----------------------------------------^--- GenCode(token): unexpected token type
'    Public Const CLIENTSITE_E_FIRST As Integer = Fix()
'    '
'    'Note:  Error processing original source shown below
'    '        CACHE_E_NOCACHE_UPDATED = (int)unchecked((int)0x80040170),
'    '        CLIENTSITE_E_FIRST = (int)unchecked((int)0x80040190),
'    '-----------------------------------^--- GenCode(token): unexpected token type
'    Public Const CLIENTSITE_E_LAST As Integer = Fix()
'    '
'    'Note:  Error processing original source shown below
'    '        CLIENTSITE_E_FIRST = (int)unchecked((int)0x80040190),
'    '        CLIENTSITE_E_LAST = (int)unchecked((int)0x8004019F),
'    '----------------------------------^--- GenCode(token): unexpected token type
'    Public Const CLIENTSITE_S_FIRST As Integer = &H40190
'    Public Const CLIENTSITE_S_LAST As Integer = &H4019F
'    Public Const CONVERT10_E_FIRST As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CLIENTSITE_S_LAST = 0x0004019F,
'    '        CONVERT10_E_FIRST = unchecked((int)0x800401C0),
'    '-----------------------------^--- GenCode(token): unexpected token type
'    Public Const CONVERT10_E_LAST As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CONVERT10_E_FIRST = unchecked((int)0x800401C0),
'    '        CONVERT10_E_LAST = unchecked((int)0x800401CF),
'    '----------------------------^--- GenCode(token): unexpected token type
'    Public Const CONVERT10_S_FIRST As Integer = &H401C0
'    Public Const CONVERT10_S_LAST As Integer = &H401CF
'    Public Const CONVERT10_E_OLESTREAM_GET As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CONVERT10_S_LAST = 0x000401CF,
'    '        CONVERT10_E_OLESTREAM_GET = unchecked((int)0x800401C0),
'    '-------------------------------------^--- GenCode(token): unexpected token type
'    Public Const CONVERT10_E_OLESTREAM_PUT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CONVERT10_E_OLESTREAM_GET = unchecked((int)0x800401C0),
'    '        CONVERT10_E_OLESTREAM_PUT = unchecked((int)0x800401C1),
'    '-------------------------------------^--- GenCode(token): unexpected token type
'    Public Const CONVERT10_E_OLESTREAM_FMT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CONVERT10_E_OLESTREAM_PUT = unchecked((int)0x800401C1),
'    '        CONVERT10_E_OLESTREAM_FMT = unchecked((int)0x800401C2),
'    '-------------------------------------^--- GenCode(token): unexpected token type
'    Public Const CONVERT10_E_OLESTREAM_BITMAP_TO_DIB As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CONVERT10_E_OLESTREAM_FMT = unchecked((int)0x800401C2),
'    '        CONVERT10_E_OLESTREAM_BITMAP_TO_DIB = unchecked((int)0x800401C3),
'    '-----------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const CONVERT10_E_STG_FMT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CONVERT10_E_OLESTREAM_BITMAP_TO_DIB = unchecked((int)0x800401C3),
'    '        CONVERT10_E_STG_FMT = unchecked((int)0x800401C4),
'    '-------------------------------^--- GenCode(token): unexpected token type
'    Public Const CONVERT10_E_STG_NO_STD_STREAM As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CONVERT10_E_STG_FMT = unchecked((int)0x800401C4),
'    '        CONVERT10_E_STG_NO_STD_STREAM = unchecked((int)0x800401C5),
'    '-----------------------------------------^--- GenCode(token): unexpected token type
'    Public Const CONVERT10_E_STG_DIB_TO_BITMAP As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CONVERT10_E_STG_NO_STD_STREAM = unchecked((int)0x800401C5),
'    '        CONVERT10_E_STG_DIB_TO_BITMAP = unchecked((int)0x800401C6),
'    '-----------------------------------------^--- GenCode(token): unexpected token type
'    Public Const CLIPBRD_E_FIRST As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CONVERT10_E_STG_DIB_TO_BITMAP = unchecked((int)0x800401C6),
'    '        CLIPBRD_E_FIRST = unchecked((int)0x800401D0),
'    '---------------------------^--- GenCode(token): unexpected token type
'    Public Const CLIPBRD_E_LAST As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CLIPBRD_E_FIRST = unchecked((int)0x800401D0),
'    '        CLIPBRD_E_LAST = unchecked((int)0x800401DF),
'    '--------------------------^--- GenCode(token): unexpected token type
'    Public Const CLIPBRD_S_FIRST As Integer = &H401D0
'    Public Const CLIPBRD_S_LAST As Integer = &H401DF
'    Public Const CLIPBRD_E_CANT_OPEN As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CLIPBRD_S_LAST = 0x000401DF,
'    '        CLIPBRD_E_CANT_OPEN = unchecked((int)0x800401D0),
'    '-------------------------------^--- GenCode(token): unexpected token type
'    Public Const CLIPBRD_E_CANT_EMPTY As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CLIPBRD_E_CANT_OPEN = unchecked((int)0x800401D0),
'    '        CLIPBRD_E_CANT_EMPTY = unchecked((int)0x800401D1),
'    '--------------------------------^--- GenCode(token): unexpected token type
'    Public Const CLIPBRD_E_CANT_SET As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CLIPBRD_E_CANT_EMPTY = unchecked((int)0x800401D1),
'    '        CLIPBRD_E_CANT_SET = unchecked((int)0x800401D2),
'    '------------------------------^--- GenCode(token): unexpected token type
'    Public Const CLIPBRD_E_BAD_DATA As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CLIPBRD_E_CANT_SET = unchecked((int)0x800401D2),
'    '        CLIPBRD_E_BAD_DATA = unchecked((int)0x800401D3),
'    '------------------------------^--- GenCode(token): unexpected token type
'    Public Const CLIPBRD_E_CANT_CLOSE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CLIPBRD_E_BAD_DATA = unchecked((int)0x800401D3),
'    '        CLIPBRD_E_CANT_CLOSE = unchecked((int)0x800401D4),
'    '--------------------------------^--- GenCode(token): unexpected token type
'    Public Const CO_E_FIRST As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CLIPBRD_E_CANT_CLOSE = unchecked((int)0x800401D4),
'    '        CO_E_FIRST = unchecked((int)0x800401F0),
'    '----------------------^--- GenCode(token): unexpected token type
'    Public Const CO_E_LAST As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_FIRST = unchecked((int)0x800401F0),
'    '        CO_E_LAST = unchecked((int)0x800401FF),
'    '---------------------^--- GenCode(token): unexpected token type
'    Public Const CO_S_FIRST As Integer = &H401F0
'    Public Const CO_S_LAST As Integer = &H401FF
'    Public Const CO_E_NOTINITIALIZED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CO_S_LAST = 0x000401FF,
'    '        CO_E_NOTINITIALIZED = unchecked((int)0x800401F0),
'    '-------------------------------^--- GenCode(token): unexpected token type
'    Public Const CO_E_ALREADYINITIALIZED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_NOTINITIALIZED = unchecked((int)0x800401F0),
'    '        CO_E_ALREADYINITIALIZED = unchecked((int)0x800401F1),
'    '-----------------------------------^--- GenCode(token): unexpected token type
'    Public Const CO_E_CANTDETERMINEClass As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_ALREADYINITIALIZED = unchecked((int)0x800401F1),
'    '        CO_E_CANTDETERMINECLASS = unchecked((int)0x800401F2),
'    '-----------------------------------^--- GenCode(token): unexpected token type
'    Public Const CO_E_CLASSSTRING As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_CANTDETERMINECLASS = unchecked((int)0x800401F2),
'    '        CO_E_CLASSSTRING = unchecked((int)0x800401F3),
'    '----------------------------^--- GenCode(token): unexpected token type
'    Public Const CO_E_IIDSTRING As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_CLASSSTRING = unchecked((int)0x800401F3),
'    '        CO_E_IIDSTRING = unchecked((int)0x800401F4),
'    '--------------------------^--- GenCode(token): unexpected token type
'    Public Const CO_E_APPNOTFOUND As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_IIDSTRING = unchecked((int)0x800401F4),
'    '        CO_E_APPNOTFOUND = unchecked((int)0x800401F5),
'    '----------------------------^--- GenCode(token): unexpected token type
'    Public Const CO_E_APPSINGLEUSE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_APPNOTFOUND = unchecked((int)0x800401F5),
'    '        CO_E_APPSINGLEUSE = unchecked((int)0x800401F6),
'    '-----------------------------^--- GenCode(token): unexpected token type
'    Public Const CO_E_ERRORINAPP As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_APPSINGLEUSE = unchecked((int)0x800401F6),
'    '        CO_E_ERRORINAPP = unchecked((int)0x800401F7),
'    '---------------------------^--- GenCode(token): unexpected token type
'    Public Const CO_E_DLLNOTFOUND As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_ERRORINAPP = unchecked((int)0x800401F7),
'    '        CO_E_DLLNOTFOUND = unchecked((int)0x800401F8),
'    '----------------------------^--- GenCode(token): unexpected token type
'    Public Const CO_E_ERRORINDLL As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_DLLNOTFOUND = unchecked((int)0x800401F8),
'    '        CO_E_ERRORINDLL = unchecked((int)0x800401F9),
'    '---------------------------^--- GenCode(token): unexpected token type
'    Public Const CO_E_WRONGOSFORAPP As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_ERRORINDLL = unchecked((int)0x800401F9),
'    '        CO_E_WRONGOSFORAPP = unchecked((int)0x800401FA),
'    '------------------------------^--- GenCode(token): unexpected token type
'    Public Const CO_E_OBJNOTREG As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_WRONGOSFORAPP = unchecked((int)0x800401FA),
'    '        CO_E_OBJNOTREG = unchecked((int)0x800401FB),
'    '--------------------------^--- GenCode(token): unexpected token type
'    Public Const CO_E_OBJISREG As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_OBJNOTREG = unchecked((int)0x800401FB),
'    '        CO_E_OBJISREG = unchecked((int)0x800401FC),
'    '-------------------------^--- GenCode(token): unexpected token type
'    Public Const CO_E_OBJNOTCONNECTED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_OBJISREG = unchecked((int)0x800401FC),
'    '        CO_E_OBJNOTCONNECTED = unchecked((int)0x800401FD),
'    '--------------------------------^--- GenCode(token): unexpected token type
'    Public Const CO_E_APPDIDNTREG As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_OBJNOTCONNECTED = unchecked((int)0x800401FD),
'    '        CO_E_APPDIDNTREG = unchecked((int)0x800401FE),
'    '----------------------------^--- GenCode(token): unexpected token type
'    Public Const CO_E_RELEASED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_APPDIDNTREG = unchecked((int)0x800401FE),
'    '        CO_E_RELEASED = unchecked((int)0x800401FF),
'    '-------------------------^--- GenCode(token): unexpected token type
'    Public Const CACHE_S_FORMATETC_NOTSUPPORTED As Integer = &H40170
'    Public Const CACHE_S_SAMECACHE As Integer = &H40171
'    Public Const CACHE_S_SOMECACHES_NOTUPDATED As Integer = &H40172
'    Public Const CONVERT10_S_NO_PRESENTATION As Integer = &H401C0
'    Public Const CO_E_CLASS_CREATE_FAILED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CONVERT10_S_NO_PRESENTATION = 0x000401C0,
'    '        CO_E_CLASS_CREATE_FAILED = unchecked((int)0x80080001),
'    '------------------------------------^--- GenCode(token): unexpected token type
'    Public Const CO_E_SCM_ERROR As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_CLASS_CREATE_FAILED = unchecked((int)0x80080001),
'    '        CO_E_SCM_ERROR = unchecked((int)0x80080002),
'    '--------------------------^--- GenCode(token): unexpected token type
'    Public Const CO_E_SCM_RPC_FAILURE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_SCM_ERROR = unchecked((int)0x80080002),
'    '        CO_E_SCM_RPC_FAILURE = unchecked((int)0x80080003),
'    '--------------------------------^--- GenCode(token): unexpected token type
'    Public Const CO_E_BAD_PATH As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_SCM_RPC_FAILURE = unchecked((int)0x80080003),
'    '        CO_E_BAD_PATH = unchecked((int)0x80080004),
'    '-------------------------^--- GenCode(token): unexpected token type
'    Public Const CO_E_SERVER_EXEC_FAILURE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_BAD_PATH = unchecked((int)0x80080004),
'    '        CO_E_SERVER_EXEC_FAILURE = unchecked((int)0x80080005),
'    '------------------------------------^--- GenCode(token): unexpected token type
'    Public Const CO_E_OBJSRV_RPC_FAILURE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_SERVER_EXEC_FAILURE = unchecked((int)0x80080005),
'    '        CO_E_OBJSRV_RPC_FAILURE = unchecked((int)0x80080006),
'    '-----------------------------------^--- GenCode(token): unexpected token type
'    Public Const CO_E_SERVER_STOPPING As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_OBJSRV_RPC_FAILURE = unchecked((int)0x80080006),
'    '        CO_E_SERVER_STOPPING = unchecked((int)0x80080008),
'    '--------------------------------^--- GenCode(token): unexpected token type
'    Public Const CO_S_NOTALLINTERFACES As Integer = &H80012
'    Public Const CERT_E_EXPIRED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CO_S_NOTALLINTERFACES = 0x00080012,
'    '        CERT_E_EXPIRED = unchecked((int)0x800B0101),
'    '--------------------------^--- GenCode(token): unexpected token type
'    Public Const CERT_E_VALIDIYPERIODNESTING As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CERT_E_EXPIRED = unchecked((int)0x800B0101),
'    '        CERT_E_VALIDIYPERIODNESTING = unchecked((int)0x800B0102),
'    '---------------------------------------^--- GenCode(token): unexpected token type
'    Public Const CERT_E_ROLE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CERT_E_VALIDIYPERIODNESTING = unchecked((int)0x800B0102),
'    '        CERT_E_ROLE = unchecked((int)0x800B0103),
'    '-----------------------^--- GenCode(token): unexpected token type
'    Public Const CERT_E_CRITICAL As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CERT_E_ROLE = unchecked((int)0x800B0103),
'    '        CERT_E_CRITICAL = unchecked((int)0x800B0105),
'    '---------------------------^--- GenCode(token): unexpected token type
'    Public Const CERT_E_PURPOSE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CERT_E_CRITICAL = unchecked((int)0x800B0105),
'    '        CERT_E_PURPOSE = unchecked((int)0x800B0106),
'    '--------------------------^--- GenCode(token): unexpected token type
'    Public Const CERT_E_ISSUERCHAINING As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CERT_E_PURPOSE = unchecked((int)0x800B0106),
'    '        CERT_E_ISSUERCHAINING = unchecked((int)0x800B0107),
'    '---------------------------------^--- GenCode(token): unexpected token type
'    Public Const CERT_E_MALFORMED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CERT_E_ISSUERCHAINING = unchecked((int)0x800B0107),
'    '        CERT_E_MALFORMED = unchecked((int)0x800B0108),
'    '----------------------------^--- GenCode(token): unexpected token type
'    Public Const CERT_E_UNTRUSTEDROOT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CERT_E_MALFORMED = unchecked((int)0x800B0108),
'    '        CERT_E_UNTRUSTEDROOT = unchecked((int)0x800B0109),
'    '--------------------------------^--- GenCode(token): unexpected token type
'    Public Const CERT_E_CHAINING As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CERT_E_UNTRUSTEDROOT = unchecked((int)0x800B0109),
'    '        CERT_E_CHAINING = unchecked((int)0x800B010A),
'    '---------------------------^--- GenCode(token): unexpected token type
'    Public Const COMPLEXREGION As Integer = 3
'    Public Const COLORONCOLOR As Integer = 3
'    Public Const CLIP_TO_PATH As Integer = 4097
'    Public Const CLOSECHANNEL As Integer = 4112
'    Public Const CM_OUT_OF_GAMUT As Integer = 255
'    Public Const CM_IN_GAMUT As Integer = 0
'    Public Const CLIP_DEFAULT_PRECIS As Integer = 0
'    Public Const CLIP_CHARACTER_PRECIS As Integer = 1
'    Public Const CLIP_STROKE_PRECIS As Integer = 2
'    Public Const CLIP_MASK As Integer = &HF
'    Public Const CLIP_LH_ANGLES As Integer = Machine.Shift.Left(1, 4)
'    Public Const CLIP_TT_ALWAYS As Integer = Machine.Shift.Left(2, 4)
'    Public Const CLIP_EMBEDDED As Integer = Machine.Shift.Left(8, 4)
'    Public Const CHINESEBIG5_CHARSET As Integer = 136
'    Public Const CLR_INVALID As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                          CHINESEBIG5_CHARSET = 136,
'    '        CLR_INVALID = unchecked((int)0xFFFFFFFF),
'    '-----------------------^--- GenCode(token): unexpected token type
'    Public Const CURVECAPS As Integer = 28
'    Public Const CLIPCAPS As Integer = 36
'    Public Const COLORRES As Integer = 108
'    Public Const CC_NONE As Integer = 0
'    Public Const CC_CIRCLES As Integer = 1
'    Public Const CC_PIE As Integer = 2
'    Public Const CC_CHORD As Integer = 4
'    Public Const CC_ELLIPSES As Integer = 8
'    Public Const CC_WIDE As Integer = 16
'    Public Const CC_STYLED As Integer = 32
'    Public Const CC_WIDESTYLED As Integer = 64
'    Public Const CC_INTERIORS As Integer = 128
'    Public Const CC_ROUNDRECT As Integer = 256
'    Public Const CP_NONE As Integer = 0
'    Public Const CP_RECTANGLE As Integer = 1
'    Public Const CP_REGION As Integer = 2
'    Public Const CBM_INIT As Integer = &H4
'    Public Const CCHDEVICENAME As Integer = 32
'    Public Const CCHFORMNAME As Integer = 32
'    Public Const CA_NEGATIVE As Integer = &H1
'    Public Const CA_LOG_FILTER As Integer = &H2
'    Public Const CONNECT_UPDATE_PROFILE As Integer = &H1
'    Public Const CONNECT_UPDATE_RECENT As Integer = &H2
'    Public Const CONNECT_TEMPORARY As Integer = &H4
'    Public Const CONNECT_INTERACTIVE As Integer = &H8
'    Public Const CONNECT_PROMPT As Integer = &H10
'    Public Const CONNECT_NEED_DRIVE As Integer = &H20
'    Public Const CONNECT_REFCOUNT As Integer = &H40
'    Public Const CONNECT_REDIRECT As Integer = &H80
'    Public Const CONNECT_LOCALDRIVE As Integer = &H100
'    Public Const CONNECT_CURRENT_MEDIA As Integer = &H200
'    Public Const CONNECT_DEFERRED As Integer = &H400
'    Public Const CONNECT_RESERVED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CONNECT_DEFERRED = 0x00000400,
'    '        CONNECT_RESERVED = unchecked((int)0xFF000000),
'    '----------------------------^--- GenCode(token): unexpected token type
'    Public Const CONNDLG_RO_PATH As Integer = &H1
'    Public Const CONNDLG_CONN_POINT As Integer = &H2
'    Public Const CONNDLG_USE_MRU As Integer = &H4
'    Public Const CONNDLG_HIDE_BOX As Integer = &H8
'    Public Const CONNDLG_PERSIST As Integer = &H10
'    Public Const CONNDLG_NOT_PERSIST As Integer = &H20
'    Public Const CT_CTYPE1 As Integer = &H1
'    Public Const CT_CTYPE2 As Integer = &H2
'    Public Const CT_CTYPE3 As Integer = &H4
'    Public Const C1_UPPER As Integer = &H1
'    Public Const C1_LOWER As Integer = &H2
'    Public Const C1_DIGIT As Integer = &H4
'    Public Const C1_SPACE As Integer = &H8
'    Public Const C1_PUNCT As Integer = &H10
'    Public Const C1_CNTRL As Integer = &H20
'    Public Const C1_BLANK As Integer = &H40
'    Public Const C1_XDIGIT As Integer = &H80
'    Public Const C1_ALPHA As Integer = &H100
'    Public Const C2_LEFTTORIGHT As Integer = &H1
'    Public Const C2_RIGHTTOLEFT As Integer = &H2
'    Public Const C2_EUROPENUMBER As Integer = &H3
'    Public Const C2_EUROPESEPARATOR As Integer = &H4
'    Public Const C2_EUROPETERMINATOR As Integer = &H5
'    Public Const C2_ARABICNUMBER As Integer = &H6
'    Public Const C2_COMMONSEPARATOR As Integer = &H7
'    Public Const C2_BLOCKSEPARATOR As Integer = &H8
'    Public Const C2_SEGMENTSEPARATOR As Integer = &H9
'    Public Const C2_WHITESPACE As Integer = &HA
'    Public Const C2_OTHERNEUTRAL As Integer = &HB
'    Public Const C2_NOTAPPLICABLE As Integer = &H0
'    Public Const C3_NONSPACING As Integer = &H1
'    Public Const C3_DIACRITIC As Integer = &H2
'    Public Const C3_VOWELMARK As Integer = &H4
'    Public Const C3_SYMBOL As Integer = &H8
'    Public Const C3_KATAKANA As Integer = &H10
'    Public Const C3_HIRAGANA As Integer = &H20
'    Public Const C3_HALFWIDTH As Integer = &H40
'    Public Const C3_FULLWIDTH As Integer = &H80
'    Public Const C3_IDEOGRAPH As Integer = &H100
'    Public Const C3_KASHIDA As Integer = &H200
'    Public Const C3_LEXICAL As Integer = &H400
'    Public Const C3_ALPHA As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        C3_LEXICAL = 0x0400,
'    '        C3_ALPHA = unchecked((int)0x8000),
'    '--------------------^--- GenCode(token): unexpected token type
'    Public Const C3_NOTAPPLICABLE As Integer = &H0
'    Public Const CP_INSTALLED As Integer = &H1
'    Public Const CP_SUPPORTED As Integer = &H2
'    Public Const CP_ACP As Integer = 0
'    Public Const CP_OEMCP As Integer = 1
'    Public Const CP_MACCP As Integer = 2
'    Public Const CP_UTF7 As Integer = 65000
'    Public Const CP_UTF8 As Integer = 65001
'    Public Const CTRY_DEFAULT As Integer = 0
'    Public Const CTRY_AUSTRALIA As Integer = 61
'    Public Const CTRY_AUSTRIA As Integer = 43
'    Public Const CTRY_BELGIUM As Integer = 32
'    Public Const CTRY_BRAZIL As Integer = 55
'    Public Const CTRY_BULGARIA As Integer = 359
'    Public Const CTRY_CANADA As Integer = 2
'    Public Const CTRY_CROATIA As Integer = 385
'    Public Const CTRY_CZECH As Integer = 42
'    Public Const CTRY_DENMARK As Integer = 45
'    Public Const CTRY_FINLAND As Integer = 358
'    Public Const CTRY_FRANCE As Integer = 33
'    Public Const CTRY_GERMANY As Integer = 49
'    Public Const CTRY_GREECE As Integer = 30
'    Public Const CTRY_HONG_KONG As Integer = 852
'    Public Const CTRY_HUNGARY As Integer = 36
'    Public Const CTRY_ICELAND As Integer = 354
'    Public Const CTRY_IRELAND As Integer = 353
'    Public Const CTRY_ITALY As Integer = 39
'    Public Const CTRY_JAPAN As Integer = 81
'    Public Const CTRY_MEXICO As Integer = 52
'    Public Const CTRY_NETHERLANDS As Integer = 31
'    Public Const CTRY_NEW_ZEALAND As Integer = 64
'    Public Const CTRY_NORWAY As Integer = 47
'    Public Const CTRY_POLAND As Integer = 48
'    Public Const CTRY_PORTUGAL As Integer = 351
'    Public Const CTRY_PRCHINA As Integer = 86
'    Public Const CTRY_ROMANIA As Integer = 40
'    Public Const CTRY_RUSSIA As Integer = 7
'    Public Const CTRY_SINGAPORE As Integer = 65
'    Public Const CTRY_SLOVAK As Integer = 42
'    Public Const CTRY_SLOVENIA As Integer = 386
'    Public Const CTRY_SOUTH_KOREA As Integer = 82
'    Public Const CTRY_SPAIN As Integer = 34
'    Public Const CTRY_SWEDEN As Integer = 46
'    Public Const CTRY_SWITZERLAND As Integer = 41
'    Public Const CTRY_TAIWAN As Integer = 886
'    Public Const CTRY_TURKEY As Integer = 90
'    Public Const CTRY_UNITED_KINGDOM As Integer = 44
'    Public Const CTRY_UNITED_STATES As Integer = 1
'    Public Const CAL_ICALINTVALUE As Integer = &H1
'    Public Const CAL_SCALNAME As Integer = &H2
'    Public Const CAL_IYEAROFFSETRANGE As Integer = &H3
'    Public Const CAL_SERASTRING As Integer = &H4
'    Public Const CAL_SSHORTDATE As Integer = &H5
'    Public Const CAL_SLONGDATE As Integer = &H6
'    Public Const CAL_SDAYNAME1 As Integer = &H7
'    Public Const CAL_SDAYNAME2 As Integer = &H8
'    Public Const CAL_SDAYNAME3 As Integer = &H9
'    Public Const CAL_SDAYNAME4 As Integer = &HA
'    Public Const CAL_SDAYNAME5 As Integer = &HB
'    Public Const CAL_SDAYNAME6 As Integer = &HC
'    Public Const CAL_SDAYNAME7 As Integer = &HD
'    Public Const CAL_SABBREVDAYNAME1 As Integer = &HE
'    Public Const CAL_SABBREVDAYNAME2 As Integer = &HF
'    Public Const CAL_SABBREVDAYNAME3 As Integer = &H10
'    Public Const CAL_SABBREVDAYNAME4 As Integer = &H11
'    Public Const CAL_SABBREVDAYNAME5 As Integer = &H12
'    Public Const CAL_SABBREVDAYNAME6 As Integer = &H13
'    Public Const CAL_SABBREVDAYNAME7 As Integer = &H14
'    Public Const CAL_SMONTHNAME1 As Integer = &H15
'    Public Const CAL_SMONTHNAME2 As Integer = &H16
'    Public Const CAL_SMONTHNAME3 As Integer = &H17
'    Public Const CAL_SMONTHNAME4 As Integer = &H18
'    Public Const CAL_SMONTHNAME5 As Integer = &H19
'    Public Const CAL_SMONTHNAME6 As Integer = &H1A
'    Public Const CAL_SMONTHNAME7 As Integer = &H1B
'    Public Const CAL_SMONTHNAME8 As Integer = &H1C
'    Public Const CAL_SMONTHNAME9 As Integer = &H1D
'    Public Const CAL_SMONTHNAME10 As Integer = &H1E
'    Public Const CAL_SMONTHNAME11 As Integer = &H1F
'    Public Const CAL_SMONTHNAME12 As Integer = &H20
'    Public Const CAL_SMONTHNAME13 As Integer = &H21
'    Public Const CAL_SABBREVMONTHNAME1 As Integer = &H22
'    Public Const CAL_SABBREVMONTHNAME2 As Integer = &H23
'    Public Const CAL_SABBREVMONTHNAME3 As Integer = &H24
'    Public Const CAL_SABBREVMONTHNAME4 As Integer = &H25
'    Public Const CAL_SABBREVMONTHNAME5 As Integer = &H26
'    Public Const CAL_SABBREVMONTHNAME6 As Integer = &H27
'    Public Const CAL_SABBREVMONTHNAME7 As Integer = &H28
'    Public Const CAL_SABBREVMONTHNAME8 As Integer = &H29
'    Public Const CAL_SABBREVMONTHNAME9 As Integer = &H2A
'    Public Const CAL_SABBREVMONTHNAME10 As Integer = &H2B
'    Public Const CAL_SABBREVMONTHNAME11 As Integer = &H2C
'    Public Const CAL_SABBREVMONTHNAME12 As Integer = &H2D
'    Public Const CAL_SABBREVMONTHNAME13 As Integer = &H2E
'    Public Const CAL_GREGORIAN As Integer = 1
'    Public Const CAL_GREGORIAN_US As Integer = 2
'    Public Const CAL_JAPAN As Integer = 3
'    Public Const CAL_TAIWAN As Integer = 4
'    Public Const CAL_KOREA As Integer = 5
'    Public Const CAL_HIJRI As Integer = 6
'    Public Const CAL_THAI As Integer = 7
'    Public Const CAL_HEBREW As Integer = 8
'    Public Const CONTAINER_INHERIT_ACE As Integer = &H2
'    Public Const COMPRESSION_FORMAT_NONE As Integer = &H0
'    Public Const COMPRESSION_FORMAT_DEFAULT As Integer = &H1
'    Public Const COMPRESSION_FORMAT_LZNT1 As Integer = &H2
'    Public Const COMPRESSION_ENGINE_STANDARD As Integer = &H0
'    Public Const COMPRESSION_ENGINE_MAXIMUM As Integer = &H100
'    Public Const CS_VREDRAW As Integer = &H1
'    Public Const CS_HREDRAW As Integer = &H2
'    Public Const CS_KEYCVTWINDOW As Integer = &H4
'    Public Const CS_DBLCLKS As Integer = &H8
'    Public Const CS_OWNDC As Integer = &H20
'    Public Const CS_CLASSDC As Integer = &H40
'    Public Const CS_PARENTDC As Integer = &H80
'    Public Const CS_NOKEYCVT As Integer = &H100
'    Public Const CS_NOCLOSE As Integer = &H200
'    Public Const CS_SAVEBITS As Integer = &H800
'    Public Const CS_BYTEALIGNCLIENT As Integer = &H1000
'    Public Const CS_BYTEALIGNWINDOW As Integer = &H2000
'    Public Const CS_GLOBALClass As Integer = &H4000
'    Public Const CS_IME As Integer = &H10000
'    Public Const CF_TEXT As Integer = 1
'    Public Const CF_BITMAP As Integer = 2
'    Public Const CF_METAFILEPICT As Integer = 3
'    Public Const CF_SYLK As Integer = 4
'    Public Const CF_DIF As Integer = 5
'    Public Const CF_TIFF As Integer = 6
'    Public Const CF_OEMTEXT As Integer = 7
'    Public Const CF_DIB As Integer = 8
'    Public Const CF_PALETTE As Integer = 9
'    Public Const CF_PENDATA As Integer = 10
'    Public Const CF_RIFF As Integer = 11
'    Public Const CF_WAVE As Integer = 12
'    Public Const CF_UNICODETEXT As Integer = 13
'    Public Const CF_ENHMETAFILE As Integer = 14
'    Public Const CF_HDROP As Integer = 15
'    Public Const CF_LOCALE As Integer = 16
'    Public Const CF_MAX As Integer = 17
'    Public Const CF_OWNERDISPLAY As Integer = &H80
'    Public Const CF_DSPTEXT As Integer = &H81
'    Public Const CF_DSPBITMAP As Integer = &H82
'    Public Const CF_DSPMETAFILEPICT As Integer = &H83
'    Public Const CF_DSPENHMETAFILE As Integer = &H8E
'    Public Const CF_PRIVATEFIRST As Integer = &H200
'    Public Const CF_PRIVATELAST As Integer = &H2FF
'    Public Const CF_GDIOBJFIRST As Integer = &H300
'    Public Const CF_GDIOBJLAST As Integer = &H3FF
'    Public Const CW_USEDEFAULT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CF_GDIOBJLAST = 0x03FF,
'    '        CW_USEDEFAULT = (unchecked((int)0x80000000)),
'    '--------------------------^--- GenCode(token): unexpected token type
'    Public Const CWP_ALL As Integer = &H0
'    Public Const CWP_SKIPINVISIBLE As Integer = &H1
'    Public Const CWP_SKIPDISABLED As Integer = &H2
'    Public Const CWP_SKIPTRANSPARENT As Integer = &H4
'    Public Const CTLCOLOR_MSGBOX As Integer = 0
'    Public Const CTLCOLOR_EDIT As Integer = 1
'    Public Const CTLCOLOR_LISTBOX As Integer = 2
'    Public Const CTLCOLOR_BTN As Integer = 3
'    Public Const CTLCOLOR_DLG As Integer = 4
'    Public Const CTLCOLOR_SCROLLBAR As Integer = 5
'    Public Const CTLCOLOR_STATIC As Integer = 6
'    Public Const CTLCOLOR_MAX As Integer = 7
'    Public Const COLOR_SCROLLBAR As Integer = 0
'    Public Const COLOR_BACKGROUND As Integer = 1
'    Public Const COLOR_ACTIVECAPTION As Integer = 2
'    Public Const COLOR_INACTIVECAPTION As Integer = 3
'    Public Const COLOR_MENU As Integer = 4
'    Public Const COLOR_WINDOW As Integer = 5
'    Public Const COLOR_WINDOWFRAME As Integer = 6
'    Public Const COLOR_MENUTEXT As Integer = 7
'    Public Const COLOR_WINDOWTEXT As Integer = 8
'    Public Const COLOR_CAPTIONTEXT As Integer = 9
'    Public Const COLOR_ACTIVEBORDER As Integer = 10
'    Public Const COLOR_INACTIVEBORDER As Integer = 11
'    Public Const COLOR_APPWORKSPACE As Integer = 12
'    Public Const COLOR_HIGHLIGHT As Integer = 13
'    Public Const COLOR_HIGHLIGHTTEXT As Integer = 14
'    Public Const COLOR_BTNFACE As Integer = 15
'    Public Const COLOR_BTNSHADOW As Integer = 16
'    Public Const COLOR_GRAYTEXT As Integer = 17
'    Public Const COLOR_BTNTEXT As Integer = 18
'    Public Const COLOR_INACTIVECAPTIONTEXT As Integer = 19
'    Public Const COLOR_BTNHIGHLIGHT As Integer = 20
'    Public Const COLOR_3DDKSHADOW As Integer = 21
'    Public Const COLOR_3DLIGHT As Integer = 22
'    Public Const COLOR_INFOTEXT As Integer = 23
'    Public Const COLOR_INFOBK As Integer = 24
'    Public Const COLOR_DESKTOP As Integer = 1
'    Public Const COLOR_3DFACE As Integer = 15
'    Public Const COLOR_3DSHADOW As Integer = 16
'    Public Const COLOR_3DHIGHLIGHT As Integer = 20
'    Public Const COLOR_3DHILIGHT As Integer = 20
'    Public Const COLOR_BTNHILIGHT As Integer = 20
'    Public Const CB_OKAY As Integer = 0
'    Public Const CB_ERR As Integer = - 1
'    Public Const CB_ERRSPACE As Integer = - 2
'    Public Const CBN_ERRSPACE As Integer = - 1
'    Public Const CBN_SELCHANGE As Integer = 1
'    Public Const CBN_DBLCLK As Integer = 2
'    Public Const CBN_SETFOCUS As Integer = 3
'    Public Const CBN_KILLFOCUS As Integer = 4
'    Public Const CBN_EDITCHANGE As Integer = 5
'    Public Const CBN_EDITUPDATE As Integer = 6
'    Public Const CBN_DROPDOWN As Integer = 7
'    Public Const CBN_CLOSEUP As Integer = 8
'    Public Const CBN_SELENDOK As Integer = 9
'    Public Const CBN_SELENDCANCEL As Integer = 10
'    Public Const CBS_SIMPLE As Integer = &H1
'    Public Const CBS_DROPDOWN As Integer = &H2
'    Public Const CBS_DROPDOWNLIST As Integer = &H3
'    Public Const CBS_OWNERDRAWFIXED As Integer = &H10
'    Public Const CBS_OWNERDRAWVARIABLE As Integer = &H20
'    Public Const CBS_AUTOHSCROLL As Integer = &H40
'    Public Const CBS_OEMCONVERT As Integer = &H80
'    Public Const CBS_SORT As Integer = &H100
'    Public Const CBS_HASSTRINGS As Integer = &H200
'    Public Const CBS_NOINTEGRALHEIGHT As Integer = &H400
'    Public Const CBS_DISABLENOSCROLL As Integer = &H800
'    Public Const CBS_UPPERCASE As Integer = &H2000
'    Public Const CBS_LOWERCASE As Integer = &H4000
'    Public Const CB_GETEDITSEL As Integer = &H140
'    Public Const CB_LIMITTEXT As Integer = &H141
'    Public Const CB_SETEDITSEL As Integer = &H142
'    Public Const CB_ADDSTRING As Integer = &H143
'    Public Const CB_DELETESTRING As Integer = &H144
'    Public Const CB_DIR As Integer = &H145
'    Public Const CB_GETCOUNT As Integer = &H146
'    Public Const CB_GETCURSEL As Integer = &H147
'    Public Const CB_GETLBTEXT As Integer = &H148
'    Public Const CB_GETLBTEXTLEN As Integer = &H149
'    Public Const CB_INSERTSTRING As Integer = &H14A
'    Public Const CB_RESETCONTENT As Integer = &H14B
'    Public Const CB_FINDSTRING As Integer = &H14C
'    Public Const CB_SELECTSTRING As Integer = &H14D
'    Public Const CB_SETCURSEL As Integer = &H14E
'    Public Const CB_SHOWDROPDOWN As Integer = &H14F
'    Public Const CB_GETITEMDATA As Integer = &H150
'    Public Const CB_SETITEMDATA As Integer = &H151
'    Public Const CB_GETDROPPEDCONTROLRECT As Integer = &H152
'    Public Const CB_SETITEMHEIGHT As Integer = &H153
'    Public Const CB_GETITEMHEIGHT As Integer = &H154
'    Public Const CB_SETEXTENDEDUI As Integer = &H155
'    Public Const CB_GETEXTENDEDUI As Integer = &H156
'    Public Const CB_GETDROPPEDSTATE As Integer = &H157
'    Public Const CB_FINDSTRINGEXACT As Integer = &H158
'    Public Const CB_SETLOCALE As Integer = &H159
'    Public Const CB_GETLOCALE As Integer = &H15A
'    Public Const CB_GETTOPINDEX As Integer = &H15B
'    Public Const CB_SETTOPINDEX As Integer = &H15C
'    Public Const CB_GETHORIZONTALEXTENT As Integer = &H15D
'    Public Const CB_SETHORIZONTALEXTENT As Integer = &H15E
'    Public Const CB_GETDROPPEDWIDTH As Integer = &H15F
'    Public Const CB_SETDROPPEDWIDTH As Integer = &H160
'    Public Const CB_INITSTORAGE As Integer = &H161
'    Public Const CB_MSGMAX As Integer = &H162
'    ' CB_MSGMAX = 0x015B;
'    Public Const CDS_UPDATEREGISTRY As Integer = &H1
'    Public Const CDS_TEST As Integer = &H2
'    Public Const CDS_FULLSCREEN As Integer = &H4
'    Public Const CDS_GLOBAL As Integer = &H8
'    Public Const CDS_SET_PRIMARY As Integer = &H10
'    Public Const CDS_RESET As Integer = &H40000000
'    Public Const CDS_SETRECT As Integer = &H20000000
'    Public Const CDS_NORESET As Integer = &H10000000
'    Public Const CBEN_FIRST As Integer = 0 - 800
'    Public Const CBEN_LAST As Integer = 0 - 830
'    Public Const CDRF_DODEFAULT As Integer = &H0
'    Public Const CDRF_NEWFONT As Integer = &H2
'    Public Const CDRF_SKIPDEFAULT As Integer = &H4
'    Public Const CDRF_NOTIFYPOSTPAINT As Integer = &H10
'    Public Const CDRF_NOTIFYITEMDRAW As Integer = &H20
'    Public Const CDRF_NOTIFYSUBITEMDRAW As Integer = CDRF_NOTIFYITEMDRAW
'    Public Const CDRF_NOTIFYPOSTERASE As Integer = &H40
'    Public Const CDRF_NOTIFYITEMERASE As Integer = &H80
'    Public Const CDDS_PREPAINT As Integer = &H1
'    Public Const CDDS_POSTPAINT As Integer = &H2
'    Public Const CDDS_PREERASE As Integer = &H3
'    Public Const CDDS_POSTERASE As Integer = &H4
'    Public Const CDDS_ITEM As Integer = &H10000
'    Public Const CDDS_SUBITEM As Integer = &H20000
'    Public Const CDDS_ITEMPREPAINT As Integer = &H10000 Or &H1
'    Public Const CDDS_ITEMPOSTPAINT As Integer = &H10000 Or &H2
'    Public Const CDDS_ITEMPREERASE As Integer = &H10000 Or &H3
'    Public Const CDDS_ITEMPOSTERASE As Integer = &H10000 Or &H4
'    Public Const CDIS_SELECTED As Integer = &H1
'    Public Const CDIS_GRAYED As Integer = &H2
'    Public Const CDIS_DISABLED As Integer = &H4
'    Public Const CDIS_CHECKED As Integer = &H8
'    Public Const CDIS_FOCUS As Integer = &H10
'    Public Const CDIS_DEFAULT As Integer = &H20
'    Public Const CDIS_HOT As Integer = &H40
'    Public Const CDIS_MARKED As Integer = &H80
'    Public Const CDIS_INDETERMINATE As Integer = &H100
'    Public Const CLR_NONE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CDIS_INDETERMINATE = 0x0100,
'    '        CLR_NONE = unchecked((int)0xFFFFFFFF),
'    '--------------------^--- GenCode(token): unexpected token type
'    Public Const CLR_DEFAULT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CLR_NONE = unchecked((int)0xFFFFFFFF),
'    '        CLR_DEFAULT = unchecked((int)0xFF000000),
'    '-----------------------^--- GenCode(token): unexpected token type
'    Public Const CLR_HILIGHT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CLR_DEFAULT = unchecked((int)0xFF000000),
'    '        CLR_HILIGHT = unchecked((int)0xFF000000),
'    '-----------------------^--- GenCode(token): unexpected token type
'    Public Const CMB_MASKED As Integer = &H2
'    Public Const CCS_TOP As Integer = &H1
'    Public Const CCS_NOMOVEY As Integer = &H2
'    Public Const CCS_BOTTOM As Integer = &H3
'    Public Const CCS_NORESIZE As Integer = &H4
'    Public Const CCS_NOPARENTALIGN As Integer = &H8
'    Public Const CCS_ADJUSTABLE As Integer = &H20
'    Public Const CCS_NODIVIDER As Integer = &H40
'    Public Const CCS_VERT As Integer = &H80
'    Public Const CCS_LEFT As Integer = &H80 Or &H1
'    Public Const CCS_RIGHT As Integer = &H80 Or &H3
'    Public Const CCS_NOMOVEX As Integer = &H80 Or &H2
'    Public Const CBEIF_TEXT As Integer = &H1
'    Public Const CBEIF_IMAGE As Integer = &H2
'    Public Const CBEIF_SELECTEDIMAGE As Integer = &H4
'    Public Const CBEIF_OVERLAY As Integer = &H8
'    Public Const CBEIF_INDENT As Integer = &H10
'    Public Const CBEIF_LPARAM As Integer = &H20
'    Public Const CBEIF_DI_SETITEM As Integer = &H10000000
'    Public Const CBEM_INSERTITEMA As Integer = &H400 + 1
'    Public Const CBEM_SETIMAGELIST As Integer = &H400 + 2
'    Public Const CBEM_GETIMAGELIST As Integer = &H400 + 3
'    Public Const CBEM_GETITEMA As Integer = &H400 + 4
'    Public Const CBEM_SETITEMA As Integer = &H400 + 5
'    Public Const CBEM_DELETEITEM As Integer = &H144
'    Public Const CBEM_GETCOMBOCONTROL As Integer = &H400 + 6
'    Public Const CBEM_GETEDITCONTROL As Integer = &H400 + 7
'    Public Const CBEM_SETEXSTYLE As Integer = &H400 + 8
'    Public Const CBEM_GETEXSTYLE As Integer = &H400 + 9
'    Public Const CBEM_HASEDITCHANGED As Integer = &H400 + 10
'    Public Const CBEM_INSERTITEMW As Integer = &H400 + 11
'    Public Const CBEM_SETITEMW As Integer = &H400 + 12
'    Public Const CBEM_GETITEMW As Integer = &H400 + 13
'    Public Const CBES_EX_NOEDITIMAGE As Integer = &H1
'    Public Const CBES_EX_NOEDITIMAGEINDENT As Integer = &H2
'    Public Const CBES_EX_PATHWORDBREAKPROC As Integer = &H4
'    Public Const CBEN_GETDISPINFO As Integer = 0 - 800 - 0
'    Public Const CBEN_INSERTITEM As Integer = 0 - 800 - 1
'    Public Const CBEN_DELETEITEM As Integer = 0 - 800 - 2
'    Public Const CBEN_BEGINEDIT As Integer = 0 - 800 - 4
'    Public Const CBEN_ENDEDITA As Integer = 0 - 800 - 5
'    Public Const CBEN_ENDEDITW As Integer = 0 - 800 - 6
'    Public Const CBENF_KILLFOCUS As Integer = 1
'    Public Const CBENF_RETURN As Integer = 2
'    Public Const CBENF_ESCAPE As Integer = 3
'    Public Const CBENF_DROPDOWN As Integer = 4
'    Public Const CBEMAXSTRLEN As Integer = 260
'    Public Const CDM_FIRST As Integer = &H400 + 100
'    Public Const CDM_LAST As Integer = &H400 + 200
'    Public Const CDM_GETSPEC As Integer = &H400 + 100 + &H0
'    Public Const CDM_GETFILEPATH As Integer = &H400 + 100 + &H1
'    Public Const CDM_GETFOLDERPATH As Integer = &H400 + 100 + &H2
'    Public Const CDM_GETFOLDERIDLIST As Integer = &H400 + 100 + &H3
'    Public Const CDM_SETCONTROLTEXT As Integer = &H400 + 100 + &H4
'    Public Const CDM_HIDECONTROL As Integer = &H400 + 100 + &H5
'    Public Const CDM_SETDEFEXT As Integer = &H400 + 100 + &H6
'    Public Const CONTROL_C_EXIT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                CDM_SETDEFEXT = ((0x0400+100)+0x0006),
'    '                                                                                                                                                                CONTROL_C_EXIT = (unchecked((int)0xC000013A)),
'    '-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const CCM_FIRST As Integer = &H2000
'    Public Const CCM_SETBKCOLOR As Integer = CCM_FIRST + 1
'    Public Const CCM_SETCOLORSCHEME As Integer = CCM_FIRST + 2
'    Public Const CCM_GETCOLORSCHEME As Integer = CCM_FIRST + 3
'    Public Const CCM_GETDROPTARGET As Integer = CCM_FIRST + 4
'    Public Const CCM_SETUNICODEFORMAT As Integer = CCM_FIRST + 5
'    Public Const CCM_GETUNICODEFORMAT As Integer = CCM_FIRST + 6
    
        Public Const CLSCTX_INPROC_SERVER As Integer = &H1
        Public Const CLSCTX_INPROC_HANDLER As Integer = &H2
        Public Const CLSCTX_LOCAL_SERVER As Integer = &H4
        Public Const CLSCTX_INPROC_SERVER16 As Integer = &H8
        Public Const CLSCTX_REMOTE_SERVER As Integer = &H10
        Public Const CLSCTX_INPROC_HANDLER16 As Integer = &H20
        Public Const CLSCTX_INPROC_SERVERX86 As Integer = &H40
        Public Const CLSCTX_INPROC_HANDLERX86 As Integer = &H80
        Public Const CLSCTX_ESERVER_HANDLER As Integer = &H100
        Public Const CLSCTX_RESERVED As Integer = &H200
        Public Const CLSCTX_NO_CODE_DOWNLOAD As Integer = &H400
    
'    Public Const CTRLINFO_EATS_RETURN As Integer = 1
'    Public Const CTRLINFO_EATS_ESCAPE As Integer = 2
    
'    Public Const DN_DEFAULTPRN As Integer = &H1
        '    Public Const DDE_FACK As Integer = &h8000
        '    Public Const DDE_FBUSY As Integer = &H4000
'    Public Const DDE_FDEFERUPD As Integer = &H4000
        '    Public Const DDE_FACKREQ As Integer = &h8000
        '    Public Const DDE_FRELEASE As Integer = &H2000
'    Public Const DDE_FREQUESTED As Integer = &H1000
'    Public Const DDE_FAPPSTATUS As Integer = &HFF
'    Public Const DDE_FNOTPROCESSED As Integer = &H0
'    Public Const DDE_FACKRESERVED As Integer = Not(Or &H4000 Or &HFF)
'    '
'    'Note:  Error processing original source shown below
'    '        DDE_FNOTPROCESSED = 0x0000,
'    '        DDE_FACKRESERVED = (~(unchecked((int)0x8000)|0x4000|0x00ff)),
'    '-------------------------------^--- GenCode(token): unexpected token type
'    Public Const DDE_FADVRESERVED As Integer = Not(Or &H4000)
'    '
'    'Note:  Error processing original source shown below
'    '        DDE_FACKRESERVED = (~(unchecked((int)0x8000)|0x4000|0x00ff)),
'    '                           DDE_FADVRESERVED = (~(unchecked((int)0x8000)|0x4000)),
'    '--------------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const DDE_FDATRESERVED As Integer = Not(Or &H2000 Or &H1000)
'    '
'    'Note:  Error processing original source shown below
'    '                           DDE_FADVRESERVED = (~(unchecked((int)0x8000)|0x4000)),
'    '                                              DDE_FDATRESERVED = (~(unchecked((int)0x8000)|0x2000|0x1000)),
'    '---------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const DDE_FPOKRESERVED As Integer = Not &H2000
'    Public Const DNS_REGISTER As Integer = &H1
'    Public Const DNS_UNREGISTER As Integer = &H2
'    Public Const DNS_FILTERON As Integer = &H4
'    Public Const DNS_FILTEROFF As Integer = &H8
'    Public Const DMLERR_NO_ERROR As Integer = 0
'    Public Const DMLERR_FIRST As Integer = &H4000
'    Public Const DMLERR_ADVACKTIMEOUT As Integer = &H4000
'    Public Const DMLERR_BUSY As Integer = &H4001
'    Public Const DMLERR_DATAACKTIMEOUT As Integer = &H4002
'    Public Const DMLERR_DLL_NOT_INITIALIZED As Integer = &H4003
'    Public Const DMLERR_DLL_USAGE As Integer = &H4004
'    Public Const DMLERR_EXECACKTIMEOUT As Integer = &H4005
'    Public Const DMLERR_INVALIDPARAMETER As Integer = &H4006
'    Public Const DMLERR_LOW_MEMORY As Integer = &H4007
'    Public Const DMLERR_MEMORY_ERROR As Integer = &H4008
'    Public Const DMLERR_NOTPROCESSED As Integer = &H4009
'    Public Const DMLERR_NO_CONV_ESTABLISHED As Integer = &H400A
'    Public Const DMLERR_POKEACKTIMEOUT As Integer = &H400B
'    Public Const DMLERR_POSTMSG_FAILED As Integer = &H400C
'    Public Const DMLERR_REENTRANCY As Integer = &H400D
'    Public Const DMLERR_SERVER_DIED As Integer = &H400E
'    Public Const DMLERR_SYS_ERROR As Integer = &H400F
'    Public Const DMLERR_UNADVACKTIMEOUT As Integer = &H4010
'    Public Const DMLERR_UNFOUND_QUEUE_ID As Integer = &H4011
'    Public Const DMLERR_LAST As Integer = &H4011
'    Public Const DIALOPTION_BILLING As Integer = &H40
'    Public Const DIALOPTION_QUIET As Integer = &H80
'    Public Const DIALOPTION_DIALTONE As Integer = &H100
'    Public Const DRV_LOAD As Integer = &H1
'    Public Const DRV_ENABLE As Integer = &H2
'    Public Const DRV_OPEN As Integer = &H3
'    Public Const DRV_CLOSE As Integer = &H4
'    Public Const DRV_DISABLE As Integer = &H5
'    Public Const DRV_FREE As Integer = &H6
'    Public Const DRV_CONFIGURE As Integer = &H7
'    Public Const DRV_QUERYCONFIGURE As Integer = &H8
'    Public Const DRV_INSTALL As Integer = &H9
'    Public Const DRV_REMOVE As Integer = &HA
'    Public Const DRV_EXITSESSION As Integer = &HB
'    Public Const DRV_POWER As Integer = &HF
'    Public Const DRV_RESERVED As Integer = &H800
'    Public Const DRV_USER As Integer = &H4000
'    Public Const DRVCNF_CANCEL As Integer = &H0
'    Public Const DRVCNF_OK As Integer = &H1
'    Public Const DRVCNF_RESTART As Integer = &H2
'    Public Const DRV_CANCEL As Integer = &H0
'    Public Const DRV_OK As Integer = &H1
'    Public Const DRV_RESTART As Integer = &H2
'    Public Const DRV_MCI_FIRST As Integer = &H800
'    Public Const DRV_MCI_LAST As Integer = &H800 + &HFFF
'    Public Const DEREGISTERED As Integer = &H5
'    Public Const DUPLICATE As Integer = &H6
'    Public Const DUPLICATE_DEREG As Integer = &H7
        Public Const DISPID_UNKNOWN As Integer = -1
'    Public Const DISPID_VALUE As Integer = 0
'    Public Const DISPID_PROPERTYPUT As Integer = - 3
'    Public Const DISPID_NEWENUM As Integer = - 4
'    Public Const DISPID_EVALUATE As Integer = - 5
'    Public Const DISPID_DESTRUCTOR As Integer = - 7
'    Public Const DISPID_COLLECT As Integer = - 8
'    Public Const DISPATCH_METHOD As Integer = &H1
'    Public Const DISPATCH_PROPERTYGET As Integer = &H2
'    Public Const DISPATCH_PROPERTYPUT As Integer = &H4
'    Public Const DISPATCH_PROPERTYPUTREF As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        DISPATCH_PROPERTYPUT = 0x4,
'    '        DISPATCH_PROPERTYPUTREF = unchecked((int)0x8),
'    '-----------------------------------^--- GenCode(token): unexpected token type
'    Public Const DROPEFFECT_NONE As Integer = 0
'    Public Const DROPEFFECT_COPY As Integer = 1
'    Public Const DROPEFFECT_MOVE As Integer = 2
'    Public Const DROPEFFECT_LINK As Integer = 4
'    Public Const DROPEFFECT_SCROLL As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                              DROPEFFECT_LINK = (4),
'    '                                                                                DROPEFFECT_SCROLL = (unchecked((int)0x80000000)),
'    '------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const DD_DEFSCROLLINSET As Integer = 11
'    Public Const DD_DEFSCROLLDELAY As Integer = 50
'    Public Const DD_DEFSCROLLINTERVAL As Integer = 50
'    Public Const DD_DEFDRAGDELAY As Integer = 200
'    Public Const DD_DEFDRAGMINDIST As Integer = 2
'    Public Const DCE_C_ERROR_STRING_LEN As Integer = 256
'    Public Const DATABITS_5 As Integer = &H1
'    Public Const DATABITS_6 As Integer = &H2
'    Public Const DATABITS_7 As Integer = &H4
'    Public Const DATABITS_8 As Integer = &H8
'    Public Const DATABITS_16 As Integer = &H10
'    Public Const DATABITS_16X As Integer = &H20
'    Public Const DTR_CONTROL_DISABLE As Integer = &H0
'    Public Const DTR_CONTROL_ENABLE As Integer = &H1
'    Public Const DTR_CONTROL_HANDSHAKE As Integer = &H2
'    Public Const DEBUG_PROCESS As Integer = &H1
'    Public Const DEBUG_ONLY_THIS_PROCESS As Integer = &H2
'    Public Const DETACHED_PROCESS As Integer = &H8
'    Public Const DRIVE_UNKNOWN As Integer = 0
'    Public Const DRIVE_NO_ROOT_DIR As Integer = 1
'    Public Const DRIVE_REMOVABLE As Integer = 2
'    Public Const DRIVE_FIXED As Integer = 3
'    Public Const DRIVE_REMOTE As Integer = 4
'    Public Const DRIVE_CDROM As Integer = 5
'    Public Const DRIVE_RAMDISK As Integer = 6
'    Public Const DONT_RESOLVE_DLL_REFERENCES As Integer = &H1
'    Public Const DDD_RAW_TARGET_PATH As Integer = &H1
'    Public Const DDD_REMOVE_DEFINITION As Integer = &H2
'    Public Const DDD_EXACT_MATCH_ON_REMOVE As Integer = &H4
'    Public Const DDD_NO_BROADCAST_SYSTEM As Integer = &H8
'    Public Const DOCKINFO_UNDOCKED As Integer = &H1
'    Public Const DOCKINFO_DOCKED As Integer = &H2
'    Public Const DOCKINFO_USER_SUPPLIED As Integer = &H4
'    Public Const DOCKINFO_USER_UNDOCKED As Integer = &H4 Or &H1
'    Public Const DOCKINFO_USER_DOCKED As Integer = &H4 Or &H2
'    Public Const DOUBLE_CLICK As Integer = &H2
'    Public Const DM_UPDATE As Integer = 1
'    Public Const DM_COPY As Integer = 2
'    Public Const DM_PROMPT As Integer = 4
'    Public Const DM_MODIFY As Integer = 8
'    Public Const DM_IN_BUFFER As Integer = 8
'    Public Const DM_IN_PROMPT As Integer = 4
'    Public Const DM_OUT_BUFFER As Integer = 2
'    Public Const DM_OUT_DEFAULT As Integer = 1
'    Public Const DC_FIELDS As Integer = 1
'    Public Const DC_PAPERS As Integer = 2
'    Public Const DC_PAPERSIZE As Integer = 3
'    Public Const DC_MINEXTENT As Integer = 4
'    Public Const DC_MAXEXTENT As Integer = 5
'    Public Const DC_BINS As Integer = 6
'    Public Const DC_DUPLEX As Integer = 7
'    Public Const DC_SIZE As Integer = 8
'    Public Const DC_EXTRA As Integer = 9
'    Public Const DC_VERSION As Integer = 10
'    Public Const DC_DRIVER As Integer = 11
'    Public Const DC_BINNAMES As Integer = 12
'    Public Const DC_ENUMRESOLUTIONS As Integer = 13
'    Public Const DC_FILEDEPENDENCIES As Integer = 14
'    Public Const DC_TRUETYPE As Integer = 15
'    Public Const DC_PAPERNAMES As Integer = 16
'    Public Const DC_ORIENTATION As Integer = 17
'    Public Const DC_COPIES As Integer = 18
'    Public Const DV_E_FORMATETC As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        DC_COPIES = 18,
'    '        DV_E_FORMATETC = unchecked((int)0x80040064),
'    '--------------------------^--- GenCode(token): unexpected token type
'    Public Const DV_E_DVTARGETDEVICE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        DV_E_FORMATETC = unchecked((int)0x80040064),
'    '        DV_E_DVTARGETDEVICE = unchecked((int)0x80040065),
'    '-------------------------------^--- GenCode(token): unexpected token type
'    Public Const DV_E_STGMEDIUM As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        DV_E_DVTARGETDEVICE = unchecked((int)0x80040065),
'    '        DV_E_STGMEDIUM = unchecked((int)0x80040066),
'    '--------------------------^--- GenCode(token): unexpected token type
'    Public Const DV_E_STATDATA As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        DV_E_STGMEDIUM = unchecked((int)0x80040066),
'    '        DV_E_STATDATA = unchecked((int)0x80040067),
'    '-------------------------^--- GenCode(token): unexpected token type
'    Public Const DV_E_LINDEX As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        DV_E_STATDATA = unchecked((int)0x80040067),
'    '        DV_E_LINDEX = unchecked((int)0x80040068),
'    '-----------------------^--- GenCode(token): unexpected token type
'    Public Const DV_E_TYMED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        DV_E_LINDEX = unchecked((int)0x80040068),
'    '        DV_E_TYMED = unchecked((int)0x80040069),
'    '----------------------^--- GenCode(token): unexpected token type
'    Public Const DV_E_CLIPFORMAT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        DV_E_TYMED = unchecked((int)0x80040069),
'    '        DV_E_CLIPFORMAT = unchecked((int)0x8004006A),
'    '---------------------------^--- GenCode(token): unexpected token type
'    Public Const DV_E_DVASPECT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        DV_E_CLIPFORMAT = unchecked((int)0x8004006A),
'    '        DV_E_DVASPECT = unchecked((int)0x8004006B),
'    '-------------------------^--- GenCode(token): unexpected token type
'    Public Const DV_E_DVTARGETDEVICE_SIZE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        DV_E_DVASPECT = unchecked((int)0x8004006B),
'    '        DV_E_DVTARGETDEVICE_SIZE = unchecked((int)0x8004006C),
'    '------------------------------------^--- GenCode(token): unexpected token type
'    Public Const DV_E_NOIVIEWOBJECT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        DV_E_DVTARGETDEVICE_SIZE = unchecked((int)0x8004006C),
'    '        DV_E_NOIVIEWOBJECT = unchecked((int)0x8004006D),
'    '------------------------------^--- GenCode(token): unexpected token type
'    Public Const DRAGDROP_E_FIRST As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        DV_E_NOIVIEWOBJECT = unchecked((int)0x8004006D),
'    '        DRAGDROP_E_FIRST = unchecked((int)0x80040100),
'    '----------------------------^--- GenCode(token): unexpected token type
'    Public Const DRAGDROP_E_LAST As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        DRAGDROP_E_FIRST = unchecked((int)0x80040100),
'    '        DRAGDROP_E_LAST = unchecked((int)0x8004010F),
'    '---------------------------^--- GenCode(token): unexpected token type
'    Public Const DRAGDROP_S_FIRST As Integer = &H40100
'    Public Const DRAGDROP_S_LAST As Integer = &H4010F
'    Public Const DRAGDROP_E_NOTREGISTERED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        DRAGDROP_S_LAST = 0x0004010F,
'    '        DRAGDROP_E_NOTREGISTERED = unchecked((int)0x80040100),
'    '------------------------------------^--- GenCode(token): unexpected token type
'    Public Const DRAGDROP_E_ALREADYREGISTERED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        DRAGDROP_E_NOTREGISTERED = unchecked((int)0x80040100),
'    '        DRAGDROP_E_ALREADYREGISTERED = unchecked((int)0x80040101),
'    '----------------------------------------^--- GenCode(token): unexpected token type
'    Public Const DRAGDROP_E_INVALIDHWND As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        DRAGDROP_E_ALREADYREGISTERED = unchecked((int)0x80040101),
'    '        DRAGDROP_E_INVALIDHWND = unchecked((int)0x80040102),
'    '----------------------------------^--- GenCode(token): unexpected token type
'    Public Const DATA_E_FIRST As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        DRAGDROP_E_INVALIDHWND = unchecked((int)0x80040102),
'    '        DATA_E_FIRST = unchecked((int)0x80040130),
'    '------------------------^--- GenCode(token): unexpected token type
'    Public Const DATA_E_LAST As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        DATA_E_FIRST = unchecked((int)0x80040130),
'    '        DATA_E_LAST = unchecked((int)0x8004013F),
'    '-----------------------^--- GenCode(token): unexpected token type
'    Public Const DATA_S_FIRST As Integer = &H40130
'    Public Const DATA_S_LAST As Integer = &H4013F
'    Public Const DRAGDROP_S_DROP As Integer = &H40100
'    Public Const DRAGDROP_S_CANCEL As Integer = &H40101
'    Public Const DRAGDROP_S_USEDEFAULTCURSORS As Integer = &H40102
'    Public Const DATA_S_SAMEFORMATETC As Integer = &H40130
        '    Public Const DISP_E_UNKNOWNInterfaceAs Integer = 
        '    '
        '    'Note:  Error processing original source shown below
        '    '        DATA_S_SAMEFORMATETC = 0x00040130,
        '    '        DISP_E_UNKNOWNINTERFACE = unchecked((int)0x80020001),
        '    '-----------------------------------^--- GenCode(token): unexpected token type
        Public Const DISP_E_MEMBERNOTFOUND As Integer = &H80020003
        '    '
        '    'Note:  Error processing original source shown below
        '    '        DISP_E_UNKNOWNINTERFACE = unchecked((int)0x80020001),
        '    '        DISP_E_MEMBERNOTFOUND = unchecked((int)0x80020003),
        '    '---------------------------------^--- GenCode(token): unexpected token type
        '    Public Const DISP_E_PARAMNOTFOUND As Integer = 
        '    '
        '    'Note:  Error processing original source shown below
        '    '        DISP_E_MEMBERNOTFOUND = unchecked((int)0x80020003),
        '    '        DISP_E_PARAMNOTFOUND = unchecked((int)0x80020004),
        '    '--------------------------------^--- GenCode(token): unexpected token type
        '    Public Const DISP_E_TYPEMISMATCH As Integer = 
        '    '
        '    'Note:  Error processing original source shown below
        '    '        DISP_E_PARAMNOTFOUND = unchecked((int)0x80020004),
        '    '        DISP_E_TYPEMISMATCH = unchecked((int)0x80020005),
        '    '-------------------------------^--- GenCode(token): unexpected token type
        '    Public Const DISP_E_UNKNOWNNAME As Integer = 
        '    '
        '    'Note:  Error processing original source shown below
        '    '        DISP_E_TYPEMISMATCH = unchecked((int)0x80020005),
        '    '        DISP_E_UNKNOWNNAME = unchecked((int)0x80020006),
        '    '------------------------------^--- GenCode(token): unexpected token type
        '    Public Const DISP_E_NONAMEDARGS As Integer = 
        '    '
        '    'Note:  Error processing original source shown below
        '    '        DISP_E_UNKNOWNNAME = unchecked((int)0x80020006),
        '    '        DISP_E_NONAMEDARGS = unchecked((int)0x80020007),
        '    '------------------------------^--- GenCode(token): unexpected token type
        '    Public Const DISP_E_BADVARTYPE As Integer = 
        '    '
        '    'Note:  Error processing original source shown below
        '    '        DISP_E_NONAMEDARGS = unchecked((int)0x80020007),
        '    '        DISP_E_BADVARTYPE = unchecked((int)0x80020008),
        '    '-----------------------------^--- GenCode(token): unexpected token type
        '    Public Const DISP_E_EXCEPTION As Integer = 
        '    '
        '    'Note:  Error processing original source shown below
        '    '        DISP_E_BADVARTYPE = unchecked((int)0x80020008),
        '    '        DISP_E_EXCEPTION = unchecked((int)0x80020009),
        '    '----------------------------^--- GenCode(token): unexpected token type
        '    Public Const DISP_E_OVERFLOW As Integer = 
        '    '
        '    'Note:  Error processing original source shown below
        '    '        DISP_E_EXCEPTION = unchecked((int)0x80020009),
        '    '        DISP_E_OVERFLOW = unchecked((int)0x8002000A),
        '    '---------------------------^--- GenCode(token): unexpected token type
        '    Public Const DISP_E_BADINDEX As Integer = 
        '    '
        '    'Note:  Error processing original source shown below
        '    '        DISP_E_OVERFLOW = unchecked((int)0x8002000A),
        '    '        DISP_E_BADINDEX = unchecked((int)0x8002000B),
        '    '---------------------------^--- GenCode(token): unexpected token type
        '    Public Const DISP_E_UNKNOWNLCID As Integer = 
        '    '
        '    'Note:  Error processing original source shown below
        '    '        DISP_E_BADINDEX = unchecked((int)0x8002000B),
        '    '        DISP_E_UNKNOWNLCID = unchecked((int)0x8002000C),
        '    '------------------------------^--- GenCode(token): unexpected token type
        '    Public Const DISP_E_ARRAYISLOCKED As Integer = 
        '    '
        '    'Note:  Error processing original source shown below
        '    '        DISP_E_UNKNOWNLCID = unchecked((int)0x8002000C),
        '    '        DISP_E_ARRAYISLOCKED = unchecked((int)0x8002000D),
        '    '--------------------------------^--- GenCode(token): unexpected token type
        '    Public Const DISP_E_BADPARAMCOUNT As Integer = 
        '    '
        '    'Note:  Error processing original source shown below
        '    '        DISP_E_ARRAYISLOCKED = unchecked((int)0x8002000D),
        '    '        DISP_E_BADPARAMCOUNT = unchecked((int)0x8002000E),
        '    '--------------------------------^--- GenCode(token): unexpected token type
        '    Public Const DISP_E_PARAMNOTOPTIONAL As Integer = 
        '    '
        '    'Note:  Error processing original source shown below
        '    '        DISP_E_BADPARAMCOUNT = unchecked((int)0x8002000E),
        '    '        DISP_E_PARAMNOTOPTIONAL = unchecked((int)0x8002000F),
        '    '-----------------------------------^--- GenCode(token): unexpected token type
        '    Public Const DISP_E_BADCALLEE As Integer = 
        '    '
        '    'Note:  Error processing original source shown below
        '    '        DISP_E_PARAMNOTOPTIONAL = unchecked((int)0x8002000F),
        '    '        DISP_E_BADCALLEE = unchecked((int)0x80020010),
        '    '----------------------------^--- GenCode(token): unexpected token type
        '    Public Const DISP_E_NOTACOLLECTION As Integer = 
        '    '
        '    'Note:  Error processing original source shown below
        '    '        DISP_E_BADCALLEE = unchecked((int)0x80020010),
        '    '        DISP_E_NOTACOLLECTION = unchecked((int)0x80020011),
        '    '---------------------------------^--- GenCode(token): unexpected token type
        '    Public Const DIGSIG_E_ENCODE As Integer = 
        '    '
        '    'Note:  Error processing original source shown below
        '    '        DISP_E_NOTACOLLECTION = unchecked((int)0x80020011),
        '    '        DIGSIG_E_ENCODE = unchecked((int)0x800B0005),
        '    '---------------------------^--- GenCode(token): unexpected token type
        '    Public Const DIGSIG_E_DECODE As Integer = 
        '    '
        '    'Note:  Error processing original source shown below
        '    '        DIGSIG_E_ENCODE = unchecked((int)0x800B0005),
        '    '        DIGSIG_E_DECODE = unchecked((int)0x800B0006),
        '    '---------------------------^--- GenCode(token): unexpected token type
        '    Public Const DIGSIG_E_EXTENSIBILITY As Integer = 
        '    '
        '    'Note:  Error processing original source shown below
        '    '        DIGSIG_E_DECODE = unchecked((int)0x800B0006),
        '    '        DIGSIG_E_EXTENSIBILITY = unchecked((int)0x800B0007),
        '    '----------------------------------^--- GenCode(token): unexpected token type
        '    Public Const DIGSIG_E_CRYPTO As Integer = 
        '    '
        '    'Note:  Error processing original source shown below
        '    '        DIGSIG_E_EXTENSIBILITY = unchecked((int)0x800B0007),
        '    '        DIGSIG_E_CRYPTO = unchecked((int)0x800B0008),
        '    '---------------------------^--- GenCode(token): unexpected token type
        '    Public Const DCB_RESET As Integer = &H1
        '    Public Const DCB_ACCUMULATE As Integer = &H2
        '    Public Const DCB_DIRTY As Integer = &H2
        '    Public Const DCB_SET As Integer = &H1 Or &H2
        '    Public Const DCB_ENABLE As Integer = &H4
        '    Public Const DCB_DISABLE As Integer = &H8
        '    Public Const DRAFTMODE As Integer = 7
        '    Public Const DEVICEDATA As Integer = 19
        '    Public Const DRAWPATTERNRECT As Integer = 25
        '    Public Const DOWNLOADFACE As Integer = 514
        '    Public Const DOWNLOADHEADER As Integer = 4111
        '    Public Const DEFAULT_QUALITY As Integer = 0
        '    Public Const DRAFT_QUALITY As Integer = 1
        '    Public Const DEFAULT_PITCH As Integer = 0
        '    Public Const DEFAULT_CHARSET As Integer = 1
        '    Public Const DEVICE_FONTTYPE As Integer = &H2
        '    Public Const DKGRAY_BRUSH As Integer = 3
        '    Public Const DEVICE_DEFAULT_FONT As Integer = 14
        '    Public Const DEFAULT_PALETTE As Integer = 15
        '    Public Const DEFAULT_GUI_FONT As Integer = 17
        '    Public Const DRIVERVERSION As Integer = 0
        '    Public Const DESKTOPVERTRES As Integer = 117
        '    Public Const DESKTOPHORZRES As Integer = 118
        '    Public Const DT_PLOTTER As Integer = 0
        '    Public Const DT_RASDISPLAY As Integer = 1
        '    Public Const DT_RASPRINTER As Integer = 2
        '    Public Const DT_RASCAMERA As Integer = 3
        '    Public Const DT_CHARSTREAM As Integer = 4
        '    Public Const DT_METAFILE As Integer = 5
        '    Public Const DT_DISPFILE As Integer = 6
        '    Public Const DIB_RGB_COLORS As Integer = 0
        '    Public Const DIB_PAL_COLORS As Integer = 1
        '    Public Const DM_SPECVERSION As Integer = &H401
        '    Public Const DM_ORIENTATION As Integer = &H1
        '    Public Const DM_PAPERSIZE As Integer = &H2
        '    Public Const DM_PAPERLENGTH As Integer = &H4
        '    Public Const DM_PAPERWIDTH As Integer = &H8
        '    Public Const DM_SCALE As Integer = &H10
        '    Public Const DM_COPIES As Integer = &H100
        '    Public Const DM_DEFAULTSOURCE As Integer = &H200
        '    Public Const DM_PRINTQUALITY As Integer = &H400
        '    Public Const DM_COLOR As Integer = &H800
        '    Public Const DM_DUPLEX As Integer = &H1000
        '    Public Const DM_YRESOLUTION As Integer = &H2000
        '    Public Const DM_TTOPTION As Integer = &H4000
        '    Public Const DM_COLLATE As Integer = &H8000
        '    Public Const DM_FORMNAME As Integer = &H10000
        '    Public Const DM_LOGPIXELS As Integer = &H20000
        '    Public Const DM_BITSPERPEL As Integer = &H40000
        '    Public Const DM_PELSWIDTH As Integer = &H80000
        '    Public Const DM_PELSHEIGHT As Integer = &H100000
        '    Public Const DM_DISPLAYFLAGS As Integer = &H200000
        '    Public Const DM_DISPLAYFREQUENCY As Integer = &H400000
        '    Public Const DM_PANNINGWIDTH As Integer = &H800000
        '    Public Const DM_PANNINGHEIGHT As Integer = &H1000000
        '    Public Const DM_ICMMETHOD As Integer = &H2000000
        '    Public Const DM_ICMINTENT As Integer = &H4000000
        '    Public Const DM_MEDIATYPE As Integer = &H8000000
        '    Public Const DM_DITHERTYPE As Integer = &H10000000
        '    Public Const DM_ICCMANUFACTURER As Integer = &H20000000
        '    Public Const DM_ICCMODEL As Integer = &H40000000
        '    Public Const DMORIENT_PORTRAIT As Integer = 1
        '    Public Const DMORIENT_LANDSCAPE As Integer = 2
        '    Public Const DMPAPER_LETTER As Integer = 1
        '    Public Const DMPAPER_LETTERSMALL As Integer = 2
        '    Public Const DMPAPER_TABLOID As Integer = 3
        '    Public Const DMPAPER_LEDGER As Integer = 4
        '    Public Const DMPAPER_LEGAL As Integer = 5
        '    Public Const DMPAPER_STATEMENT As Integer = 6
        '    Public Const DMPAPER_EXECUTIVE As Integer = 7
        '    Public Const DMPAPER_A3 As Integer = 8
        '    Public Const DMPAPER_A4 As Integer = 9
        '    Public Const DMPAPER_A4SMALL As Integer = 10
        '    Public Const DMPAPER_A5 As Integer = 11
        '    Public Const DMPAPER_B4 As Integer = 12
        '    Public Const DMPAPER_B5 As Integer = 13
        '    Public Const DMPAPER_FOLIO As Integer = 14
        '    Public Const DMPAPER_QUARTO As Integer = 15
        '    Public Const DMPAPER_10X14 As Integer = 16
        '    Public Const DMPAPER_11X17 As Integer = 17
        '    Public Const DMPAPER_NOTE As Integer = 18
        '    Public Const DMPAPER_ENV_9 As Integer = 19
        '    Public Const DMPAPER_ENV_10 As Integer = 20
        '    Public Const DMPAPER_ENV_11 As Integer = 21
        '    Public Const DMPAPER_ENV_12 As Integer = 22
        '    Public Const DMPAPER_ENV_14 As Integer = 23
        '    Public Const DMPAPER_CSHEET As Integer = 24
        '    Public Const DMPAPER_DSHEET As Integer = 25
        '    Public Const DMPAPER_ESHEET As Integer = 26
        '    Public Const DMPAPER_ENV_DL As Integer = 27
        '    Public Const DMPAPER_ENV_C5 As Integer = 28
        '    Public Const DMPAPER_ENV_C3 As Integer = 29
        '    Public Const DMPAPER_ENV_C4 As Integer = 30
        '    Public Const DMPAPER_ENV_C6 As Integer = 31
        '    Public Const DMPAPER_ENV_C65 As Integer = 32
        '    Public Const DMPAPER_ENV_B4 As Integer = 33
        '    Public Const DMPAPER_ENV_B5 As Integer = 34
        '    Public Const DMPAPER_ENV_B6 As Integer = 35
        '    Public Const DMPAPER_ENV_ITALY As Integer = 36
        '    Public Const DMPAPER_ENV_MONARCH As Integer = 37
        '    Public Const DMPAPER_ENV_PERSONAL As Integer = 38
        '    Public Const DMPAPER_FANFOLD_US As Integer = 39
        '    Public Const DMPAPER_FANFOLD_STD_GERMAN As Integer = 40
        '    Public Const DMPAPER_FANFOLD_LGL_GERMAN As Integer = 41
        '    Public Const DMPAPER_ISO_B4 As Integer = 42
        '    Public Const DMPAPER_JAPANESE_POSTCARD As Integer = 43
        '    Public Const DMPAPER_9X11 As Integer = 44
        '    Public Const DMPAPER_10X11 As Integer = 45
        '    Public Const DMPAPER_15X11 As Integer = 46
        '    Public Const DMPAPER_ENV_INVITE As Integer = 47
        '    Public Const DMPAPER_RESERVED_48 As Integer = 48
        '    Public Const DMPAPER_RESERVED_49 As Integer = 49
        '    Public Const DMPAPER_LETTER_EXTRA As Integer = 50
        '    Public Const DMPAPER_LEGAL_EXTRA As Integer = 51
        '    Public Const DMPAPER_TABLOID_EXTRA As Integer = 52
        '    Public Const DMPAPER_A4_EXTRA As Integer = 53
        '    Public Const DMPAPER_LETTER_TRANSVERSE As Integer = 54
        '    Public Const DMPAPER_A4_TRANSVERSE As Integer = 55
        '    Public Const DMPAPER_LETTER_EXTRA_TRANSVERSE As Integer = 56
        '    Public Const DMPAPER_A_PLUS As Integer = 57
        '    Public Const DMPAPER_B_PLUS As Integer = 58
        '    Public Const DMPAPER_LETTER_PLUS As Integer = 59
        '    Public Const DMPAPER_A4_PLUS As Integer = 60
        '    Public Const DMPAPER_A5_TRANSVERSE As Integer = 61
        '    Public Const DMPAPER_B5_TRANSVERSE As Integer = 62
        '    Public Const DMPAPER_A3_EXTRA As Integer = 63
        '    Public Const DMPAPER_A5_EXTRA As Integer = 64
        '    Public Const DMPAPER_B5_EXTRA As Integer = 65
        '    Public Const DMPAPER_A2 As Integer = 66
        '    Public Const DMPAPER_A3_TRANSVERSE As Integer = 67
        '    Public Const DMPAPER_A3_EXTRA_TRANSVERSE As Integer = 68
        '    Public Const DMPAPER_DBL_JAPANESE_POSTCARD As Integer = 69
        '    Public Const DMPAPER_A6 As Integer = 70
        '    Public Const DMPAPER_JENV_KAKU2 As Integer = 71
        '    Public Const DMPAPER_JENV_KAKU3 As Integer = 72
        '    Public Const DMPAPER_JENV_CHOU3 As Integer = 73
        '    Public Const DMPAPER_JENV_CHOU4 As Integer = 74
        '    Public Const DMPAPER_LETTER_ROTATED As Integer = 75
        '    Public Const DMPAPER_A3_ROTATED As Integer = 76
        '    Public Const DMPAPER_A4_ROTATED As Integer = 77
        '    Public Const DMPAPER_A5_ROTATED As Integer = 78
        '    Public Const DMPAPER_B4_JIS_ROTATED As Integer = 79
        '    Public Const DMPAPER_B5_JIS_ROTATED As Integer = 80
        '    Public Const DMPAPER_JAPANESE_POSTCARD_ROTATED As Integer = 81
        '    Public Const DMPAPER_DBL_JAPANESE_POSTCARD_ROTATED As Integer = 82
        '    Public Const DMPAPER_A6_ROTATED As Integer = 83
        '    Public Const DMPAPER_JENV_KAKU2_ROTATED As Integer = 84
        '    Public Const DMPAPER_JENV_KAKU3_ROTATED As Integer = 85
        '    Public Const DMPAPER_JENV_CHOU3_ROTATED As Integer = 86
        '    Public Const DMPAPER_JENV_CHOU4_ROTATED As Integer = 87
        '    Public Const DMPAPER_B6_JIS As Integer = 88
        '    Public Const DMPAPER_B6_JIS_ROTATED As Integer = 89
        '    Public Const DMPAPER_12X11 As Integer = 90
        '    Public Const DMPAPER_JENV_YOU4 As Integer = 91
        '    Public Const DMPAPER_JENV_YOU4_ROTATED As Integer = 92
        '    Public Const DMPAPER_P16K As Integer = 93
        '    Public Const DMPAPER_P32K As Integer = 94
        '    Public Const DMPAPER_P32KBIG As Integer = 95
        '    Public Const DMPAPER_PENV_1 As Integer = 96
        '    Public Const DMPAPER_PENV_2 As Integer = 97
        '    Public Const DMPAPER_PENV_3 As Integer = 98
        '    Public Const DMPAPER_PENV_4 As Integer = 99
        '    Public Const DMPAPER_PENV_5 As Integer = 100
        '    Public Const DMPAPER_PENV_6 As Integer = 101
        '    Public Const DMPAPER_PENV_7 As Integer = 102
        '    Public Const DMPAPER_PENV_8 As Integer = 103
        '    Public Const DMPAPER_PENV_9 As Integer = 104
        '    Public Const DMPAPER_PENV_10 As Integer = 105
        '    Public Const DMPAPER_P16K_ROTATED As Integer = 106
        '    Public Const DMPAPER_P32K_ROTATED As Integer = 107
        '    Public Const DMPAPER_P32KBIG_ROTATED As Integer = 108
        '    Public Const DMPAPER_PENV_1_ROTATED As Integer = 109
        '    Public Const DMPAPER_PENV_2_ROTATED As Integer = 110
        '    Public Const DMPAPER_PENV_3_ROTATED As Integer = 111
        '    Public Const DMPAPER_PENV_4_ROTATED As Integer = 112
        '    Public Const DMPAPER_PENV_5_ROTATED As Integer = 113
        '    Public Const DMPAPER_PENV_6_ROTATED As Integer = 114
        '    Public Const DMPAPER_PENV_7_ROTATED As Integer = 115
        '    Public Const DMPAPER_PENV_8_ROTATED As Integer = 116
        '    Public Const DMPAPER_PENV_9_ROTATED As Integer = 117
        '    Public Const DMPAPER_PENV_10_ROTATED As Integer = 118
        '    Public Const DMPAPER_LAST As Integer = DMPAPER_PENV_10_ROTATED
        '    Public Const DMPAPER_USER As Integer = 256
        '    Public Const DMBIN_UPPER As Integer = 1
        '    Public Const DMBIN_ONLYONE As Integer = 1
        '    Public Const DMBIN_LOWER As Integer = 2
        '    Public Const DMBIN_MIDDLE As Integer = 3
        '    Public Const DMBIN_MANUAL As Integer = 4
        '    Public Const DMBIN_ENVELOPE As Integer = 5
        '    Public Const DMBIN_ENVMANUAL As Integer = 6
        '    Public Const DMBIN_AUTO As Integer = 7
        '    Public Const DMBIN_TRACTOR As Integer = 8
        '    Public Const DMBIN_SMALLFMT As Integer = 9
        '    Public Const DMBIN_LARGEFMT As Integer = 10
        '    Public Const DMBIN_LARGECAPACITY As Integer = 11
        '    Public Const DMBIN_CASSETTE As Integer = 14
        '    Public Const DMBIN_FORMSOURCE As Integer = 15
        '    Public Const DMBIN_LAST As Integer = 15
        '    Public Const DMBIN_USER As Integer = 256
        '    Public Const DMRES_DRAFT As Integer = - 1
        '    Public Const DMRES_LOW As Integer = - 2
        '    Public Const DMRES_MEDIUM As Integer = - 3
        '    Public Const DMRES_HIGH As Integer = - 4
        '    Public Const DMCOLOR_MONOCHROME As Integer = 1
        '    Public Const DMCOLOR_COLOR As Integer = 2
        '    Public Const DMDUP_SIMPLEX As Integer = 1
        '    Public Const DMDUP_VERTICAL As Integer = 2
        '    Public Const DMDUP_HORIZONTAL As Integer = 3
        '    Public Const DMTT_BITMAP As Integer = 1
        '    Public Const DMTT_DOWNLOAD As Integer = 2
        '    Public Const DMTT_SUBDEV As Integer = 3
        '    Public Const DMTT_DOWNLOAD_OUTLINE As Integer = 4
        '    Public Const DMCOLLATE_FALSE As Integer = 0
        '    Public Const DMCOLLATE_TRUE As Integer = 1
        '    Public Const DMDISPLAYFLAGS_TEXTMODE As Integer = &H4
        '    Public Const DMICMMETHOD_NONE As Integer = 1
        '    Public Const DMICMMETHOD_SYSTEM As Integer = 2
        '    Public Const DMICMMETHOD_DRIVER As Integer = 3
        '    Public Const DMICMMETHOD_DEVICE As Integer = 4
        '    Public Const DMICMMETHOD_USER As Integer = 256
        '    Public Const DMICM_SATURATE As Integer = 1
        '    Public Const DMICM_CONTRAST As Integer = 2
        '    Public Const DMICM_COLORMETRIC As Integer = 3
        '    Public Const DMICM_USER As Integer = 256
        '    Public Const DMMEDIA_STANDARD As Integer = 1
        '    Public Const DMMEDIA_TRANSPARENCY As Integer = 2
        '    Public Const DMMEDIA_GLOSSY As Integer = 3
        '    Public Const DMMEDIA_USER As Integer = 256
        '    Public Const DMDITHER_NONE As Integer = 1
        '    Public Const DMDITHER_COARSE As Integer = 2
        '    Public Const DMDITHER_FINE As Integer = 3
        '    Public Const DMDITHER_LINEART As Integer = 4
        '    Public Const DMDITHER_GRAYSCALE As Integer = 5
        '    Public Const DMDITHER_USER As Integer = 256
        '    Public Const DC_BINADJUST As Integer = 19
        '    Public Const DC_EMF_COMPLIANT As Integer = 20
        '    Public Const DC_DATATYPE_PRODUCED As Integer = 21
        '    Public Const DC_COLLATE As Integer = 22
        '    Public Const DCTT_BITMAP As Integer = &H1
        '    Public Const DCTT_DOWNLOAD As Integer = &H2
        '    Public Const DCTT_SUBDEV As Integer = &H4
        '    Public Const DCTT_DOWNLOAD_OUTLINE As Integer = &H8
        '    Public Const DCBA_FACEUPNONE As Integer = &H0
        '    Public Const DCBA_FACEUPCENTER As Integer = &H1
        '    Public Const DCBA_FACEUPLEFT As Integer = &H2
        '    Public Const DCBA_FACEUPRIGHT As Integer = &H3
        '    Public Const DCBA_FACEDOWNNONE As Integer = &H100
        '    Public Const DCBA_FACEDOWNCENTER As Integer = &H101
        '    Public Const DCBA_FACEDOWNLEFT As Integer = &H102
        '    Public Const DCBA_FACEDOWNRIGHT As Integer = &H103
        '    Public Const DI_APPBANDING As Integer = &H1
        '    Public Const DISC_UPDATE_PROFILE As Integer = &H1
        '    Public Const DISC_NO_FORCE As Integer = &H40
        '    Public Const DATE_SHORTDATE As Integer = &H1
        '    Public Const DATE_LONGDATE As Integer = &H2
        '    Public Const DATE_USE_ALT_CALENDAR As Integer = &H4
        '    Public Const DUPLICATE_CLOSE_SOURCE As Integer = &H1
        '    Public Const DUPLICATE_SAME_ACCESS As Integer = &H2
        '    Public Const DELETE As Integer = &H10000
        '    Public Const DOMAIN_USER_RID_ADMIN As Integer = &H1F4
        '    Public Const DOMAIN_USER_RID_GUEST As Integer = &H1F5
        '    Public Const DOMAIN_GROUP_RID_ADMINS As Integer = &H200
        '    Public Const DOMAIN_GROUP_RID_USERS As Integer = &H201
        '    Public Const DOMAIN_GROUP_RID_GUESTS As Integer = &H202
        '    Public Const DOMAIN_ALIAS_RID_ADMINS As Integer = &H220
        '    Public Const DOMAIN_ALIAS_RID_USERS As Integer = &H221
        '    Public Const DOMAIN_ALIAS_RID_GUESTS As Integer = &H222
        '    Public Const DOMAIN_ALIAS_RID_POWER_USERS As Integer = &H223
        '    Public Const DOMAIN_ALIAS_RID_ACCOUNT_OPS As Integer = &H224
        '    Public Const DOMAIN_ALIAS_RID_SYSTEM_OPS As Integer = &H225
        '    Public Const DOMAIN_ALIAS_RID_PRINT_OPS As Integer = &H226
        '    Public Const DOMAIN_ALIAS_RID_BACKUP_OPS As Integer = &H227
        '    Public Const DOMAIN_ALIAS_RID_REPLICATOR As Integer = &H228
        '    Public Const DACL_SECURITY_INFORMATION As Integer = 0

        '    ' WINVER >= 0x0500
        '    ' Japanese Double Postcard 200 x 148 mm 
        '    ' A6 105 x 148 mm                 
        '    ' Japanese Envelope Kaku #2       
        '    ' Japanese Envelope Kaku #3       
        '    ' Japanese Envelope Chou #3       
        '    ' Japanese Envelope Chou #4       
        '    ' Letter Rotated 11 x 8 1/2 11 in 
        '    ' A3 Rotated 420 x 297 mm         
        '    ' A4 Rotated 297 x 210 mm         
        '    ' A5 Rotated 210 x 148 mm         
        '    ' B4 (JIS) Rotated 364 x 257 mm   
        '    ' B5 (JIS) Rotated 257 x 182 mm   
        '    ' Japanese Postcard Rotated 148 x 100 mm 
        '    ' Double Japanese Postcard Rotated 148 x 200 mm 
        '    ' A6 Rotated 148 x 105 mm         
        '    ' Japanese Envelope Kaku #2 Rotated 
        '    ' Japanese Envelope Kaku #3 Rotated 
        '    ' Japanese Envelope Chou #3 Rotated 
        '    ' Japanese Envelope Chou #4 Rotated 
        '    ' B6 (JIS) 128 x 182 mm           
        '    ' B6 (JIS) Rotated 182 x 128 mm   
        '    ' 12 x 11 in                      
        '    ' Japanese Envelope You #4        
        '    ' Japanese Envelope You #4 Rotated
        '    ' PRC 16K 146 x 215 mm            
        '    ' PRC 32K 97 x 151 mm             
        '    ' PRC 32K(Big) 97 x 151 mm        
        '    ' PRC Envelope #1 102 x 165 mm    
        '    ' PRC Envelope #2 102 x 176 mm    
        '    ' PRC Envelope #3 125 x 176 mm    
        '    ' PRC Envelope #4 110 x 208 mm    
        '    ' PRC Envelope #5 110 x 220 mm    
        '    ' PRC Envelope #6 120 x 230 mm    
        '    ' PRC Envelope #7 160 x 230 mm    
        '    ' PRC Envelope #8 120 x 309 mm    
        '    ' PRC Envelope #9 229 x 324 mm    
        '    ' PRC Envelope #10 324 x 458 mm   
        '    ' PRC 16K Rotated                 
        '    ' PRC 32K Rotated                 
        '    ' PRC 32K(Big) Rotated            
        '    ' PRC Envelope #1 Rotated 165 x 102 mm 
        '    ' PRC Envelope #2 Rotated 176 x 102 mm 
        '    ' PRC Envelope #3 Rotated 176 x 125 mm 
        '    ' PRC Envelope #4 Rotated 208 x 110 mm 
        '    ' PRC Envelope #5 Rotated 220 x 110 mm 
        '    ' PRC Envelope #6 Rotated 230 x 120 mm 
        '    ' PRC Envelope #7 Rotated 230 x 160 mm 
        '    ' PRC Envelope #8 Rotated 309 x 120 mm 
        '    ' PRC Envelope #9 Rotated 324 x 229 mm 
        '    ' PRC Envelope #10 Rotated 458 x 324 mm 


        '    Private __unknown As X00000004
        '    Private __unknown As __unknown
        '    Private DLL_PROCESS_ATTACH As __unknown = 1
        '    Private DLL_THREAD_ATTACH As __unknown = 2
        '    Private DLL_THREAD_DETACH As __unknown = 3
        '    Private DLL_PROCESS_DETACH As __unknown = 0
        '    Private DBG_CONTINUE As __unknown = &H10002
        '    Private DBG_TERMINATE_THREAD As __unknown = &H40010003
        '    Private DBG_TERMINATE_PROCESS As __unknown = &H40010004
        '    Private DBG_CONTROL_C As __unknown = &H40010005
        '    Private DBG_CONTROL_BREAK As __unknown = &H40010008
        '    Private DBG_EXCEPTION_NOT_HANDLED As __unknown = 
        '    '
        '    'Note:  Error processing original source shown below
        '    '                                                                                      DBG_CONTROL_BREAK = (0x40010008),
        '    '                                                                                                          DBG_EXCEPTION_NOT_HANDLED = (unchecked((int)0x80010001)),
        '    '----------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
        '    Private DEF_PRIORITY As __unknown = 1
        '    Private DI_CHANNEL As __unknown = 1
        '    Private DI_READ_SPOOL_JOB As __unknown = 3
        '    Private DIFFERENCE As __unknown = 11
        '    Private DESKTOP_READOBJECTS As __unknown = &H1
        '    Private DESKTOP_CREATEWINDOW As __unknown = &H2
        '    Private DESKTOP_CREATEMENU As __unknown = &H4
        '    Private DESKTOP_HOOKCONTROL As __unknown = &H8
        '    Private DESKTOP_JOURNALRECORD As __unknown = &H10
        '    Private DESKTOP_JOURNALPLAYBACK As __unknown = &H20
        '    Private DESKTOP_ENUMERATE As __unknown = &H40
        '    Private DESKTOP_WRITEOBJECTS As __unknown = &H80
        '    Private DESKTOP_SWITCHDESKTOP As __unknown = &H100
        '    Private DF_ALLOWOTHERACCOUNTHOOK As __unknown = &H1
        '    Private DFC_CAPTION As __unknown = 1
        '    Private DFC_MENU As __unknown = 2
        '    Private DFC_SCROLL As __unknown = 3
        '    Private DFC_BUTTON As __unknown = 4
        '    Private DFCS_CAPTIONCLOSE As __unknown = &H0
        '    Private DFCS_CAPTIONMIN As __unknown = &H1
        '    Private DFCS_CAPTIONMAX As __unknown = &H2
        '    Private DFCS_CAPTIONRESTORE As __unknown = &H3
        '    Private DFCS_CAPTIONHELP As __unknown = &H4
        '    Private DFCS_MENUARROW As __unknown = &H0
        '    Private DFCS_MENUCHECK As __unknown = &H1
        '    Private DFCS_MENUBULLET As __unknown = &H2
        '    Private DFCS_MENUARROWRIGHT As __unknown = &H4
        '    Private DFCS_SCROLLUP As __unknown = &H0
        '    Private DFCS_SCROLLDOWN As __unknown = &H1
        '    Private DFCS_SCROLLLEFT As __unknown = &H2
        '    Private DFCS_SCROLLRIGHT As __unknown = &H3
        '    Private DFCS_SCROLLCOMBOBOX As __unknown = &H5
        '    Private DFCS_SCROLLSIZEGRIP As __unknown = &H8
        '    Private DFCS_SCROLLSIZEGRIPRIGHT As __unknown = &H10
        '    Private DFCS_BUTTONCHECK As __unknown = &H0
        '    Private DFCS_BUTTONRADIOIMAGE As __unknown = &H1
        '    Private DFCS_BUTTONRADIOMASK As __unknown = &H2
        '    Private DFCS_BUTTONRADIO As __unknown = &H4
        '    Private DFCS_BUTTON3STATE As __unknown = &H8
        '    Private DFCS_BUTTONPUSH As __unknown = &H10
        '    Private DFCS_INACTIVE As __unknown = &H100
        '    Private DFCS_PUSHED As __unknown = &H200
        '    Private DFCS_CHECKED As __unknown = &H400
        '    Private DFCS_ADJUSTRECT As __unknown = &H2000
        '    Private DFCS_FLAT As __unknown = &H4000
        '    Private DFCS_MONO As __unknown = 
        '    '
        '    'Note:  Error processing original source shown below
        '    '        DFCS_FLAT = 0x4000,
        '    '        DFCS_MONO = unchecked((int)0x8000),
        '    '---------------------^--- GenCode(token): unexpected token type
        '    Private DC_ACTIVE As __unknown = &H1
        '    Private DC_SMALLCAP As __unknown = &H2
        '    Private DC_ICON As __unknown = &H4
        '    Private DC_TEXT As __unknown = &H8
        '    Private DC_INBUTTON As __unknown = &H10
        '    Private DLGWINDOWEXTRA As __unknown = 30
        '    Private DOF_EXECUTABLE As __unknown = 
        '    '
        '    'Note:  Error processing original source shown below
        '    '        DLGWINDOWEXTRA = 30,
        '    '        DOF_EXECUTABLE = unchecked((int)0x8001),
        '    '--------------------------^--- GenCode(token): unexpected token type
        '    Private DOF_DOCUMENT As __unknown = 
        '    '
        '    'Note:  Error processing original source shown below
        '    '        DOF_EXECUTABLE = unchecked((int)0x8001),
        '    '        DOF_DOCUMENT = unchecked((int)0x8002),
        '    '------------------------^--- GenCode(token): unexpected token type
        '    Private DOF_DIRECTORY As __unknown = 
        '    '
        '    'Note:  Error processing original source shown below
        '    '        DOF_DOCUMENT = unchecked((int)0x8002),
        '    '        DOF_DIRECTORY = unchecked((int)0x8003),
        '    '-------------------------^--- GenCode(token): unexpected token type
        '    Private DOF_MULTIPLE As __unknown = 
        '    '
        '    'Note:  Error processing original source shown below
        '    '        DOF_DIRECTORY = unchecked((int)0x8003),
        '    '        DOF_MULTIPLE = unchecked((int)0x8004),
        '    '------------------------^--- GenCode(token): unexpected token type
        '    Private DOF_PROGMAN As __unknown = &H1
        '    Private DOF_SHELLDATA As __unknown = &H2
        '    Private DO_DROPFILE As __unknown = &H454C4946
        '    Private DO_PRINTFILE As __unknown = &H544E5250
        '    Private DT_TOP As __unknown = &H0
        '    Private DT_LEFT As __unknown = &H0
        '    Private DT_CENTER As __unknown = &H1
        '    Private DT_RIGHT As __unknown = &H2
        '    Private DT_VCENTER As __unknown = &H4
        '    Private DT_BOTTOM As __unknown = &H8
        '    Private DT_WORDBREAK As __unknown = &H10
        '    Private DT_SINGLELINE As __unknown = &H20
        '    Private DT_EXPANDTABS As __unknown = &H40
        '    Private DT_TABSTOP As __unknown = &H80
        '    Private DT_NOCLIP As __unknown = &H100
        '    Private DT_CALCRECT As __unknown = &H400
        '    Private DT_NOPREFIX As __unknown = &H800
        '    Private DT_INTERNAL As __unknown = &H1000
        '    Private DT_EDITCONTROL As __unknown = &H2000
        '    Private DT_PATH_ELLIPSIS As __unknown = &H4000
        '    Private DT_END_ELLIPSIS As __unknown = &H8000
        '    Private DT_MODIFYSTRING As __unknown = &H10000
        '    Private DT_RTLREADING As __unknown = &H20000
        '    Private DT_WORD_ELLIPSIS As __unknown = &H40000
        '    Private DST_COMPLEX As __unknown = &H0
        '    Private DST_TEXT As __unknown = &H1
        '    Private DST_PREFIXTEXT As __unknown = &H2
        '    Private DST_ICON As __unknown = &H3
        '    Private DST_BITMAP As __unknown = &H4
        '    Private DSS_NORMAL As __unknown = &H0
        '    Private DSS_UNION As __unknown = &H10
        '    Private DSS_DISABLED As __unknown = &H20
        '    Private DSS_MONO As __unknown = &H80
        '    Private DSS_RIGHT As __unknown = 
        '    '
        '    'Note:  Error processing original source shown below
        '    '        DSS_MONO = 0x0080,
        '    '        DSS_RIGHT = unchecked((int)0x8000),
        '    '---------------------^--- GenCode(token): unexpected token type
        '    Private DCX_WINDOW As __unknown = &H1
        '    Private DCX_CACHE As __unknown = &H2
        '    Private DCX_NORESETATTRS As __unknown = &H4
        '    Private DCX_CLIPCHILDREN As __unknown = &H8
        '    Private DCX_CLIPSIBLINGS As __unknown = &H10
        '    Private DCX_PARENTCLIP As __unknown = &H20
        '    Private DCX_EXCLUDERGN As __unknown = &H40
        '    Private DCX_INTERSECTRGN As __unknown = &H80
        '    Private DCX_EXCLUDEUPDATE As __unknown = &H100
        '    Private DCX_INTERSECTUPDATE As __unknown = &H200
        '    Private DCX_LOCKWINDOWUPDATE As __unknown = &H400
        '    Private DCX_VALIDATE As __unknown = &H200000
        '    Private DI_MASK As __unknown = &H1
        '    Private DI_IMAGE As __unknown = &H2
        '    Private DI_NORMAL As __unknown = &H3
        '    Private DI_COMPAT As __unknown = &H4
        '    Private DI_DEFAULTSIZE As __unknown = &H8
        '    Private DWL_MSGRESULT As __unknown = 0
        '    Private DWL_DLGPROC As __unknown = 4
        '    Private DWL_USER As __unknown = 8
        '    Private DDL_READWRITE As __unknown = &H0
        '    Private DDL_READONLY As __unknown = &H1
        '    Private DDL_HIDDEN As __unknown = &H2
        '    Private DDL_SYSTEM As __unknown = &H4
        '    Private DDL_DIRECTORY As __unknown = &H10
        '    Private DDL_ARCHIVE As __unknown = &H20
        '    Private DDL_POSTMSGS As __unknown = &H2000
        '    Private DDL_DRIVES As __unknown = &H4000
        '    Private DDL_EXCLUSIVE As __unknown = 
        '    '
        '    'Note:  Error processing original source shown below
        '    '        DDL_DRIVES = 0x4000,
        '    '        DDL_EXCLUSIVE = unchecked((int)0x8000),
        '    '-------------------------^--- GenCode(token): unexpected token type
        '    Private DS_ABSALIGN As __unknown = &H1
        '    Private DS_SYSMODAL As __unknown = &H2
        '    Private DS_LOCALEDIT As __unknown = &H20
        '    Private DS_SETFONT As __unknown = &H40
        '    Private DS_MODALFRAME As __unknown = 
        '    '
        '    'Note:  Error processing original source shown below
        '    '        DS_SETFONT = 0x40,
        '    '        DS_MODALFRAME = unchecked((int)0x80),
        '    '-------------------------^--- GenCode(token): unexpected token type
        '    Private DS_NOIDLEMSG As __unknown = &H100
        '    Private DS_SETFOREGROUND As __unknown = &H200
        '    Private DS_3DLOOK As __unknown = &H4
        '    Private DS_FIXEDSYS As __unknown = &H8
        '    Private DS_NOFAILCREATE As __unknown = &H10
        '    Private DS_CONTROL As __unknown = &H400
        '    Private DS_CENTER As __unknown = &H800
        '    Private DS_CENTERMOUSE As __unknown = &H1000
        '    Private DS_CONTEXTHELP As __unknown = &H2000
        '    Private DM_GETDEFID As __unknown = &H400 + 0
        '    Private DM_SETDEFID As __unknown = &H400 + 1
        '    Private DM_REPOSITION As __unknown = &H400 + 2
        '    Private DC_HASDEFID As __unknown = &H534B
        '    Private DLGC_WANTARROWS As __unknown = &H1
        Public Const DLGC_WANTTAB As Integer = &H2
        '    Private DLGC_WANTALLKEYS As __unknown = &H4
        '    Private DLGC_WANTMESSAGE As __unknown = &H4
        '    Private DLGC_HASSETSEL As __unknown = &H8
        '    Private DLGC_DEFPUSHBUTTON As __unknown = &H10
        '    Private DLGC_UNDEFPUSHBUTTON As __unknown = &H20
        '    Private DLGC_RADIOBUTTON As __unknown = &H40
        '    Private DLGC_WANTCHARS As __unknown = &H80
        '    Private DLGC_STATIC As __unknown = &H100
        '    Private DLGC_BUTTON As __unknown = &H2000
        '    Private DISP_CHANGE_SUCCESSFUL As __unknown = 0
        '    Private DISP_CHANGE_RESTART As __unknown = 1
        '    Private DISP_CHANGE_FAILED As __unknown = - 1
        '    Private DISP_CHANGE_BADMODE As __unknown = - 2
        '    Private DISP_CHANGE_NOTUPDATED As __unknown = - 3
        '    Private DISP_CHANGE_BADFLAGS As __unknown = - 4
        '    Private DISP_CHANGE_BADPARAM As __unknown = - 5
        '    Private DECIMAL_NEG As __unknown = 
        '    '
        '    'Note:  Error processing original source shown below
        '    '        DISP_CHANGE_BADPARAM = -5,
        '    '        DECIMAL_NEG = (unchecked((int)0x80)),
        '    '------------------------^--- GenCode(token): unexpected token type
        '    Private DTN_FIRST As __unknown = 0 - 760
        '    Private DTN_LAST As __unknown = 0 - 799
        '    Private DL_BEGINDRAG As __unknown = &H400 + 133
        '    Private DL_DRAGGING As __unknown = &H400 + 134
        '    Private DL_DROPPED As __unknown = &H400 + 135
        '    Private DL_CANCELDRAG As __unknown = &H400 + 136
        '    Private DL_CURSORSET As __unknown = 0
        '    Private DL_STOPCURSOR As __unknown = 1
        '    Private DL_COPYCURSOR As __unknown = 2
        '    Private DL_MOVECURSOR As __unknown = 3
        '    Private DTM_FIRST As __unknown = &H1000
        '    Private DTM_GETSYSTEMTIME As __unknown = &H1000 + 1
        '    Private DTM_SETSYSTEMTIME As __unknown = &H1000 + 2
        '    Private DTM_GETRANGE As __unknown = &H1000 + 3
        '    Private DTM_SETRANGE As __unknown = &H1000 + 4
        '    Private DTM_SETFORMATA As __unknown = &H1000 + 5
        '    Private DTM_SETFORMATW As __unknown = &H1000 + 50
        '    Private DTM_SETMCCOLOR As __unknown = &H1000 + 6
        '    Private DTM_GETMCCOLOR As __unknown = &H1000 + 7
        '    Private DTM_GETMONTHCAL As __unknown = &H1000 + 8
        '    Private DTM_SETMCFONT As __unknown = &H1000 + 9
        '    Private DTM_GETMCFONT As __unknown = &H1000 + 10
        '    Private DTS_UPDOWN As __unknown = &H1
        '    Private DTS_SHOWNONE As __unknown = &H2
        '    Private DTS_SHORTDATEFORMAT As __unknown = &H0
        '    Private DTS_LONGDATEFORMAT As __unknown = &H4
        '    Private DTS_TIMEFORMAT As __unknown = &H9
        '    Private DTS_APPCANPARSE As __unknown = &H10
        '    Private DTS_RIGHTALIGN As __unknown = &H20
        '    Private DTN_DATETIMECHANGE As __unknown = 0 - 760 + 1
        '    Private DTN_USERSTRINGA As __unknown = 0 - 760 + 2
        '    Private DTN_USERSTRINGW As __unknown = 0 - 760 + 15
        '    Private DTN_WMKEYDOWNA As __unknown = 0 - 760 + 3
        '    Private DTN_WMKEYDOWNW As __unknown = 0 - 760 + 16
        '    Private DTN_FORMATA As __unknown = 0 - 760 + 4
        '    Private DTN_FORMATW As __unknown = 0 - 760 + 17
        '    Private DTN_FORMATQUERYA As __unknown = 0 - 760 + 5
        '    Private DTN_FORMATQUERYW As __unknown = 0 - 760 + 18
        '    Private DTN_DROPDOWN As __unknown = 0 - 760 + 6
        '    Private DTN_CLOSEUP As __unknown = 0 - 760 + 7
        '    Private DATA_E_FORMATETC As __unknown = 
        '    '
        '    'Note:  Error processing original source shown below
        '    '                                                                                                                                                                                    DTN_CLOSEUP = ((0-760)+7),
        '    '                                                                                                                                                                                                  DATA_E_FORMATETC = unchecked((int)0x80040064),
        '    '----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
        '    Private DMPAPER_FIRST As __unknown = 1
        '    Private DMBIN_FIRST As __unknown = 1
        '    Private DSTINVERT As __unknown = &H550009
        '    Private __unknown As __unknown

        '    Public Const DVASPECT_CONTENT As Integer = 1
        '    Public Const DVASPECT_THUMBNAIL As Integer = 2
        '    Public Const DVASPECT_ICON As Integer = 4
        '    Public Const DVASPECT_DOCPRINT As Integer = 8
        '    Public Const DVASPECT_OPAQUE As Integer = 16
        '    Public Const DVASPECT_TRANSPARENT As Integer = 32

        '    Public Const DATADIR_GET As Integer = 1
        '    Public Const DATADIR_SET As Integer = 2



        '    Public Const EC_ENABLEALL As Integer = 0
        '    Public Const EC_ENABLEONE As Integer = &H80
        '    Public Const EC_DISABLE As Integer = &H8
        '    Public Const EC_QUERYWAITING As Integer = 2
        '    Public Const edt1 As Integer = &H480
        '    Public Const edt2 As Integer = &H481
        '    Public Const edt3 As Integer = &H482
        '    Public Const edt4 As Integer = &H483
        '    Public Const edt5 As Integer = &H484
        '    Public Const edt6 As Integer = &H485
        '    Public Const edt7 As Integer = &H486
        '    Public Const edt8 As Integer = &H487
        '    Public Const edt9 As Integer = &H488
        '    Public Const edt10 As Integer = &H489
        '    Public Const edt11 As Integer = &H48A
        '    Public Const edt12 As Integer = &H48B
        '    Public Const edt13 As Integer = &H48C
        '    Public Const edt14 As Integer = &H48D
        '    Public Const edt15 As Integer = &H48E
        '    Public Const edt16 As Integer = &H48F
        '    Public Const EXCEPTION_EXECUTE_HANDLER As Integer = 1
        '    Public Const EXCEPTION_CONTINUE_SEARCH As Integer = 0
        '    Public Const EXCEPTION_CONTINUE_EXECUTION As Integer = - 1
        '    Public Const EMBDHLP_INPROC_HANDLER As Integer = &H0
        '    Public Const EMBDHLP_INPROC_SERVER As Integer = &H1
        '    Public Const EMBDHLP_CREATENOW As Integer = &H0
        '    Public Const EMBDHLP_DELAYCREATE As Integer = &H10000
        '    Public Const EXCEPTION_DEBUG_EVENT As Integer = 1
        '    Public Const EXIT_THREAD_DEBUG_EVENT As Integer = 4
        '    Public Const EXIT_PROCESS_DEBUG_EVENT As Integer = 5
        '    Public Const EVENPARITY As Integer = 2
        '    Public Const EV_RXCHAR As Integer = &H1
        '    Public Const EV_RXFLAG As Integer = &H2
        '    Public Const EV_TXEMPTY As Integer = &H4
        '    Public Const EV_CTS As Integer = &H8
        '    Public Const EV_DSR As Integer = &H10
        '    Public Const EV_RLSD As Integer = &H20
        '    Public Const EV_BREAK As Integer = &H40
        '    Public Const EV_ERR As Integer = &H80
        '    Public Const EV_RING As Integer = &H100
        '    Public Const EV_PERR As Integer = &H200
        '    Public Const EV_RX80FULL As Integer = &H400
        '    Public Const EV_EVENT1 As Integer = &H800
        '    Public Const EV_EVENT2 As Integer = &H1000
        '    Public Const ENHANCED_KEY As Integer = &H100
        '    Public Const ENABLE_PROCESSED_INPUT As Integer = &H1
        '    Public Const ENABLE_LINE_INPUT As Integer = &H2
        '    Public Const ENABLE_ECHO_INPUT As Integer = &H4
        '    Public Const ENABLE_WINDOW_INPUT As Integer = &H8
        '    Public Const ENABLE_MOUSE_INPUT As Integer = &H10
        '    Public Const ENABLE_PROCESSED_OUTPUT As Integer = &H1
        '    Public Const ENABLE_WRAP_AT_EOL_OUTPUT As Integer = &H2
        '    Public Const ERROR_SUCCESS As Integer = 0
        '    Public Const ERROR_INVALID_FUNCTION As Integer = 1
        '    Public Const ERROR_FILE_NOT_FOUND As Integer = 2
        '    Public Const ERROR_PATH_NOT_FOUND As Integer = 3
        '    Public Const ERROR_TOO_MANY_OPEN_FILES As Integer = 4
        '    Public Const ERROR_ACCESS_DENIED As Integer = 5
        '    Public Const ERROR_INVALID_HANDLE As Integer = 6
        '    Public Const ERROR_ARENA_TRASHED As Integer = 7
        '    Public Const ERROR_NOT_ENOUGH_MEMORY As Integer = 8
        '    Public Const ERROR_INVALID_BLOCK As Integer = 9
        '    Public Const ERROR_BAD_ENVIRONMENT As Integer = 10
        '    Public Const ERROR_BAD_FORMAT As Integer = 11
        '    Public Const ERROR_INVALID_ACCESS As Integer = 12
        '    Public Const ERROR_INVALID_DATA As Integer = 13
        '    Public Const ERROR_OUTOFMEMORY As Integer = 14
        '    Public Const ERROR_INVALID_DRIVE As Integer = 15
        '    Public Const ERROR_CURRENT_DIRECTORY As Integer = 16
        '    Public Const ERROR_NOT_SAME_DEVICE As Integer = 17
        '    Public Const ERROR_NO_MORE_FILES As Integer = 18
        '    Public Const ERROR_WRITE_PROTECT As Integer = 19
        '    Public Const ERROR_BAD_UNIT As Integer = 20
        '    Public Const ERROR_NOT_READY As Integer = 21
        '    Public Const ERROR_BAD_COMMAND As Integer = 22
        '    Public Const ERROR_CRC As Integer = 23
        '    Public Const ERROR_BAD_LENGTH As Integer = 24
        '    Public Const ERROR_SEEK As Integer = 25
        '    Public Const ERROR_NOT_DOS_DISK As Integer = 26
        '    Public Const ERROR_SECTOR_NOT_FOUND As Integer = 27
        '    Public Const ERROR_OUT_OF_PAPER As Integer = 28
        '    Public Const ERROR_WRITE_FAULT As Integer = 29
        '    Public Const ERROR_READ_FAULT As Integer = 30
        '    Public Const ERROR_GEN_FAILURE As Integer = 31
        '    Public Const ERROR_SHARING_VIOLATION As Integer = 32
        '    Public Const ERROR_LOCK_VIOLATION As Integer = 33
        '    Public Const ERROR_WRONG_DISK As Integer = 34
        '    Public Const ERROR_SHARING_BUFFER_EXCEEDED As Integer = 36
        '    Public Const ERROR_HANDLE_EOF As Integer = 38
        '    Public Const ERROR_HANDLE_DISK_FULL As Integer = 39
        '    Public Const ERROR_NOT_SUPPORTED As Integer = 50
        '    Public Const ERROR_REM_NOT_LIST As Integer = 51
        '    Public Const ERROR_DUP_NAME As Integer = 52
        '    Public Const ERROR_BAD_NETPATH As Integer = 53
        '    Public Const ERROR_NETWORK_BUSY As Integer = 54
        '    Public Const ERROR_DEV_NOT_EXIST As Integer = 55
        '    Public Const ERROR_TOO_MANY_CMDS As Integer = 56
        '    Public Const ERROR_ADAP_HDW_ERR As Integer = 57
        '    Public Const ERROR_BAD_NET_RESP As Integer = 58
        '    Public Const ERROR_UNEXP_NET_ERR As Integer = 59
        '    Public Const ERROR_BAD_REM_ADAP As Integer = 60
        '    Public Const ERROR_PRINTQ_FULL As Integer = 61
        '    Public Const ERROR_NO_SPOOL_SPACE As Integer = 62
        '    Public Const ERROR_PRINT_CANCELLED As Integer = 63
        '    Public Const ERROR_NETNAME_DELETED As Integer = 64
        '    Public Const ERROR_NETWORK_ACCESS_DENIED As Integer = 65
        '    Public Const ERROR_BAD_DEV_TYPE As Integer = 66
        '    Public Const ERROR_BAD_NET_NAME As Integer = 67
        '    Public Const ERROR_TOO_MANY_NAMES As Integer = 68
        '    Public Const ERROR_TOO_MANY_SESS As Integer = 69
        '    Public Const ERROR_SHARING_PAUSED As Integer = 70
        '    Public Const ERROR_REQ_NOT_ACCEP As Integer = 71
        '    Public Const ERROR_REDIR_PAUSED As Integer = 72
        '    Public Const ERROR_FILE_EXISTS As Integer = 80
        '    Public Const ERROR_CANNOT_MAKE As Integer = 82
        '    Public Const ERROR_FAIL_I24 As Integer = 83
        '    Public Const ERROR_OUT_OF_STRUCTURES As Integer = 84
        '    Public Const ERROR_ALREADY_ASSIGNED As Integer = 85
        '    Public Const ERROR_INVALID_PASSWORD As Integer = 86
        '    Public Const ERROR_INVALID_PARAMETER As Integer = 87
        '    Public Const ERROR_NET_WRITE_FAULT As Integer = 88
        '    Public Const ERROR_NO_PROC_SLOTS As Integer = 89
        '    Public Const ERROR_TOO_MANY_SEMAPHORES As Integer = 100
        '    Public Const ERROR_EXCL_SEM_ALREADY_OWNED As Integer = 101
        '    Public Const ERROR_SEM_IS_SET As Integer = 102
        '    Public Const ERROR_TOO_MANY_SEM_REQUESTS As Integer = 103
        '    Public Const ERROR_INVALID_AT_INTERRUPT_TIME As Integer = 104
        '    Public Const ERROR_SEM_OWNER_DIED As Integer = 105
        '    Public Const ERROR_SEM_USER_LIMIT As Integer = 106
        '    Public Const ERROR_DISK_CHANGE As Integer = 107
        '    Public Const ERROR_DRIVE_LOCKED As Integer = 108
        '    Public Const ERROR_BROKEN_PIPE As Integer = 109
        '    Public Const ERROR_OPEN_FAILED As Integer = 110
        '    Public Const ERROR_BUFFER_OVERFLOW As Integer = 111
        '    Public Const ERROR_DISK_FULL As Integer = 112
        '    Public Const ERROR_NO_MORE_SEARCH_HANDLES As Integer = 113
        '    Public Const ERROR_INVALID_TARGET_HANDLE As Integer = 114
        '    Public Const ERROR_INVALID_CATEGORY As Integer = 117
        '    Public Const ERROR_INVALID_VERIFY_SWITCH As Integer = 118
        '    Public Const ERROR_BAD_DRIVER_LEVEL As Integer = 119
        '    Public Const ERROR_CALL_NOT_IMPLEMENTED As Integer = 120
        '    Public Const ERROR_SEM_TIMEOUT As Integer = 121
        '    Public Const ERROR_INSUFFICIENT_BUFFER As Integer = 122
        '    Public Const ERROR_INVALID_NAME As Integer = 123
        '    Public Const ERROR_INVALID_LEVEL As Integer = 124
        '    Public Const ERROR_NO_VOLUME_LABEL As Integer = 125
        '    Public Const ERROR_MOD_NOT_FOUND As Integer = 126
        '    Public Const ERROR_PROC_NOT_FOUND As Integer = 127
        '    Public Const ERROR_WAIT_NO_CHILDREN As Integer = 128
        '    Public Const ERROR_CHILD_NOT_COMPLETE As Integer = 129
        '    Public Const ERROR_DIRECT_ACCESS_HANDLE As Integer = 130
        '    Public Const ERROR_NEGATIVE_SEEK As Integer = 131
        '    Public Const ERROR_SEEK_ON_DEVICE As Integer = 132
        '    Public Const ERROR_IS_JOIN_TARGET As Integer = 133
        '    Public Const ERROR_IS_JOINED As Integer = 134
        '    Public Const ERROR_IS_SUBSTED As Integer = 135
        '    Public Const ERROR_NOT_JOINED As Integer = 136
        '    Public Const ERROR_NOT_SUBSTED As Integer = 137
        '    Public Const ERROR_JOIN_TO_JOIN As Integer = 138
        '    Public Const ERROR_SUBST_TO_SUBST As Integer = 139
        '    Public Const ERROR_JOIN_TO_SUBST As Integer = 140
        '    Public Const ERROR_SUBST_TO_JOIN As Integer = 141
        '    Public Const ERROR_BUSY_DRIVE As Integer = 142
        '    Public Const ERROR_SAME_DRIVE As Integer = 143
        '    Public Const ERROR_DIR_NOT_ROOT As Integer = 144
        '    Public Const ERROR_DIR_NOT_EMPTY As Integer = 145
        '    Public Const ERROR_IS_SUBST_PATH As Integer = 146
        '    Public Const ERROR_IS_JOIN_PATH As Integer = 147
        '    Public Const ERROR_PATH_BUSY As Integer = 148
        '    Public Const ERROR_IS_SUBST_TARGET As Integer = 149
        '    Public Const ERROR_SYSTEM_TRACE As Integer = 150
        '    Public Const ERROR_INVALID_EVENT_COUNT As Integer = 151
        '    Public Const ERROR_TOO_MANY_MUXWAITERS As Integer = 152
        '    Public Const ERROR_INVALID_LIST_FORMAT As Integer = 153
        '    Public Const ERROR_LABEL_TOO_LONG As Integer = 154
        '    Public Const ERROR_TOO_MANY_TCBS As Integer = 155
        '    Public Const ERROR_SIGNAL_REFUSED As Integer = 156
        '    Public Const ERROR_DISCARDED As Integer = 157
        '    Public Const ERROR_NOT_LOCKED As Integer = 158
        '    Public Const ERROR_BAD_THREADID_ADDR As Integer = 159
        '    Public Const ERROR_BAD_ARGUMENTS As Integer = 160
        '    Public Const ERROR_BAD_PATHNAME As Integer = 161
        '    Public Const ERROR_SIGNAL_PENDING As Integer = 162
        '    Public Const ERROR_MAX_THRDS_REACHED As Integer = 164
        '    Public Const ERROR_LOCK_FAILED As Integer = 167
        '    Public Const ERROR_BUSY As Integer = 170
        '    Public Const ERROR_CANCEL_VIOLATION As Integer = 173
        '    Public Const ERROR_ATOMIC_LOCKS_NOT_SUPPORTED As Integer = 174
        '    Public Const ERROR_INVALID_SEGMENT_NUMBER As Integer = 180
        '    Public Const ERROR_INVALID_ORDINAL As Integer = 182
        '    Public Const ERROR_ALREADY_EXISTS As Integer = 183
        '    Public Const ERROR_INVALID_FLAG_NUMBER As Integer = 186
        '    Public Const ERROR_SEM_NOT_FOUND As Integer = 187
        '    Public Const ERROR_INVALID_STARTING_CODESEG As Integer = 188
        '    Public Const ERROR_INVALID_STACKSEG As Integer = 189
        '    Public Const ERROR_INVALID_MODULETYPE As Integer = 190
        '    Public Const ERROR_INVALID_EXE_SIGNATURE As Integer = 191
        '    Public Const ERROR_EXE_MARKED_INVALID As Integer = 192
        '    Public Const ERROR_BAD_EXE_FORMAT As Integer = 193
        '    Public Const ERROR_ITERATED_DATA_EXCEEDS_64k As Integer = 194
        '    Public Const ERROR_INVALID_MINALLOCSIZE As Integer = 195
        '    Public Const ERROR_DYNLINK_FROM_INVALID_RING As Integer = 196
        '    Public Const ERROR_IOPL_NOT_ENABLED As Integer = 197
        '    Public Const ERROR_INVALID_SEGDPL As Integer = 198
        '    Public Const ERROR_AUTODATASEG_EXCEEDS_64k As Integer = 199
        '    Public Const ERROR_RING2SEG_MUST_BE_MOVABLE As Integer = 200
        '    Public Const ERROR_RELOC_CHAIN_XEEDS_SEGLIM As Integer = 201
        '    Public Const ERROR_INFLOOP_IN_RELOC_CHAIN As Integer = 202
        '    Public Const ERROR_ENVVAR_NOT_FOUND As Integer = 203
        '    Public Const ERROR_NO_SIGNAL_SENT As Integer = 205
        '    Public Const ERROR_FILENAME_EXCED_RANGE As Integer = 206
        '    Public Const ERROR_RING2_STACK_IN_USE As Integer = 207
        '    Public Const ERROR_META_EXPANSION_TOO_LONG As Integer = 208
        '    Public Const ERROR_INVALID_SIGNAL_NUMBER As Integer = 209
        '    Public Const ERROR_THREAD_1_INACTIVE As Integer = 210
        '    Public Const ERROR_LOCKED As Integer = 212
        '    Public Const ERROR_TOO_MANY_MODULES As Integer = 214
        '    Public Const ERROR_NESTING_NOT_ALLOWED As Integer = 215
        '    Public Const ERROR_EXE_MACHINE_TYPE_MISMATCH As Integer = 216
        '    Public Const ERROR_BAD_PIPE As Integer = 230
        '    Public Const ERROR_PIPE_BUSY As Integer = 231
        '    Public Const ERROR_NO_DATA As Integer = 232
        '    Public Const ERROR_PIPE_NOT_CONNECTED As Integer = 233
        '    Public Const ERROR_MORE_DATA As Integer = 234
        '    Public Const ERROR_VC_DISCONNECTED As Integer = 240
        '    Public Const ERROR_INVALID_EA_NAME As Integer = 254
        '    Public Const ERROR_EA_LIST_INCONSISTENT As Integer = 255
        '    Public Const ERROR_NO_MORE_ITEMS As Integer = 259
        '    Public Const ERROR_CANNOT_COPY As Integer = 266
        '    Public Const ERROR_DIRECTORY As Integer = 267
        '    Public Const ERROR_EAS_DIDNT_FIT As Integer = 275
        '    Public Const ERROR_EA_FILE_CORRUPT As Integer = 276
        '    Public Const ERROR_EA_TABLE_FULL As Integer = 277
        '    Public Const ERROR_INVALID_EA_HANDLE As Integer = 278
        '    Public Const ERROR_EAS_NOT_SUPPORTED As Integer = 282
        '    Public Const ERROR_NOT_OWNER As Integer = 288
        '    Public Const ERROR_TOO_MANY_POSTS As Integer = 298
        '    Public Const ERROR_PARTIAL_COPY As Integer = 299
        '    Public Const ERROR_MR_MID_NOT_FOUND As Integer = 317
        '    Public Const ERROR_INVALID_ADDRESS As Integer = 487
        '    Public Const ERROR_ARITHMETIC_OVERFLOW As Integer = 534
        '    Public Const ERROR_PIPE_CONNECTED As Integer = 535
        '    Public Const ERROR_PIPE_LISTENING As Integer = 536
        '    Public Const ERROR_EA_ACCESS_DENIED As Integer = 994
        '    Public Const ERROR_OPERATION_ABORTED As Integer = 995
        '    Public Const ERROR_IO_INCOMPLETE As Integer = 996
        '    Public Const ERROR_IO_PENDING As Integer = 997
        '    Public Const ERROR_NOACCESS As Integer = 998
        '    Public Const ERROR_SWAPERROR As Integer = 999
        '    Public Const ERROR_STACK_OVERFLOW As Integer = 1001
        '    Public Const ERROR_INVALID_MESSAGE As Integer = 1002
        '    Public Const ERROR_CAN_NOT_COMPLETE As Integer = 1003
        '    Public Const ERROR_INVALID_FLAGS As Integer = 1004
        '    Public Const ERROR_UNRECOGNIZED_VOLUME As Integer = 1005
        '    Public Const ERROR_FILE_INVALID As Integer = 1006
        '    Public Const ERROR_FULLSCREEN_MODE As Integer = 1007
        '    Public Const ERROR_NO_TOKEN As Integer = 1008
        '    Public Const ERROR_BADDB As Integer = 1009
        '    Public Const ERROR_BADKEY As Integer = 1010
        '    Public Const ERROR_CANTOPEN As Integer = 1011
        '    Public Const ERROR_CANTREAD As Integer = 1012
        '    Public Const ERROR_CANTWRITE As Integer = 1013
        '    Public Const ERROR_REGISTRY_RECOVERED As Integer = 1014
        '    Public Const ERROR_REGISTRY_CORRUPT As Integer = 1015
        '    Public Const ERROR_REGISTRY_IO_FAILED As Integer = 1016
        '    Public Const ERROR_NOT_REGISTRY_FILE As Integer = 1017
        '    Public Const ERROR_KEY_DELETED As Integer = 1018
        '    Public Const ERROR_NO_LOG_SPACE As Integer = 1019
        '    Public Const ERROR_KEY_HAS_CHILDREN As Integer = 1020
        '    Public Const ERROR_CHILD_MUST_BE_VOLATILE As Integer = 1021
        '    Public Const ERROR_NOTIFY_ENUM_DIR As Integer = 1022
        '    Public Const ERROR_DEPENDENT_SERVICES_RUNNING As Integer = 1051
        '    Public Const ERROR_INVALID_SERVICE_CONTROL As Integer = 1052
        '    Public Const ERROR_SERVICE_REQUEST_TIMEOUT As Integer = 1053
        '    Public Const ERROR_SERVICE_NO_THREAD As Integer = 1054
        '    Public Const ERROR_SERVICE_DATABASE_LOCKED As Integer = 1055
        '    Public Const ERROR_SERVICE_ALREADY_RUNNING As Integer = 1056
        '    Public Const ERROR_INVALID_SERVICE_ACCOUNT As Integer = 1057
        '    Public Const ERROR_SERVICE_DISABLED As Integer = 1058
        '    Public Const ERROR_CIRCULAR_DEPENDENCY As Integer = 1059
        '    Public Const ERROR_SERVICE_DOES_NOT_EXIST As Integer = 1060
        '    Public Const ERROR_SERVICE_CANNOT_ACCEPT_CTRL As Integer = 1061
        '    Public Const ERROR_SERVICE_NOT_ACTIVE As Integer = 1062
        '    Public Const ERROR_FAILED_SERVICE_CONTROLLER_CONNECT As Integer = 1063
        '    Public Const ERROR_EXCEPTION_IN_SERVICE As Integer = 1064
        '    Public Const ERROR_DATABASE_DOES_NOT_EXIST As Integer = 1065
        '    Public Const ERROR_SERVICE_SPECIFIC_ERROR As Integer = 1066
        '    Public Const ERROR_PROCESS_ABORTED As Integer = 1067
        '    Public Const ERROR_SERVICE_DEPENDENCY_FAIL As Integer = 1068
        '    Public Const ERROR_SERVICE_LOGON_FAILED As Integer = 1069
        '    Public Const ERROR_SERVICE_START_HANG As Integer = 1070
        '    Public Const ERROR_INVALID_SERVICE_LOCK As Integer = 1071
        '    Public Const ERROR_SERVICE_MARKED_FOR_DELETE As Integer = 1072
        '    Public Const ERROR_SERVICE_EXISTS As Integer = 1073
        '    Public Const ERROR_ALREADY_RUNNING_LKG As Integer = 1074
        '    Public Const ERROR_SERVICE_DEPENDENCY_DELETED As Integer = 1075
        '    Public Const ERROR_BOOT_ALREADY_ACCEPTED As Integer = 1076
        '    Public Const ERROR_SERVICE_NEVER_STARTED As Integer = 1077
        '    Public Const ERROR_DUPLICATE_SERVICE_NAME As Integer = 1078
        '    Public Const ERROR_DIFFERENT_SERVICE_ACCOUNT As Integer = 1079
        '    Public Const ERROR_END_OF_MEDIA As Integer = 1100
        '    Public Const ERROR_FILEMARK_DETECTED As Integer = 1101
        '    Public Const ERROR_BEGINNING_OF_MEDIA As Integer = 1102
        '    Public Const ERROR_SETMARK_DETECTED As Integer = 1103
        '    Public Const ERROR_NO_DATA_DETECTED As Integer = 1104
        '    Public Const ERROR_PARTITION_FAILURE As Integer = 1105
        '    Public Const ERROR_INVALID_BLOCK_LENGTH As Integer = 1106
        '    Public Const ERROR_DEVICE_NOT_PARTITIONED As Integer = 1107
        '    Public Const ERROR_UNABLE_TO_LOCK_MEDIA As Integer = 1108
        '    Public Const ERROR_UNABLE_TO_UNLOAD_MEDIA As Integer = 1109
        '    Public Const ERROR_MEDIA_CHANGED As Integer = 1110
        '    Public Const ERROR_BUS_RESET As Integer = 1111
        '    Public Const ERROR_NO_MEDIA_IN_DRIVE As Integer = 1112
        '    Public Const ERROR_NO_UNICODE_TRANSLATION As Integer = 1113
        '    Public Const ERROR_DLL_INIT_FAILED As Integer = 1114
        '    Public Const ERROR_SHUTDOWN_IN_PROGRESS As Integer = 1115
        '    Public Const ERROR_NO_SHUTDOWN_IN_PROGRESS As Integer = 1116
        '    Public Const ERROR_IO_DEVICE As Integer = 1117
        '    Public Const ERROR_SERIAL_NO_DEVICE As Integer = 1118
        '    Public Const ERROR_IRQ_BUSY As Integer = 1119
        '    Public Const ERROR_MORE_WRITES As Integer = 1120
        '    Public Const ERROR_COUNTER_TIMEOUT As Integer = 1121
        '    Public Const ERROR_FLOPPY_ID_MARK_NOT_FOUND As Integer = 1122
        '    Public Const ERROR_FLOPPY_WRONG_CYLINDER As Integer = 1123
        '    Public Const ERROR_FLOPPY_UNKNOWN_ERROR As Integer = 1124
        '    Public Const ERROR_FLOPPY_BAD_REGISTERS As Integer = 1125
        '    Public Const ERROR_DISK_RECALIBRATE_FAILED As Integer = 1126
        '    Public Const ERROR_DISK_OPERATION_FAILED As Integer = 1127
        '    Public Const ERROR_DISK_RESET_FAILED As Integer = 1128
        '    Public Const ERROR_EOM_OVERFLOW As Integer = 1129
        '    Public Const ERROR_NOT_ENOUGH_SERVER_MEMORY As Integer = 1130
        '    Public Const ERROR_POSSIBLE_DEADLOCK As Integer = 1131
        '    Public Const ERROR_MAPPED_ALIGNMENT As Integer = 1132
        '    Public Const ERROR_SET_POWER_STATE_VETOED As Integer = 1140
        '    Public Const ERROR_SET_POWER_STATE_FAILED As Integer = 1141
        '    Public Const ERROR_TOO_MANY_LINKS As Integer = 1142
        '    Public Const ERROR_OLD_WIN_VERSION As Integer = 1150
        '    Public Const ERROR_APP_WRONG_OS As Integer = 1151
        '    Public Const ERROR_SINGLE_INSTANCE_APP As Integer = 1152
        '    Public Const ERROR_RMODE_APP As Integer = 1153
        '    Public Const ERROR_INVALID_DLL As Integer = 1154
        '    Public Const ERROR_NO_ASSOCIATION As Integer = 1155
        '    Public Const ERROR_DDE_FAIL As Integer = 1156
        '    Public Const ERROR_DLL_NOT_FOUND As Integer = 1157
        '    Public Const ERROR_BAD_USERNAME As Integer = 2202
        '    Public Const ERROR_NOT_CONNECTED As Integer = 2250
        '    Public Const ERROR_OPEN_FILES As Integer = 2401
        '    Public Const ERROR_ACTIVE_CONNECTIONS As Integer = 2402
        '    Public Const ERROR_DEVICE_IN_USE As Integer = 2404
        '    Public Const ERROR_BAD_DEVICE As Integer = 1200
        '    Public Const ERROR_CONNECTION_UNAVAIL As Integer = 1201
        '    Public Const ERROR_DEVICE_ALREADY_REMEMBERED As Integer = 1202
        '    Public Const ERROR_NO_NET_OR_BAD_PATH As Integer = 1203
        '    Public Const ERROR_BAD_PROVIDER As Integer = 1204
        '    Public Const ERROR_CANNOT_OPEN_PROFILE As Integer = 1205
        '    Public Const ERROR_BAD_PROFILE As Integer = 1206
        '    Public Const ERROR_NOT_CONTAINER As Integer = 1207
        '    Public Const ERROR_EXTENDED_ERROR As Integer = 1208
        '    Public Const ERROR_INVALID_GROUPNAME As Integer = 1209
        '    Public Const ERROR_INVALID_COMPUTERNAME As Integer = 1210
        '    Public Const ERROR_INVALID_EVENTNAME As Integer = 1211
        '    Public Const ERROR_INVALID_DOMAINNAME As Integer = 1212
        '    Public Const ERROR_INVALID_SERVICENAME As Integer = 1213
        '    Public Const ERROR_INVALID_NETNAME As Integer = 1214
        '    Public Const ERROR_INVALID_SHARENAME As Integer = 1215
        '    Public Const ERROR_INVALID_PASSWORDNAME As Integer = 1216
        '    Public Const ERROR_INVALID_MESSAGENAME As Integer = 1217
        '    Public Const ERROR_INVALID_MESSAGEDEST As Integer = 1218
        '    Public Const ERROR_SESSION_CREDENTIAL_CONFLICT As Integer = 1219
        '    Public Const ERROR_REMOTE_SESSION_LIMIT_EXCEEDED As Integer = 1220
        '    Public Const ERROR_DUP_DOMAINNAME As Integer = 1221
        '    Public Const ERROR_NO_NETWORK As Integer = 1222
        '    Public Const ERROR_CANCELLED As Integer = 1223
        '    Public Const ERROR_USER_MAPPED_FILE As Integer = 1224
        '    Public Const ERROR_CONNECTION_REFUSED As Integer = 1225
        '    Public Const ERROR_GRACEFUL_DISCONNECT As Integer = 1226
        '    Public Const ERROR_ADDRESS_ALREADY_ASSOCIATED As Integer = 1227
        '    Public Const ERROR_ADDRESS_NOT_ASSOCIATED As Integer = 1228
        '    Public Const ERROR_CONNECTION_INVALID As Integer = 1229
        '    Public Const ERROR_CONNECTION_ACTIVE As Integer = 1230
        '    Public Const ERROR_NETWORK_UNREACHABLE As Integer = 1231
        '    Public Const ERROR_HOST_UNREACHABLE As Integer = 1232
        '    Public Const ERROR_PROTOCOL_UNREACHABLE As Integer = 1233
        '    Public Const ERROR_PORT_UNREACHABLE As Integer = 1234
        '    Public Const ERROR_REQUEST_ABORTED As Integer = 1235
        '    Public Const ERROR_CONNECTION_ABORTED As Integer = 1236
        '    Public Const ERROR_RETRY As Integer = 1237
        '    Public Const ERROR_CONNECTION_COUNT_LIMIT As Integer = 1238
        '    Public Const ERROR_LOGIN_TIME_RESTRICTION As Integer = 1239
        '    Public Const ERROR_LOGIN_WKSTA_RESTRICTION As Integer = 1240
        '    Public Const ERROR_INCORRECT_ADDRESS As Integer = 1241
        '    Public Const ERROR_ALREADY_REGISTERED As Integer = 1242
        '    Public Const ERROR_SERVICE_NOT_FOUND As Integer = 1243
        '    Public Const ERROR_NOT_AUTHENTICATED As Integer = 1244
        '    Public Const ERROR_NOT_LOGGED_ON As Integer = 1245
        '    Public Const ERROR_CONTINUE As Integer = 1246
        '    Public Const ERROR_ALREADY_INITIALIZED As Integer = 1247
        '    Public Const ERROR_NO_MORE_DEVICES As Integer = 1248
        '    Public Const ERROR_NOT_ALL_ASSIGNED As Integer = 1300
        '    Public Const ERROR_SOME_NOT_MAPPED As Integer = 1301
        '    Public Const ERROR_NO_QUOTAS_FOR_ACCOUNT As Integer = 1302
        '    Public Const ERROR_LOCAL_USER_SESSION_KEY As Integer = 1303
        '    Public Const ERROR_NULL_LM_PASSWORD As Integer = 1304
        '    Public Const ERROR_UNKNOWN_REVISION As Integer = 1305
        '    Public Const ERROR_REVISION_MISMATCH As Integer = 1306
        '    Public Const ERROR_INVALID_OWNER As Integer = 1307
        '    Public Const ERROR_INVALID_PRIMARY_GROUP As Integer = 1308
        '    Public Const ERROR_NO_IMPERSONATION_TOKEN As Integer = 1309
        '    Public Const ERROR_CANT_DISABLE_MANDATORY As Integer = 1310
        '    Public Const ERROR_NO_LOGON_SERVERS As Integer = 1311
        '    Public Const ERROR_NO_SUCH_LOGON_SESSION As Integer = 1312
        '    Public Const ERROR_NO_SUCH_PRIVILEGE As Integer = 1313
        '    Public Const ERROR_PRIVILEGE_NOT_HELD As Integer = 1314
        '    Public Const ERROR_INVALID_ACCOUNT_NAME As Integer = 1315
        '    Public Const ERROR_USER_EXISTS As Integer = 1316
        '    Public Const ERROR_NO_SUCH_USER As Integer = 1317
        '    Public Const ERROR_GROUP_EXISTS As Integer = 1318
        '    Public Const ERROR_NO_SUCH_GROUP As Integer = 1319
        '    Public Const ERROR_MEMBER_IN_GROUP As Integer = 1320
        '    Public Const ERROR_MEMBER_NOT_IN_GROUP As Integer = 1321
        '    Public Const ERROR_LAST_ADMIN As Integer = 1322
        '    Public Const ERROR_WRONG_PASSWORD As Integer = 1323
        '    Public Const ERROR_ILL_FORMED_PASSWORD As Integer = 1324
        '    Public Const ERROR_PASSWORD_RESTRICTION As Integer = 1325
        '    Public Const ERROR_LOGON_FAILURE As Integer = 1326
        '    Public Const ERROR_ACCOUNT_RESTRICTION As Integer = 1327
        '    Public Const ERROR_INVALID_LOGON_HOURS As Integer = 1328
        '    Public Const ERROR_INVALID_WORKSTATION As Integer = 1329
        '    Public Const ERROR_PASSWORD_EXPIRED As Integer = 1330
        '    Public Const ERROR_ACCOUNT_DISABLED As Integer = 1331
        '    Public Const ERROR_NONE_MAPPED As Integer = 1332
        '    Public Const ERROR_TOO_MANY_LUIDS_REQUESTED As Integer = 1333
        '    Public Const ERROR_LUIDS_EXHAUSTED As Integer = 1334
        '    Public Const ERROR_INVALID_SUB_AUTHORITY As Integer = 1335
        '    Public Const ERROR_INVALID_ACL As Integer = 1336
        '    Public Const ERROR_INVALID_SID As Integer = 1337
        '    Public Const ERROR_INVALID_SECURITY_DESCR As Integer = 1338
        '    Public Const ERROR_BAD_INHERITANCE_ACL As Integer = 1340
        '    Public Const ERROR_SERVER_DISABLED As Integer = 1341
        '    Public Const ERROR_SERVER_NOT_DISABLED As Integer = 1342
        '    Public Const ERROR_INVALID_ID_AUTHORITY As Integer = 1343
        '    Public Const ERROR_ALLOTTED_SPACE_EXCEEDED As Integer = 1344
        '    Public Const ERROR_INVALID_GROUP_ATTRIBUTES As Integer = 1345
        '    Public Const ERROR_BAD_IMPERSONATION_LEVEL As Integer = 1346
        '    Public Const ERROR_CANT_OPEN_ANONYMOUS As Integer = 1347
        '    Public Const ERROR_BAD_VALIDATION_Class As Integer = 1348
        '    Public Const ERROR_BAD_TOKEN_TYPE As Integer = 1349
        '    Public Const ERROR_NO_SECURITY_ON_OBJECT As Integer = 1350
        '    Public Const ERROR_CANT_ACCESS_DOMAIN_INFO As Integer = 1351
        '    Public Const ERROR_INVALID_SERVER_STATE As Integer = 1352
        '    Public Const ERROR_INVALID_DOMAIN_STATE As Integer = 1353
        '    Public Const ERROR_INVALID_DOMAIN_ROLE As Integer = 1354
        '    Public Const ERROR_NO_SUCH_DOMAIN As Integer = 1355
        '    Public Const ERROR_DOMAIN_EXISTS As Integer = 1356
        '    Public Const ERROR_DOMAIN_LIMIT_EXCEEDED As Integer = 1357
        '    Public Const ERROR_INTERNAL_DB_CORRUPTION As Integer = 1358
        '    Public Const ERROR_INTERNAL_ERROR As Integer = 1359
        '    Public Const ERROR_GENERIC_NOT_MAPPED As Integer = 1360
        '    Public Const ERROR_BAD_DESCRIPTOR_FORMAT As Integer = 1361
        '    Public Const ERROR_NOT_LOGON_PROCESS As Integer = 1362
        '    Public Const ERROR_LOGON_SESSION_EXISTS As Integer = 1363
        '    Public Const ERROR_NO_SUCH_PACKAGE As Integer = 1364
        '    Public Const ERROR_BAD_LOGON_SESSION_STATE As Integer = 1365
        '    Public Const ERROR_LOGON_SESSION_COLLISION As Integer = 1366
        '    Public Const ERROR_INVALID_LOGON_TYPE As Integer = 1367
        '    Public Const ERROR_CANNOT_IMPERSONATE As Integer = 1368
        '    Public Const ERROR_RXACT_INVALID_STATE As Integer = 1369
        '    Public Const ERROR_RXACT_COMMIT_FAILURE As Integer = 1370
        '    Public Const ERROR_SPECIAL_ACCOUNT As Integer = 1371
        '    Public Const ERROR_SPECIAL_GROUP As Integer = 1372
        '    Public Const ERROR_SPECIAL_USER As Integer = 1373
        '    Public Const ERROR_MEMBERS_PRIMARY_GROUP As Integer = 1374
        '    Public Const ERROR_TOKEN_ALREADY_IN_USE As Integer = 1375
        '    Public Const ERROR_NO_SUCH_ALIAS As Integer = 1376
        '    Public Const ERROR_MEMBER_NOT_IN_ALIAS As Integer = 1377
        '    Public Const ERROR_MEMBER_IN_ALIAS As Integer = 1378
        '    Public Const ERROR_ALIAS_EXISTS As Integer = 1379
        '    Public Const ERROR_LOGON_NOT_GRANTED As Integer = 1380
        '    Public Const ERROR_TOO_MANY_SECRETS As Integer = 1381
        '    Public Const ERROR_SECRET_TOO_LONG As Integer = 1382
        '    Public Const ERROR_INTERNAL_DB_ERROR As Integer = 1383
        '    Public Const ERROR_TOO_MANY_CONTEXT_IDS As Integer = 1384
        '    Public Const ERROR_LOGON_TYPE_NOT_GRANTED As Integer = 1385
        '    Public Const ERROR_NT_CROSS_ENCRYPTION_REQUIRED As Integer = 1386
        '    Public Const ERROR_NO_SUCH_MEMBER As Integer = 1387
        '    Public Const ERROR_INVALID_MEMBER As Integer = 1388
        '    Public Const ERROR_TOO_MANY_SIDS As Integer = 1389
        '    Public Const ERROR_LM_CROSS_ENCRYPTION_REQUIRED As Integer = 1390
        '    Public Const ERROR_NO_INHERITANCE As Integer = 1391
        '    Public Const ERROR_FILE_CORRUPT As Integer = 1392
        '    Public Const ERROR_DISK_CORRUPT As Integer = 1393
        '    Public Const ERROR_NO_USER_SESSION_KEY As Integer = 1394
        '    Public Const ERROR_LICENSE_QUOTA_EXCEEDED As Integer = 1395
        '    Public Const ERROR_WRONG_TARGET_NAME As Integer = 1396
        '    Public Const ERROR_MUTUAL_AUTH_FAILED As Integer = 1397
        '    Public Const ERROR_TIME_SKEW As Integer = 1398
        '    Public Const ERROR_CURRENT_DOMAIN_NOT_ALLOWED As Integer = 1399
        '    Public Const ERROR_INVALID_WINDOW_HANDLE As Integer = 1400
        '    Public Const ERROR_INVALID_MENU_HANDLE As Integer = 1401
        '    Public Const ERROR_INVALID_CURSOR_HANDLE As Integer = 1402
        '    Public Const ERROR_INVALID_ACCEL_HANDLE As Integer = 1403
        '    Public Const ERROR_INVALID_HOOK_HANDLE As Integer = 1404
        '    Public Const ERROR_INVALID_DWP_HANDLE As Integer = 1405
        '    Public Const ERROR_TLW_WITH_WSCHILD As Integer = 1406
        '    Public Const ERROR_CANNOT_FIND_WND_Class As Integer = 1407
        '    Public Const ERROR_WINDOW_OF_OTHER_THREAD As Integer = 1408
        '    Public Const ERROR_HOTKEY_ALREADY_REGISTERED As Integer = 1409
        '    Public Const ERROR_CLASS_ALREADY_EXISTS As Integer = 1410
        '    Public Const ERROR_CLASS_DOES_NOT_EXIST As Integer = 1411
        '    Public Const ERROR_CLASS_HAS_WINDOWS As Integer = 1412
        '    Public Const ERROR_INVALID_INDEX As Integer = 1413
        '    Public Const ERROR_INVALID_ICON_HANDLE As Integer = 1414
        '    Public Const ERROR_PRIVATE_DIALOG_INDEX As Integer = 1415
        '    Public Const ERROR_LISTBOX_ID_NOT_FOUND As Integer = 1416
        '    Public Const ERROR_NO_WILDCARD_CHARACTERS As Integer = 1417
        '    Public Const ERROR_CLIPBOARD_NOT_OPEN As Integer = 1418
        '    Public Const ERROR_HOTKEY_NOT_REGISTERED As Integer = 1419
        '    Public Const ERROR_WINDOW_NOT_DIALOG As Integer = 1420
        '    Public Const ERROR_CONTROL_ID_NOT_FOUND As Integer = 1421
        '    Public Const ERROR_INVALID_COMBOBOX_MESSAGE As Integer = 1422
        '    Public Const ERROR_WINDOW_NOT_COMBOBOX As Integer = 1423
        '    Public Const ERROR_INVALID_EDIT_HEIGHT As Integer = 1424
        '    Public Const ERROR_DC_NOT_FOUND As Integer = 1425
        '    Public Const ERROR_INVALID_HOOK_FILTER As Integer = 1426
        '    Public Const ERROR_INVALID_FILTER_PROC As Integer = 1427
        '    Public Const ERROR_HOOK_NEEDS_HMOD As Integer = 1428
        '    Public Const ERROR_GLOBAL_ONLY_HOOK As Integer = 1429
        '    Public Const ERROR_JOURNAL_HOOK_SET As Integer = 1430
        '    Public Const ERROR_HOOK_NOT_INSTALLED As Integer = 1431
        '    Public Const ERROR_INVALID_LB_MESSAGE As Integer = 1432
        '    Public Const ERROR_SETCOUNT_ON_BAD_LB As Integer = 1433
        '    Public Const ERROR_LB_WITHOUT_TABSTOPS As Integer = 1434
        '    Public Const ERROR_DESTROY_OBJECT_OF_OTHER_THREAD As Integer = 1435
        '    Public Const ERROR_CHILD_WINDOW_MENU As Integer = 1436
        '    Public Const ERROR_NO_SYSTEM_MENU As Integer = 1437
        '    Public Const ERROR_INVALID_MSGBOX_STYLE As Integer = 1438
        '    Public Const ERROR_INVALID_SPI_VALUE As Integer = 1439
        '    Public Const ERROR_SCREEN_ALREADY_LOCKED As Integer = 1440
        '    Public Const ERROR_HWNDS_HAVE_DIFF_PARENT As Integer = 1441
        '    Public Const ERROR_NOT_CHILD_WINDOW As Integer = 1442
        '    Public Const ERROR_INVALID_GW_COMMAND As Integer = 1443
        '    Public Const ERROR_INVALID_THREAD_ID As Integer = 1444
        '    Public Const ERROR_NON_MDICHILD_WINDOW As Integer = 1445
        '    Public Const ERROR_POPUP_ALREADY_ACTIVE As Integer = 1446
        '    Public Const ERROR_NO_SCROLLBARS As Integer = 1447
        '    Public Const ERROR_INVALID_SCROLLBAR_RANGE As Integer = 1448
        '    Public Const ERROR_INVALID_SHOWWIN_COMMAND As Integer = 1449
        '    Public Const ERROR_NO_SYSTEM_RESOURCES As Integer = 1450
        '    Public Const ERROR_NONPAGED_SYSTEM_RESOURCES As Integer = 1451
        '    Public Const ERROR_PAGED_SYSTEM_RESOURCES As Integer = 1452
        '    Public Const ERROR_WORKING_SET_QUOTA As Integer = 1453
        '    Public Const ERROR_PAGEFILE_QUOTA As Integer = 1454
        '    Public Const ERROR_COMMITMENT_LIMIT As Integer = 1455
        '    Public Const ERROR_MENU_ITEM_NOT_FOUND As Integer = 1456
        '    Public Const ERROR_INVALID_KEYBOARD_HANDLE As Integer = 1457
        '    Public Const ERROR_HOOK_TYPE_NOT_ALLOWED As Integer = 1458
        '    Public Const ERROR_REQUIRES_INTERACTIVE_WINDOWSTATION As Integer = 1459
        '    Public Const ERROR_TIMEOUT As Integer = 1460
        '    Public Const ERROR_EVENTLOG_FILE_CORRUPT As Integer = 1500
        '    Public Const ERROR_EVENTLOG_CANT_START As Integer = 1501
        '    Public Const ERROR_LOG_FILE_FULL As Integer = 1502
        '    Public Const ERROR_EVENTLOG_FILE_CHANGED As Integer = 1503
        '    Public Const EPT_S_INVALID_ENTRY As Integer = 1751
        '    Public Const EPT_S_CANT_PERFORM_OP As Integer = 1752
        '    Public Const EPT_S_NOT_REGISTERED As Integer = 1753
        '    Public Const ERROR_INVALID_USER_BUFFER As Integer = 1784
        '    Public Const ERROR_UNRECOGNIZED_MEDIA As Integer = 1785
        '    Public Const ERROR_NO_TRUST_LSA_SECRET As Integer = 1786
        '    Public Const ERROR_NO_TRUST_SAM_ACCOUNT As Integer = 1787
        '    Public Const ERROR_TRUSTED_DOMAIN_FAILURE As Integer = 1788
        '    Public Const ERROR_TRUSTED_RELATIONSHIP_FAILURE As Integer = 1789
        '    Public Const ERROR_TRUST_FAILURE As Integer = 1790
        '    Public Const ERROR_NETLOGON_NOT_STARTED As Integer = 1792
        '    Public Const ERROR_ACCOUNT_EXPIRED As Integer = 1793
        '    Public Const ERROR_REDIRECTOR_HAS_OPEN_HANDLES As Integer = 1794
        '    Public Const ERROR_PRINTER_DRIVER_ALREADY_INSTALLED As Integer = 1795
        '    Public Const ERROR_UNKNOWN_PORT As Integer = 1796
        '    Public Const ERROR_UNKNOWN_PRINTER_DRIVER As Integer = 1797
        '    Public Const ERROR_UNKNOWN_PRINTPROCESSOR As Integer = 1798
        '    Public Const ERROR_INVALID_SEPARATOR_FILE As Integer = 1799
        '    Public Const ERROR_INVALID_PRIORITY As Integer = 1800
        '    Public Const ERROR_INVALID_PRINTER_NAME As Integer = 1801
        '    Public Const ERROR_PRINTER_ALREADY_EXISTS As Integer = 1802
        '    Public Const ERROR_INVALID_PRINTER_COMMAND As Integer = 1803
        '    Public Const ERROR_INVALID_DATATYPE As Integer = 1804
        '    Public Const ERROR_INVALID_ENVIRONMENT As Integer = 1805
        '    Public Const ERROR_NOLOGON_INTERDOMAIN_TRUST_ACCOUNT As Integer = 1807
        '    Public Const ERROR_NOLOGON_WORKSTATION_TRUST_ACCOUNT As Integer = 1808
        '    Public Const ERROR_NOLOGON_SERVER_TRUST_ACCOUNT As Integer = 1809
        '    Public Const ERROR_DOMAIN_TRUST_INCONSISTENT As Integer = 1810
        '    Public Const ERROR_SERVER_HAS_OPEN_HANDLES As Integer = 1811
        '    Public Const ERROR_RESOURCE_DATA_NOT_FOUND As Integer = 1812
        '    Public Const ERROR_RESOURCE_TYPE_NOT_FOUND As Integer = 1813
        '    Public Const ERROR_RESOURCE_NAME_NOT_FOUND As Integer = 1814
        '    Public Const ERROR_RESOURCE_LANG_NOT_FOUND As Integer = 1815
        '    Public Const ERROR_NOT_ENOUGH_QUOTA As Integer = 1816
        '    Public Const EPT_S_CANT_CREATE As Integer = 1899
        '    Public Const ERROR_INVALID_TIME As Integer = 1901
        '    Public Const ERROR_INVALID_FORM_NAME As Integer = 1902
        '    Public Const ERROR_INVALID_FORM_SIZE As Integer = 1903
        '    Public Const ERROR_ALREADY_WAITING As Integer = 1904
        '    Public Const ERROR_PRINTER_DELETED As Integer = 1905
        '    Public Const ERROR_INVALID_PRINTER_STATE As Integer = 1906
        '    Public Const ERROR_PASSWORD_MUST_CHANGE As Integer = 1907
        '    Public Const ERROR_DOMAIN_CONTROLLER_NOT_FOUND As Integer = 1908
        '    Public Const ERROR_ACCOUNT_LOCKED_OUT As Integer = 1909
        '    Public Const ERROR_NO_BROWSER_SERVERS_FOUND As Integer = 6118
        '    Public Const ERROR_INVALID_PIXEL_FORMAT As Integer = 2000
        '    Public Const ERROR_BAD_DRIVER As Integer = 2001
        '    Public Const ERROR_INVALID_WINDOW_STYLE As Integer = 2002
        '    Public Const ERROR_METAFILE_NOT_SUPPORTED As Integer = 2003
        '    Public Const ERROR_TRANSFORM_NOT_SUPPORTED As Integer = 2004
        '    Public Const ERROR_CLIPPING_NOT_SUPPORTED As Integer = 2005
        '    Public Const ERROR_UNKNOWN_PRINT_MONITOR As Integer = 3000
        '    Public Const ERROR_PRINTER_DRIVER_IN_USE As Integer = 3001
        '    Public Const ERROR_SPOOL_FILE_NOT_FOUND As Integer = 3002
        '    Public Const ERROR_SPL_NO_STARTDOC As Integer = 3003
        '    Public Const ERROR_SPL_NO_ADDJOB As Integer = 3004
        '    Public Const ERROR_PRINT_PROCESSOR_ALREADY_INSTALLED As Integer = 3005
        '    Public Const ERROR_PRINT_MONITOR_ALREADY_INSTALLED As Integer = 3006
        '    Public Const ERROR_INVALID_PRINT_MONITOR As Integer = 3007
        '    Public Const ERROR_PRINT_MONITOR_IN_USE As Integer = 3008
        '    Public Const ERROR_PRINTER_HAS_JOBS_QUEUED As Integer = 3009
        '    Public Const ERROR_SUCCESS_REBOOT_REQUIRED As Integer = 3010
        '    Public Const ERROR_SUCCESS_RESTART_REQUIRED As Integer = 3011
        '    Public Const ERROR_WINS_INTERNAL As Integer = 4000
        '    Public Const ERROR_CAN_NOT_DEL_LOCAL_WINS As Integer = 4001
        '    Public Const ERROR_STATIC_INIT As Integer = 4002
        '    Public Const ERROR_INC_BACKUP As Integer = 4003
        '    Public Const ERROR_FULL_BACKUP As Integer = 4004
        '    Public Const ERROR_REC_NON_EXISTENT As Integer = 4005
        '    Public Const ERROR_RPL_NOT_ALLOWED As Integer = 4006
        '    Public Const E_UNEXPECTED As Integer = 
        '    '
        '    'Note:  Error processing original source shown below
        '    '        ERROR_RPL_NOT_ALLOWED = 4006,
        '    '        E_UNEXPECTED = unchecked((int)0x8000FFFF),
        '    '------------------------^--- GenCode(token): unexpected token type
        '    Public Const E_NOTIMPL As Integer = 
        '    '
        '    'Note:  Error processing original source shown below
        '    '        E_UNEXPECTED = unchecked((int)0x8000FFFF),
        '    '        E_NOTIMPL = unchecked((int)0x80004001),
        '    '---------------------^--- GenCode(token): unexpected token type
        '    Public Const E_OUTOFMEMORY As Integer = 
        '    '
        '    'Note:  Error processing original source shown below
        '    '        E_NOTIMPL = unchecked((int)0x80004001),
        '    '        E_OUTOFMEMORY = unchecked((int)0x8007000E),
        '    '-------------------------^--- GenCode(token): unexpected token type
        '    Public Const E_INVALIDARG As Integer = 
        '    '
        '    'Note:  Error processing original source shown below
        '    '        E_OUTOFMEMORY = unchecked((int)0x8007000E),
        '    '        E_INVALIDARG = unchecked((int)0x80070057),
        '    '------------------------^--- GenCode(token): unexpected token type
        '    Public Const E_NOInterfaceAs Integer = 
        '    '
        '    'Note:  Error processing original source shown below
        '    '        E_INVALIDARG = unchecked((int)0x80070057),
        '    '        E_NOINTERFACE = unchecked((int)0x80004002),
        '    '-------------------------^--- GenCode(token): unexpected token type
        '    Public Const E_POINTER As Integer = 
        '    '
        '    'Note:  Error processing original source shown below
        '    '        E_NOINTERFACE = unchecked((int)0x80004002),
        '    '        E_POINTER = unchecked((int)0x80004003),
        '    '---------------------^--- GenCode(token): unexpected token type
        '    Public Const E_HANDLE As Integer = 
        '    '
        '    'Note:  Error processing original source shown below
        '    '        E_POINTER = unchecked((int)0x80004003),
        '    '        E_HANDLE = unchecked((int)0x80070006),
        '    '--------------------^--- GenCode(token): unexpected token type
        '    Public Const E_ABORT As Integer = 
        '    '
        '    'Note:  Error processing original source shown below
        '    '        E_HANDLE = unchecked((int)0x80070006),
        '    '        E_ABORT = unchecked((int)0x80004004),
        '    '-------------------^--- GenCode(token): unexpected token type
        '    Public Const E_FAIL As Integer = 
        '    '
        '    'Note:  Error processing original source shown below
        '    '        E_ABORT = unchecked((int)0x80004004),
        '    '        E_FAIL = unchecked((int)0x80004005),
        '    '------------------^--- GenCode(token): unexpected token type
        '    Public Const E_ACCESSDENIED As Integer = 
        '    '
        '    'Note:  Error processing original source shown below
        '    '        E_FAIL = unchecked((int)0x80004005),
        '    '        E_ACCESSDENIED = unchecked((int)0x80070005);
        '    '--------------------------^--- GenCode(token): unexpected token type
        '    ' E_NOTIMPL = unchecked((int)0x80000001);
        '    ' int E_OUTOFMEMORY = unchecked((int)0x80000002);
        '    ' int E_INVALIDARG = unchecked((int)0x80000003);
        '    ' int E_NOINTERFACE = unchecked((int)0x80000004);
        '    ' int E_POINTER = unchecked((int)0x80000005);
        '    ' int E_HANDLE = unchecked((int)0x80000006);
        '    ' int E_ABORT = unchecked((int)0x80000007);
        '    ' int E_FAIL = unchecked((int)0x80000008);
        '    ' int E_ACCESSDENIED = unchecked((int)0x80000009);
        '    Public Const E_PENDING As Integer = 
        '    '
        '    'Note:  Error processing original source shown below
        '    '        // int E_ACCESSDENIED = unchecked((int)0x80000009);
        '    '        public const int E_PENDING = unchecked((int)0x8000000A),
        '    '--------------------------------------^--- GenCode(token): unexpected token type
        '    Public Const ENUM_E_FIRST As Integer = 
        '    '
        '    'Note:  Error processing original source shown below
        '    '        public const int E_PENDING = unchecked((int)0x8000000A),
        '    '        ENUM_E_FIRST = unchecked((int)0x800401B0),
        '    '------------------------^--- GenCode(token): unexpected token type
        '    Public Const ENUM_E_LAST As Integer = 
        '    '
        '    'Note:  Error processing original source shown below
        '    '        ENUM_E_FIRST = unchecked((int)0x800401B0),
        '    '        ENUM_E_LAST = unchecked((int)0x800401BF),
        '    '-----------------------^--- GenCode(token): unexpected token type
        '    Public Const ENUM_S_FIRST As Integer = &H401B0
        '    Public Const ENUM_S_LAST As Integer = &H401BF
        '    Public Const [ERROR] As Integer = 0
        '    Public Const ETO_OPAQUE As Integer = &H2
        '    Public Const ETO_CLIPPED As Integer = &H4
        '    Public Const ETO_GLYPH_INDEX As Integer = &H10
        '    Public Const ETO_RTLREADING As Integer = &H80
        '    Public Const ETO_IGNORELANGUAGE As Integer = &H1000
        '    Public Const ENDDOC As Integer = 11
        '    Public Const ENABLEDUPLEX As Integer = 28
        '    Public Const ENUMPAPERBINS As Integer = 31
        '    Public Const EPSPRINTING As Integer = 33
        '    Public Const ENUMPAPERMETRICS As Integer = 34
        '    Public Const EXTTEXTOUT As Integer = 512
        '    Public Const ENABLERELATIVEWIDTHS As Integer = 768
        '    Public Const ENABLEPAIRKERNING As Integer = 769
        '    Public Const END_PATH As Integer = 4098
        '    Public Const EXT_DEVICE_CAPS As Integer = 4099
        '    Public Const ENCAPSULATED_POSTSCRIPT As Integer = 4116
        '    Public Const EASTEUROPE_CHARSET As Integer = 238
        '    Public Const ELF_VENDOR_SIZE As Integer = 4
        '    Public Const ELF_VERSION As Integer = 0
        '    Public Const ELF_CULTURE_LATIN As Integer = 0
        '    Public Const ENHMETA_SIGNATURE As Integer = &H464D4520
        '    Public Const ENHMETA_STOCK_OBJECT As Integer = 
        '    '
        '    'Note:  Error processing original source shown below
        '    '        ENHMETA_SIGNATURE = 0x464D4520,
        '    '        ENHMETA_STOCK_OBJECT = unchecked((int)0x80000000),
        '    '--------------------------------^--- GenCode(token): unexpected token type
        '    Public Const EMR_HEADER As Integer = 1
        '    Public Const EMR_POLYBEZIER As Integer = 2
        '    Public Const EMR_POLYGON As Integer = 3
        '    Public Const EMR_POLYLINE As Integer = 4
        '    Public Const EMR_POLYBEZIERTO As Integer = 5
        '    Public Const EMR_POLYLINETO As Integer = 6
        '    Public Const EMR_POLYPOLYLINE As Integer = 7
        '    Public Const EMR_POLYPOLYGON As Integer = 8
        '    Public Const EMR_SETWINDOWEXTEX As Integer = 9
        '    Public Const EMR_SETWINDOWORGEX As Integer = 10
        '    Public Const EMR_SETVIEWPORTEXTEX As Integer = 11
        '    Public Const EMR_SETVIEWPORTORGEX As Integer = 12
        '    Public Const EMR_SETBRUSHORGEX As Integer = 13
        '    Public Const EMR_EOF As Integer = 14
        '    Public Const EMR_SETPIXELV As Integer = 15
        '    Public Const EMR_SETMAPPERFLAGS As Integer = 16
        '    Public Const EMR_SETMAPMODE As Integer = 17
        '    Public Const EMR_SETBKMODE As Integer = 18
        '    Public Const EMR_SETPOLYFILLMODE As Integer = 19
        '    Public Const EMR_SETROP2 As Integer = 20
        '    Public Const EMR_SETSTRETCHBLTMODE As Integer = 21
        '    Public Const EMR_SETTEXTALIGN As Integer = 22
        '    Public Const EMR_SETCOLORADJUSTMENT As Integer = 23
        '    Public Const EMR_SETTEXTCOLOR As Integer = 24
        '    Public Const EMR_SETBKCOLOR As Integer = 25
        '    Public Const EMR_OFFSETCLIPRGN As Integer = 26
        '    Public Const EMR_MOVETOEX As Integer = 27
        '    Public Const EMR_SETMETARGN As Integer = 28
        '    Public Const EMR_EXCLUDECLIPRECT As Integer = 29
        '    Public Const EMR_INTERSECTCLIPRECT As Integer = 30
        '    Public Const EMR_SCALEVIEWPORTEXTEX As Integer = 31
        '    Public Const EMR_SCALEWINDOWEXTEX As Integer = 32
        '    Public Const EMR_SAVEDC As Integer = 33
        '    Public Const EMR_RESTOREDC As Integer = 34
        '    Public Const EMR_SETWORLDTRANSFORM As Integer = 35
        '    Public Const EMR_MODIFYWORLDTRANSFORM As Integer = 36
        '    Public Const EMR_SELECTOBJECT As Integer = 37
        '    Public Const EMR_CREATEPEN As Integer = 38
        '    Public Const EMR_CREATEBRUSHINDIRECT As Integer = 39
        '    Public Const EMR_DELETEOBJECT As Integer = 40
        '    Public Const EMR_ANGLEARC As Integer = 41
        '    Public Const EMR_ELLIPSE As Integer = 42
        '    Public Const EMR_RECTANGLE As Integer = 43
        '    Public Const EMR_ROUNDRECT As Integer = 44
        '    Public Const EMR_ARC As Integer = 45
        '    Public Const EMR_CHORD As Integer = 46
        '    Public Const EMR_PIE As Integer = 47
        '    Public Const EMR_SELECTPALETTE As Integer = 48
        '    Public Const EMR_CREATEPALETTE As Integer = 49
        '    Public Const EMR_SETPALETTEENTRIES As Integer = 50
        '    Public Const EMR_RESIZEPALETTE As Integer = 51
        '    Public Const EMR_REALIZEPALETTE As Integer = 52
        '    Public Const EMR_EXTFLOODFILL As Integer = 53
        '    Public Const EMR_LINETO As Integer = 54
        '    Public Const EMR_ARCTO As Integer = 55
        '    Public Const EMR_POLYDRAW As Integer = 56
        '    Public Const EMR_SETARCDIRECTION As Integer = 57
        '    Public Const EMR_SETMITERLIMIT As Integer = 58
        '    Public Const EMR_BEGINPATH As Integer = 59
        '    Public Const EMR_ENDPATH As Integer = 60
        '    Public Const EMR_CLOSEFIGURE As Integer = 61
        '    Public Const EMR_FILLPATH As Integer = 62
        '    Public Const EMR_STROKEANDFILLPATH As Integer = 63
        '    Public Const EMR_STROKEPATH As Integer = 64
        '    Public Const EMR_FLATTENPATH As Integer = 65
        '    Public Const EMR_WIDENPATH As Integer = 66
        '    Public Const EMR_SELECTCLIPPATH As Integer = 67
        '    Public Const EMR_ABORTPATH As Integer = 68
        '    Public Const EMR_GDICOMMENT As Integer = 70
        '    Public Const EMR_FILLRGN As Integer = 71
        '    Public Const EMR_FRAMERGN As Integer = 72
        '    Public Const EMR_INVERTRGN As Integer = 73
        '    Public Const EMR_PAINTRGN As Integer = 74
        '    Public Const EMR_EXTSELECTCLIPRGN As Integer = 75
        '    Public Const EMR_BITBLT As Integer = 76
        '    Public Const EMR_STRETCHBLT As Integer = 77
        '    Public Const EMR_MASKBLT As Integer = 78
        '    Public Const EMR_PLGBLT As Integer = 79
        '    Public Const EMR_SETDIBITSTODEVICE As Integer = 80
        '    Public Const EMR_STRETCHDIBITS As Integer = 81
        '    Public Const EMR_EXTCREATEFONTINDIRECTW As Integer = 82
        '    Public Const EMR_EXTTEXTOUTA As Integer = 83
        '    Public Const EMR_EXTTEXTOUTW As Integer = 84
        '    Public Const EMR_POLYBEZIER16 As Integer = 85
        '    Public Const EMR_POLYGON16 As Integer = 86
        '    Public Const EMR_POLYLINE16 As Integer = 87
        '    Public Const EMR_POLYBEZIERTO16 As Integer = 88
        '    Public Const EMR_POLYLINETO16 As Integer = 89
        '    Public Const EMR_POLYPOLYLINE16 As Integer = 90
        '    Public Const EMR_POLYPOLYGON16 As Integer = 91
        '    Public Const EMR_POLYDRAW16 As Integer = 92
        '    Public Const EMR_CREATEMONOBRUSH As Integer = 93
        '    Public Const EMR_CREATEDIBPATTERNBRUSHPT As Integer = 94
        '    Public Const EMR_EXTCREATEPEN As Integer = 95
        '    Public Const EMR_POLYTEXTOUTA As Integer = 96
        '    Public Const EMR_POLYTEXTOUTW As Integer = 97
        '    Public Const EMR_SETICMMODE As Integer = 98
        '    Public Const EMR_CREATECOLORSPACE As Integer = 99
        '    Public Const EMR_SETCOLORSPACE As Integer = 100
        '    Public Const EMR_DELETECOLORSPACE As Integer = 101
        '    Public Const EMR_GLSRECORD As Integer = 102
        '    Public Const EMR_GLSBOUNDEDRECORD As Integer = 103
        '    Public Const EMR_PIXELFORMAT As Integer = 104
        '    Public Const EMR_MIN As Integer = 1
        '    Public Const EMR_MAX As Integer = 104
        '    ' EMR_MAX = 97;
        '    Public Const EPS_SIGNATURE As Integer = &H46535045
        '    Public Const ENUM_ALL_CALENDARS As Integer = 
        '    '
        '    'Note:  Error processing original source shown below
        '    '        public const int EPS_SIGNATURE = 0x46535045,
        '    '        ENUM_ALL_CALENDARS = unchecked((int)0xFfffffff),
        '    '------------------------------^--- GenCode(token): unexpected token type
        '    Public Const ERROR_SEVERITY_SUCCESS As Integer = &H0
        '    Public Const ERROR_SEVERITY_INFORMATIONAL As Integer = &H40000000
        '    Public Const ERROR_SEVERITY_WARNING As Integer = 
        '    '
        '    'Note:  Error processing original source shown below
        '    '        ERROR_SEVERITY_INFORMATIONAL = 0x40000000,
        '    '        ERROR_SEVERITY_WARNING = unchecked((int)0x80000000),
        '    '----------------------------------^--- GenCode(token): unexpected token type
        '    Public Const ERROR_SEVERITY_ERROR As Integer = 
        '    '
        '    'Note:  Error processing original source shown below
        '    '        ERROR_SEVERITY_WARNING = unchecked((int)0x80000000),
        '    '        ERROR_SEVERITY_ERROR = unchecked((int)0xC0000000),
        '    '--------------------------------^--- GenCode(token): unexpected token type
        '    Public Const EXCEPTION_NONCONTINUABLE As Integer = &H1
        '    Public Const EXCEPTION_MAXIMUM_PARAMETERS As Integer = 15
        '    Public Const EVENT_MODIFY_STATE As Integer = &H2
        '    Public Const EVENT_OBJECT_SELECTION As Integer = &H8006
        '    Public Const EVENTLOG_SEQUENTIAL_READ As Integer = 0
        '    Private EVENTLOG_SEEK_READ As X0001 = 0
        '    Private __unknown As X0001
        '    Private EVENTLOG_FORWARDS_READ As X0002 = 0
        '    Private __unknown As X0002
        '    Private EVENTLOG_BACKWARDS_READ As X0004 = 0
        '    Private __unknown As X0004
        '    Private EVENTLOG_SUCCESS As X0008 = 0
        '    Private __unknown As X0008
        '    Private EVENTLOG_ERROR_TYPE As X0000 = &H1
        '    Private EVENTLOG_WARNING_TYPE As X0000 = &H2
        '    Private EVENTLOG_INFORMATION_TYPE As X0000 = &H4
        '    Private EVENTLOG_AUDIT_SUCCESS As X0000 = &H8
        '    Private EVENTLOG_AUDIT_FAILURE As X0000 = &H10
        '    Private EVENTLOG_START_PAIRED_EVENT As X0000 = &H1
        '    Private EVENTLOG_END_PAIRED_EVENT As X0000 = &H2
        '    Private EVENTLOG_END_ALL_PAIRED_EVENTS As X0000 = &H4
        '    Private EVENTLOG_PAIRED_EVENT_ACTIVE As X0000 = &H8
        '    Private EVENTLOG_PAIRED_EVENT_INACTIVE As X0000 = &H10
        '    Private EDGE_RAISED As X0000 = &H1 Or &H4
        '    Private EDGE_SUNKEN As X0000 = &H2 Or &H8
        '    Private EDGE_ETCHED As X0000 = &H2 Or &H4
        '    Private EDGE_BUMP As X0000 = &H1 Or &H8
        '    Private EW_RESTARTWINDOWS As X0000 = &H42
        '    Private EW_REBOOTSYSTEM As X0000 = &H43
        '    Private EW_EXITANDEXECAPP As X0000 = &H44
        '    Private ENDSESSION_LOGOFF As X0000 = 
        '    '
        '    'Note:  Error processing original source shown below
        '    '        EW_EXITANDEXECAPP = 0x0044,
        '    '        ENDSESSION_LOGOFF = unchecked((int)0x80000000),
        '    '-----------------------------^--- GenCode(token): unexpected token type
        '    Private EWX_LOGOFF As X0000 = 0
        '    Private EWX_SHUTDOWN As X0000 = 1
        '    Private EWX_REBOOT As X0000 = 2
        '    Private EWX_FORCE As X0000 = 4
        '    Private EWX_POWEROFF As X0000 = 8
        '    Private ESB_ENABLE_BOTH As X0000 = &H0
        '    Private ESB_DISABLE_BOTH As X0000 = &H3
        '    Private ESB_DISABLE_LEFT As X0000 = &H1
        '    Private ESB_DISABLE_RIGHT As X0000 = &H2
        '    Private ESB_DISABLE_UP As X0000 = &H1
        '    Private ESB_DISABLE_DOWN As X0000 = &H2
        '    Private ESB_DISABLE_LTUP As X0000 = &H1
        '    Private ESB_DISABLE_RTDN As X0000 = &H2
        '    Private ES_LEFT As X0000 = &H0
        '    Private ES_CENTER As X0000 = &H1
        '    Private ES_RIGHT As X0000 = &H2
        '    Private ES_MULTILINE As X0000 = &H4
        '    Private ES_UPPERCASE As X0000 = &H8
        '    Private ES_LOWERCASE As X0000 = &H10
        '    Private ES_PASSWORD As X0000 = &H20
        '    Private ES_AUTOVSCROLL As X0000 = &H40
        '    Private ES_AUTOHSCROLL As X0000 = &H80
        '    Private ES_NOHIDESEL As X0000 = &H100
        '    Private ES_OEMCONVERT As X0000 = &H400
        '    Private ES_READONLY As X0000 = &H800
        '    Private ES_WANTRETURN As X0000 = &H1000
        '    Private ES_NUMBER As X0000 = &H2000
        '    Private EN_SETFOCUS As X0000 = &H100
        '    Private EN_KILLFOCUS As X0000 = &H200
        '    Private EN_CHANGE As X0000 = &H300
        '    Private EN_UPDATE As X0000 = &H400
        '    Private EN_ERRSPACE As X0000 = &H500
        '    Private EN_MAXTEXT As X0000 = &H501
        '    Private EN_HSCROLL As X0000 = &H601
        '    Private EN_VSCROLL As X0000 = &H602
        '    Private EC_LEFTMARGIN As X0000 = &H1
        '    Private EC_RIGHTMARGIN As X0000 = &H2
        '    Private EC_USEFONTINFO As X0000 = &HFFFF
        '    Private EM_GETSEL As X0000 = &HB0
        '    Private EM_SETSEL As X0000 = &HB1
        '    Private EM_GETRECT As X0000 = &HB2
        '    Private EM_SETRECT As X0000 = &HB3
        '    Private EM_SETRECTNP As X0000 = &HB4
        '    Private EM_SCROLL As X0000 = &HB5
        '    Private EM_LINESCROLL As X0000 = &HB6
        '    Private EM_SCROLLCARET As X0000 = &HB7
        '    Private EM_GETMODIFY As X0000 = &HB8
        '    Private EM_SETMODIFY As X0000 = &HB9
        '    Private EM_GETLINECOUNT As X0000 = &HBA
        '    Private EM_LINEINDEX As X0000 = &HBB
        '    Private EM_SETHANDLE As X0000 = &HBC
        '    Private EM_GETHANDLE As X0000 = &HBD
        '    Private EM_GETTHUMB As X0000 = &HBE
        '    Private EM_LINELENGTH As X0000 = &HC1
        '    Private EM_REPLACESEL As X0000 = &HC2
        '    Private EM_GETLINE As X0000 = &HC4
        '    Private EM_LIMITTEXT As X0000 = &HC5
        '    Private EM_CANUNDO As X0000 = &HC6
        Public Const EM_UNDO As Integer = &HC7
'    Private EM_FMTLINES As X0000 = &HC8
'    Private EM_LINEFROMCHAR As X0000 = &HC9
'    Private EM_SETTABSTOPS As X0000 = &HCB
'    Private EM_SETPASSWORDCHAR As X0000 = &HCC
'    Private EM_EMPTYUNDOBUFFER As X0000 = &HCD
'    Private EM_GETFIRSTVISIBLELINE As X0000 = &HCE
'    Private EM_SETREADONLY As X0000 = &HCF
'    Private EM_SETWORDBREAKPROC As X0000 = &HD0
'    Private EM_GETWORDBREAKPROC As X0000 = &HD1
'    Private EM_GETPASSWORDCHAR As X0000 = &HD2
'    Private EM_SETMARGINS As X0000 = &HD3
'    Private EM_GETMARGINS As X0000 = &HD4
'    Private EM_SETLIMITTEXT As X0000 = &HC5
'    Private EM_GETLIMITTEXT As X0000 = &HD5
'    Private EM_POSFROMCHAR As X0000 = &HD6
'    Private EM_CHARFROMPOS As X0000 = &HD7
'    Private ENUM_CURRENT_SETTINGS As X0000 = - 1
'    Private ENUM_REGISTRY_SETTINGS As X0000 = - 2
'    Private E_DRAW As X0000 = 
'    '
'    'Note:  Error processing original source shown below
'    '                                ENUM_REGISTRY_SETTINGS = (-2),
'    '                                                         E_DRAW = unchecked((int)0x80040140),
'    '-------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Private EXCEPTION_ACCESS_VIOLATION As X0000 = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                         E_DRAW = unchecked((int)0x80040140),
'    '        EXCEPTION_ACCESS_VIOLATION = (unchecked((int)0xC0000005)),
'    '---------------------------------------^--- GenCode(token): unexpected token type
'    Private EXCEPTION_DATATYPE_MISALIGNMENT As X0000 = 
'    '
'    'Note:  Error processing original source shown below
'    '        EXCEPTION_ACCESS_VIOLATION = (unchecked((int)0xC0000005)),
'    '                                     EXCEPTION_DATATYPE_MISALIGNMENT = (unchecked((int)0x80000002)),
'    '-------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Private EXCEPTION_BREAKPOINT As X0000 = 
'    '
'    'Note:  Error processing original source shown below
'    '                                     EXCEPTION_DATATYPE_MISALIGNMENT = (unchecked((int)0x80000002)),
'    '                                                                       EXCEPTION_BREAKPOINT = (unchecked((int)0x80000003)),
'    '------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Private EXCEPTION_SINGLE_STEP As X0000 = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                       EXCEPTION_BREAKPOINT = (unchecked((int)0x80000003)),
'    '                                                                                              EXCEPTION_SINGLE_STEP = (unchecked((int)0x80000004)),
'    '------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Private EXCEPTION_ARRAY_BOUNDS_EXCEEDED As X0000 = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                              EXCEPTION_SINGLE_STEP = (unchecked((int)0x80000004)),
'    '                                                                                                                      EXCEPTION_ARRAY_BOUNDS_EXCEEDED = (unchecked((int)0xC000008C)),
'    '----------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Private EXCEPTION_FLT_DENORMAL_OPERAND As X0000 = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                      EXCEPTION_ARRAY_BOUNDS_EXCEEDED = (unchecked((int)0xC000008C)),
'    '                                                                                                                                                        EXCEPTION_FLT_DENORMAL_OPERAND = (unchecked((int)0xC000008D)),
'    '-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Private EXCEPTION_FLT_DIVIDE_BY_ZERO As X0000 = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                        EXCEPTION_FLT_DENORMAL_OPERAND = (unchecked((int)0xC000008D)),
'    '                                                                                                                                                                                         EXCEPTION_FLT_DIVIDE_BY_ZERO = (unchecked((int)0xC000008E)),
'    '--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Private EXCEPTION_FLT_INEXACT_RESULT As X0000 = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                                                         EXCEPTION_FLT_DIVIDE_BY_ZERO = (unchecked((int)0xC000008E)),
'    '                                                                                                                                                                                                                        EXCEPTION_FLT_INEXACT_RESULT = (unchecked((int)0xC000008F)),
'    '---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Private EXCEPTION_FLT_INVALID_OPERATION As X0000 = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                                                                                        EXCEPTION_FLT_INEXACT_RESULT = (unchecked((int)0xC000008F)),
'    '                                                                                                                                                                                                                                                       EXCEPTION_FLT_INVALID_OPERATION = (unchecked((int)0xC0000090)),
'    '-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Private EXCEPTION_FLT_OVERFLOW As X0000 = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                                                                                                                       EXCEPTION_FLT_INVALID_OPERATION = (unchecked((int)0xC0000090)),
'    '                                                                                                                                                                                                                                                                                         EXCEPTION_FLT_OVERFLOW = (unchecked((int)0xC0000091)),
'    '--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Private EXCEPTION_FLT_STACK_CHECK As X0000 = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                                                                                                                                                         EXCEPTION_FLT_OVERFLOW = (unchecked((int)0xC0000091)),
'    '                                                                                                                                                                                                                                                                                                                  EXCEPTION_FLT_STACK_CHECK = (unchecked((int)0xC0000092)),
'    '------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Private EXCEPTION_FLT_UNDERFLOW As X0000 = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                                                                                                                                                                                  EXCEPTION_FLT_STACK_CHECK = (unchecked((int)0xC0000092)),
'    '                                                                                                                                                                                                                                                                                                                                              EXCEPTION_FLT_UNDERFLOW = (unchecked((int)0xC0000093)),
'    '--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Private EXCEPTION_INT_DIVIDE_BY_ZERO As X0000 = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                                                                                                                                                                                                              EXCEPTION_FLT_UNDERFLOW = (unchecked((int)0xC0000093)),
'    '                                                                                                                                                                                                                                                                                                                                                                        EXCEPTION_INT_DIVIDE_BY_ZERO = (unchecked((int)0xC0000094)),
'    '---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Private EXCEPTION_INT_OVERFLOW As X0000 = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                                                                                                                                                                                                                                        EXCEPTION_INT_DIVIDE_BY_ZERO = (unchecked((int)0xC0000094)),
'    '                                                                                                                                                                                                                                                                                                                                                                                                       EXCEPTION_INT_OVERFLOW = (unchecked((int)0xC0000095)),
'    '----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Private EXCEPTION_PRIV_INSTRUCTION As X0000 = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                                                                                                                                                                                                                                                                       EXCEPTION_INT_OVERFLOW = (unchecked((int)0xC0000095)),
'    '                                                                                                                                                                                                                                                                                                                                                                                                                                EXCEPTION_PRIV_INSTRUCTION = (unchecked((int)0xC0000096)),
'    '---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Private EXCEPTION_IN_PAGE_ERROR As X0000 = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                                                                                                                                                                                                                                                                                                EXCEPTION_PRIV_INSTRUCTION = (unchecked((int)0xC0000096)),
'    '                                                                                                                                                                                                                                                                                                                                                                                                                                                             EXCEPTION_IN_PAGE_ERROR = (unchecked((int)0xC0000006)),
'    '-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Private EXCEPTION_ILLEGAL_INSTRUCTION As X0000 = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                                                                                                                                                                                                                                                                                                                             EXCEPTION_IN_PAGE_ERROR = (unchecked((int)0xC0000006)),
'    '                                                                                                                                                                                                                                                                                                                                                                                                                                                                                       EXCEPTION_ILLEGAL_INSTRUCTION = (unchecked((int)0xC000001D)),
'    '-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Private EXCEPTION_NONCONTINUABLE_EXCEPTION As X0000 = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                                                                                                                                                                                                                                                                                                                                                       EXCEPTION_ILLEGAL_INSTRUCTION = (unchecked((int)0xC000001D)),
'    '                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                       EXCEPTION_NONCONTINUABLE_EXCEPTION = (unchecked((int)0xC0000025)),
'    '--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Private EXCEPTION_STACK_OVERFLOW As X0000 = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                       EXCEPTION_NONCONTINUABLE_EXCEPTION = (unchecked((int)0xC0000025)),
'    '                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                            EXCEPTION_STACK_OVERFLOW = (unchecked((int)0xC00000FD)),
'    '-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Private EXCEPTION_INVALID_DISPOSITION As X0000 = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                            EXCEPTION_STACK_OVERFLOW = (unchecked((int)0xC00000FD)),
'    '                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                       EXCEPTION_INVALID_DISPOSITION = (unchecked((int)0xC0000026)),
'    '-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Private EXCEPTION_GUARD_PAGE As X0000 = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                       EXCEPTION_INVALID_DISPOSITION = (unchecked((int)0xC0000026)),
'    '                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                       EXCEPTION_GUARD_PAGE = (unchecked((int)0x80000001)),
'    '------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Private EXCEPTION_INVALID_HANDLE As X0000 = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                       EXCEPTION_GUARD_PAGE = (unchecked((int)0x80000001)),
'    '                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                              EXCEPTION_INVALID_HANDLE = (unchecked((int)0xC0000008)),
'    '---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Private EVENT_ALL_ACCESS As X0000 = &HF0000 Or &H100000 Or &H3
'    Private __unknown As X0000
    
    
    
    
    
'    Public Const FNERR_FILENAMECODES As Integer = &H3000
'    Public Const FNERR_SUBCLASSFAILURE As Integer = &H3001
'    Public Const FNERR_INVALIDFILENAME As Integer = &H3002
     Public Const FNERR_BUFFERTOOSMALL As Integer = &H3003
'    Public Const FRERR_FINDREPLACECODES As Integer = &H4000
'    Public Const FRERR_BUFFERLENGTHZERO As Integer = &H4001
'    Public Const FR_DOWN As Integer = &H1
'    Public Const FR_WHOLEWORD As Integer = &H2
'    Public Const FR_MATCHCASE As Integer = &H4
'    Public Const FR_FINDNEXT As Integer = &H8
'    Public Const FR_REPLACE As Integer = &H10
'    Public Const FR_REPLACEALL As Integer = &H20
'    Public Const FR_DIALOGTERM As Integer = &H40
'    Public Const FR_SHOWHELP As Integer = &H80
'    Public Const FR_ENABLEHOOK As Integer = &H100
'    Public Const FR_ENABLETEMPLATE As Integer = &H200
'    Public Const FR_NOUPDOWN As Integer = &H400
'    Public Const FR_NOMATCHCASE As Integer = &H800
'    Public Const FR_NOWHOLEWORD As Integer = &H1000
'    Public Const FR_ENABLETEMPLATEHANDLE As Integer = &H2000
'    Public Const FR_HIDEUPDOWN As Integer = &H4000
'    Public Const FR_HIDEMATCHCASE As Integer = &H8000
'    Public Const FR_HIDEWHOLEWORD As Integer = &H10000
'    Public Const [FALSE] As Boolean = False
'    Public Const frm1 As Integer = &H434
'    Public Const frm2 As Integer = &H435
'    Public Const frm3 As Integer = &H436
'    Public Const frm4 As Integer = &H437
'    Public Const FILEOPENORD As Integer = 1536
'    Public Const FINDDLGORD As Integer = 1540
'    Public Const FONTDLGORD As Integer = 1542
'    Public Const FORMATDLGORD31 As Integer = 1543
'    Public Const FORMATDLGORD30 As Integer = 1544
'    Public Const FADF_AUTO As Integer = &H1
'    Public Const FADF_STATIC As Integer = &H2
'    Public Const FADF_EMBEDDED As Integer = &H4
'    Public Const FADF_FIXEDSIZE As Integer = &H10
'    Public Const FADF_BSTR As Integer = &H100
'    Public Const FADF_UNKNOWN As Integer = &H200
'    Public Const FADF_DISPATCH As Integer = &H400
'    Public Const FADF_VARIANT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                              FADF_DISPATCH = (0x400),
'    '                                                                                                              FADF_VARIANT = (unchecked((int)0x800)),
'    '-------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const FADF_RESERVED As Integer = &HF0E8
'    Public Const FO_MOVE As Integer = &H1
'    Public Const FO_COPY As Integer = &H2
'    Public Const FO_DELETE As Integer = &H3
'    Public Const FO_RENAME As Integer = &H4
'    Public Const FOF_MULTIDESTFILES As Integer = &H1
'    Public Const FOF_CONFIRMMOUSE As Integer = &H2
'    Public Const FOF_SILENT As Integer = &H4
'    Public Const FOF_RENAMEONCOLLISION As Integer = &H8
'    Public Const FOF_NOCONFIRMATION As Integer = &H10
'    Public Const FOF_WANTMAPPINGHANDLE As Integer = &H20
'    Public Const FOF_ALLOWUNDO As Integer = &H40
'    Public Const FOF_FILESONLY As Integer = &H80
'    Public Const FOF_SIMPLEPROGRESS As Integer = &H100
'    Public Const FOF_NOCONFIRMMKDIR As Integer = &H200
'    Public Const FOF_NOERRORUI As Integer = &H400
'    Public Const FILE_BEGIN As Integer = 0
'    Public Const FILE_CURRENT As Integer = 1
'    Public Const FILE_END As Integer = 2
'    Public Const FILE_FLAG_WRITE_THROUGH As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        FILE_END = 2,
'    '        FILE_FLAG_WRITE_THROUGH = unchecked((int)0x80000000),
'    '-----------------------------------^--- GenCode(token): unexpected token type
'    Public Const FILE_FLAG_OVERLAPPED As Integer = &H40000000
'    Public Const FILE_FLAG_NO_BUFFERING As Integer = &H20000000
'    Public Const FILE_FLAG_RANDOM_ACCESS As Integer = &H10000000
'    Public Const FILE_FLAG_SEQUENTIAL_SCAN As Integer = &H8000000
'    Public Const FILE_FLAG_DELETE_ON_CLOSE As Integer = &H4000000
'    Public Const FILE_FLAG_BACKUP_SEMANTICS As Integer = &H2000000
'    Public Const FILE_FLAG_POSIX_SEMANTICS As Integer = &H1000000
'    Public Const FILE_TYPE_UNKNOWN As Integer = &H0
'    Public Const FILE_TYPE_DISK As Integer = &H1
'    Public Const FILE_TYPE_CHAR As Integer = &H2
'    Public Const FILE_TYPE_PIPE As Integer = &H3
'    Public Const FILE_TYPE_REMOTE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        FILE_TYPE_PIPE = 0x0003,
'    '        FILE_TYPE_REMOTE = unchecked((int)0x8000),
'    '----------------------------^--- GenCode(token): unexpected token type
'    Public Const FORMAT_MESSAGE_ALLOCATE_BUFFER As Integer = &H100
'    Public Const FORMAT_MESSAGE_IGNORE_INSERTS As Integer = &H200
'    Public Const FORMAT_MESSAGE_FROM_STRING As Integer = &H400
'    Public Const FORMAT_MESSAGE_FROM_HMODULE As Integer = &H800
'    Public Const FORMAT_MESSAGE_FROM_SYSTEM As Integer = &H1000
'    Public Const FORMAT_MESSAGE_ARGUMENT_ARRAY As Integer = &H2000
'    Public Const FORMAT_MESSAGE_MAX_WIDTH_MASK As Integer = &HFF
'    Public Const FIND_FIRST_EX_CASE_SENSITIVE As Integer = &H1
'    Public Const FROM_LEFT_1ST_BUTTON_PRESSED As Integer = &H1
'    Public Const FROM_LEFT_2ND_BUTTON_PRESSED As Integer = &H4
'    Public Const FROM_LEFT_3RD_BUTTON_PRESSED As Integer = &H8
'    Public Const FROM_LEFT_4TH_BUTTON_PRESSED As Integer = &H10
'    Public Const FOCUS_EVENT As Integer = &H10
'    Public Const FOREGROUND_BLUE As Integer = &H1
'    Public Const FOREGROUND_GREEN As Integer = &H2
'    Public Const FOREGROUND_RED As Integer = &H4
'    Public Const FOREGROUND_INTENSITY As Integer = &H8
     Public Const [FALSE] As Integer = 0
'    Public Const FACILITY_WINDOWS As Integer = 8
'    Public Const FACILITY_STORAGE As Integer = 3
'    Public Const FACILITY_RPC As Integer = 1
'    Public Const FACILITY_SSPI As Integer = 9
     Public Const FACILITY_WIN32 As Integer = 7
'    Public Const FACILITY_CONTROL As Integer = 10
'    Public Const FACILITY_NULL As Integer = 0
'    Public Const FACILITY_INTERNET As Integer = 12
'    Public Const FACILITY_ITF As Integer = 4
'    Public Const FACILITY_DISPATCH As Integer = 2
'    Public Const FACILITY_CERT As Integer = 11
'    Public Const FACILITY_NT_BIT As Integer = &H10000000
'    Public Const FLUSHOUTPUT As Integer = 6
'    Public Const FIXED_PITCH As Integer = 1
'    Public Const FS_LATIN1 As Integer = &H1
'    Public Const FS_LATIN2 As Integer = &H2
'    Public Const FS_CYRILLIC As Integer = &H4
'    Public Const FS_GREEK As Integer = &H8
'    Public Const FS_TURKISH As Integer = &H10
'    Public Const FS_HEBREW As Integer = &H20
'    Public Const FS_ARABIC As Integer = &H40
'    Public Const FS_BALTIC As Integer = &H80
'    Public Const FS_VIETNAMESE As Integer = &H100
'    Public Const FS_THAI As Integer = &H10000
'    Public Const FS_JISJAPAN As Integer = &H20000
'    Public Const FS_CHINESESIMP As Integer = &H40000
'    Public Const FS_WANSUNG As Integer = &H80000
'    Public Const FS_CHINESETRAD As Integer = &H100000
'    Public Const FS_JOHAB As Integer = &H200000
'    Public Const FS_SYMBOL As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        FS_JOHAB = 0x00200000,
'    '        FS_SYMBOL = unchecked((int)0x80000000),
'    '---------------------^--- GenCode(token): unexpected token type
'    Public Const FF_DONTCARE As Integer = Machine.Shift.Left(0, 4)
'    Public Const FF_ROMAN As Integer = Machine.Shift.Left(1, 4)
'    Public Const FF_SWISS As Integer = Machine.Shift.Left(2, 4)
'    Public Const FF_MODERN As Integer = Machine.Shift.Left(3, 4)
'    Public Const FF_SCRIPT As Integer = Machine.Shift.Left(4, 4)
'    Public Const FF_DECORATIVE As Integer = Machine.Shift.Left(5, 4)
'    Public Const FW_DONTCARE As Integer = 0
'    Public Const FW_THIN As Integer = 100
'    Public Const FW_EXTRALIGHT As Integer = 200
'    Public Const FW_LIGHT As Integer = 300
'    Public Const FW_NORMAL As Integer = 400
'    Public Const FW_MEDIUM As Integer = 500
'    Public Const FW_SEMIBOLD As Integer = 600
'    Public Const FW_BOLD As Integer = 700
'    Public Const FW_EXTRABOLD As Integer = 800
'    Public Const FW_HEAVY As Integer = 900
'    Public Const FW_ULTRALIGHT As Integer = 200
'    Public Const FW_REGULAR As Integer = 400
'    Public Const FW_DEMIBOLD As Integer = 600
'    Public Const FW_ULTRABOLD As Integer = 800
'    Public Const FW_BLACK As Integer = 900
'    Public Const FLOODFILLBORDER As Integer = 0
'    Public Const FLOODFILLSURFACE As Integer = 1
'    Public Const FLI_MASK As Integer = &H103B
'    Public Const FLI_GLYPHS As Integer = &H40000
'    Public Const FONTMAPPER_MAX As Integer = 10
'    Public Const FILE_READ_DATA As Integer = &H1
'    Public Const FILE_LIST_DIRECTORY As Integer = &H1
'    Public Const FILE_WRITE_DATA As Integer = &H2
'    Public Const FILE_ADD_FILE As Integer = &H2
'    Public Const FILE_APPEND_DATA As Integer = &H4
'    Public Const FILE_ADD_SUBDIRECTORY As Integer = &H4
'    Public Const FILE_CREATE_PIPE_INSTANCE As Integer = &H4
'    Public Const FILE_READ_EA As Integer = &H8
'    Public Const FILE_WRITE_EA As Integer = &H10
'    Public Const FILE_EXECUTE As Integer = &H20
'    Public Const FILE_TRAVERSE As Integer = &H20
'    Public Const FILE_DELETE_CHILD As Integer = &H40
'    Public Const FILE_READ_ATTRIBUTES As Integer = &H80
'    Public Const FILE_WRITE_ATTRIBUTES As Integer = &H100
'    Public Const FILE_SHARE_READ As Integer = &H1
'    Public Const FILE_SHARE_WRITE As Integer = &H2
'    Public Const FILE_SHARE_DELETE As Integer = &H4
'    Public Const FILE_ATTRIBUTE_READONLY As Integer = &H1
'    Public Const FILE_ATTRIBUTE_HIDDEN As Integer = &H2
'    Public Const FILE_ATTRIBUTE_SYSTEM As Integer = &H4
'    Public Const FILE_ATTRIBUTE_DIRECTORY As Integer = &H10
'    Public Const FILE_ATTRIBUTE_ARCHIVE As Integer = &H20
'    Public Const FILE_ATTRIBUTE_NORMAL As Integer = &H80
'    Public Const FILE_ATTRIBUTE_TEMPORARY As Integer = &H100
'    Public Const FILE_ATTRIBUTE_COMPRESSED As Integer = &H800
'    Public Const FILE_ATTRIBUTE_OFFLINE As Integer = &H1000
'    Public Const FILE_NOTIFY_CHANGE_FILE_NAME As Integer = &H1
'    Public Const FILE_NOTIFY_CHANGE_DIR_NAME As Integer = &H2
'    Public Const FILE_NOTIFY_CHANGE_ATTRIBUTES As Integer = &H4
'    Public Const FILE_NOTIFY_CHANGE_SIZE As Integer = &H8
'    Public Const FILE_NOTIFY_CHANGE_LAST_WRITE As Integer = &H10
'    Public Const FILE_NOTIFY_CHANGE_LAST_ACCESS As Integer = &H20
'    Public Const FILE_NOTIFY_CHANGE_CREATION As Integer = &H40
'    Public Const FILE_NOTIFY_CHANGE_SECURITY As Integer = &H100
'    Public Const FILE_ACTION_ADDED As Integer = &H1
'    Public Const FILE_ACTION_REMOVED As Integer = &H2
'    Public Const FILE_ACTION_MODIFIED As Integer = &H3
'    Public Const FILE_ACTION_RENAMED_OLD_NAME As Integer = &H4
'    Public Const FILE_ACTION_RENAMED_NEW_NAME As Integer = &H5
'    Public Const FILE_CASE_SENSITIVE_SEARCH As Integer = &H1
'    Public Const FILE_CASE_PRESERVED_NAMES As Integer = &H2
'    Public Const FILE_UNICODE_ON_DISK As Integer = &H4
'    Public Const FILE_PERSISTENT_ACLS As Integer = &H8
'    Public Const FILE_FILE_COMPRESSION As Integer = &H10
'    Public Const FILE_VOLUME_IS_COMPRESSED As Integer = &H8000
'    Public Const FAILED_ACCESS_ACE_FLAG As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        FILE_VOLUME_IS_COMPRESSED = 0x00008000,
'    '        FAILED_ACCESS_ACE_FLAG = (unchecked((int)0x80)),
'    '-----------------------------------^--- GenCode(token): unexpected token type
'    Public Const FRAME_FPO As Integer = 0
'    Public Const FRAME_TRAP As Integer = 1
'    Public Const FRAME_TSS As Integer = 2
'    Public Const FRAME_NONFPO As Integer = 3
'    Public Const FORM_USER As Integer = &H0
'    Public Const FORM_BUILTIN As Integer = &H1
'    Public Const FORM_PRINTER As Integer = &H2
'    Public Const FVIRTKEY As Integer = &H1
'    Public Const FNOINVERT As Integer = &H2
'    Public Const FSHIFT As Integer = &H4
'    Public Const FCONTROL As Integer = &H8
'    Public Const FALT As Integer = &H10
'    Public Const FKF_FILTERKEYSON As Integer = &H1
'    Public Const FKF_AVAILABLE As Integer = &H2
'    Public Const FKF_HOTKEYACTIVE As Integer = &H4
'    Public Const FKF_CONFIRMHOTKEY As Integer = &H8
'    Public Const FKF_HOTKEYSOUND As Integer = &H10
'    Public Const FKF_INDICATOR As Integer = &H20
'    Public Const FKF_CLICKON As Integer = &H40
'    Public Const FS_CASE_IS_PRESERVED As Integer = &H2
'    Public Const FS_CASE_SENSITIVE As Integer = &H1
'    Public Const FS_UNICODE_STORED_ON_DISK As Integer = &H4
'    Public Const FS_PERSISTENT_ACLS As Integer = &H8
'    Public Const FS_VOL_IS_COMPRESSED As Integer = &H8000
'    Public Const FS_FILE_COMPRESSION As Integer = &H10
'    Public Const FILE_MAP_COPY As Integer = &H1
'    Public Const FILE_MAP_WRITE As Integer = &H2
'    Public Const FILE_MAP_READ As Integer = &H4
'    Public Const FILE_ALL_ACCESS As Integer = &HF0000 Or &H100000 Or &H1FF
    
    
    
    
'    Public Const grp1 As Integer = &H430
'    Public Const grp2 As Integer = &H431
'    Public Const grp3 As Integer = &H432
'    Public Const grp4 As Integer = &H433
'    Public Const GCS_COMPREADSTR As Integer = &H1
'    Public Const GCS_COMPREADATTR As Integer = &H2
'    Public Const GCS_COMPREADCLAUSE As Integer = &H4
'    Public Const GCS_COMPSTR As Integer = &H8
'    Public Const GCS_COMPATTR As Integer = &H10
'    Public Const GCS_COMPCLAUSE As Integer = &H20
'    Public Const GCS_CURSORPOS As Integer = &H80
'    Public Const GCS_DELTASTART As Integer = &H100
'    Public Const GCS_RESULTREADSTR As Integer = &H200
'    Public Const GCS_RESULTREADCLAUSE As Integer = &H400
'    Public Const GCS_RESULTSTR As Integer = &H800
'    Public Const GCS_RESULTCLAUSE As Integer = &H1000
'    Public Const GGL_LEVEL As Integer = &H1
'    Public Const GGL_INDEX As Integer = &H2
'    Public Const GGL_STRING As Integer = &H3
'    Public Const GGL_PRIVATE As Integer = &H4
'    Public Const GL_LEVEL_NOGUIDELINE As Integer = &H0
'    Public Const GL_LEVEL_FATAL As Integer = &H1
'    Public Const GL_LEVEL_ERROR As Integer = &H2
'    Public Const GL_LEVEL_WARNING As Integer = &H3
'    Public Const GL_LEVEL_INFORMATION As Integer = &H4
'    Public Const GL_ID_UNKNOWN As Integer = &H0
'    Public Const GL_ID_NOMODULE As Integer = &H1
'    Public Const GL_ID_NODICTIONARY As Integer = &H10
'    Public Const GL_ID_CANNOTSAVE As Integer = &H11
'    Public Const GL_ID_NOCONVERT As Integer = &H20
'    Public Const GL_ID_TYPINGERROR As Integer = &H21
'    Public Const GL_ID_TOOMANYSTROKE As Integer = &H22
'    Public Const GL_ID_READINGCONFLICT As Integer = &H23
'    Public Const GL_ID_INPUTREADING As Integer = &H24
'    Public Const GL_ID_INPUTRADICAL As Integer = &H25
'    Public Const GL_ID_INPUTCODE As Integer = &H26
'    Public Const GL_ID_INPUTSYMBOL As Integer = &H27
'    Public Const GL_ID_CHOOSECANDIDATE As Integer = &H28
'    Public Const GL_ID_REVERSECONVERSION As Integer = &H29
'    Public Const GL_ID_PRIVATE_FIRST As Integer = &H8000
'    Public Const GL_ID_PRIVATE_LAST As Integer = &HFFFF
'    Public Const GCL_CONVERSION As Integer = &H1
'    Public Const GCL_REVERSECONVERSION As Integer = &H2
'    Public Const GCL_REVERSE_LENGTH As Integer = &H3
'    Public Const GROUP_NAME As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        GCL_REVERSE_LENGTH = 0x0003,
'    '        GROUP_NAME = unchecked((int)0x80),
'    '----------------------^--- GenCode(token): unexpected token type
'    Public Const GMEM_FIXED As Integer = &H0
'    Public Const GMEM_MOVEABLE As Integer = &H2
'    Public Const GMEM_NOCOMPACT As Integer = &H10
'    Public Const GMEM_NODISCARD As Integer = &H20
'    Public Const GMEM_ZEROINIT As Integer = &H40
'    Public Const GMEM_MODIFY As Integer = &H80
'    Public Const GMEM_DISCARDABLE As Integer = &H100
'    Public Const GMEM_NOT_BANKED As Integer = &H1000
'    Public Const GMEM_SHARE As Integer = &H2000
'    Public Const GMEM_DDESHARE As Integer = &H2000
'    Public Const GMEM_NOTIFY As Integer = &H4000
'    Public Const GMEM_LOWER As Integer = &H1000
'    Public Const GMEM_VALID_FLAGS As Integer = &H7F72
'    Public Const GMEM_INVALID_HANDLE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        GMEM_VALID_FLAGS = 0x7F72,
'    '        GMEM_INVALID_HANDLE = unchecked((int)0x8000),
'    '-------------------------------^--- GenCode(token): unexpected token type
'    Public Const GHND As Integer = &H2 Or &H40
'    Public Const GPTR As Integer = &H0 Or &H40
'    Public Const GMEM_DISCARDED As Integer = &H4000
'    Public Const GMEM_LOCKCOUNT As Integer = &HFF
'    Public Const GET_TAPE_MEDIA_INFORMATION As Integer = 0
'    Public Const GET_TAPE_DRIVE_INFORMATION As Integer = 1
'    Public Const GDI_ERROR As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        GET_TAPE_DRIVE_INFORMATION = 1,
'    '        GDI_ERROR = (unchecked((int)0xFFFFFFFF)),
'    '----------------------^--- GenCode(token): unexpected token type
'    Public Const GETCOLORTABLE As Integer = 5
'    Public Const GETPHYSPAGESIZE As Integer = 12
'    Public Const GETPRINTINGOFFSET As Integer = 13
'    Public Const GETSCALINGFACTOR As Integer = 14
'    Public Const GETPENWIDTH As Integer = 16
'    Public Const GETTECHNOLGY As Integer = 20
'    Public Const GETTECHNOLOGY As Integer = 20
'    Public Const GETVECTORPENSIZE As Integer = 26
'    Public Const GETVECTORBRUSHSIZE As Integer = 27
'    Public Const GETSETPAPERBINS As Integer = 29
'    Public Const GETSETPRINTORIENT As Integer = 30
'    Public Const GETSETPAPERMETRICS As Integer = 35
'    Public Const GETDEVICEUNITS As Integer = 42
'    Public Const GETEXTENDEDTEXTMETRICS As Integer = 256
'    Public Const GETEXTENTTABLE As Integer = 257
'    Public Const GETPAIRKERNTABLE As Integer = 258
'    Public Const GETTRACKKERNTABLE As Integer = 259
'    Public Const GETFACENAME As Integer = 513
'    Public Const GETSETSCREENPARAMS As Integer = 3072
'    Public Const GB2312_CHARSET As Integer = 134
'    Public Const GREEK_CHARSET As Integer = 161
'    Public Const GM_COMPATIBLE As Integer = 1
'    Public Const GM_ADVANCED As Integer = 2
'    Public Const GM_LAST As Integer = 2
'    Public Const GRAY_BRUSH As Integer = 2
'    Public Const GGO_METRICS As Integer = 0
'    Public Const GGO_BITMAP As Integer = 1
'    Public Const GGO_NATIVE As Integer = 2
'    Public Const GGO_GRAY2_BITMAP As Integer = 4
'    Public Const GGO_GRAY4_BITMAP As Integer = 5
'    Public Const GGO_GRAY8_BITMAP As Integer = 6
'    Public Const GGO_GLYPH_INDEX As Integer = &H80
'    Public Const GCP_DBCS As Integer = &H1
'    Public Const GCP_REORDER As Integer = &H2
'    Public Const GCP_USEKERNING As Integer = &H8
'    Public Const GCP_GLYPHSHAPE As Integer = &H10
'    Public Const GCP_LIGATE As Integer = &H20
'    Public Const GCP_DIACRITIC As Integer = &H100
'    Public Const GCP_KASHIDA As Integer = &H400
'    Public Const GCP_ERROR As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        GCP_KASHIDA = 0x0400,
'    '        GCP_ERROR = unchecked((int)0x8000),
'    '---------------------^--- GenCode(token): unexpected token type
'    Public Const GCP_JUSTIFY As Integer = &H10000
'    Public Const GCP_CLASSIN As Integer = &H80000
'    Public Const GCP_MAXEXTENT As Integer = &H100000
'    Public Const GCP_JUSTIFYIN As Integer = &H200000
'    Public Const GCP_DISPLAYZWG As Integer = &H400000
'    Public Const GCP_SYMSWAPOFF As Integer = &H800000
'    Public Const GCP_NUMERICOVERRIDE As Integer = &H1000000
'    Public Const GCP_NEUTRALOVERRIDE As Integer = &H2000000
'    Public Const GCP_NUMERICSLATIN As Integer = &H4000000
'    Public Const GCP_NUMERICSLOCAL As Integer = &H8000000
'    Public Const GCPCLASS_LATIN As Integer = 1
'    Public Const GCPCLASS_HEBREW As Integer = 2
'    Public Const GCPCLASS_ARABIC As Integer = 2
'    Public Const GCPCLASS_NEUTRAL As Integer = 3
'    Public Const GCPCLASS_LOCALNUMBER As Integer = 4
'    Public Const GCPCLASS_LATINNUMBER As Integer = 5
'    Public Const GCPCLASS_LATINNUMERICTERMINATOR As Integer = 6
'    Public Const GCPCLASS_LATINNUMERICSEPARATOR As Integer = 7
'    Public Const GCPCLASS_NUMERICSEPARATOR As Integer = 8
'    Public Const GCPCLASS_PREBOUNDLTR As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        GCPCLASS_NUMERICSEPARATOR = 8,
'    '        GCPCLASS_PREBOUNDLTR = unchecked((int)0x80),
'    '--------------------------------^--- GenCode(token): unexpected token type
'    Public Const GCPCLASS_PREBOUNDRTL As Integer = &H40
'    Public Const GCPCLASS_POSTBOUNDLTR As Integer = &H20
'    Public Const GCPCLASS_POSTBOUNDRTL As Integer = &H10
'    Public Const GCPGLYPH_LINKBEFORE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        GCPCLASS_POSTBOUNDRTL = 0x10,
'    '        GCPGLYPH_LINKBEFORE = unchecked((int)0x8000),
'    '-------------------------------^--- GenCode(token): unexpected token type
'    Public Const GCPGLYPH_LINKAFTER As Integer = &H4000
'    Public Const GDICOMMENT_IDENTIFIER As Integer = &H43494447
'    Public Const GDICOMMENT_WINDOWS_METAFILE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        GDICOMMENT_IDENTIFIER = 0x43494447,
'    '        GDICOMMENT_WINDOWS_METAFILE = unchecked((int)0x80000001),
'    '---------------------------------------^--- GenCode(token): unexpected token type
'    Public Const GDICOMMENT_BEGINGROUP As Integer = &H2
'    Public Const GDICOMMENT_ENDGROUP As Integer = &H3
'    Public Const GDICOMMENT_MULTIFORMATS As Integer = &H40000004
'    Public Const GENERIC_READ As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        GDICOMMENT_MULTIFORMATS = 0x40000004,
'    '        GENERIC_READ = (unchecked((int)0x80000000)),
'    '-------------------------^--- GenCode(token): unexpected token type
'    Public Const GENERIC_WRITE As Integer = &H40000000
'    Public Const GENERIC_EXECUTE As Integer = &H20000000
'    Public Const GENERIC_ALL As Integer = &H10000000
'    Public Const GROUP_SECURITY_INFORMATION As Integer = 0
'    Private __unknown As X00000002
'    Private __unknown As __unknown
'    Private GWL_WNDPROC As __unknown = - 4
'    Private GWL_HINSTANCE As __unknown = - 6
'    Private GWL_HWNDPARENT As __unknown = - 8
'    Private GWL_STYLE As __unknown = - 16
'    Private GWL_EXSTYLE As __unknown = - 20
'    Private GWL_USERDATA As __unknown = - 21
'    Private GWL_ID As __unknown = - 12
'    Private GCL_MENUNAME As __unknown = - 8
'    Private GCL_HBRBACKGROUND As __unknown = - 10
'    Private GCL_HCURSOR As __unknown = - 12
'    Private GCL_HICON As __unknown = - 14
'    Private GCL_HMODULE As __unknown = - 16
'    Private GCL_CBWNDEXTRA As __unknown = - 18
'    Private GCL_CBCLSEXTRA As __unknown = - 20
'    Private GCL_WNDPROC As __unknown = - 24
'    Private GCL_STYLE As __unknown = - 26
'    Private GCW_ATOM As __unknown = - 32
'    Private GCL_HICONSM As __unknown = - 34
'    Private GMDI_USEDISABLED As __unknown = &H1
'    Private GMDI_GOINTOPOPUPS As __unknown = &H2
'    Private GW_HWNDFIRST As __unknown = 0
'    Private GW_HWNDLAST As __unknown = 1
'    Private GW_HWNDNEXT As __unknown = 2
'    Private GW_HWNDPREV As __unknown = 3
'    Private GW_OWNER As __unknown = 4
        Public Const GW_CHILD As UInteger = 5
'    Private GW_MAX As __unknown = 5
'    Private GMR_VISIBLE As __unknown = 0
'    Private GMR_DAYSTATE As __unknown = 1
'    Private GDTR_MIN As __unknown = &H1
'    Private GDTR_MAX As __unknown = &H2
'    Private GDT_ERROR As __unknown = - 1
'    Private GDT_VALID As __unknown = 0
'    Private GDT_NONE As __unknown = 1
'    Private __unknown As __unknown
    
    
    
'    Public Const HDATA_APPOWNED As Integer = &H1
'    Public Const HANGUP_PENDING As Integer = &H4
'    Public Const HANGUP_COMPLETE As Integer = &H5
'    Public Const HIGH_PRIORITY_Class As Integer = &H80
'    Public Const HANDLE_FLAG_INHERIT As Integer = &H1
'    Public Const HANDLE_FLAG_PROTECT_FROM_CLOSE As Integer = &H2
'    Public Const HINSTANCE_ERROR As Integer = 32
'    Public Const HW_PROFILE_GUIDLEN As Integer = 39
'    Public Const HP_ALGID As Integer = &H1
'    Public Const HP_HASHVAL As Integer = &H2
'    Public Const HP_HASHSIZE As Integer = &H4
'    Public Const HALFTONE As Integer = 4
'    Public Const HANGEUL_CHARSET As Integer = 129
'    Public Const HEBREW_CHARSET As Integer = 177
'    Public Const HOLLOW_BRUSH As Integer = 5
'    Public Const HS_HORIZONTAL As Integer = 0
'    Public Const HS_VERTICAL As Integer = 1
'    Public Const HS_FDIAGONAL As Integer = 2
'    Public Const HS_BDIAGONAL As Integer = 3
'    Public Const HS_CROSS As Integer = 4
'    Public Const HS_DIAGCROSS As Integer = 5
'    Public Const HORZSIZE As Integer = 4
'    Public Const HORZRES As Integer = 8
'    Public Const HEAP_NO_SERIALIZE As Integer = &H1
'    Public Const HEAP_GROWABLE As Integer = &H2
'    Public Const HEAP_GENERATE_EXCEPTIONS As Integer = &H4
'    Public Const HEAP_ZERO_MEMORY As Integer = &H8
'    Public Const HEAP_REALLOC_IN_PLACE_ONLY As Integer = &H10
'    Public Const HEAP_TAIL_CHECKING_ENABLED As Integer = &H20
'    Public Const HEAP_FREE_CHECKING_ENABLED As Integer = &H40
'    Public Const HEAP_DISABLE_COALESCE_ON_FREE As Integer = &H80
'    Public Const HEAP_CREATE_ALIGN_16 As Integer = &H10000
'    Public Const HEAP_CREATE_ENABLE_TRACING As Integer = &H20000
'    Public Const HEAP_MAXIMUM_TAG As Integer = &HFFF
'    Public Const HEAP_PSEUDO_TAG_FLAG As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        HEAP_MAXIMUM_TAG = 0x0FFF,
'    '        HEAP_PSEUDO_TAG_FLAG = unchecked((int)0x8000),
'    '--------------------------------^--- GenCode(token): unexpected token type
'    Public Const HEAP_TAG_SHIFT As Integer = 16
'    Public Const HIDE_WINDOW As Integer = 0
'    Public Const HC_ACTION As Integer = 0
'    Public Const HC_GETNEXT As Integer = 1
'    Public Const HC_SKIP As Integer = 2
'    Public Const HC_NOREMOVE As Integer = 3
'    Public Const HC_NOREM As Integer = 3
'    Public Const HC_SYSMODALON As Integer = 4
'    Public Const HC_SYSMODALOFF As Integer = 5
'    Public Const HCBT_MOVESIZE As Integer = 0
'    Public Const HCBT_MINMAX As Integer = 1
'    Public Const HCBT_QS As Integer = 2
'    Public Const HCBT_CREATEWND As Integer = 3
'    Public Const HCBT_DESTROYWND As Integer = 4
'    Public Const HCBT_ACTIVATE As Integer = 5
'    Public Const HCBT_CLICKSKIPPED As Integer = 6
'    Public Const HCBT_KEYSKIPPED As Integer = 7
'    Public Const HCBT_SYSCOMMAND As Integer = 8
'    Public Const HCBT_SETFOCUS As Integer = 9
'    Public Const HSHELL_WINDOWCREATED As Integer = 1
'    Public Const HSHELL_WINDOWDESTROYED As Integer = 2
'    Public Const HSHELL_ACTIVATESHELLWINDOW As Integer = 3
'    Public Const HSHELL_WINDOWACTIVATED As Integer = 4
'    Public Const HSHELL_GETMINRECT As Integer = 5
'    Public Const HSHELL_REDRAW As Integer = 6
'    Public Const HSHELL_TASKMAN As Integer = 7
'    Public Const HSHELL_LANGUAGE As Integer = 8
'    Public Const HKL_PREV As Integer = 0
'    Public Const HKL_NEXT As Integer = 1
'    Public Const HTERROR As Integer = - 2
'    Public Const HTTRANSPARENT As Integer = - 1
'    Public Const HTNOWHERE As Integer = 0
'    Public Const HTCLIENT As Integer = 1
'    Public Const HTCAPTION As Integer = 2
'    Public Const HTSYSMENU As Integer = 3
'    Public Const HTGROWBOX As Integer = 4
'    Public Const HTSIZE As Integer = 4
'    Public Const HTMENU As Integer = 5
'    Public Const HTHSCROLL As Integer = 6
'    Public Const HTVSCROLL As Integer = 7
'    Public Const HTMINBUTTON As Integer = 8
'    Public Const HTMAXBUTTON As Integer = 9
'    Public Const HTLEFT As Integer = 10
'    Public Const HTRIGHT As Integer = 11
'    Public Const HTTOP As Integer = 12
'    Public Const HTTOPLEFT As Integer = 13
'    Public Const HTTOPRIGHT As Integer = 14
'    Public Const HTBOTTOM As Integer = 15
'    Public Const HTBOTTOMLEFT As Integer = 16
'    Public Const HTBOTTOMRIGHT As Integer = 17
'    Public Const HTBORDER As Integer = 18
'    Public Const HTREDUCE As Integer = 8
'    Public Const HTZOOM As Integer = 9
'    Public Const HTSIZEFIRST As Integer = 10
'    Public Const HTSIZELAST As Integer = 17
'    Public Const HTOBJECT As Integer = 19
'    Public Const HTCLOSE As Integer = 20
'    Public Const HTHELP As Integer = 21
'    Public Const HOVER_DEFAULT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        HTHELP = 21,
'    '        HOVER_DEFAULT = unchecked((int)0xFFFFFFFF),
'    '-------------------------^--- GenCode(token): unexpected token type
'    Public Const HELPINFO_WINDOW As Integer = &H1
'    Public Const HELPINFO_MENUITEM As Integer = &H2
'    Public Const HELP_CONTEXT As Integer = &H1
'    Public Const HELP_QUIT As Integer = &H2
'    Public Const HELP_INDEX As Integer = &H3
'    Public Const HELP_CONTENTS As Integer = &H3
'    Public Const HELP_HELPONHELP As Integer = &H4
'    Public Const HELP_SETINDEX As Integer = &H5
'    Public Const HELP_SETCONTENTS As Integer = &H5
'    Public Const HELP_CONTEXTPOPUP As Integer = &H8
'    Public Const HELP_FORCEFILE As Integer = &H9
'    Public Const HELP_KEY As Integer = &H101
'    Public Const HELP_COMMAND As Integer = &H102
'    Public Const HELP_PARTIALKEY As Integer = &H105
'    Public Const HELP_MULTIKEY As Integer = &H201
'    Public Const HELP_SETWINPOS As Integer = &H203
'    Public Const HELP_CONTEXTMENU As Integer = &HA
'    Public Const HELP_FINDER As Integer = &HB
        '    Public Const HELP_WM_HELP As Integer = &HC
'    Public Const HELP_SETPOPUP_POS As Integer = &HD
'    Public Const HELP_TCARD As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        HELP_SETPOPUP_POS = 0x000d,
'    '        HELP_TCARD = unchecked((int)0x8000),
'    '----------------------^--- GenCode(token): unexpected token type
'    Public Const HELP_TCARD_DATA As Integer = &H10
'    Public Const HELP_TCARD_OTHER_CALLER As Integer = &H11
'    Public Const HCF_HIGHCONTRASTON As Integer = &H1
'    Public Const HCF_AVAILABLE As Integer = &H2
'    Public Const HCF_HOTKEYACTIVE As Integer = &H4
'    Public Const HCF_CONFIRMHOTKEY As Integer = &H8
'    Public Const HCF_HOTKEYSOUND As Integer = &H10
'    Public Const HCF_INDICATOR As Integer = &H20
'    Public Const HCF_HOTKEYAVAILABLE As Integer = &H40
'    Public Const HDM_FIRST As Integer = &H1200
'    Public Const HDN_FIRST As Integer = 0 - 300
'    Public Const HDN_LAST As Integer = 0 - 399
'    Public Const HDS_HORZ As Integer = &H0
'    Public Const HDS_BUTTONS As Integer = &H2
'    Public Const HDS_HOTTRACK As Integer = &H4
'    Public Const HDS_HIDDEN As Integer = &H8
'    Public Const HDS_DRAGDROP As Integer = &H40
'    Public Const HDS_FULLDRAG As Integer = &H80
'    Public Const HDI_WIDTH As Integer = &H1
'    Public Const HDI_HEIGHT As Integer = &H1
     Public Const HDI_TEXT As Integer = &H2
     Public Const HDI_FORMAT As Integer = &H4
'    Public Const HDI_LPARAM As Integer = &H8
'    Public Const HDI_BITMAP As Integer = &H10
     Public Const HDI_IMAGE As Integer = &H20
'    Public Const HDI_DI_SETITEM As Integer = &H40
'    Public Const HDI_ORDER As Integer = &H80
'    Public Const HDF_LEFT As Integer = 0
'    Public Const HDF_RIGHT As Integer = 1
'    Public Const HDF_CENTER As Integer = 2
'    Public Const HDF_JUSTIFYMASK As Integer = &H3
'    Public Const HDF_RTLREADING As Integer = 4
'    Public Const HDF_OWNERDRAW As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        HDF_RTLREADING = 4,
'    '        HDF_OWNERDRAW = unchecked((int)0x8000),
'    '-------------------------^--- GenCode(token): unexpected token type
     Public Const HDF_STRING As Integer = &H4000
'    Public Const HDF_BITMAP As Integer = &H2000
     Public Const HDF_BITMAP_ON_RIGHT As Integer = &H1000
     Public Const HDF_IMAGE As Integer = &H800
'    Public Const HDM_GETITEMCOUNT As Integer = &H1200 + 0
'    Public Const HDM_INSERTITEMA As Integer = &H1200 + 1
'    Public Const HDM_INSERTITEMW As Integer = &H1200 + 10
'    Public Const HDM_DELETEITEM As Integer = &H1200 + 2
'    Public Const HDM_GETITEMA As Integer = &H1200 + 3
'    Public Const HDM_GETITEMW As Integer = &H1200 + 11
'    Public Const HDM_SETITEMA As Integer = &H1200 + 4
     Public Const HDM_SETITEMW As Integer = &H1200 + 12
'    Public Const HDM_LAYOUT As Integer = &H1200 + 5
'    Public Const HHT_NOWHERE As Integer = &H1
'    Public Const HHT_ONHEADER As Integer = &H2
'    Public Const HHT_ONDIVIDER As Integer = &H4
'    Public Const HHT_ONDIVOPEN As Integer = &H8
'    Public Const HHT_ABOVE As Integer = &H100
'    Public Const HHT_BELOW As Integer = &H200
'    Public Const HHT_TORIGHT As Integer = &H400
'    Public Const HHT_TOLEFT As Integer = &H800
'    Public Const HDM_HITTEST As Integer = &H1200 + 6
'    Public Const HDM_GETITEMRECT As Integer = &H1200 + 7
     Public Const HDM_SETIMAGELIST As Integer = &H1200 + 8
'    Public Const HDM_GETIMAGELIST As Integer = &H1200 + 9
'    Public Const HDM_ORDERTOINDEX As Integer = &H1200 + 15
'    Public Const HDM_CREATEDRAGIMAGE As Integer = &H1200 + 16
'    Public Const HDM_GETORDERARRAY As Integer = &H1200 + 17
'    Public Const HDM_SETORDERARRAY As Integer = &H1200 + 18
'    Public Const HDM_SETHOTDIVIDER As Integer = &H1200 + 19
'    Public Const HDN_ITEMCHANGINGA As Integer = 0 - 300 - 0
'    Public Const HDN_ITEMCHANGINGW As Integer = 0 - 300 - 20
'    Public Const HDN_ITEMCHANGEDA As Integer = 0 - 300 - 1
'    Public Const HDN_ITEMCHANGEDW As Integer = 0 - 300 - 21
'    Public Const HDN_ITEMCLICKA As Integer = 0 - 300 - 2
'    Public Const HDN_ITEMCLICKW As Integer = 0 - 300 - 22
'    Public Const HDN_ITEMDBLCLICKA As Integer = 0 - 300 - 3
'    Public Const HDN_ITEMDBLCLICKW As Integer = 0 - 300 - 23
'    Public Const HDN_DIVIDERDBLCLICKA As Integer = 0 - 300 - 5
'    Public Const HDN_DIVIDERDBLCLICKW As Integer = 0 - 300 - 25
'    Public Const HDN_BEGINTRACKA As Integer = 0 - 300 - 6
'    Public Const HDN_BEGINTRACKW As Integer = 0 - 300 - 26
'    Public Const HDN_ENDTRACKA As Integer = 0 - 300 - 7
'    Public Const HDN_ENDTRACKW As Integer = 0 - 300 - 27
'    Public Const HDN_TRACKA As Integer = 0 - 300 - 8
'    Public Const HDN_TRACKW As Integer = 0 - 300 - 28
'    Public Const HDN_GETDISPINFOA As Integer = 0 - 300 - 9
'    Public Const HDN_GETDISPINFOW As Integer = 0 - 300 - 29
'    Public Const HDN_BEGINDRAG As Integer = 0 - 300 - 10
'    Public Const HDN_ENDDRAG As Integer = 0 - 300 - 11
'    Public Const HIST_BACK As Integer = 0
'    Public Const HIST_FORWARD As Integer = 1
'    Public Const HIST_FAVORITES As Integer = 2
'    Public Const HIST_ADDTOFAVORITES As Integer = 3
'    Public Const HIST_VIEWTREE As Integer = 4
'    Public Const HOTKEYF_SHIFT As Integer = &H1
'    Public Const HOTKEYF_CONTROL As Integer = &H2
'    Public Const HOTKEYF_ALT As Integer = &H4
'    Public Const HOTKEYF_EXT As Integer = &H8
'    Public Const HKCOMB_NONE As Integer = &H1
'    Public Const HKCOMB_S As Integer = &H2
'    Public Const HKCOMB_C As Integer = &H4
'    Public Const HKCOMB_A As Integer = &H8
'    Public Const HKCOMB_SC As Integer = &H10
'    Public Const HKCOMB_SA As Integer = &H20
'    Public Const HKCOMB_CA As Integer = &H40
'    Public Const HKCOMB_SCA As Integer = &H80
'    Public Const HKM_SETHOTKEY As Integer = &H400 + 1
'    Public Const HKM_GETHOTKEY As Integer = &H400 + 2
'    Public Const HKM_SETRULES As Integer = &H400 + 3
'    Public Const HWND_TOP As Integer = 0
'    Public Const HWND_BOTTOM As Integer = 1
'    Public Const HWND_TOPMOST As Integer = - 1
'    Public Const HWND_NOTOPMOST As Integer = - 2
'    Public Const HICF_OTHER As Integer = &H0
'    Public Const HICF_MOUSE As Integer = &H1
'    Public Const HICF_ARROWKEYS As Integer = &H2
'    Public Const HICF_ACCELERATOR As Integer = &H4
'    Public Const HICF_DUPACCEL As Integer = &H8
'    Public Const HICF_ENTERING As Integer = &H10
'    Public Const HICF_LEAVING As Integer = &H20
'    Public Const HICF_RESELECT As Integer = &H40
'    Public Const HICF_TOGGLEDROPDOWN As Integer = &H100
'    Public Const HINST_COMMCTRL As Integer = - 1
    
    
    
'    Public Const ITALIC_FONTTYPE As Integer = &H200
'    Public Const ico1 As Integer = &H43C
'    Public Const ico2 As Integer = &H43D
'    Public Const ico3 As Integer = &H43E
'    Public Const ico4 As Integer = &H43F
'    Public Const IMC_GETCANDIDATEPOS As Integer = &H7
'    Public Const IMC_SETCANDIDATEPOS As Integer = &H8
'    Public Const IMC_GETCOMPOSITIONFONT As Integer = &H9
'    Public Const IMC_SETCOMPOSITIONFONT As Integer = &HA
'    Public Const IMC_GETCOMPOSITIONWINDOW As Integer = &HB
'    Public Const IMC_SETCOMPOSITIONWINDOW As Integer = &HC
'    Public Const IMC_GETSTATUSWINDOWPOS As Integer = &HF
'    Public Const IMC_SETSTATUSWINDOWPOS As Integer = &H10
'    Public Const IMC_CLOSESTATUSWINDOW As Integer = &H21
'    Public Const IMC_OPENSTATUSWINDOW As Integer = &H22
'    Public Const ISC_SHOWUICANDIDATEWINDOW As Integer = &H1
'    Public Const ISC_SHOWUICOMPOSITIONWINDOW As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        ISC_SHOWUICANDIDATEWINDOW = 0x00000001,
'    '        ISC_SHOWUICOMPOSITIONWINDOW = unchecked((int)0x80000000),
'    '---------------------------------------^--- GenCode(token): unexpected token type
'    Public Const ISC_SHOWUIGUIDELINE As Integer = &H40000000
'    Public Const ISC_SHOWUIALLCANDIDATEWINDOW As Integer = &HF
'    Public Const ISC_SHOWUIALL As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        ISC_SHOWUIALLCANDIDATEWINDOW = 0x0000000F,
'    '        ISC_SHOWUIALL = unchecked((int)0xC000000F),
'    '-------------------------^--- GenCode(token): unexpected token type
'    Public Const IME_CHOTKEY_IME_NONIME_TOGGLE As Integer = &H10
'    Public Const IME_CHOTKEY_SHAPE_TOGGLE As Integer = &H11
'    Public Const IME_CHOTKEY_SYMBOL_TOGGLE As Integer = &H12
'    Public Const IME_JHOTKEY_CLOSE_OPEN As Integer = &H30
'    Public Const IME_KHOTKEY_SHAPE_TOGGLE As Integer = &H50
'    Public Const IME_KHOTKEY_HANJACONVERT As Integer = &H51
'    Public Const IME_KHOTKEY_ENGLISH As Integer = &H52
'    Public Const IME_THOTKEY_IME_NONIME_TOGGLE As Integer = &H70
'    Public Const IME_THOTKEY_SHAPE_TOGGLE As Integer = &H71
'    Public Const IME_THOTKEY_SYMBOL_TOGGLE As Integer = &H72
'    Public Const IME_HOTKEY_DSWITCH_FIRST As Integer = &H100
'    Public Const IME_HOTKEY_DSWITCH_LAST As Integer = &H11F
'    Public Const IME_HOTKEY_PRIVATE_FIRST As Integer = &H200
'    Public Const IME_ITHOTKEY_RESEND_RESULTSTR As Integer = &H200
'    Public Const IME_ITHOTKEY_PREVIOUS_COMPOSITION As Integer = &H201
'    Public Const IME_ITHOTKEY_UISTYLE_TOGGLE As Integer = &H202
'    Public Const IME_HOTKEY_PRIVATE_LAST As Integer = &H21F
'    Public Const IMEVER_0310 As Integer = &H3000A
'    Public Const IMEVER_0400 As Integer = &H40000
'    Public Const IME_PROP_AT_CARET As Integer = &H10000
'    Public Const IME_PROP_SPECIAL_UI As Integer = &H20000
'    Public Const IME_PROP_CANDLIST_START_FROM_1 As Integer = &H40000
'    Public Const IME_PROP_UNICODE As Integer = &H80000
'    Public Const IGP_PROPERTY As Integer = &H4
'    Public Const IGP_CONVERSION As Integer = &H8
'    Public Const IGP_SENTENCE As Integer = &HC
'    Public Const IGP_UI As Integer = &H10
'    Public Const IGP_SETCOMPSTR As Integer = &H14
'    Public Const IGP_SELECT As Integer = &H18
'    Public Const IME_CMODE_ALPHANUMERIC As Integer = &H0
'    Public Const IME_CMODE_NATIVE As Integer = &H1
'    Public Const IME_CMODE_CHINESE As Integer = &H1
'    Public Const IME_CMODE_HANGEUL As Integer = &H1
'    Public Const IME_CMODE_HANGUL As Integer = &H1
'    Public Const IME_CMODE_JAPANESE As Integer = &H1
'    Public Const IME_CMODE_KATAKANA As Integer = &H2
'    Public Const IME_CMODE_LANGUAGE As Integer = &H3
'    Public Const IME_CMODE_FULLSHAPE As Integer = &H8
'    Public Const IME_CMODE_ROMAN As Integer = &H10
'    Public Const IME_CMODE_CHARCODE As Integer = &H20
'    Public Const IME_CMODE_HANJACONVERT As Integer = &H40
'    Public Const IME_CMODE_SOFTKBD As Integer = &H80
'    Public Const IME_CMODE_NOCONVERSION As Integer = &H100
'    Public Const IME_CMODE_EUDC As Integer = &H200
'    Public Const IME_CMODE_SYMBOL As Integer = &H400
'    Public Const IME_SMODE_NONE As Integer = &H0
'    Public Const IME_SMODE_PLAURALCLAUSE As Integer = &H1
'    Public Const IME_SMODE_SINGLECONVERT As Integer = &H2
'    Public Const IME_SMODE_AUTOMATIC As Integer = &H4
'    Public Const IME_SMODE_PHRASEPREDICT As Integer = &H8
'    Public Const IME_CAND_UNKNOWN As Integer = &H0
'    Public Const IME_CAND_READ As Integer = &H1
'    Public Const IME_CAND_CODE As Integer = &H2
'    Public Const IME_CAND_MEANING As Integer = &H3
'    Public Const IME_CAND_RADICAL As Integer = &H4
'    Public Const IME_CAND_STROKE As Integer = &H5
'    Public Const IMN_CLOSESTATUSWINDOW As Integer = &H1
'    Public Const IMN_OPENSTATUSWINDOW As Integer = &H2
'    Public Const IMN_CHANGECANDIDATE As Integer = &H3
'    Public Const IMN_CLOSECANDIDATE As Integer = &H4
'    Public Const IMN_OPENCANDIDATE As Integer = &H5
'    Public Const IMN_SETCONVERSIONMODE As Integer = &H6
'    Public Const IMN_SETSENTENCEMODE As Integer = &H7
'    Public Const IMN_SETOPENSTATUS As Integer = &H8
'    Public Const IMN_SETCANDIDATEPOS As Integer = &H9
'    Public Const IMN_SETCOMPOSITIONFONT As Integer = &HA
'    Public Const IMN_SETCOMPOSITIONWINDOW As Integer = &HB
'    Public Const IMN_SETSTATUSWINDOWPOS As Integer = &HC
'    Public Const IMN_GUIDELINE As Integer = &HD
'    Public Const IMN_PRIVATE As Integer = &HE
'    Public Const IMM_ERROR_NODATA As Integer = - 1
'    Public Const IMM_ERROR_GENERAL As Integer = - 2
'    Public Const IME_CONFIG_GENERAL As Integer = 1
'    Public Const IME_CONFIG_REGISTERWORD As Integer = 2
'    Public Const IME_CONFIG_SELECTDICTIONARY As Integer = 3
'    Public Const IME_ESC_QUERY_SUPPORT As Integer = &H3
'    Public Const IME_ESC_RESERVED_FIRST As Integer = &H4
'    Public Const IME_ESC_RESERVED_LAST As Integer = &H7FF
'    Public Const IME_ESC_PRIVATE_FIRST As Integer = &H800
'    Public Const IME_ESC_PRIVATE_LAST As Integer = &HFFF
'    Public Const IME_ESC_SEQUENCE_TO_INTERNAL As Integer = &H1001
'    Public Const IME_ESC_GET_EUDC_DICTIONARY As Integer = &H1003
'    Public Const IME_ESC_SET_EUDC_DICTIONARY As Integer = &H1004
'    Public Const IME_ESC_MAX_KEY As Integer = &H1005
'    Public Const IME_ESC_IME_NAME As Integer = &H1006
'    Public Const IME_ESC_SYNC_HOTKEY As Integer = &H1007
'    Public Const IME_ESC_HANJA_MODE As Integer = &H1008
'    Public Const IME_ESC_AUTOMATA As Integer = &H1009
'    Public Const IME_ESC_PRIVATE_HOTKEY As Integer = &H100A
'    Public Const IME_REGWORD_STYLE_EUDC As Integer = &H1
'    Public Const IME_REGWORD_STYLE_USER_FIRST As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        IME_REGWORD_STYLE_EUDC = 0x00000001,
'    '        IME_REGWORD_STYLE_USER_FIRST = unchecked((int)0x80000000),
'    '----------------------------------------^--- GenCode(token): unexpected token type
'    Public Const IME_REGWORD_STYLE_USER_LAST As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        IME_REGWORD_STYLE_USER_FIRST = unchecked((int)0x80000000),
'    '        IME_REGWORD_STYLE_USER_LAST = unchecked((int)0xFFFFFFFF),
'    '---------------------------------------^--- GenCode(token): unexpected token type
'    Public Const IDLFLAG_NONE As Integer = 0
'    Public Const IDLFLAG_FIN As Integer = &H1
'    Public Const IDLFLAG_FOUT As Integer = &H2
'    Public Const IDLFLAG_FLCID As Integer = &H4
'    Public Const IDLFLAG_FRETVAL As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                    IDLFLAG_FLCID = ((0x4)),
'    '                                                                    IDLFLAG_FRETVAL = ((unchecked((int)0x8))),
'    '-----------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const IMPLTYPEFLAG_FDEFAULT As Integer = &H1
'    Public Const IMPLTYPEFLAG_FSOURCE As Integer = &H2
'    Public Const IMPLTYPEFLAG_FRESTRICTED As Integer = &H4
'    Public Const IMPLTYPEFLAG_FDEFAULTVTABLE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                     IMPLTYPEFLAG_FRESTRICTED = (0x4),
'    '                                                                                                                                                                IMPLTYPEFLAG_FDEFAULTVTABLE = (unchecked((int)0x8)),
'    '------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const ID_DEFAULTINST As Integer = - 2
'    Public Const ID_PSRESTARTWINDOWS As Integer = &H2
'    Public Const ID_PSREBOOTSYSTEM As Integer = &H2 Or &H1
'    Public Const IDLE_PRIORITY_Class As Integer = &H40
'    Public Const IGNORE As Integer = 0
'    Public Const INFINITE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        IGNORE = 0,
'    '        INFINITE = unchecked((int)0xFFFFFFFF),
'    '--------------------^--- GenCode(token): unexpected token type
'    Public Const IE_BADID As Integer = - 1
'    Public Const IE_OPEN As Integer = - 2
'    Public Const IE_NOPEN As Integer = - 3
'    Public Const IE_MEMORY As Integer = - 4
'    Public Const IE_DEFAULT As Integer = - 5
'    Public Const IE_HARDWARE As Integer = - 10
'    Public Const IE_BYTESIZE As Integer = - 11
'    Public Const IE_BAUDRATE As Integer = - 12
'    Public Const INPLACE_E_NOTUNDOABLE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                             IE_BAUDRATE = (-12),
'    '                                                                                                           INPLACE_E_NOTUNDOABLE = unchecked((int)0x800401A0),
'    '------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const INPLACE_E_NOTOOLSPACE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                           INPLACE_E_NOTUNDOABLE = unchecked((int)0x800401A0),
'    '        INPLACE_E_NOTOOLSPACE = unchecked((int)0x800401A1),
'    '---------------------------------^--- GenCode(token): unexpected token type
'    Public Const INPLACE_E_FIRST As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        INPLACE_E_NOTOOLSPACE = unchecked((int)0x800401A1),
'    '        INPLACE_E_FIRST = unchecked((int)0x800401A0),
'    '---------------------------^--- GenCode(token): unexpected token type
'    Public Const INPLACE_E_LAST As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        INPLACE_E_FIRST = unchecked((int)0x800401A0),
'    '        INPLACE_E_LAST = unchecked((int)0x800401AF),
'    '--------------------------^--- GenCode(token): unexpected token type
'    Public Const INPLACE_S_FIRST As Integer = &H401A0
'    Public Const INPLACE_S_LAST As Integer = &H401AF
'    Public Const INPLACE_S_TRUNCATED As Integer = &H401A0
'    Public Const INPUTLANGCHANGE_SYSCHARSET As Integer = &H1
'    Public Const INPUTLANGCHANGE_FORWARD As Integer = &H2
'    Public Const INPUTLANGCHANGE_BACKWARD As Integer = &H4
'    Public Const ILLUMINANT_DEVICE_DEFAULT As Integer = 0
'    Public Const ILLUMINANT_A As Integer = 1
'    Public Const ILLUMINANT_B As Integer = 2
'    Public Const ILLUMINANT_C As Integer = 3
'    Public Const ILLUMINANT_D50 As Integer = 4
'    Public Const ILLUMINANT_D55 As Integer = 5
'    Public Const ILLUMINANT_D65 As Integer = 6
'    Public Const ILLUMINANT_D75 As Integer = 7
'    Public Const ILLUMINANT_F2 As Integer = 8
'    Public Const ILLUMINANT_MAX_INDEX As Integer = 8
'    Public Const ILLUMINANT_TUNGSTEN As Integer = 1
'    Public Const ILLUMINANT_DAYLIGHT As Integer = 3
'    Public Const ILLUMINANT_FLUORESCENT As Integer = 8
'    Public Const ILLUMINANT_NTSC As Integer = 3
'    Public Const ICM_OFF As Integer = 1
'    Public Const ICM_ON As Integer = 2
'    Public Const ICM_QUERY As Integer = 3
'    Public Const IO_COMPLETION_MODIFY_STATE As Integer = &H2
'    Public Const INHERIT_ONLY_ACE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        IO_COMPLETION_MODIFY_STATE = 0x0002,
'    '        INHERIT_ONLY_ACE = (unchecked((int)0x8)),
'    '-----------------------------^--- GenCode(token): unexpected token type
'    Public Const IMAGE_DOS_SIGNATURE As Integer = &H5A4D
'    Public Const IMAGE_OS2_SIGNATURE As Integer = &H454E
'    Public Const IMAGE_OS2_SIGNATURE_LE As Integer = &H454C
'    Public Const IMAGE_VXD_SIGNATURE As Integer = &H454C
'    Public Const IMAGE_NT_SIGNATURE As Integer = &H4550
'    Public Const IMAGE_SIZEOF_FILE_HEADER As Integer = 20
'    Public Const IMAGE_FILE_RELOCS_STRIPPED As Integer = &H1
'    Public Const IMAGE_FILE_LINE_NUMS_STRIPPED As Integer = &H4
'    Public Const IMAGE_FILE_LOCAL_SYMS_STRIPPED As Integer = &H8
'    Public Const IMAGE_FILE_AGGRESIVE_WS_TRIM As Integer = &H10
'    Public Const IMAGE_FILE_BYTES_REVERSED_LO As Integer = &H80
'    Public Const IMAGE_FILE_32BIT_MACHINE As Integer = &H100
'    Public Const IMAGE_FILE_DEBUG_STRIPPED As Integer = &H200
'    Public Const IMAGE_FILE_REMOVABLE_RUN_FROM_SWAP As Integer = &H400
'    Public Const IMAGE_FILE_NET_RUN_FROM_SWAP As Integer = &H800
'    Public Const IMAGE_FILE_SYSTEM As Integer = &H1000
'    Public Const IMAGE_FILE_DLL As Integer = &H2000
'    Public Const IMAGE_FILE_UP_SYSTEM_ONLY As Integer = &H4000
'    Public Const IMAGE_FILE_BYTES_REVERSED_HI As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        IMAGE_FILE_UP_SYSTEM_ONLY = 0x4000,
'    '        IMAGE_FILE_BYTES_REVERSED_HI = unchecked((int)0x8000),
'    '----------------------------------------^--- GenCode(token): unexpected token type
'    Public Const IMAGE_FILE_MACHINE_UNKNOWN As Integer = 0
'    Public Const IMAGE_FILE_MACHINE_I386 As Integer = &H14C
'    Public Const IMAGE_FILE_MACHINE_R3000 As Integer = &H162
'    Public Const IMAGE_FILE_MACHINE_R4000 As Integer = &H166
'    Public Const IMAGE_FILE_MACHINE_R10000 As Integer = &H168
'    Public Const IMAGE_FILE_MACHINE_ALPHA As Integer = &H184
'    Public Const IMAGE_FILE_MACHINE_POWERPC As Integer = &H1F0
'    Public Const IMAGE_NUMBEROF_DIRECTORY_ENTRIES As Integer = 16
'    Public Const IMAGE_SIZEOF_ROM_OPTIONAL_HEADER As Integer = 56
'    Public Const IMAGE_SIZEOF_STD_OPTIONAL_HEADER As Integer = 28
'    Public Const IMAGE_SIZEOF_NT_OPTIONAL_HEADER As Integer = 224
'    Public Const IMAGE_NT_OPTIONAL_HDR_MAGIC As Integer = &H10B
'    Public Const IMAGE_ROM_OPTIONAL_HDR_MAGIC As Integer = &H107
'    Public Const IMAGE_SUBSYSTEM_UNKNOWN As Integer = 0
'    Public Const IMAGE_SUBSYSTEM_NATIVE As Integer = 1
'    Public Const IMAGE_SUBSYSTEM_WINDOWS_GUI As Integer = 2
'    Public Const IMAGE_SUBSYSTEM_WINDOWS_CUI As Integer = 3
'    Public Const IMAGE_SUBSYSTEM_OS2_CUI As Integer = 5
'    Public Const IMAGE_SUBSYSTEM_POSIX_CUI As Integer = 7
'    Public Const IMAGE_SUBSYSTEM_RESERVED8 As Integer = 8
'    Public Const IMAGE_DIRECTORY_ENTRY_EXPORT As Integer = 0
'    Public Const IMAGE_DIRECTORY_ENTRY_IMPORT As Integer = 1
'    Public Const IMAGE_DIRECTORY_ENTRY_RESOURCE As Integer = 2
'    Public Const IMAGE_DIRECTORY_ENTRY_EXCEPTION As Integer = 3
'    Public Const IMAGE_DIRECTORY_ENTRY_SECURITY As Integer = 4
'    Public Const IMAGE_DIRECTORY_ENTRY_BASERELOC As Integer = 5
'    Public Const IMAGE_DIRECTORY_ENTRY_DEBUG As Integer = 6
'    Public Const IMAGE_DIRECTORY_ENTRY_COPYRIGHT As Integer = 7
'    Public Const IMAGE_DIRECTORY_ENTRY_GLOBALPTR As Integer = 8
'    Public Const IMAGE_DIRECTORY_ENTRY_TLS As Integer = 9
'    Public Const IMAGE_DIRECTORY_ENTRY_LOAD_CONFIG As Integer = 10
'    Public Const IMAGE_DIRECTORY_ENTRY_BOUND_IMPORT As Integer = 11
'    Public Const IMAGE_DIRECTORY_ENTRY_IAT As Integer = 12
'    Public Const IMAGE_SIZEOF_SHORT_NAME As Integer = 8
'    Public Const IMAGE_SIZEOF_SECTION_HEADER As Integer = 40
'    Public Const IMAGE_SCN_TYPE_NO_PAD As Integer = &H8
'    Public Const IMAGE_SCN_CNT_CODE As Integer = &H20
'    Public Const IMAGE_SCN_CNT_INITIALIZED_DATA As Integer = &H40
'    Public Const IMAGE_SCN_CNT_UNINITIALIZED_DATA As Integer = &H80
'    Public Const IMAGE_SCN_LNK_OTHER As Integer = &H100
'    Public Const IMAGE_SCN_LNK_INFO As Integer = &H200
'    Public Const IMAGE_SCN_LNK_REMOVE As Integer = &H800
'    Public Const IMAGE_SCN_LNK_COMDAT As Integer = &H1000
'    Public Const IMAGE_SCN_MEM_FARDATA As Integer = &H8000
'    Public Const IMAGE_SCN_MEM_PURGEABLE As Integer = &H20000
'    Public Const IMAGE_SCN_MEM_16BIT As Integer = &H20000
'    Public Const IMAGE_SCN_MEM_LOCKED As Integer = &H40000
'    Public Const IMAGE_SCN_MEM_PRELOAD As Integer = &H80000
'    Public Const IMAGE_SCN_ALIGN_1BYTES As Integer = &H100000
'    Public Const IMAGE_SCN_ALIGN_2BYTES As Integer = &H200000
'    Public Const IMAGE_SCN_ALIGN_4BYTES As Integer = &H300000
'    Public Const IMAGE_SCN_ALIGN_8BYTES As Integer = &H400000
'    Public Const IMAGE_SCN_ALIGN_16BYTES As Integer = &H500000
'    Public Const IMAGE_SCN_ALIGN_32BYTES As Integer = &H600000
'    Public Const IMAGE_SCN_ALIGN_64BYTES As Integer = &H700000
'    Public Const IMAGE_SCN_LNK_NRELOC_OVFL As Integer = &H1000000
'    Public Const IMAGE_SCN_MEM_DISCARDABLE As Integer = &H2000000
'    Public Const IMAGE_SCN_MEM_NOT_CACHED As Integer = &H4000000
'    Public Const IMAGE_SCN_MEM_NOT_PAGED As Integer = &H8000000
'    Public Const IMAGE_SCN_MEM_SHARED As Integer = &H10000000
'    Public Const IMAGE_SCN_MEM_EXECUTE As Integer = &H20000000
'    Public Const IMAGE_SCN_MEM_READ As Integer = &H40000000
'    Public Const IMAGE_SCN_MEM_WRITE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        IMAGE_SCN_MEM_READ = 0x40000000,
'    '        IMAGE_SCN_MEM_WRITE = unchecked((int)0x80000000),
'    '-------------------------------^--- GenCode(token): unexpected token type
'    Public Const IMAGE_SCN_SCALE_INDEX As Integer = &H1
'    Public Const IMAGE_SIZEOF_SYMBOL As Integer = 18
'    Public Const IMAGE_SYM_TYPE_NULL As Integer = &H0
'    Public Const IMAGE_SYM_TYPE_VOID As Integer = &H1
'    Public Const IMAGE_SYM_TYPE_CHAR As Integer = &H2
'    Public Const IMAGE_SYM_TYPE_SHORT As Integer = &H3
'    Public Const IMAGE_SYM_TYPE_INT As Integer = &H4
'    Public Const IMAGE_SYM_TYPE_LONG As Integer = &H5
'    Public Const IMAGE_SYM_TYPE_FLOAT As Integer = &H6
'    Public Const IMAGE_SYM_TYPE_DOUBLE As Integer = &H7
'    Public Const IMAGE_SYM_TYPE_STRUCT As Integer = &H8
'    Public Const IMAGE_SYM_TYPE_UNION As Integer = &H9
'    Public Const IMAGE_SYM_TYPE_ENUM As Integer = &HA
'    Public Const IMAGE_SYM_TYPE_MOE As Integer = &HB
'    Public Const IMAGE_SYM_TYPE_BYTE As Integer = &HC
'    Public Const IMAGE_SYM_TYPE_WORD As Integer = &HD
'    Public Const IMAGE_SYM_TYPE_UINT As Integer = &HE
'    Public Const IMAGE_SYM_TYPE_DWORD As Integer = &HF
'    Public Const IMAGE_SYM_TYPE_PCODE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        IMAGE_SYM_TYPE_DWORD = 0x000F,
'    '        IMAGE_SYM_TYPE_PCODE = unchecked((int)0x8000),
'    '--------------------------------^--- GenCode(token): unexpected token type
'    Public Const IMAGE_SYM_DTYPE_NULL As Integer = 0
'    Public Const IMAGE_SYM_DTYPE_POINTER As Integer = 1
'    Public Const IMAGE_SYM_DTYPE_FUNCTION As Integer = 2
'    Public Const IMAGE_SYM_DTYPE_ARRAY As Integer = 3
'    Public Const IMAGE_SYM_CLASS_NULL As Integer = &H0
'    Public Const IMAGE_SYM_CLASS_AUTOMATIC As Integer = &H1
'    Public Const IMAGE_SYM_CLASS_STATIC As Integer = &H3
'    Public Const IMAGE_SYM_CLASS_REGISTER As Integer = &H4
'    Public Const IMAGE_SYM_CLASS_LABEL As Integer = &H6
'    Public Const IMAGE_SYM_CLASS_UNDEFINED_LABEL As Integer = &H7
'    Public Const IMAGE_SYM_CLASS_MEMBER_OF_STRUCT As Integer = &H8
'    Public Const IMAGE_SYM_CLASS_ARGUMENT As Integer = &H9
'    Public Const IMAGE_SYM_CLASS_STRUCT_TAG As Integer = &HA
'    Public Const IMAGE_SYM_CLASS_MEMBER_OF_UNION As Integer = &HB
'    Public Const IMAGE_SYM_CLASS_UNION_TAG As Integer = &HC
'    Public Const IMAGE_SYM_CLASS_TYPE_DEFINITION As Integer = &HD
'    Public Const IMAGE_SYM_CLASS_UNDEFINED_STATIC As Integer = &HE
'    Public Const IMAGE_SYM_CLASS_ENUM_TAG As Integer = &HF
'    Public Const IMAGE_SYM_CLASS_MEMBER_OF_ENUM As Integer = &H10
'    Public Const IMAGE_SYM_CLASS_REGISTER_PARAM As Integer = &H11
'    Public Const IMAGE_SYM_CLASS_BIT_FIELD As Integer = &H12
'    Public Const IMAGE_SYM_CLASS_BLOCK As Integer = &H64
'    Public Const IMAGE_SYM_CLASS_FUNCTION As Integer = &H65
'    Public Const IMAGE_SYM_CLASS_END_OF_STRUCT As Integer = &H66
'    Public Const IMAGE_SYM_CLASS_FILE As Integer = &H67
'    Public Const IMAGE_SYM_CLASS_SECTION As Integer = &H68
'    Public Const IMAGE_SIZEOF_AUX_SYMBOL As Integer = 18
'    Public Const IMAGE_COMDAT_SELECT_NODUPLICATES As Integer = 1
'    Public Const IMAGE_COMDAT_SELECT_ANY As Integer = 2
'    Public Const IMAGE_COMDAT_SELECT_SAME_SIZE As Integer = 3
'    Public Const IMAGE_COMDAT_SELECT_EXACT_MATCH As Integer = 4
'    Public Const IMAGE_COMDAT_SELECT_ASSOCIATIVE As Integer = 5
'    Public Const IMAGE_COMDAT_SELECT_LARGEST As Integer = 6
'    Public Const IMAGE_COMDAT_SELECT_NEWEST As Integer = 7
'    Public Const IMAGE_SIZEOF_RELOCATION As Integer = 10
'    Public Const IMAGE_REL_I386_ABSOLUTE As Integer = &H0
'    Public Const IMAGE_REL_I386_DIR16 As Integer = &H1
'    Public Const IMAGE_REL_I386_REL16 As Integer = &H2
'    Public Const IMAGE_REL_I386_DIR32 As Integer = &H6
'    Public Const IMAGE_REL_I386_DIR32NB As Integer = &H7
'    Public Const IMAGE_REL_I386_SEG12 As Integer = &H9
'    Public Const IMAGE_REL_I386_SECTION As Integer = &HA
'    Public Const IMAGE_REL_I386_SECREL As Integer = &HB
'    Public Const IMAGE_REL_I386_REL32 As Integer = &H14
'    Public Const IMAGE_REL_MIPS_ABSOLUTE As Integer = &H0
'    Public Const IMAGE_REL_MIPS_REFHALF As Integer = &H1
'    Public Const IMAGE_REL_MIPS_REFWORD As Integer = &H2
'    Public Const IMAGE_REL_MIPS_JMPADDR As Integer = &H3
'    Public Const IMAGE_REL_MIPS_REFHI As Integer = &H4
'    Public Const IMAGE_REL_MIPS_REFLO As Integer = &H5
'    Public Const IMAGE_REL_MIPS_GPREL As Integer = &H6
'    Public Const IMAGE_REL_MIPS_LITERAL As Integer = &H7
'    Public Const IMAGE_REL_MIPS_SECTION As Integer = &HA
'    Public Const IMAGE_REL_MIPS_SECREL As Integer = &HB
'    Public Const IMAGE_REL_MIPS_SECRELLO As Integer = &HC
'    Public Const IMAGE_REL_MIPS_SECRELHI As Integer = &HD
'    Public Const IMAGE_REL_MIPS_REFWORDNB As Integer = &H22
'    Public Const IMAGE_REL_MIPS_PAIR As Integer = &H25
'    Public Const IMAGE_REL_ALPHA_ABSOLUTE As Integer = &H0
'    Public Const IMAGE_REL_ALPHA_REFLONG As Integer = &H1
'    Public Const IMAGE_REL_ALPHA_REFQUAD As Integer = &H2
'    Public Const IMAGE_REL_ALPHA_GPREL32 As Integer = &H3
'    Public Const IMAGE_REL_ALPHA_LITERAL As Integer = &H4
'    Public Const IMAGE_REL_ALPHA_LITUSE As Integer = &H5
'    Public Const IMAGE_REL_ALPHA_GPDISP As Integer = &H6
'    Public Const IMAGE_REL_ALPHA_BRADDR As Integer = &H7
'    Public Const IMAGE_REL_ALPHA_HINT As Integer = &H8
'    Public Const IMAGE_REL_ALPHA_INLINE_REFLONG As Integer = &H9
'    Public Const IMAGE_REL_ALPHA_REFHI As Integer = &HA
'    Public Const IMAGE_REL_ALPHA_REFLO As Integer = &HB
'    Public Const IMAGE_REL_ALPHA_PAIR As Integer = &HC
'    Public Const IMAGE_REL_ALPHA_MATCH As Integer = &HD
'    Public Const IMAGE_REL_ALPHA_SECTION As Integer = &HE
'    Public Const IMAGE_REL_ALPHA_SECREL As Integer = &HF
'    Public Const IMAGE_REL_ALPHA_REFLONGNB As Integer = &H10
'    Public Const IMAGE_REL_ALPHA_SECRELLO As Integer = &H11
'    Public Const IMAGE_REL_ALPHA_SECRELHI As Integer = &H12
'    Public Const IMAGE_REL_PPC_ABSOLUTE As Integer = &H0
'    Public Const IMAGE_REL_PPC_ADDR64 As Integer = &H1
'    Public Const IMAGE_REL_PPC_ADDR32 As Integer = &H2
'    Public Const IMAGE_REL_PPC_ADDR24 As Integer = &H3
'    Public Const IMAGE_REL_PPC_ADDR16 As Integer = &H4
'    Public Const IMAGE_REL_PPC_ADDR14 As Integer = &H5
'    Public Const IMAGE_REL_PPC_REL24 As Integer = &H6
'    Public Const IMAGE_REL_PPC_REL14 As Integer = &H7
'    Public Const IMAGE_REL_PPC_TOCREL16 As Integer = &H8
'    Public Const IMAGE_REL_PPC_TOCREL14 As Integer = &H9
'    Public Const IMAGE_REL_PPC_ADDR32NB As Integer = &HA
'    Public Const IMAGE_REL_PPC_SECREL As Integer = &HB
'    Public Const IMAGE_REL_PPC_SECTION As Integer = &HC
'    Public Const IMAGE_REL_PPC_IFGLUE As Integer = &HD
'    Public Const IMAGE_REL_PPC_IMGLUE As Integer = &HE
'    Public Const IMAGE_REL_PPC_SECREL16 As Integer = &HF
'    Public Const IMAGE_REL_PPC_REFHI As Integer = &H10
'    Public Const IMAGE_REL_PPC_REFLO As Integer = &H11
'    Public Const IMAGE_REL_PPC_PAIR As Integer = &H12
'    Public Const IMAGE_REL_PPC_SECRELLO As Integer = &H13
'    Public Const IMAGE_REL_PPC_SECRELHI As Integer = &H14
'    Public Const IMAGE_REL_PPC_TYPEMASK As Integer = &HFF
'    Public Const IMAGE_REL_PPC_NEG As Integer = &H100
'    Public Const IMAGE_REL_PPC_BRTAKEN As Integer = &H200
'    Public Const IMAGE_REL_PPC_BRNTAKEN As Integer = &H400
'    Public Const IMAGE_REL_PPC_TOCDEFN As Integer = &H800
'    Public Const IMAGE_SIZEOF_LINENUMBER As Integer = 6
'    Public Const IMAGE_SIZEOF_BASE_RELOCATION As Integer = 8
'    Public Const IMAGE_REL_BASED_ABSOLUTE As Integer = 0
'    Public Const IMAGE_REL_BASED_HIGH As Integer = 1
'    Public Const IMAGE_REL_BASED_LOW As Integer = 2
'    Public Const IMAGE_REL_BASED_HIGHLOW As Integer = 3
'    Public Const IMAGE_REL_BASED_HIGHADJ As Integer = 4
'    Public Const IMAGE_REL_BASED_MIPS_JMPADDR As Integer = 5
'    Public Const IMAGE_REL_BASED_SECTION As Integer = 6
'    Public Const IMAGE_REL_BASED_REL32 As Integer = 7
'    Public Const IMAGE_ARCHIVE_START_SIZE As Integer = 8
'    Public Const IMAGE_SIZEOF_ARCHIVE_MEMBER_HDR As Integer = 60
'    Public Const IMAGE_ORDINAL_FLAG As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        IMAGE_SIZEOF_ARCHIVE_MEMBER_HDR = 60,
'    '        IMAGE_ORDINAL_FLAG = unchecked((int)0x80000000),
'    '------------------------------^--- GenCode(token): unexpected token type
'    Public Const IMAGE_RESOURCE_NAME_IS_STRING As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        IMAGE_ORDINAL_FLAG = unchecked((int)0x80000000),
'    '        IMAGE_RESOURCE_NAME_IS_STRING = unchecked((int)0x80000000),
'    '-----------------------------------------^--- GenCode(token): unexpected token type
'    Public Const IMAGE_RESOURCE_DATA_IS_DIRECTORY As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        IMAGE_RESOURCE_NAME_IS_STRING = unchecked((int)0x80000000),
'    '        IMAGE_RESOURCE_DATA_IS_DIRECTORY = unchecked((int)0x80000000),
'    '--------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const IMAGE_DEBUG_TYPE_UNKNOWN As Integer = 0
'    Public Const IMAGE_DEBUG_TYPE_COFF As Integer = 1
'    Public Const IMAGE_DEBUG_TYPE_CODEVIEW As Integer = 2
'    Public Const IMAGE_DEBUG_TYPE_FPO As Integer = 3
'    Public Const IMAGE_DEBUG_TYPE_MISC As Integer = 4
'    Public Const IMAGE_DEBUG_TYPE_EXCEPTION As Integer = 5
'    Public Const IMAGE_DEBUG_TYPE_FIXUP As Integer = 6
'    Public Const IMAGE_DEBUG_TYPE_OMAP_TO_SRC As Integer = 7
'    Public Const IMAGE_DEBUG_TYPE_OMAP_FROM_SRC As Integer = 8
'    Public Const IMAGE_DEBUG_MISC_EXENAME As Integer = 1
'    Public Const IMAGE_SEPARATE_DEBUG_SIGNATURE As Integer = &H4944
'    Public Const IMAGE_SEPARATE_DEBUG_FLAGS_MASK As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        IMAGE_SEPARATE_DEBUG_SIGNATURE = 0x4944,
'    '        IMAGE_SEPARATE_DEBUG_FLAGS_MASK = unchecked((int)0x8000),
'    '-------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const IMAGE_SEPARATE_DEBUG_MISMATCH As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        IMAGE_SEPARATE_DEBUG_FLAGS_MASK = unchecked((int)0x8000),
'    '        IMAGE_SEPARATE_DEBUG_MISMATCH = unchecked((int)0x8000),
'    '-----------------------------------------^--- GenCode(token): unexpected token type
'    Public Const IS_TEXT_UNICODE_ASCII16 As Integer = &H1
'    Public Const IS_TEXT_UNICODE_REVERSE_ASCII16 As Integer = &H10
'    Public Const IS_TEXT_UNICODE_STATISTICS As Integer = &H2
'    Public Const IS_TEXT_UNICODE_REVERSE_STATISTICS As Integer = &H20
'    Public Const IS_TEXT_UNICODE_CONTROLS As Integer = &H4
'    Public Const IS_TEXT_UNICODE_REVERSE_CONTROLS As Integer = &H40
'    Public Const IS_TEXT_UNICODE_SIGNATURE As Integer = &H8
'    Public Const IS_TEXT_UNICODE_REVERSE_SIGNATURE As Integer = &H80
'    Public Const IS_TEXT_UNICODE_ILLEGAL_CHARS As Integer = &H100
'    Public Const IS_TEXT_UNICODE_ODD_LENGTH As Integer = &H200
'    Public Const IS_TEXT_UNICODE_DBCS_LEADBYTE As Integer = &H400
'    Public Const IS_TEXT_UNICODE_NULL_BYTES As Integer = &H1000
'    Public Const IS_TEXT_UNICODE_UNICODE_MASK As Integer = &HF
'    Public Const IS_TEXT_UNICODE_REVERSE_MASK As Integer = &HF0
'    Public Const IS_TEXT_UNICODE_NOT_UNICODE_MASK As Integer = &HF00
'    Public Const IS_TEXT_UNICODE_NOT_ASCII_MASK As Integer = &HF000
'    Public Const ICON_SMALL As Integer = 0
'    Public Const ICON_BIG As Integer = 1
'    Public Const IDANI_OPEN As Integer = 1
'    Public Const IDANI_CLOSE As Integer = 2
'    Public Const IDANI_CAPTION As Integer = 3
'    Public Const IDHOT_SNAPWINDOW As Integer = - 1
'    Public Const IDHOT_SNAPDESKTOP As Integer = - 2
'    Public Const IDC_ARROW As Integer = 32512
'    Public Const IDC_IBEAM As Integer = 32513
'    Public Const IDC_WAIT As Integer = 32514
'    Public Const IDC_CROSS As Integer = 32515
'    Public Const IDC_UPARROW As Integer = 32516
'    Public Const IDC_SIZE As Integer = 32640
'    Public Const IDC_ICON As Integer = 32641
'    Public Const IDC_SIZENWSE As Integer = 32642
'    Public Const IDC_SIZENESW As Integer = 32643
'    Public Const IDC_SIZEWE As Integer = 32644
'    Public Const IDC_SIZENS As Integer = 32645
'    Public Const IDC_SIZEALL As Integer = 32646
'    Public Const IDC_NO As Integer = 32648
'    Public Const IDC_APPSTARTING As Integer = 32650
'    Public Const IDC_HELP As Integer = 32651
'    Public Const IMAGE_BITMAP As Integer = 0
'    Public Const IMAGE_ICON As Integer = 1
'    Public Const IMAGE_CURSOR As Integer = 2
'    Public Const IMAGE_ENHMETAFILE As Integer = 3
'    Public Const IDI_APPLICATION As Integer = 32512
'    Public Const IDI_HAND As Integer = 32513
'    Public Const IDI_QUESTION As Integer = 32514
'    Public Const IDI_EXCLAMATION As Integer = 32515
'    Public Const IDI_ASTERISK As Integer = 32516
'    Public Const IDI_WINLOGO As Integer = 32517
'    Public Const IDI_WARNING As Integer = 32515
'    Public Const IDI_ERROR As Integer = 32513
'    Public Const IDI_INFORMATION As Integer = 32516
'    Public Const IDOK As Integer = 1
'    Public Const IDCANCEL As Integer = 2
'    Public Const IDABORT As Integer = 3
'    Public Const IDRETRY As Integer = 4
'    Public Const IDIGNORE As Integer = 5
'    Public Const IDYES As Integer = 6
'    Public Const IDNO As Integer = 7
'    Public Const IDCLOSE As Integer = 8
'    Public Const IDHELP As Integer = 9
'    Public Const IDH_NO_HELP As Integer = 28440
'    Public Const IDH_MISSING_CONTEXT As Integer = 28441
'    Public Const IDH_GENERIC_HELP_BUTTON As Integer = 28442
'    Public Const IDH_OK As Integer = 28443
'    Public Const IDH_CANCEL As Integer = 28444
'    Public Const IDH_HELP As Integer = 28445
'    Public Const ICC_LISTVIEW_CLASSES As Integer = &H1
'    Public Const ICC_TREEVIEW_CLASSES As Integer = &H2
'    Public Const ICC_BAR_CLASSES As Integer = &H4
'    Public Const ICC_TAB_CLASSES As Integer = &H8
'    Public Const ICC_UPDOWN_Class As Integer = &H10
'    Public Const ICC_PROGRESS_Class As Integer = &H20
'    Public Const ICC_HOTKEY_Class As Integer = &H40
'    Public Const ICC_ANIMATE_Class As Integer = &H80
'    Public Const ICC_WIN95_CLASSES As Integer = &HFF
'    Public Const ICC_DATE_CLASSES As Integer = &H100
'    Public Const ICC_USEREX_CLASSES As Integer = &H200
'    Public Const ICC_COOL_CLASSES As Integer = &H400
'    Public Const ILC_MASK As Integer = &H1
'    Public Const ILC_COLOR As Integer = &H0
'    Public Const ILC_COLORDDB As Integer = &HFE
'    Public Const ILC_COLOR4 As Integer = &H4
'    Public Const ILC_COLOR8 As Integer = &H8
'    Public Const ILC_COLOR16 As Integer = &H10
'    Public Const ILC_COLOR24 As Integer = &H18
'    Public Const ILC_COLOR32 As Integer = &H20
'    Public Const ILC_PALETTE As Integer = &H800
'    Public Const ILD_NORMAL As Integer = &H0
'    Public Const ILD_TRANSPARENT As Integer = &H1
'    Public Const ILD_MASK As Integer = &H10
'    Public Const ILD_IMAGE As Integer = &H20
'    Public Const ILD_ROP As Integer = &H40
'    Public Const ILD_BLEND25 As Integer = &H2
'    Public Const ILD_BLEND50 As Integer = &H4
'    Public Const ILD_OVERLAYMASK As Integer = &HF00
'    Public Const ILD_SELECTED As Integer = &H4
'    Public Const ILD_FOCUS As Integer = &H2
'    Public Const ILD_BLEND As Integer = &H4
'    Public Const ILCF_MOVE As Integer = &H0
'    Public Const ILCF_SWAP As Integer = &H1
'    Public Const IDB_STD_SMALL_COLOR As Integer = 0
'    Public Const IDB_STD_LARGE_COLOR As Integer = 1
'    Public Const IDB_VIEW_SMALL_COLOR As Integer = 4
'    Public Const IDB_VIEW_LARGE_COLOR As Integer = 5
'    Public Const IDB_HIST_SMALL_COLOR As Integer = 8
'    Public Const IDB_HIST_LARGE_COLOR As Integer = 9
'    Public Const I_INDENTCALLBACK As Integer = - 1
'    Public Const I_IMAGECALLBACK As Integer = - 1
'    Public Const I_CHILDRENCALLBACK As Integer = - 1
'    Public Const IO_COMPLETION_ALL_ACCESS As Integer = &HF0000 Or &H100000 Or &H3
'    Public Const INVALID_HANDLE_VALUE As Integer = - 1
'    Public Const IPN_FIRST As Integer = 0 - 860
'    Public Const IPN_LAST As Integer = 0 - 879
    
    
'    Public Const JOYERR_BASE As Integer = 160
'    Public Const JOYERR_NOERROR As Integer = 0
'    Public Const JOYERR_PARMS As Integer = 160 + 5
'    Public Const JOYERR_NOCANDO As Integer = 160 + 6
'    Public Const JOYERR_UNPLUGGED As Integer = 160 + 7
'    Public Const JOY_BUTTON1 As Integer = &H1
'    Public Const JOY_BUTTON2 As Integer = &H2
'    Public Const JOY_BUTTON3 As Integer = &H4
'    Public Const JOY_BUTTON4 As Integer = &H8
'    Public Const JOY_BUTTON1CHG As Integer = &H100
'    Public Const JOY_BUTTON2CHG As Integer = &H200
'    Public Const JOY_BUTTON3CHG As Integer = &H400
'    Public Const JOY_BUTTON4CHG As Integer = &H800
'    Public Const JOY_BUTTON5 As Integer = &H10
'    Public Const JOY_BUTTON6 As Integer = &H20
'    Public Const JOY_BUTTON7 As Integer = &H40
'    Public Const JOY_BUTTON8 As Integer = &H80
'    Public Const JOY_BUTTON9 As Integer = &H100
'    Public Const JOY_BUTTON10 As Integer = &H200
'    Public Const JOY_BUTTON11 As Integer = &H400
'    Public Const JOY_BUTTON12 As Integer = &H800
'    Public Const JOY_BUTTON13 As Integer = &H1000
'    Public Const JOY_BUTTON14 As Integer = &H2000
'    Public Const JOY_BUTTON15 As Integer = &H4000
'    Public Const JOY_BUTTON16 As Integer = &H8000
'    Public Const JOY_BUTTON17 As Integer = &H10000
'    Public Const JOY_BUTTON18 As Integer = &H20000
'    Public Const JOY_BUTTON19 As Integer = &H40000
'    Public Const JOY_BUTTON20 As Integer = &H80000
'    Public Const JOY_BUTTON21 As Integer = &H100000
'    Public Const JOY_BUTTON22 As Integer = &H200000
'    Public Const JOY_BUTTON23 As Integer = &H400000
'    Public Const JOY_BUTTON24 As Integer = &H800000
'    Public Const JOY_BUTTON25 As Integer = &H1000000
'    Public Const JOY_BUTTON26 As Integer = &H2000000
'    Public Const JOY_BUTTON27 As Integer = &H4000000
'    Public Const JOY_BUTTON28 As Integer = &H8000000
'    Public Const JOY_BUTTON29 As Integer = &H10000000
'    Public Const JOY_BUTTON30 As Integer = &H20000000
'    Public Const JOY_BUTTON31 As Integer = &H40000000
'    Public Const JOY_BUTTON32 As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        JOY_BUTTON31 = 0x40000000,
'    '        JOY_BUTTON32 = unchecked((int)0x80000000),
'    '------------------------^--- GenCode(token): unexpected token type
'    Public Const JOY_POVFORWARD As Integer = 0
'    Public Const JOY_POVRIGHT As Integer = 9000
'    Public Const JOY_POVBACKWARD As Integer = 18000
'    Public Const JOY_POVLEFT As Integer = 27000
'    Public Const JOY_RETURNX As Integer = &H1
'    Public Const JOY_RETURNY As Integer = &H2
'    Public Const JOY_RETURNZ As Integer = &H4
'    Public Const JOY_RETURNR As Integer = &H8
'    Public Const JOY_RETURNU As Integer = &H10
'    Public Const JOY_RETURNV As Integer = &H20
'    Public Const JOY_RETURNPOV As Integer = &H40
'    Public Const JOY_RETURNBUTTONS As Integer = &H80
'    Public Const JOY_RETURNRAWDATA As Integer = &H100
'    Public Const JOY_RETURNPOVCTS As Integer = &H200
'    Public Const JOY_RETURNCENTERED As Integer = &H400
'    Public Const JOY_USEDEADZONE As Integer = &H800
'    Public Const JOY_CAL_READALWAYS As Integer = &H10000
'    Public Const JOY_CAL_READXYONLY As Integer = &H20000
'    Public Const JOY_CAL_READ3 As Integer = &H40000
'    Public Const JOY_CAL_READ4 As Integer = &H80000
'    Public Const JOY_CAL_READXONLY As Integer = &H100000
'    Public Const JOY_CAL_READYONLY As Integer = &H200000
'    Public Const JOY_CAL_READ5 As Integer = &H400000
'    Public Const JOY_CAL_READ6 As Integer = &H800000
'    Public Const JOY_CAL_READZONLY As Integer = &H1000000
'    Public Const JOY_CAL_READRONLY As Integer = &H2000000
'    Public Const JOY_CAL_READUONLY As Integer = &H4000000
'    Public Const JOY_CAL_READVONLY As Integer = &H8000000
'    Public Const JOYSTICKID1 As Integer = 0
'    Public Const JOYSTICKID2 As Integer = 1
'    Public Const JOYCAPS_HASZ As Integer = &H1
'    Public Const JOYCAPS_HASR As Integer = &H2
'    Public Const JOYCAPS_HASU As Integer = &H4
'    Public Const JOYCAPS_HASV As Integer = &H8
'    Public Const JOYCAPS_HASPOV As Integer = &H10
'    Public Const JOYCAPS_POV4DIR As Integer = &H20
'    Public Const JOYCAPS_POVCTS As Integer = &H40
'    Public Const JOHAB_CHARSET As Integer = 130
'    Public Const JOB_CONTROL_PAUSE As Integer = 1
'    Public Const JOB_CONTROL_RESUME As Integer = 2
'    Public Const JOB_CONTROL_CANCEL As Integer = 3
'    Public Const JOB_CONTROL_RESTART As Integer = 4
'    Public Const JOB_CONTROL_DELETE As Integer = 5
'    Public Const JOB_CONTROL_SENT_TO_PRINTER As Integer = 6
'    Public Const JOB_CONTROL_LAST_PAGE_EJECTED As Integer = 7
'    Public Const JOB_STATUS_PAUSED As Integer = &H1
'    Public Const JOB_STATUS_ERROR As Integer = &H2
'    Public Const JOB_STATUS_DELETING As Integer = &H4
'    Public Const JOB_STATUS_SPOOLING As Integer = &H8
'    Public Const JOB_STATUS_PRINTING As Integer = &H10
'    Public Const JOB_STATUS_OFFLINE As Integer = &H20
'    Public Const JOB_STATUS_PAPEROUT As Integer = &H40
'    Public Const JOB_STATUS_PRINTED As Integer = &H80
'    Public Const JOB_STATUS_DELETED As Integer = &H100
'    Public Const JOB_STATUS_BLOCKED_DEVQ As Integer = &H200
'    Public Const JOB_STATUS_USER_INTERVENTION As Integer = &H400
'    Public Const JOB_STATUS_RESTART As Integer = &H800
'    Public Const JOB_POSITION_UNSPECIFIED As Integer = 0
'    Public Const JOB_NOTIFY_TYPE As Integer = &H1
'    Public Const JOB_NOTIFY_FIELD_PRINTER_NAME As Integer = &H0
'    Public Const JOB_NOTIFY_FIELD_MACHINE_NAME As Integer = &H1
'    Public Const JOB_NOTIFY_FIELD_PORT_NAME As Integer = &H2
'    Public Const JOB_NOTIFY_FIELD_USER_NAME As Integer = &H3
'    Public Const JOB_NOTIFY_FIELD_NOTIFY_NAME As Integer = &H4
'    Public Const JOB_NOTIFY_FIELD_DATATYPE As Integer = &H5
'    Public Const JOB_NOTIFY_FIELD_PRINT_PROCESSOR As Integer = &H6
'    Public Const JOB_NOTIFY_FIELD_PARAMETERS As Integer = &H7
'    Public Const JOB_NOTIFY_FIELD_DRIVER_NAME As Integer = &H8
'    Public Const JOB_NOTIFY_FIELD_DEVMODE As Integer = &H9
'    Public Const JOB_NOTIFY_FIELD_STATUS As Integer = &HA
'    Public Const JOB_NOTIFY_FIELD_STATUS_STRING As Integer = &HB
'    Public Const JOB_NOTIFY_FIELD_SECURITY_DESCRIPTOR As Integer = &HC
'    Public Const JOB_NOTIFY_FIELD_DOCUMENT As Integer = &HD
'    Public Const JOB_NOTIFY_FIELD_PRIORITY As Integer = &HE
'    Public Const JOB_NOTIFY_FIELD_POSITION As Integer = &HF
'    Public Const JOB_NOTIFY_FIELD_SUBMITTED As Integer = &H10
'    Public Const JOB_NOTIFY_FIELD_START_TIME As Integer = &H11
'    Public Const JOB_NOTIFY_FIELD_UNTIL_TIME As Integer = &H12
'    Public Const JOB_NOTIFY_FIELD_TIME As Integer = &H13
'    Public Const JOB_NOTIFY_FIELD_TOTAL_PAGES As Integer = &H14
'    Public Const JOB_NOTIFY_FIELD_PAGES_PRINTED As Integer = &H15
'    Public Const JOB_NOTIFY_FIELD_TOTAL_BYTES As Integer = &H16
'    Public Const JOB_NOTIFY_FIELD_BYTES_PRINTED As Integer = &H17
'    Public Const JOB_ACCESS_ADMINISTER As Integer = &H10
    
    
    
'    Public Const KEY_EVENT As Integer = &H1
'    Public Const KP_IV As Integer = 1
'    Public Const KP_SALT As Integer = 2
'    Public Const KP_PADDING As Integer = 3
'    Public Const KP_MODE As Integer = 4
'    Public Const KP_MODE_BITS As Integer = 5
'    Public Const KP_PERMISSIONS As Integer = 6
'    Public Const KP_ALGID As Integer = 7
'    Public Const KP_BLOCKLEN As Integer = 8
'    Public Const KEY_QUERY_VALUE As Integer = &H1
'    Public Const KEY_SET_VALUE As Integer = &H2
'    Public Const KEY_CREATE_SUB_KEY As Integer = &H4
'    Public Const KEY_ENUMERATE_SUB_KEYS As Integer = &H8
'    Public Const KEY_NOTIFY As Integer = &H10
'    Public Const KEY_CREATE_LINK As Integer = &H20
'    Public Const KF_EXTENDED As Integer = &H100
'    Public Const KF_DLGMODE As Integer = &H800
'    Public Const KF_MENUMODE As Integer = &H1000
'    Public Const KF_ALTDOWN As Integer = &H2000
'    Public Const KF_REPEAT As Integer = &H4000
'    Public Const KF_UP As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        KF_REPEAT = 0x4000,
'    '        KF_UP = unchecked((int)0x8000),
'    '-----------------^--- GenCode(token): unexpected token type
'    Public Const KLF_ACTIVATE As Integer = &H1
'    Public Const KLF_SUBSTITUTE_OK As Integer = &H2
'    Public Const KLF_UNLOADPREVIOUS As Integer = &H4
'    Public Const KLF_REORDER As Integer = &H8
'    Public Const KLF_REPLACELANG As Integer = &H10
'    Public Const KLF_NOTELLSHELL As Integer = &H80
'    Public Const KL_NAMELENGTH As Integer = 9
'    Public Const KEYEVENTF_EXTENDEDKEY As Integer = &H1
'    Public Const KEYEVENTF_KEYUP As Integer = &H2
    
    
'    Public Const lst1 As Integer = &H460
'    Public Const lst2 As Integer = &H461
'    Public Const lst3 As Integer = &H462
'    Public Const lst4 As Integer = &H463
'    Public Const lst5 As Integer = &H464
'    Public Const lst6 As Integer = &H465
'    Public Const lst7 As Integer = &H466
'    Public Const lst8 As Integer = &H467
'    Public Const lst9 As Integer = &H468
'    Public Const lst10 As Integer = &H469
'    Public Const lst11 As Integer = &H46A
'    Public Const lst12 As Integer = &H46B
'    Public Const lst13 As Integer = &H46C
'    Public Const lst14 As Integer = &H46D
'    Public Const lst15 As Integer = &H46E
'    Public Const lst16 As Integer = &H46F
'    Public Const LZERROR_BADINHANDLE As Integer = - 1
'    Public Const LZERROR_BADOUTHANDLE As Integer = - 2
'    Public Const LZERROR_READ As Integer = - 3
'    Public Const LZERROR_WRITE As Integer = - 4
'    Public Const LZERROR_GLOBALLOC As Integer = - 5
'    Public Const LZERROR_GLOBLOCK As Integer = - 6
'    Public Const LZERROR_BADVALUE As Integer = - 7
'    Public Const LZERROR_UNKNOWNALG As Integer = - 8
'    Public Const LISTEN_OUTSTANDING As Integer = &H1
'    Public Const LMEM_FIXED As Integer = &H0
'    Public Const LMEM_MOVEABLE As Integer = &H2
'    Public Const LMEM_NOCOMPACT As Integer = &H10
'    Public Const LMEM_NODISCARD As Integer = &H20
'    Public Const LMEM_ZEROINIT As Integer = &H40
'    Public Const LMEM_MODIFY As Integer = &H80
'    Public Const LMEM_DISCARDABLE As Integer = &HF00
'    Public Const LMEM_VALID_FLAGS As Integer = &HF72
'    Public Const LMEM_INVALID_HANDLE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        LMEM_VALID_FLAGS = 0x0F72,
'    '        LMEM_INVALID_HANDLE = unchecked((int)0x8000),
'    '-------------------------------^--- GenCode(token): unexpected token type
'    Public Const LHND As Integer = &H2 Or &H40
'    Public Const LPTR As Integer = &H0 Or &H40
'    Public Const LMEM_DISCARDED As Integer = &H4000
'    Public Const LMEM_LOCKCOUNT As Integer = &HFF
'    Public Const LOAD_DLL_DEBUG_EVENT As Integer = 6
'    Public Const LPTx As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        LOAD_DLL_DEBUG_EVENT = 6,
'    '        LPTx = unchecked((int)0x80),
'    '----------------^--- GenCode(token): unexpected token type
'    Public Const LOCKFILE_FAIL_IMMEDIATELY As Integer = &H1
'    Public Const LOCKFILE_EXCLUSIVE_LOCK As Integer = &H2
'    Public Const LOAD_LIBRARY_AS_DATAFILE As Integer = &H2
'    Public Const LOAD_WITH_ALTERED_SEARCH_PATH As Integer = &H8
'    Public Const LOGON32_LOGON_INTERACTIVE As Integer = 2
'    Public Const LOGON32_LOGON_NETWORK As Integer = 3
'    Public Const LOGON32_LOGON_BATCH As Integer = 4
'    Public Const LOGON32_LOGON_SERVICE As Integer = 5
'    Public Const LOGON32_PROVIDER_DEFAULT As Integer = 0
'    Public Const LOGON32_PROVIDER_WINNT35 As Integer = 1
'    Public Const LOGON32_PROVIDER_WINNT40 As Integer = 2
'    Public Const LEFT_ALT_PRESSED As Integer = &H2
'    Public Const LEFT_CTRL_PRESSED As Integer = &H8
'    Public Const LCS_CALIBRATED_RGB As Integer = &H0
'    Public Const LCS_DEVICE_RGB As Integer = &H1
'    Public Const LCS_DEVICE_CMYK As Integer = &H2
'    Public Const LCS_GM_BUSINESS As Integer = &H1
'    Public Const LCS_GM_GRAPHICS As Integer = &H2
'    Public Const LCS_GM_IMAGES As Integer = &H4
'    Public Const LF_FACESIZE As Integer = 32
'    Public Const LF_FULLFACESIZE As Integer = 64
'    Public Const LTGRAY_BRUSH As Integer = 1
'    Public Const LINECAPS As Integer = 30
'    Public Const LOGPIXELSX As Integer = 88
'    Public Const LOGPIXELSY As Integer = 90
'    Public Const LC_NONE As Integer = 0
'    Public Const LC_POLYLINE As Integer = 2
'    Public Const LC_MARKER As Integer = 4
'    Public Const LC_POLYMARKER As Integer = 8
'    Public Const LC_WIDE As Integer = 16
'    Public Const LC_STYLED As Integer = 32
'    Public Const LC_WIDESTYLED As Integer = 64
'    Public Const LC_INTERIORS As Integer = 128
'    Public Const LPD_DOUBLEBUFFER As Integer = &H1
'    Public Const LPD_STEREO As Integer = &H2
'    Public Const LPD_SUPPORT_GDI As Integer = &H10
'    Public Const LPD_SUPPORT_OPENGL As Integer = &H20
'    Public Const LPD_SHARE_DEPTH As Integer = &H40
'    Public Const LPD_SHARE_STENCIL As Integer = &H80
'    Public Const LPD_SHARE_ACCUM As Integer = &H100
'    Public Const LPD_SWAP_EXCHANGE As Integer = &H200
'    Public Const LPD_SWAP_COPY As Integer = &H400
'    Public Const LPD_TRANSPARENT As Integer = &H1000
'    Public Const LPD_TYPE_RGBA As Integer = 0
'    Public Const LPD_TYPE_COLORINDEX As Integer = 1
'    Public Const LPSTR_TEXTCALLBACK As Integer = - 1
'    Public Const LCMAP_LOWERCASE As Integer = &H100
'    Public Const LCMAP_UPPERCASE As Integer = &H200
'    Public Const LCMAP_SORTKEY As Integer = &H400
'    Public Const LCMAP_BYTEREV As Integer = &H800
'    Public Const LCMAP_HIRAGANA As Integer = &H100000
'    Public Const LCMAP_KATAKANA As Integer = &H200000
'    Public Const LCMAP_HALFWIDTH As Integer = &H400000
'    Public Const LCMAP_FULLWIDTH As Integer = &H800000
'    Public Const LCMAP_LINGUISTIC_CASING As Integer = &H1000000
'    Public Const LCMAP_SIMPLIFIED_CHINESE As Integer = &H2000000
'    Public Const LCMAP_TRADITIONAL_CHINESE As Integer = &H4000000
'    Public Const LCID_INSTALLED As Integer = &H1
'    Public Const LCID_SUPPORTED As Integer = &H2
'    Public Const LOCALE_NOUSEROVERRIDE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        LCID_SUPPORTED = 0x00000002,
'    '        LOCALE_NOUSEROVERRIDE = unchecked((int)0x80000000),
'    '---------------------------------^--- GenCode(token): unexpected token type
'    Public Const LOCALE_USE_CP_ACP As Integer = &H40000000
'    Public Const LOCALE_ILANGUAGE As Integer = &H1
'    Public Const LOCALE_SLANGUAGE As Integer = &H2
'    Public Const LOCALE_SENGLANGUAGE As Integer = &H1001
'    Public Const LOCALE_SABBREVLANGNAME As Integer = &H3
'    Public Const LOCALE_SNATIVELANGNAME As Integer = &H4
'    Public Const LOCALE_ICOUNTRY As Integer = &H5
'    Public Const LOCALE_SCOUNTRY As Integer = &H6
'    Public Const LOCALE_SENGCOUNTRY As Integer = &H1002
'    Public Const LOCALE_SABBREVCTRYNAME As Integer = &H7
'    Public Const LOCALE_SNATIVECTRYNAME As Integer = &H8
'    Public Const LOCALE_IDEFAULTLANGUAGE As Integer = &H9
'    Public Const LOCALE_IDEFAULTCOUNTRY As Integer = &HA
'    Public Const LOCALE_IDEFAULTCODEPAGE As Integer = &HB
'    Public Const LOCALE_IDEFAULTANSICODEPAGE As Integer = &H1004
'    Public Const LOCALE_IDEFAULTMACCODEPAGE As Integer = &H1011
'    Public Const LOCALE_SLIST As Integer = &HC
'    Public Const LOCALE_IMEASURE As Integer = &HD
'    Public Const LOCALE_SDECIMAL As Integer = &HE
'    Public Const LOCALE_STHOUSAND As Integer = &HF
'    Public Const LOCALE_SGROUPING As Integer = &H10
'    Public Const LOCALE_IDIGITS As Integer = &H11
'    Public Const LOCALE_ILZERO As Integer = &H12
'    Public Const LOCALE_INEGNUMBER As Integer = &H1010
'    Public Const LOCALE_SNATIVEDIGITS As Integer = &H13
'    Public Const LOCALE_SCURRENCY As Integer = &H14
'    Public Const LOCALE_SINTLSYMBOL As Integer = &H15
'    Public Const LOCALE_SMONDECIMALSEP As Integer = &H16
'    Public Const LOCALE_SMONTHOUSANDSEP As Integer = &H17
'    Public Const LOCALE_SMONGROUPING As Integer = &H18
'    Public Const LOCALE_ICURRDIGITS As Integer = &H19
'    Public Const LOCALE_IINTLCURRDIGITS As Integer = &H1A
'    Public Const LOCALE_ICURRENCY As Integer = &H1B
'    Public Const LOCALE_INEGCURR As Integer = &H1C
'    Public Const LOCALE_SDATE As Integer = &H1D
'    Public Const LOCALE_STIME As Integer = &H1E
'    Public Const LOCALE_SSHORTDATE As Integer = &H1F
'    Public Const LOCALE_SLONGDATE As Integer = &H20
'    Public Const LOCALE_STIMEFORMAT As Integer = &H1003
'    Public Const LOCALE_IDATE As Integer = &H21
'    Public Const LOCALE_ILDATE As Integer = &H22
'    Public Const LOCALE_ITIME As Integer = &H23
'    Public Const LOCALE_ITIMEMARKPOSN As Integer = &H1005
'    Public Const LOCALE_ICENTURY As Integer = &H24
'    Public Const LOCALE_ITLZERO As Integer = &H25
'    Public Const LOCALE_IDAYLZERO As Integer = &H26
'    Public Const LOCALE_IMONLZERO As Integer = &H27
'    Public Const LOCALE_S1159 As Integer = &H28
'    Public Const LOCALE_S2359 As Integer = &H29
'    Public Const LOCALE_ICALENDARTYPE As Integer = &H1009
'    Public Const LOCALE_IOPTIONALCALENDAR As Integer = &H100B
'    Public Const LOCALE_IFIRSTDAYOFWEEK As Integer = &H100C
'    Public Const LOCALE_IFIRSTWEEKOFYEAR As Integer = &H100D
'    Public Const LOCALE_SDAYNAME1 As Integer = &H2A
'    Public Const LOCALE_SDAYNAME2 As Integer = &H2B
'    Public Const LOCALE_SDAYNAME3 As Integer = &H2C
'    Public Const LOCALE_SDAYNAME4 As Integer = &H2D
'    Public Const LOCALE_SDAYNAME5 As Integer = &H2E
'    Public Const LOCALE_SDAYNAME6 As Integer = &H2F
'    Public Const LOCALE_SDAYNAME7 As Integer = &H30
'    Public Const LOCALE_SABBREVDAYNAME1 As Integer = &H31
'    Public Const LOCALE_SABBREVDAYNAME2 As Integer = &H32
'    Public Const LOCALE_SABBREVDAYNAME3 As Integer = &H33
'    Public Const LOCALE_SABBREVDAYNAME4 As Integer = &H34
'    Public Const LOCALE_SABBREVDAYNAME5 As Integer = &H35
'    Public Const LOCALE_SABBREVDAYNAME6 As Integer = &H36
'    Public Const LOCALE_SABBREVDAYNAME7 As Integer = &H37
'    Public Const LOCALE_SMONTHNAME1 As Integer = &H38
'    Public Const LOCALE_SMONTHNAME2 As Integer = &H39
'    Public Const LOCALE_SMONTHNAME3 As Integer = &H3A
'    Public Const LOCALE_SMONTHNAME4 As Integer = &H3B
'    Public Const LOCALE_SMONTHNAME5 As Integer = &H3C
'    Public Const LOCALE_SMONTHNAME6 As Integer = &H3D
'    Public Const LOCALE_SMONTHNAME7 As Integer = &H3E
'    Public Const LOCALE_SMONTHNAME8 As Integer = &H3F
'    Public Const LOCALE_SMONTHNAME9 As Integer = &H40
'    Public Const LOCALE_SMONTHNAME10 As Integer = &H41
'    Public Const LOCALE_SMONTHNAME11 As Integer = &H42
'    Public Const LOCALE_SMONTHNAME12 As Integer = &H43
'    Public Const LOCALE_SMONTHNAME13 As Integer = &H100E
'    Public Const LOCALE_SABBREVMONTHNAME1 As Integer = &H44
'    Public Const LOCALE_SABBREVMONTHNAME2 As Integer = &H45
'    Public Const LOCALE_SABBREVMONTHNAME3 As Integer = &H46
'    Public Const LOCALE_SABBREVMONTHNAME4 As Integer = &H47
'    Public Const LOCALE_SABBREVMONTHNAME5 As Integer = &H48
'    Public Const LOCALE_SABBREVMONTHNAME6 As Integer = &H49
'    Public Const LOCALE_SABBREVMONTHNAME7 As Integer = &H4A
'    Public Const LOCALE_SABBREVMONTHNAME8 As Integer = &H4B
'    Public Const LOCALE_SABBREVMONTHNAME9 As Integer = &H4C
'    Public Const LOCALE_SABBREVMONTHNAME10 As Integer = &H4D
'    Public Const LOCALE_SABBREVMONTHNAME11 As Integer = &H4E
'    Public Const LOCALE_SABBREVMONTHNAME12 As Integer = &H4F
'    Public Const LOCALE_SABBREVMONTHNAME13 As Integer = &H100F
'    Public Const LOCALE_SPOSITIVESIGN As Integer = &H50
'    Public Const LOCALE_SNEGATIVESIGN As Integer = &H51
'    Public Const LOCALE_IPOSSIGNPOSN As Integer = &H52
'    Public Const LOCALE_INEGSIGNPOSN As Integer = &H53
'    Public Const LOCALE_IPOSSYMPRECEDES As Integer = &H54
'    Public Const LOCALE_IPOSSEPBYSPACE As Integer = &H55
'    Public Const LOCALE_INEGSYMPRECEDES As Integer = &H56
'    Public Const LOCALE_INEGSEPBYSPACE As Integer = &H57
'    Public Const LOCALE_FONTSIGNATURE As Integer = &H58
'    Public Const LOCALE_SISO639LANGNAME As Integer = &H59
'    Public Const LOCALE_SISO3166CTRYNAME As Integer = &H5A
'    Public Const LANG_NEUTRAL As Integer = &H0
'    Public Const LANG_AFRIKAANS As Integer = &H36
'    Public Const LANG_ALBANIAN As Integer = &H1C
'    Public Const LANG_ARABIC As Integer = &H1
'    Public Const LANG_BASQUE As Integer = &H2D
'    Public Const LANG_BELARUSIAN As Integer = &H23
'    Public Const LANG_BULGARIAN As Integer = &H2
'    Public Const LANG_CATALAN As Integer = &H3
'    Public Const LANG_CHINESE As Integer = &H4
'    Public Const LANG_CROATIAN As Integer = &H1A
'    Public Const LANG_CZECH As Integer = &H5
'    Public Const LANG_DANISH As Integer = &H6
'    Public Const LANG_DUTCH As Integer = &H13
'    Public Const LANG_ENGLISH As Integer = &H9
'    Public Const LANG_ESTONIAN As Integer = &H25
'    Public Const LANG_FAEROESE As Integer = &H38
'    Public Const LANG_FARSI As Integer = &H29
'    Public Const LANG_FINNISH As Integer = &HB
'    Public Const LANG_FRENCH As Integer = &HC
'    Public Const LANG_GERMAN As Integer = &H7
'    Public Const LANG_GREEK As Integer = &H8
'    Public Const LANG_HEBREW As Integer = &HD
'    Public Const LANG_HUNGARIAN As Integer = &HE
'    Public Const LANG_ICELANDIC As Integer = &HF
'    Public Const LANG_INDONESIAN As Integer = &H21
'    Public Const LANG_ITALIAN As Integer = &H10
'    Public Const LANG_JAPANESE As Integer = &H11
'    Public Const LANG_KOREAN As Integer = &H12
'    Public Const LANG_LATVIAN As Integer = &H26
'    Public Const LANG_LITHUANIAN As Integer = &H27
'    Public Const LANG_NORWEGIAN As Integer = &H14
'    Public Const LANG_POLISH As Integer = &H15
'    Public Const LANG_PORTUGUESE As Integer = &H16
'    Public Const LANG_ROMANIAN As Integer = &H18
'    Public Const LANG_RUSSIAN As Integer = &H19
'    Public Const LANG_SERBIAN As Integer = &H1A
'    Public Const LANG_SLOVAK As Integer = &H1B
'    Public Const LANG_SLOVENIAN As Integer = &H24
'    Public Const LANG_SPANISH As Integer = &HA
'    Public Const LANG_SWEDISH As Integer = &H1D
'    Public Const LANG_THAI As Integer = &H1E
'    Public Const LANG_TURKISH As Integer = &H1F
'    Public Const LANG_UKRAINIAN As Integer = &H22
'    Public Const LANG_VIETNAMESE As Integer = &H2A
'    Public Const LR_DEFAULTCOLOR As Integer = &H0
'    Public Const LR_MONOCHROME As Integer = &H1
'    Public Const LR_COLOR As Integer = &H2
'    Public Const LR_COPYRETURNORG As Integer = &H4
'    Public Const LR_COPYDELETEORG As Integer = &H8
'    Public Const LR_LOADFROMFILE As Integer = &H10
'    Public Const LR_LOADTRANSPARENT As Integer = &H20
'    Public Const LR_DEFAULTSIZE As Integer = &H40
'    Public Const LR_VGACOLOR As Integer = &H80
'    Public Const LR_LOADMAP3DCOLORS As Integer = &H1000
'    Public Const LR_CREATEDIBSECTION As Integer = &H2000
'    Public Const LR_COPYFROMRESOURCE As Integer = &H4000
'    Public Const LR_SHARED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        LR_COPYFROMRESOURCE = 0x4000,
'    '        LR_SHARED = unchecked((int)0x8000),
'    '---------------------^--- GenCode(token): unexpected token type
'    Public Const LB_CTLCODE As Integer = 0
'    Public Const LB_OKAY As Integer = 0
'    Public Const LB_ERR As Integer = - 1
'    Public Const LB_ERRSPACE As Integer = - 2
'    Public Const LBN_ERRSPACE As Integer = - 2
'    Public Const LBN_SELCHANGE As Integer = 1
'    Public Const LBN_DBLCLK As Integer = 2
'    Public Const LBN_SELCANCEL As Integer = 3
'    Public Const LBN_SETFOCUS As Integer = 4
'    Public Const LBN_KILLFOCUS As Integer = 5
'    Public Const LB_ADDSTRING As Integer = &H180
'    Public Const LB_INSERTSTRING As Integer = &H181
'    Public Const LB_DELETESTRING As Integer = &H182
'    Public Const LB_SELITEMRANGEEX As Integer = &H183
'    Public Const LB_RESETCONTENT As Integer = &H184
'    Public Const LB_SETSEL As Integer = &H185
'    Public Const LB_SETCURSEL As Integer = &H186
'    Public Const LB_GETSEL As Integer = &H187
'    Public Const LB_GETCURSEL As Integer = &H188
'    Public Const LB_GETTEXT As Integer = &H189
'    Public Const LB_GETTEXTLEN As Integer = &H18A
'    Public Const LB_GETCOUNT As Integer = &H18B
'    Public Const LB_SELECTSTRING As Integer = &H18C
'    Public Const LB_DIR As Integer = &H18D
'    Public Const LB_GETTOPINDEX As Integer = &H18E
'    Public Const LB_FINDSTRING As Integer = &H18F
'    Public Const LB_GETSELCOUNT As Integer = &H190
'    Public Const LB_GETSELITEMS As Integer = &H191
'    Public Const LB_SETTABSTOPS As Integer = &H192
'    Public Const LB_GETHORIZONTALEXTENT As Integer = &H193
'    Public Const LB_SETHORIZONTALEXTENT As Integer = &H194
'    Public Const LB_SETCOLUMNWIDTH As Integer = &H195
'    Public Const LB_ADDFILE As Integer = &H196
'    Public Const LB_SETTOPINDEX As Integer = &H197
'    Public Const LB_GETITEMRECT As Integer = &H198
'    Public Const LB_GETITEMDATA As Integer = &H199
'    Public Const LB_SETITEMDATA As Integer = &H19A
'    Public Const LB_SELITEMRANGE As Integer = &H19B
'    Public Const LB_SETANCHORINDEX As Integer = &H19C
'    Public Const LB_GETANCHORINDEX As Integer = &H19D
'    Public Const LB_SETCARETINDEX As Integer = &H19E
'    Public Const LB_GETCARETINDEX As Integer = &H19F
'    Public Const LB_SETITEMHEIGHT As Integer = &H1A0
'    Public Const LB_GETITEMHEIGHT As Integer = &H1A1
'    Public Const LB_FINDSTRINGEXACT As Integer = &H1A2
'    Public Const LB_SETLOCALE As Integer = &H1A5
'    Public Const LB_GETLOCALE As Integer = &H1A6
'    Public Const LB_SETCOUNT As Integer = &H1A7
'    Public Const LB_INITSTORAGE As Integer = &H1A8
'    Public Const LB_ITEMFROMPOINT As Integer = &H1A9
'    Public Const LB_MSGMAX As Integer = &H1B0
'    ' LB_MSGMAX = 0x01A8;
'    Public Const LBS_NOTIFY As Integer = &H1
'    Public Const LBS_SORT As Integer = &H2
'    Public Const LBS_NOREDRAW As Integer = &H4
'    Public Const LBS_MULTIPLESEL As Integer = &H8
'    Public Const LBS_OWNERDRAWFIXED As Integer = &H10
'    Public Const LBS_OWNERDRAWVARIABLE As Integer = &H20
'    Public Const LBS_HASSTRINGS As Integer = &H40
'    Public Const LBS_USETABSTOPS As Integer = &H80
'    Public Const LBS_NOINTEGRALHEIGHT As Integer = &H100
'    Public Const LBS_MULTICOLUMN As Integer = &H200
'    Public Const LBS_WANTKEYBOARDINPUT As Integer = &H400
'    Public Const LBS_EXTENDEDSEL As Integer = &H800
'    Public Const LBS_DISABLENOSCROLL As Integer = &H1000
'    Public Const LBS_NODATA As Integer = &H2000
'    Public Const LBS_NOSEL As Integer = &H4000
'    Public Const LBS_STANDARD As Integer = &H1 Or &H2 Or &H200000 Or &H800000
'    Public Const LVM_FIRST As Integer = &H1000
'    Public Const LVN_FIRST As Integer = 0 - 100
'    Public Const LVN_LAST As Integer = 0 - 199
'    Public Const LVS_ICON As Integer = &H0
'    Public Const LVS_REPORT As Integer = &H1
'    Public Const LVS_SMALLICON As Integer = &H2
'    Public Const LVS_LIST As Integer = &H3
'    Public Const LVS_TYPEMASK As Integer = &H3
'    Public Const LVS_SINGLESEL As Integer = &H4
'    Public Const LVS_SHOWSELALWAYS As Integer = &H8
'    Public Const LVS_SORTASCENDING As Integer = &H10
'    Public Const LVS_SORTDESCENDING As Integer = &H20
'    Public Const LVS_SHAREIMAGELISTS As Integer = &H40
'    Public Const LVS_NOLABELWRAP As Integer = &H80
'    Public Const LVS_AUTOARRANGE As Integer = &H100
'    Public Const LVS_EDITLABELS As Integer = &H200
'    Public Const LVS_OWNERDATA As Integer = &H1000
'    Public Const LVS_NOSCROLL As Integer = &H2000
'    Public Const LVS_TYPESTYLEMASK As Integer = &HFC00
'    Public Const LVS_ALIGNTOP As Integer = &H0
'    Public Const LVS_ALIGNLEFT As Integer = &H800
'    Public Const LVS_ALIGNMASK As Integer = &HC00
'    Public Const LVS_OWNERDRAWFIXED As Integer = &H400
'    Public Const LVS_NOCOLUMNHEADER As Integer = &H4000
'    Public Const LVS_NOSORTHEADER As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        LVS_NOCOLUMNHEADER = 0x4000,
'    '        LVS_NOSORTHEADER = unchecked((int)0x8000),
'    '----------------------------^--- GenCode(token): unexpected token type
'    Public Const LVM_GETBKCOLOR As Integer = &H1000 + 0
'    Public Const LVM_SETBKCOLOR As Integer = &H1000 + 1
'    Public Const LVM_GETIMAGELIST As Integer = &H1000 + 2
'    Public Const LVSIL_NORMAL As Integer = 0
'    Public Const LVSIL_SMALL As Integer = 1
'    Public Const LVSIL_STATE As Integer = 2
'    Public Const LVM_SETIMAGELIST As Integer = &H1000 + 3
'    Public Const LVM_GETITEMCOUNT As Integer = &H1000 + 4
'    Public Const LVIF_TEXT As Integer = &H1
'    Public Const LVIF_IMAGE As Integer = &H2
'    Public Const LVIF_PARAM As Integer = &H4
'    Public Const LVIF_STATE As Integer = &H8
'    Public Const LVIF_INDENT As Integer = &H10
'    Public Const LVIF_NORECOMPUTE As Integer = &H800
'    Public Const LVIS_FOCUSED As Integer = &H1
'    Public Const LVIS_SELECTED As Integer = &H2
'    Public Const LVIS_CUT As Integer = &H4
'    Public Const LVIS_DROPHILITED As Integer = &H8
'    Public Const LVIS_OVERLAYMASK As Integer = &HF00
'    Public Const LVIS_STATEIMAGEMASK As Integer = &HF000
'    Public Const LVM_GETITEMA As Integer = &H1000 + 5
'    Public Const LVM_GETITEMW As Integer = &H1000 + 75
'    Public Const LVM_SETITEMA As Integer = &H1000 + 6
'    Public Const LVM_SETITEMW As Integer = &H1000 + 76
'    Public Const LVM_INSERTITEMA As Integer = &H1000 + 7
'    Public Const LVM_INSERTITEMW As Integer = &H1000 + 77
'    Public Const LVM_DELETEITEM As Integer = &H1000 + 8
'    Public Const LVM_DELETEALLITEMS As Integer = &H1000 + 9
'    Public Const LVM_GETCALLBACKMASK As Integer = &H1000 + 10
'    Public Const LVM_SETCALLBACKMASK As Integer = &H1000 + 11
'    Public Const LVNI_ALL As Integer = &H0
'    Public Const LVNI_FOCUSED As Integer = &H1
'    Public Const LVNI_SELECTED As Integer = &H2
'    Public Const LVNI_CUT As Integer = &H4
'    Public Const LVNI_DROPHILITED As Integer = &H8
'    Public Const LVNI_ABOVE As Integer = &H100
'    Public Const LVNI_BELOW As Integer = &H200
'    Public Const LVNI_TOLEFT As Integer = &H400
'    Public Const LVNI_TORIGHT As Integer = &H800
'    Public Const LVM_GETNEXTITEM As Integer = &H1000 + 12
'    Public Const LVFI_PARAM As Integer = &H1
'    Public Const LVFI_STRING As Integer = &H2
'    Public Const LVFI_PARTIAL As Integer = &H8
'    Public Const LVFI_WRAP As Integer = &H20
'    Public Const LVFI_NEARESTXY As Integer = &H40
'    Public Const LVM_FINDITEMA As Integer = &H1000 + 13
'    Public Const LVM_FINDITEMW As Integer = &H1000 + 83
'    Public Const LVIR_BOUNDS As Integer = 0
'    Public Const LVIR_ICON As Integer = 1
'    Public Const LVIR_LABEL As Integer = 2
'    Public Const LVIR_SELECTBOUNDS As Integer = 3
'    Public Const LVM_GETITEMRECT As Integer = &H1000 + 14
'    Public Const LVM_SETITEMPOSITION As Integer = &H1000 + 15
'    Public Const LVM_GETITEMPOSITION As Integer = &H1000 + 16
'    Public Const LVM_GETSTRINGWIDTHA As Integer = &H1000 + 17
'    Public Const LVM_GETSTRINGWIDTHW As Integer = &H1000 + 87
'    Public Const LVHT_NOWHERE As Integer = &H1
'    Public Const LVHT_ONITEMICON As Integer = &H2
'    Public Const LVHT_ONITEMLABEL As Integer = &H4
'    Public Const LVHT_ONITEMSTATEICON As Integer = &H8
'    Public Const LVHT_ONITEM As Integer = &H2 Or &H4 Or &H8
'    Public Const LVHT_ABOVE As Integer = &H8
'    Public Const LVHT_BELOW As Integer = &H10
'    Public Const LVHT_TORIGHT As Integer = &H20
'    Public Const LVHT_TOLEFT As Integer = &H40
'    Public Const LVM_HITTEST As Integer = &H1000 + 18
'    Public Const LVM_ENSUREVISIBLE As Integer = &H1000 + 19
'    Public Const LVM_SCROLL As Integer = &H1000 + 20
'    Public Const LVM_REDRAWITEMS As Integer = &H1000 + 21
'    Public Const LVA_DEFAULT As Integer = &H0
'    Public Const LVA_ALIGNLEFT As Integer = &H1
'    Public Const LVA_ALIGNTOP As Integer = &H2
'    Public Const LVA_SNAPTOGRID As Integer = &H5
'    Public Const LVM_ARRANGE As Integer = &H1000 + 22
        Public Const LVM_EDITLABELA As Integer = &H1000 + 23
        Public Const LVM_EDITLABELW As Integer = &H1000 + 118
'    Public Const LVM_GETEDITCONTROL As Integer = &H1000 + 24
'    Public Const LVCF_FMT As Integer = &H1
'    Public Const LVCF_WIDTH As Integer = &H2
'    Public Const LVCF_TEXT As Integer = &H4
'    Public Const LVCF_SUBITEM As Integer = &H8
'    Public Const LVCF_IMAGE As Integer = &H10
'    Public Const LVCF_ORDER As Integer = &H20
'    Public Const LVCFMT_LEFT As Integer = &H0
'    Public Const LVCFMT_RIGHT As Integer = &H1
'    Public Const LVCFMT_CENTER As Integer = &H2
'    Public Const LVCFMT_JUSTIFYMASK As Integer = &H3
'    Public Const LVCFMT_IMAGE As Integer = &H800
'    Public Const LVCFMT_BITMAP_ON_RIGHT As Integer = &H1000
'    Public Const LVCFMT_COL_HAS_IMAGES As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        LVCFMT_BITMAP_ON_RIGHT = 0x1000,
'    '        LVCFMT_COL_HAS_IMAGES = unchecked((int)0x8000),
'    '---------------------------------^--- GenCode(token): unexpected token type
'    Public Const LVM_GETCOLUMNA As Integer = &H1000 + 25
'    Public Const LVM_GETCOLUMNW As Integer = &H1000 + 95
'    Public Const LVM_SETCOLUMNA As Integer = &H1000 + 26
'    Public Const LVM_SETCOLUMNW As Integer = &H1000 + 96
'    Public Const LVM_INSERTCOLUMNA As Integer = &H1000 + 27
'    Public Const LVM_INSERTCOLUMNW As Integer = &H1000 + 97
'    Public Const LVM_DELETECOLUMN As Integer = &H1000 + 28
'    Public Const LVM_GETCOLUMNWIDTH As Integer = &H1000 + 29
'    Public Const LVSCW_AUTOSIZE As Integer = - 1
'    Public Const LVSCW_AUTOSIZE_USEHEADER As Integer = - 2
'    Public Const LVM_SETCOLUMNWIDTH As Integer = &H1000 + 30
     Public Const LVM_GETHEADER As Integer = &H1000 + 31
'    Public Const LVM_CREATEDRAGIMAGE As Integer = &H1000 + 33
'    Public Const LVM_GETVIEWRECT As Integer = &H1000 + 34
'    Public Const LVM_GETTEXTCOLOR As Integer = &H1000 + 35
'    Public Const LVM_SETTEXTCOLOR As Integer = &H1000 + 36
'    Public Const LVM_GETTEXTBKCOLOR As Integer = &H1000 + 37
'    Public Const LVM_SETTEXTBKCOLOR As Integer = &H1000 + 38
'    Public Const LVM_GETTOPINDEX As Integer = &H1000 + 39
'    Public Const LVM_GETCOUNTPERPAGE As Integer = &H1000 + 40
'    Public Const LVM_GETORIGIN As Integer = &H1000 + 41
'    Public Const LVM_UPDATE As Integer = &H1000 + 42
'    Public Const LVM_SETITEMSTATE As Integer = &H1000 + 43
'    Public Const LVM_GETITEMSTATE As Integer = &H1000 + 44
'    Public Const LVM_GETITEMTEXTA As Integer = &H1000 + 45
'    Public Const LVM_GETITEMTEXTW As Integer = &H1000 + 115
'    Public Const LVM_SETITEMTEXTA As Integer = &H1000 + 46
'    Public Const LVM_SETITEMTEXTW As Integer = &H1000 + 116
'    Public Const LVSICF_NOINVALIDATEALL As Integer = &H1
'    Public Const LVSICF_NOSCROLL As Integer = &H2
'    Public Const LVM_SETITEMCOUNT As Integer = &H1000 + 47
'    Public Const LVM_SORTITEMS As Integer = &H1000 + 48
'    Public Const LVM_SETITEMPOSITION32 As Integer = &H1000 + 49
'    Public Const LVM_GETSELECTEDCOUNT As Integer = &H1000 + 50
'    Public Const LVM_GETITEMSPACING As Integer = &H1000 + 51
'    Public Const LVM_GETISEARCHSTRINGA As Integer = &H1000 + 52
'    Public Const LVM_GETISEARCHSTRINGW As Integer = &H1000 + 117
'    Public Const LVM_SETICONSPACING As Integer = &H1000 + 53
'    Public Const LVM_SETEXTENDEDLISTVIEWSTYLE As Integer = &H1000 + 54
'    Public Const LVM_GETEXTENDEDLISTVIEWSTYLE As Integer = &H1000 + 55
'    Public Const LVS_EX_GRIDLINES As Integer = &H1
'    Public Const LVS_EX_SUBITEMIMAGES As Integer = &H2
'    Public Const LVS_EX_CHECKBOXES As Integer = &H4
'    Public Const LVS_EX_TRACKSELECT As Integer = &H8
'    Public Const LVS_EX_HEADERDRAGDROP As Integer = &H10
'    Public Const LVS_EX_FULLROWSELECT As Integer = &H20
'    Public Const LVS_EX_ONECLICKACTIVATE As Integer = &H40
'    Public Const LVS_EX_TWOCLICKACTIVATE As Integer = &H80
'    Public Const LVS_EX_FLATSB As Integer = &H100
'    Public Const LVS_EX_REGIONAL As Integer = &H200
'    Public Const LVS_EX_INFOTIP As Integer = &H400
'    Public Const LVS_EX_UNDERLINEHOT As Integer = &H800
'    Public Const LVS_EX_UNDERLINECOLD As Integer = &H1000
'    Public Const LVS_EX_MULTIWORKAREAS As Integer = &H2000
'    Public Const LVM_GETSUBITEMRECT As Integer = &H1000 + 56
'    Public Const LVM_SUBITEMHITTEST As Integer = &H1000 + 57
'    Public Const LVM_SETCOLUMNORDERARRAY As Integer = &H1000 + 58
'    Public Const LVM_GETCOLUMNORDERARRAY As Integer = &H1000 + 59
'    Public Const LVM_SETHOTITEM As Integer = &H1000 + 60
'    Public Const LVM_GETHOTITEM As Integer = &H1000 + 61
'    Public Const LVM_SETHOTCURSOR As Integer = &H1000 + 62
'    Public Const LVM_GETHOTCURSOR As Integer = &H1000 + 63
'    Public Const LVM_APPROXIMATEVIEWRECT As Integer = &H1000 + 64
'    Public Const LVM_SETWORKAREA As Integer = &H1000 + 65
'    Public Const LVN_ITEMCHANGING As Integer = 0 - 100 - 0
'    Public Const LVN_ITEMCHANGED As Integer = 0 - 100 - 1
'    Public Const LVN_INSERTITEM As Integer = 0 - 100 - 2
'    Public Const LVN_DELETEITEM As Integer = 0 - 100 - 3
'    Public Const LVN_DELETEALLITEMS As Integer = 0 - 100 - 4
'    Public Const LVN_BEGINLABELEDITA As Integer = 0 - 100 - 5
'    Public Const LVN_BEGINLABELEDITW As Integer = 0 - 100 - 75
'    Public Const LVN_ENDLABELEDITA As Integer = 0 - 100 - 6
'    Public Const LVN_ENDLABELEDITW As Integer = 0 - 100 - 76
'    Public Const LVN_COLUMNCLICK As Integer = 0 - 100 - 8
'    Public Const LVN_BEGINDRAG As Integer = 0 - 100 - 9
'    Public Const LVN_BEGINRDRAG As Integer = 0 - 100 - 11
'    Public Const LVN_ODCACHEHINT As Integer = 0 - 100 - 13
'    Public Const LVN_ODFINDITEMA As Integer = 0 - 100 - 52
'    Public Const LVN_ODFINDITEMW As Integer = 0 - 100 - 79
'    Public Const LVN_ITEMACTIVATE As Integer = 0 - 100 - 14
'    Public Const LVN_ODSTATECHANGED As Integer = 0 - 100 - 15
'    Public Const LVN_GETDISPINFOA As Integer = 0 - 100 - 50
'    Public Const LVN_GETDISPINFOW As Integer = 0 - 100 - 77
'    Public Const LVN_SETDISPINFOA As Integer = 0 - 100 - 51
'    Public Const LVN_SETDISPINFOW As Integer = 0 - 100 - 78
'    Public Const LVIF_DI_SETITEM As Integer = &H1000
'    Public Const LVN_KEYDOWN As Integer = 0 - 100 - 55
'    Public Const LWA_COLORKEY As Integer = &H1
'    Public Const LWA_ALPHA As Integer = &H2
'    Public Const LVN_MARQUEEBEGIN As Integer = 0 - 100 - 56
'    ' nt5 begin 
'    ' nt5 end 
    
    
    
    
    
'    Public Const MSGF_DDEMGR As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '
'    '        public const int MSGF_DDEMGR = unchecked((int)0x8001),
'    '----------------------------------------^--- GenCode(token): unexpected token type
'    Public Const MH_CREATE As Integer = 1
'    Public Const MH_KEEP As Integer = 2
'    Public Const MH_DELETE As Integer = 3
'    Public Const MH_CLEANUP As Integer = 4
'    Public Const MAX_MONITORS As Integer = 4
'    Public Const MF_HSZ_INFO As Integer = &H1000000
'    Public Const MF_SENDMSGS As Integer = &H2000000
'    Public Const MF_POSTMSGS As Integer = &H4000000
'    Public Const MF_CALLBACKS As Integer = &H8000000
'    Public Const MF_ERRORS As Integer = &H10000000
'    Public Const MF_LINKS As Integer = &H20000000
'    Public Const MF_CONV As Integer = &H40000000
'    Public Const MF_MASK As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        MF_CONV = 0x40000000,
'    '        MF_MASK = unchecked((int)0xFF000000),
'    '-------------------^--- GenCode(token): unexpected token type
'    Public Const MULTIFILEOPENORD As Integer = 1537
'    Public Const MOD_ALT As Integer = &H1
'    Public Const MOD_CONTROL As Integer = &H2
'    Public Const MOD_SHIFT As Integer = &H4
'    Public Const MOD_LEFT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        MOD_SHIFT = 0x0004,
'    '        MOD_LEFT = unchecked((int)0x8000),
'    '--------------------^--- GenCode(token): unexpected token type
'    Public Const MOD_RIGHT As Integer = &H4000
'    Public Const MOD_ON_KEYUP As Integer = &H800
'    Public Const MOD_IGNORE_ALL_MODIFIER As Integer = &H400
'    Public Const MDMVOLFLAG_LOW As Integer = &H1
'    Public Const MDMVOLFLAG_MEDIUM As Integer = &H2
'    Public Const MDMVOLFLAG_HIGH As Integer = &H4
'    Public Const MDMVOL_LOW As Integer = &H0
'    Public Const MDMVOL_MEDIUM As Integer = &H1
'    Public Const MDMVOL_HIGH As Integer = &H2
'    Public Const MDMSPKRFLAG_OFF As Integer = &H1
'    Public Const MDMSPKRFLAG_DIAL As Integer = &H2
'    Public Const MDMSPKRFLAG_ON As Integer = &H4
'    Public Const MDMSPKRFLAG_CALLSETUP As Integer = &H8
'    Public Const MDMSPKR_OFF As Integer = &H0
'    Public Const MDMSPKR_DIAL As Integer = &H1
'    Public Const MDMSPKR_ON As Integer = &H2
'    Public Const MDMSPKR_CALLSETUP As Integer = &H3
'    Public Const MDM_COMPRESSION As Integer = &H1
'    Public Const MDM_ERROR_CONTROL As Integer = &H2
'    Public Const MDM_FORCED_EC As Integer = &H4
'    Public Const MDM_CELLULAR As Integer = &H8
'    Public Const MDM_FLOWCONTROL_HARD As Integer = &H10
'    Public Const MDM_FLOWCONTROL_SOFT As Integer = &H20
'    Public Const MDM_CCITT_OVERRIDE As Integer = &H40
'    Public Const MDM_SPEED_ADJUST As Integer = &H80
'    Public Const MDM_TONE_DIAL As Integer = &H100
'    Public Const MDM_BLIND_DIAL As Integer = &H200
'    Public Const MDM_V23_OVERRIDE As Integer = &H400
'    Public Const MAXPNAMELEN As Integer = 32
'    Public Const MAXERRORLENGTH As Integer = 256
'    Public Const MAX_JOYSTICKOEMVXDNAME As Integer = 260
'    Public Const MM_MICROSOFT As Integer = 1
'    Public Const MM_MIDI_MAPPER As Integer = 1
'    Public Const MM_WAVE_MAPPER As Integer = 2
'    Public Const MM_SNDBLST_MIDIOUT As Integer = 3
'    Public Const MM_SNDBLST_MIDIIN As Integer = 4
'    Public Const MM_SNDBLST_SYNTH As Integer = 5
'    Public Const MM_SNDBLST_WAVEOUT As Integer = 6
'    Public Const MM_SNDBLST_WAVEIN As Integer = 7
'    Public Const MM_ADLIB As Integer = 9
'    Public Const MM_MPU401_MIDIOUT As Integer = 10
'    Public Const MM_MPU401_MIDIIN As Integer = 11
'    Public Const MM_PC_JOYSTICK As Integer = 12
'    Public Const MM_JOY1MOVE As Integer = &H3A0
'    Public Const MM_JOY2MOVE As Integer = &H3A1
'    Public Const MM_JOY1ZMOVE As Integer = &H3A2
'    Public Const MM_JOY2ZMOVE As Integer = &H3A3
'    Public Const MM_JOY1BUTTONDOWN As Integer = &H3B5
'    Public Const MM_JOY2BUTTONDOWN As Integer = &H3B6
'    Public Const MM_JOY1BUTTONUP As Integer = &H3B7
'    Public Const MM_JOY2BUTTONUP As Integer = &H3B8
'    Public Const MM_MCINOTIFY As Integer = &H3B9
'    Public Const MM_WOM_OPEN As Integer = &H3BB
'    Public Const MM_WOM_CLOSE As Integer = &H3BC
'    Public Const MM_WOM_DONE As Integer = &H3BD
'    Public Const MM_WIM_OPEN As Integer = &H3BE
'    Public Const MM_WIM_CLOSE As Integer = &H3BF
'    Public Const MM_WIM_DATA As Integer = &H3C0
'    Public Const MM_MIM_OPEN As Integer = &H3C1
'    Public Const MM_MIM_CLOSE As Integer = &H3C2
'    Public Const MM_MIM_DATA As Integer = &H3C3
'    Public Const MM_MIM_LONGDATA As Integer = &H3C4
'    Public Const MM_MIM_ERROR As Integer = &H3C5
'    Public Const MM_MIM_LONGERROR As Integer = &H3C6
'    Public Const MM_MOM_OPEN As Integer = &H3C7
'    Public Const MM_MOM_CLOSE As Integer = &H3C8
'    Public Const MM_MOM_DONE As Integer = &H3C9
'    Public Const MM_DRVM_OPEN As Integer = &H3D0
'    Public Const MM_DRVM_CLOSE As Integer = &H3D1
'    Public Const MM_DRVM_DATA As Integer = &H3D2
'    Public Const MM_DRVM_ERROR As Integer = &H3D3
'    Public Const MM_STREAM_OPEN As Integer = &H3D4
'    Public Const MM_STREAM_CLOSE As Integer = &H3D5
'    Public Const MM_STREAM_DONE As Integer = &H3D6
'    Public Const MM_STREAM_ERROR As Integer = &H3D7
'    Public Const MM_MOM_POSITIONCB As Integer = &H3CA
'    Public Const MM_MCISIGNAL As Integer = &H3CB
'    Public Const MM_MIM_MOREDATA As Integer = &H3CC
'    Public Const MM_MIXM_LINE_CHANGE As Integer = &H3D0
'    Public Const MM_MIXM_CONTROL_CHANGE As Integer = &H3D1
'    Public Const MMSYSERR_BASE As Integer = 0
'    Public Const MIDIERR_BASE As Integer = 64
'    Public Const MCIERR_BASE As Integer = 256
'    Public Const MIXERR_BASE As Integer = 1024
'    Public Const MCI_STRING_OFFSET As Integer = 512
'    Public Const MCI_VD_OFFSET As Integer = 1024
'    Public Const MCI_CD_OFFSET As Integer = 1088
'    Public Const MCI_WAVE_OFFSET As Integer = 1152
'    Public Const MCI_SEQ_OFFSET As Integer = 1216
'    Public Const MMSYSERR_NOERROR As Integer = 0
'    Public Const MMSYSERR_ERROR As Integer = 0 + 1
'    Public Const MMSYSERR_BADDEVICEID As Integer = 0 + 2
'    Public Const MMSYSERR_NOTENABLED As Integer = 0 + 3
'    Public Const MMSYSERR_ALLOCATED As Integer = 0 + 4
'    Public Const MMSYSERR_INVALHANDLE As Integer = 0 + 5
'    Public Const MMSYSERR_NODRIVER As Integer = 0 + 6
'    Public Const MMSYSERR_NOMEM As Integer = 0 + 7
'    Public Const MMSYSERR_NOTSUPPORTED As Integer = 0 + 8
'    Public Const MMSYSERR_BADERRNUM As Integer = 0 + 9
'    Public Const MMSYSERR_INVALFLAG As Integer = 0 + 10
'    Public Const MMSYSERR_INVALPARAM As Integer = 0 + 11
'    Public Const MMSYSERR_HANDLEBUSY As Integer = 0 + 12
'    Public Const MMSYSERR_INVALIDALIAS As Integer = 0 + 13
'    Public Const MMSYSERR_BADDB As Integer = 0 + 14
'    Public Const MMSYSERR_KEYNOTFOUND As Integer = 0 + 15
'    Public Const MMSYSERR_READERROR As Integer = 0 + 16
'    Public Const MMSYSERR_WRITEERROR As Integer = 0 + 17
'    Public Const MMSYSERR_DELETEERROR As Integer = 0 + 18
'    Public Const MMSYSERR_VALNOTFOUND As Integer = 0 + 19
'    Public Const MMSYSERR_NODRIVERCB As Integer = 0 + 20
'    Public Const MMSYSERR_LASTERROR As Integer = 0 + 20
'    Public Const MIDIERR_UNPREPARED As Integer = 64 + 0
'    Public Const MIDIERR_STILLPLAYING As Integer = 64 + 1
'    Public Const MIDIERR_NOMAP As Integer = 64 + 2
'    Public Const MIDIERR_NOTREADY As Integer = 64 + 3
'    Public Const MIDIERR_NODEVICE As Integer = 64 + 4
'    Public Const MIDIERR_INVALIDSETUP As Integer = 64 + 5
'    Public Const MIDIERR_BADOPENMODE As Integer = 64 + 6
'    Public Const MIDIERR_DONT_CONTINUE As Integer = 64 + 7
'    Public Const MIDIERR_LASTERROR As Integer = 64 + 7
'    Public Const MIDIPATCHSIZE As Integer = 128
'    Public Const MIM_OPEN As Integer = &H3C1
'    Public Const MIM_CLOSE As Integer = &H3C2
'    Public Const MIM_DATA As Integer = &H3C3
'    Public Const MIM_LONGDATA As Integer = &H3C4
'    Public Const MIM_ERROR As Integer = &H3C5
'    Public Const MIM_LONGERROR As Integer = &H3C6
'    Public Const MOM_OPEN As Integer = &H3C7
'    Public Const MOM_CLOSE As Integer = &H3C8
'    Public Const MOM_DONE As Integer = &H3C9
'    Public Const MIM_MOREDATA As Integer = &H3CC
'    Public Const MOM_POSITIONCB As Integer = &H3CA
'    Public Const MIDI_IO_STATUS As Integer = &H20
'    Public Const MIDI_CACHE_ALL As Integer = 1
'    Public Const MIDI_CACHE_BESTFIT As Integer = 2
'    Public Const MIDI_CACHE_QUERY As Integer = 3
'    Public Const MIDI_UNCACHE As Integer = 4
'    Public Const MOD_MIDIPORT As Integer = 1
'    Public Const MOD_SYNTH As Integer = 2
'    Public Const MOD_SQSYNTH As Integer = 3
'    Public Const MOD_FMSYNTH As Integer = 4
'    Public Const MOD_MAPPER As Integer = 5
'    Public Const MIDICAPS_VOLUME As Integer = &H1
'    Public Const MIDICAPS_LRVOLUME As Integer = &H2
'    Public Const MIDICAPS_CACHE As Integer = &H4
'    Public Const MIDICAPS_STREAM As Integer = &H8
'    Public Const MHDR_DONE As Integer = &H1
'    Public Const MHDR_PREPARED As Integer = &H2
'    Public Const MHDR_INQUEUE As Integer = &H4
'    Public Const MHDR_ISSTRM As Integer = &H8
'    Public Const MEVT_F_SHORT As Integer = &H0
'    Public Const MEVT_F_LONG As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        MEVT_F_SHORT = 0x00000000,
'    '        MEVT_F_LONG = unchecked((int)0x80000000),
'    '-----------------------^--- GenCode(token): unexpected token type
'    Public Const MEVT_F_CALLBACK As Integer = &H40000000
'    Public Const MEVT_SHORTMSG As Integer = &H0
'    Public Const MEVT_TEMPO As Integer = &H1
'    Public Const MEVT_NOP As Integer = &H2
'    Public Const MEVT_LONGMSG As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                     MEVT_NOP = (0x02),
'    '                                                MEVT_LONGMSG = (unchecked((int)0x80)),
'    '-----------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const MEVT_COMMENT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                MEVT_LONGMSG = (unchecked((int)0x80)),
'    '                                                               MEVT_COMMENT = (unchecked((int)0x82)),
'    '--------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const MEVT_VERSION As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                               MEVT_COMMENT = (unchecked((int)0x82)),
'    '                                                                              MEVT_VERSION = (unchecked((int)0x84)),
'    '-----------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const MIDISTRM_ERROR As Integer = - 2
'    Public Const MIDIPROP_SET As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                             MIDISTRM_ERROR = (-2),
'    '                                                                                                              MIDIPROP_SET = unchecked((int)0x80000000),
'    '------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const MIDIPROP_GET As Integer = &H40000000
'    Public Const MIDIPROP_TIMEDIV As Integer = &H1
'    Public Const MIDIPROP_TEMPO As Integer = &H2
'    Public Const MIXER_SHORT_NAME_CHARS As Integer = 16
'    Public Const MIXER_LONG_NAME_CHARS As Integer = 64
'    Public Const MIXERR_INVALLINE As Integer = 1024 + 0
'    Public Const MIXERR_INVALCONTROL As Integer = 1024 + 1
'    Public Const MIXERR_INVALVALUE As Integer = 1024 + 2
'    Public Const MIXERR_LASTERROR As Integer = 1024 + 2
'    Public Const MIXER_OBJECTF_HANDLE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                     MIXERR_LASTERROR = (1024+2),
'    '                                                                                        MIXER_OBJECTF_HANDLE = unchecked((int)0x80000000),
'    '----------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const MIXER_OBJECTF_MIXER As Integer = &H0
'    Public Const MIXER_OBJECTF_HMIXER As Integer = Or &H0
'    '
'    'Note:  Error processing original source shown below
'    '        MIXER_OBJECTF_MIXER = 0x00000000,
'    '        MIXER_OBJECTF_HMIXER = (unchecked((int)0x80000000)|0x00000000),
'    '---------------------------------^--- GenCode(token): unexpected token type
'    Public Const MIXER_OBJECTF_WAVEOUT As Integer = &H10000000
'    Public Const MIXER_OBJECTF_HWAVEOUT As Integer = Or &H10000000
'    '
'    'Note:  Error processing original source shown below
'    '                               MIXER_OBJECTF_WAVEOUT = 0x10000000,
'    '        MIXER_OBJECTF_HWAVEOUT = (unchecked((int)0x80000000)|0x10000000),
'    '-----------------------------------^--- GenCode(token): unexpected token type
'    Public Const MIXER_OBJECTF_WAVEIN As Integer = &H20000000
'    Public Const MIXER_OBJECTF_HWAVEIN As Integer = Or &H20000000
'    '
'    'Note:  Error processing original source shown below
'    '                                 MIXER_OBJECTF_WAVEIN = 0x20000000,
'    '        MIXER_OBJECTF_HWAVEIN = (unchecked((int)0x80000000)|0x20000000),
'    '----------------------------------^--- GenCode(token): unexpected token type
'    Public Const MIXER_OBJECTF_MIDIOUT As Integer = &H30000000
'    Public Const MIXER_OBJECTF_HMIDIOUT As Integer = Or &H30000000
'    '
'    'Note:  Error processing original source shown below
'    '                                MIXER_OBJECTF_MIDIOUT = 0x30000000,
'    '        MIXER_OBJECTF_HMIDIOUT = (unchecked((int)0x80000000)|0x30000000),
'    '-----------------------------------^--- GenCode(token): unexpected token type
'    Public Const MIXER_OBJECTF_MIDIIN As Integer = &H40000000
'    Public Const MIXER_OBJECTF_HMIDIIN As Integer = Or &H40000000
'    '
'    'Note:  Error processing original source shown below
'    '                                 MIXER_OBJECTF_MIDIIN = 0x40000000,
'    '        MIXER_OBJECTF_HMIDIIN = (unchecked((int)0x80000000)|0x40000000),
'    '----------------------------------^--- GenCode(token): unexpected token type
'    Public Const MIXER_OBJECTF_AUX As Integer = &H50000000
'    Public Const MIXERLINE_LINEF_ACTIVE As Integer = &H1
'    Public Const MIXERLINE_LINEF_DISCONNECTED As Integer = &H8000
'    Public Const MIXERLINE_LINEF_SOURCE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        MIXERLINE_LINEF_DISCONNECTED = 0x00008000,
'    '        MIXERLINE_LINEF_SOURCE = unchecked((int)0x80000000),
'    '----------------------------------^--- GenCode(token): unexpected token type
'    Public Const MIXERLINE_COMPONENTTYPE_DST_FIRST As Integer = &H0
'    Public Const MIXERLINE_COMPONENTTYPE_DST_UNDEFINED As Integer = &H0 + 0
'    Public Const MIXERLINE_COMPONENTTYPE_DST_DIGITAL As Integer = &H0 + 1
'    Public Const MIXERLINE_COMPONENTTYPE_DST_LINE As Integer = &H0 + 2
'    Public Const MIXERLINE_COMPONENTTYPE_DST_MONITOR As Integer = &H0 + 3
'    Public Const MIXERLINE_COMPONENTTYPE_DST_SPEAKERS As Integer = &H0 + 4
'    Public Const MIXERLINE_COMPONENTTYPE_DST_HEADPHONES As Integer = &H0 + 5
'    Public Const MIXERLINE_COMPONENTTYPE_DST_TELEPHONE As Integer = &H0 + 6
'    Public Const MIXERLINE_COMPONENTTYPE_DST_WAVEIN As Integer = &H0 + 7
'    Public Const MIXERLINE_COMPONENTTYPE_DST_VOICEIN As Integer = &H0 + 8
'    Public Const MIXERLINE_COMPONENTTYPE_DST_LAST As Integer = &H0 + 8
'    Public Const MIXERLINE_COMPONENTTYPE_SRC_FIRST As Integer = &H1000
'    Public Const MIXERLINE_COMPONENTTYPE_SRC_UNDEFINED As Integer = &H1000 + 0
'    Public Const MIXERLINE_COMPONENTTYPE_SRC_DIGITAL As Integer = &H1000 + 1
'    Public Const MIXERLINE_COMPONENTTYPE_SRC_LINE As Integer = &H1000 + 2
'    Public Const MIXERLINE_COMPONENTTYPE_SRC_MICROPHONE As Integer = &H1000 + 3
'    Public Const MIXERLINE_COMPONENTTYPE_SRC_SYNTHESIZER As Integer = &H1000 + 4
'    Public Const MIXERLINE_COMPONENTTYPE_SRC_COMPACTDISC As Integer = &H1000 + 5
'    Public Const MIXERLINE_COMPONENTTYPE_SRC_TELEPHONE As Integer = &H1000 + 6
'    Public Const MIXERLINE_COMPONENTTYPE_SRC_PCSPEAKER As Integer = &H1000 + 7
'    Public Const MIXERLINE_COMPONENTTYPE_SRC_WAVEOUT As Integer = &H1000 + 8
'    Public Const MIXERLINE_COMPONENTTYPE_SRC_AUXILIARY As Integer = &H1000 + 9
'    Public Const MIXERLINE_COMPONENTTYPE_SRC_ANALOG As Integer = &H1000 + 10
'    Public Const MIXERLINE_COMPONENTTYPE_SRC_LAST As Integer = &H1000 + 10
'    Public Const MIXERLINE_TARGETTYPE_UNDEFINED As Integer = 0
'    Public Const MIXERLINE_TARGETTYPE_WAVEOUT As Integer = 1
'    Public Const MIXERLINE_TARGETTYPE_WAVEIN As Integer = 2
'    Public Const MIXERLINE_TARGETTYPE_MIDIOUT As Integer = 3
'    Public Const MIXERLINE_TARGETTYPE_MIDIIN As Integer = 4
'    Public Const MIXERLINE_TARGETTYPE_AUX As Integer = 5
'    Public Const MIXER_GETLINEINFOF_DESTINATION As Integer = &H0
'    Public Const MIXER_GETLINEINFOF_SOURCE As Integer = &H1
'    Public Const MIXER_GETLINEINFOF_LINEID As Integer = &H2
'    Public Const MIXER_GETLINEINFOF_COMPONENTTYPE As Integer = &H3
'    Public Const MIXER_GETLINEINFOF_TARGETTYPE As Integer = &H4
'    Public Const MIXER_GETLINEINFOF_QUERYMASK As Integer = &HF
'    Public Const MIXERCONTROL_CONTROLF_UNIFORM As Integer = &H1
'    Public Const MIXERCONTROL_CONTROLF_MULTIPLE As Integer = &H2
'    Public Const MIXERCONTROL_CONTROLF_DISABLED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        MIXERCONTROL_CONTROLF_MULTIPLE = 0x00000002,
'    '        MIXERCONTROL_CONTROLF_DISABLED = unchecked((int)0x80000000),
'    '------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const MIXERCONTROL_CT_CLASS_MASK As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        MIXERCONTROL_CONTROLF_DISABLED = unchecked((int)0x80000000),
'    '        MIXERCONTROL_CT_CLASS_MASK = unchecked((int)0xF0000000),
'    '--------------------------------------^--- GenCode(token): unexpected token type
'    Public Const MIXERCONTROL_CT_CLASS_CUSTOM As Integer = &H0
'    Public Const MIXERCONTROL_CT_CLASS_METER As Integer = &H10000000
'    Public Const MIXERCONTROL_CT_CLASS_SWITCH As Integer = &H20000000
'    Public Const MIXERCONTROL_CT_CLASS_NUMBER As Integer = &H30000000
'    Public Const MIXERCONTROL_CT_CLASS_SLIDER As Integer = &H40000000
'    Public Const MIXERCONTROL_CT_CLASS_FADER As Integer = &H50000000
'    Public Const MIXERCONTROL_CT_CLASS_TIME As Integer = &H60000000
'    Public Const MIXERCONTROL_CT_CLASS_LIST As Integer = &H70000000
'    Public Const MIXERCONTROL_CT_SUBCLASS_MASK As Integer = &HF000000
'    Public Const MIXERCONTROL_CT_SC_SWITCH_BOOLEAN As Integer = &H0
'    Public Const MIXERCONTROL_CT_SC_SWITCH_BUTTON As Integer = &H1000000
'    Public Const MIXERCONTROL_CT_SC_METER_POLLED As Integer = &H0
'    Public Const MIXERCONTROL_CT_SC_TIME_MICROSECS As Integer = &H0
'    Public Const MIXERCONTROL_CT_SC_TIME_MILLISECS As Integer = &H1000000
'    Public Const MIXERCONTROL_CT_SC_LIST_SINGLE As Integer = &H0
'    Public Const MIXERCONTROL_CT_SC_LIST_MULTIPLE As Integer = &H1000000
'    Public Const MIXERCONTROL_CT_UNITS_MASK As Integer = &HFF0000
'    Public Const MIXERCONTROL_CT_UNITS_CUSTOM As Integer = &H0
'    Public Const MIXERCONTROL_CT_UNITS_BOOLEAN As Integer = &H10000
'    Public Const MIXERCONTROL_CT_UNITS_SIGNED As Integer = &H20000
'    Public Const MIXERCONTROL_CT_UNITS_UNSIGNED As Integer = &H30000
'    Public Const MIXERCONTROL_CT_UNITS_DECIBELS As Integer = &H40000
'    Public Const MIXERCONTROL_CT_UNITS_PERCENT As Integer = &H50000
'    Public Const MIXERCONTROL_CONTROLTYPE_CUSTOM As Integer = &H0 Or &H0
'    Public Const MIXERCONTROL_CONTROLTYPE_BOOLEANMETER As Integer = &H10000000 Or &H0 Or &H10000
'    Public Const MIXERCONTROL_CONTROLTYPE_SIGNEDMETER As Integer = &H10000000 Or &H0 Or &H20000
'    Public Const MIXERCONTROL_CONTROLTYPE_PEAKMETER As Integer = (&H10000000 Or &H0 Or &H20000) + 1
'    Public Const MIXERCONTROL_CONTROLTYPE_UNSIGNEDMETER As Integer = &H10000000 Or &H0 Or &H30000
'    Public Const MIXERCONTROL_CONTROLTYPE_BOOLEAN As Integer = &H20000000 Or &H0 Or &H10000
'    Public Const MIXERCONTROL_CONTROLTYPE_ONOFF As Integer = (&H20000000 Or &H0 Or &H10000) + 1
'    Public Const MIXERCONTROL_CONTROLTYPE_MUTE As Integer = (&H20000000 Or &H0 Or &H10000) + 2
'    Public Const MIXERCONTROL_CONTROLTYPE_MONO As Integer = (&H20000000 Or &H0 Or &H10000) + 3
'    Public Const MIXERCONTROL_CONTROLTYPE_LOUDNESS As Integer = (&H20000000 Or &H0 Or &H10000) + 4
'    Public Const MIXERCONTROL_CONTROLTYPE_STEREOENH As Integer = (&H20000000 Or &H0 Or &H10000) + 5
'    Public Const MIXERCONTROL_CONTROLTYPE_BUTTON As Integer = &H20000000 Or &H1000000 Or &H10000
'    Public Const MIXERCONTROL_CONTROLTYPE_DECIBELS As Integer = &H30000000 Or &H40000
'    Public Const MIXERCONTROL_CONTROLTYPE_SIGNED As Integer = &H30000000 Or &H20000
'    Public Const MIXERCONTROL_CONTROLTYPE_UNSIGNED As Integer = &H30000000 Or &H30000
'    Public Const MIXERCONTROL_CONTROLTYPE_PERCENT As Integer = &H30000000 Or &H50000
'    Public Const MIXERCONTROL_CONTROLTYPE_SLIDER As Integer = &H40000000 Or &H20000
'    Public Const MIXERCONTROL_CONTROLTYPE_PAN As Integer = (&H40000000 Or &H20000) + 1
'    Public Const MIXERCONTROL_CONTROLTYPE_QSOUNDPAN As Integer = (&H40000000 Or &H20000) + 2
'    Public Const MIXERCONTROL_CONTROLTYPE_FADER As Integer = &H50000000 Or &H30000
'    Public Const MIXERCONTROL_CONTROLTYPE_VOLUME As Integer = (&H50000000 Or &H30000) + 1
'    Public Const MIXERCONTROL_CONTROLTYPE_BASS As Integer = (&H50000000 Or &H30000) + 2
'    Public Const MIXERCONTROL_CONTROLTYPE_TREBLE As Integer = (&H50000000 Or &H30000) + 3
'    Public Const MIXERCONTROL_CONTROLTYPE_EQUALIZER As Integer = (&H50000000 Or &H30000) + 4
'    Public Const MIXERCONTROL_CONTROLTYPE_SINGLESELECT As Integer = &H70000000 Or &H0 Or &H10000
'    Public Const MIXERCONTROL_CONTROLTYPE_MUX As Integer = (&H70000000 Or &H0 Or &H10000) + 1
'    Public Const MIXERCONTROL_CONTROLTYPE_MULTIPLESELECT As Integer = &H70000000 Or &H1000000 Or &H10000
'    Public Const MIXERCONTROL_CONTROLTYPE_MIXER As Integer = (&H70000000 Or &H1000000 Or &H10000) + 1
'    Public Const MIXERCONTROL_CONTROLTYPE_MICROTIME As Integer = &H60000000 Or &H0 Or &H30000
'    Public Const MIXERCONTROL_CONTROLTYPE_MILLITIME As Integer = &H60000000 Or &H1000000 Or &H30000
'    Public Const MIXER_GETLINECONTROLSF_ALL As Integer = &H0
'    Public Const MIXER_GETLINECONTROLSF_ONEBYID As Integer = &H1
'    Public Const MIXER_GETLINECONTROLSF_ONEBYTYPE As Integer = &H2
'    Public Const MIXER_GETLINECONTROLSF_QUERYMASK As Integer = &HF
'    Public Const MIXER_GETCONTROLDETAILSF_VALUE As Integer = &H0
'    Public Const MIXER_GETCONTROLDETAILSF_LISTTEXT As Integer = &H1
'    Public Const MIXER_GETCONTROLDETAILSF_QUERYMASK As Integer = &HF
'    Public Const MIXER_SETCONTROLDETAILSF_VALUE As Integer = &H0
'    Public Const MIXER_SETCONTROLDETAILSF_CUSTOM As Integer = &H1
'    Public Const MIXER_SETCONTROLDETAILSF_QUERYMASK As Integer = &HF
'    Public Const MMIOERR_BASE As Integer = 256
'    Public Const MMIOERR_FILENOTFOUND As Integer = 256 + 1
'    Public Const MMIOERR_OUTOFMEMORY As Integer = 256 + 2
'    Public Const MMIOERR_CANNOTOPEN As Integer = 256 + 3
'    Public Const MMIOERR_CANNOTCLOSE As Integer = 256 + 4
'    Public Const MMIOERR_CANNOTREAD As Integer = 256 + 5
'    Public Const MMIOERR_CANNOTWRITE As Integer = 256 + 6
'    Public Const MMIOERR_CANNOTSEEK As Integer = 256 + 7
'    Public Const MMIOERR_CANNOTEXPAND As Integer = 256 + 8
'    Public Const MMIOERR_CHUNKNOTFOUND As Integer = 256 + 9
'    Public Const MMIOERR_UNBUFFERED As Integer = 256 + 10
'    Public Const MMIOERR_PATHNOTFOUND As Integer = 256 + 11
'    Public Const MMIOERR_ACCESSDENIED As Integer = 256 + 12
'    Public Const MMIOERR_SHARINGVIOLATION As Integer = 256 + 13
'    Public Const MMIOERR_NETWORKERROR As Integer = 256 + 14
'    Public Const MMIOERR_TOOMANYOPENFILES As Integer = 256 + 15
'    Public Const MMIOERR_INVALIDFILE As Integer = 256 + 16
'    Public Const MMIO_RWMODE As Integer = &H3
'    Public Const MMIO_SHAREMODE As Integer = &H70
'    Public Const MMIO_CREATE As Integer = &H1000
'    Public Const MMIO_PARSE As Integer = &H100
'    Public Const MMIO_DELETE As Integer = &H200
'    Public Const MMIO_EXIST As Integer = &H4000
'    Public Const MMIO_ALLOCBUF As Integer = &H10000
'    Public Const MMIO_GETTEMP As Integer = &H20000
'    Public Const MMIO_DIRTY As Integer = &H10000000
'    Public Const MMIO_READ As Integer = &H0
'    Public Const MMIO_WRITE As Integer = &H1
'    Public Const MMIO_READWRITE As Integer = &H2
'    Public Const MMIO_COMPAT As Integer = &H0
'    Public Const MMIO_EXCLUSIVE As Integer = &H10
'    Public Const MMIO_DENYWRITE As Integer = &H20
'    Public Const MMIO_DENYREAD As Integer = &H30
'    Public Const MMIO_DENYNONE As Integer = &H40
'    Public Const MMIO_FHOPEN As Integer = &H10
'    Public Const MMIO_EMPTYBUF As Integer = &H10
'    Public Const MMIO_TOUPPER As Integer = &H10
'    Public Const MMIO_INSTALLPROC As Integer = &H10000
'    Public Const MMIO_GLOBALPROC As Integer = &H10000000
'    Public Const MMIO_REMOVEPROC As Integer = &H20000
'    Public Const MMIO_UNICODEPROC As Integer = &H1000000
'    Public Const MMIO_FINDPROC As Integer = &H40000
'    Public Const MMIO_FINDCHUNK As Integer = &H10
'    Public Const MMIO_FINDRIFF As Integer = &H20
'    Public Const MMIO_FINDLIST As Integer = &H40
'    Public Const MMIO_CREATERIFF As Integer = &H20
'    Public Const MMIO_CREATELIST As Integer = &H40
'    Public Const MMIOM_READ As Integer = &H0
'    Public Const MMIOM_WRITE As Integer = &H1
'    Public Const MMIOM_SEEK As Integer = 2
'    Public Const MMIOM_OPEN As Integer = 3
'    Public Const MMIOM_CLOSE As Integer = 4
'    Public Const MMIOM_WRITEFLUSH As Integer = 5
'    Public Const MMIOM_RENAME As Integer = 6
'    Public Const MMIOM_USER As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        MMIOM_RENAME = 6,
'    '        MMIOM_USER = unchecked((int)0x8000),
'    '----------------------^--- GenCode(token): unexpected token type
'    Public Const MMIO_DEFAULTBUFFER As Integer = 8192
'    Public Const MCIERR_INVALID_DEVICE_ID As Integer = 256 + 1
'    Public Const MCIERR_UNRECOGNIZED_KEYWORD As Integer = 256 + 3
'    Public Const MCIERR_UNRECOGNIZED_COMMAND As Integer = 256 + 5
'    Public Const MCIERR_HARDWARE As Integer = 256 + 6
'    Public Const MCIERR_INVALID_DEVICE_NAME As Integer = 256 + 7
'    Public Const MCIERR_OUT_OF_MEMORY As Integer = 256 + 8
'    Public Const MCIERR_DEVICE_OPEN As Integer = 256 + 9
'    Public Const MCIERR_CANNOT_LOAD_DRIVER As Integer = 256 + 10
'    Public Const MCIERR_MISSING_COMMAND_STRING As Integer = 256 + 11
'    Public Const MCIERR_PARAM_OVERFLOW As Integer = 256 + 12
'    Public Const MCIERR_MISSING_STRING_ARGUMENT As Integer = 256 + 13
'    Public Const MCIERR_BAD_INTEGER As Integer = 256 + 14
'    Public Const MCIERR_PARSER_INTERNAL As Integer = 256 + 15
'    Public Const MCIERR_DRIVER_INTERNAL As Integer = 256 + 16
'    Public Const MCIERR_MISSING_PARAMETER As Integer = 256 + 17
'    Public Const MCIERR_UNSUPPORTED_FUNCTION As Integer = 256 + 18
'    Public Const MCIERR_FILE_NOT_FOUND As Integer = 256 + 19
'    Public Const MCIERR_DEVICE_NOT_READY As Integer = 256 + 20
'    Public Const MCIERR_INTERNAL As Integer = 256 + 21
'    Public Const MCIERR_DRIVER As Integer = 256 + 22
'    Public Const MCIERR_CANNOT_USE_ALL As Integer = 256 + 23
'    Public Const MCIERR_MULTIPLE As Integer = 256 + 24
'    Public Const MCIERR_EXTENSION_NOT_FOUND As Integer = 256 + 25
'    Public Const MCIERR_OUTOFRANGE As Integer = 256 + 26
'    Public Const MCIERR_FLAGS_NOT_COMPATIBLE As Integer = 256 + 28
'    Public Const MCIERR_FILE_NOT_SAVED As Integer = 256 + 30
'    Public Const MCIERR_DEVICE_TYPE_REQUIRED As Integer = 256 + 31
'    Public Const MCIERR_DEVICE_LOCKED As Integer = 256 + 32
'    Public Const MCIERR_DUPLICATE_ALIAS As Integer = 256 + 33
'    Public Const MCIERR_MUST_USE_SHAREABLE As Integer = 256 + 35
'    Public Const MCIERR_MISSING_DEVICE_NAME As Integer = 256 + 36
'    Public Const MCIERR_BAD_TIME_FORMAT As Integer = 256 + 37
'    Public Const MCIERR_NO_CLOSING_QUOTE As Integer = 256 + 38
'    Public Const MCIERR_DUPLICATE_FLAGS As Integer = 256 + 39
'    Public Const MCIERR_INVALID_FILE As Integer = 256 + 40
'    Public Const MCIERR_NULL_PARAMETER_BLOCK As Integer = 256 + 41
'    Public Const MCIERR_UNNAMED_RESOURCE As Integer = 256 + 42
'    Public Const MCIERR_NEW_REQUIRES_ALIAS As Integer = 256 + 43
'    Public Const MCIERR_NOTIFY_ON_AUTO_OPEN As Integer = 256 + 44
'    Public Const MCIERR_NO_ELEMENT_ALLOWED As Integer = 256 + 45
'    Public Const MCIERR_NONAPPLICABLE_FUNCTION As Integer = 256 + 46
'    Public Const MCIERR_ILLEGAL_FOR_AUTO_OPEN As Integer = 256 + 47
'    Public Const MCIERR_FILENAME_REQUIRED As Integer = 256 + 48
'    Public Const MCIERR_EXTRA_CHARACTERS As Integer = 256 + 49
'    Public Const MCIERR_DEVICE_NOT_INSTALLED As Integer = 256 + 50
'    Public Const MCIERR_GET_CD As Integer = 256 + 51
'    Public Const MCIERR_SET_CD As Integer = 256 + 52
'    Public Const MCIERR_SET_DRIVE As Integer = 256 + 53
'    Public Const MCIERR_DEVICE_LENGTH As Integer = 256 + 54
'    Public Const MCIERR_DEVICE_ORD_LENGTH As Integer = 256 + 55
'    Public Const MCIERR_NO_INTEGER As Integer = 256 + 56
'    Public Const MCIERR_WAVE_OUTPUTSINUSE As Integer = 256 + 64
'    Public Const MCIERR_WAVE_SETOUTPUTINUSE As Integer = 256 + 65
'    Public Const MCIERR_WAVE_INPUTSINUSE As Integer = 256 + 66
'    Public Const MCIERR_WAVE_SETINPUTINUSE As Integer = 256 + 67
'    Public Const MCIERR_WAVE_OUTPUTUNSPECIFIED As Integer = 256 + 68
'    Public Const MCIERR_WAVE_INPUTUNSPECIFIED As Integer = 256 + 69
'    Public Const MCIERR_WAVE_OUTPUTSUNSUITABLE As Integer = 256 + 70
'    Public Const MCIERR_WAVE_SETOUTPUTUNSUITABLE As Integer = 256 + 71
'    Public Const MCIERR_WAVE_INPUTSUNSUITABLE As Integer = 256 + 72
'    Public Const MCIERR_WAVE_SETINPUTUNSUITABLE As Integer = 256 + 73
'    Public Const MCIERR_SEQ_DIV_INCOMPATIBLE As Integer = 256 + 80
'    Public Const MCIERR_SEQ_PORT_INUSE As Integer = 256 + 81
'    Public Const MCIERR_SEQ_PORT_NONEXISTENT As Integer = 256 + 82
'    Public Const MCIERR_SEQ_PORT_MAPNODEVICE As Integer = 256 + 83
'    Public Const MCIERR_SEQ_PORT_MISCERROR As Integer = 256 + 84
'    Public Const MCIERR_SEQ_TIMER As Integer = 256 + 85
'    Public Const MCIERR_SEQ_PORTUNSPECIFIED As Integer = 256 + 86
'    Public Const MCIERR_SEQ_NOMIDIPRESENT As Integer = 256 + 87
'    Public Const MCIERR_NO_WINDOW As Integer = 256 + 90
'    Public Const MCIERR_CREATEWINDOW As Integer = 256 + 91
'    Public Const MCIERR_FILE_READ As Integer = 256 + 92
'    Public Const MCIERR_FILE_WRITE As Integer = 256 + 93
'    Public Const MCIERR_NO_IDENTITY As Integer = 256 + 94
'    Public Const MCIERR_CUSTOM_DRIVER_BASE As Integer = 256 + 256
'    Public Const MCI_FIRST As Integer = &H800
'    Public Const MCI_OPEN As Integer = &H803
'    Public Const MCI_CLOSE As Integer = &H804
'    Public Const MCI_ESCAPE As Integer = &H805
'    Public Const MCI_PLAY As Integer = &H806
'    Public Const MCI_SEEK As Integer = &H807
'    Public Const MCI_STOP As Integer = &H808
'    Public Const MCI_PAUSE As Integer = &H809
'    Public Const MCI_INFO As Integer = &H80A
'    Public Const MCI_GETDEVCAPS As Integer = &H80B
'    Public Const MCI_SPIN As Integer = &H80C
'    Public Const MCI_SET As Integer = &H80D
'    Public Const MCI_STEP As Integer = &H80E
'    Public Const MCI_RECORD As Integer = &H80F
'    Public Const MCI_SYSINFO As Integer = &H810
'    Public Const MCI_BREAK As Integer = &H811
'    Public Const MCI_SAVE As Integer = &H813
'    Public Const MCI_STATUS As Integer = &H814
'    Public Const MCI_CUE As Integer = &H830
'    Public Const MCI_REALIZE As Integer = &H840
'    Public Const MCI_WINDOW As Integer = &H841
'    Public Const MCI_PUT As Integer = &H842
'    Public Const MCI_WHERE As Integer = &H843
'    Public Const MCI_FREEZE As Integer = &H844
'    Public Const MCI_UNFREEZE As Integer = &H845
'    Public Const MCI_LOAD As Integer = &H850
'    Public Const MCI_CUT As Integer = &H851
'    Public Const MCI_COPY As Integer = &H852
'    Public Const MCI_PASTE As Integer = &H853
'    Public Const MCI_UPDATE As Integer = &H854
'    Public Const MCI_RESUME As Integer = &H855
'    Public Const MCI_DELETE As Integer = &H856
'    Public Const MCI_USER_MESSAGES As Integer = &H800 + &H400
'    Public Const MCI_LAST As Integer = &HFFF
'    Public Const MCI_DEVTYPE_VCR As Integer = 513
'    Public Const MCI_DEVTYPE_VIDEODISC As Integer = 514
'    Public Const MCI_DEVTYPE_OVERLAY As Integer = 515
'    Public Const MCI_DEVTYPE_CD_AUDIO As Integer = 516
'    Public Const MCI_DEVTYPE_DAT As Integer = 517
'    Public Const MCI_DEVTYPE_SCANNER As Integer = 518
'    Public Const MCI_DEVTYPE_ANIMATION As Integer = 519
'    Public Const MCI_DEVTYPE_DIGITAL_VIDEO As Integer = 520
'    Public Const MCI_DEVTYPE_OTHER As Integer = 521
'    Public Const MCI_DEVTYPE_WAVEFORM_AUDIO As Integer = 522
'    Public Const MCI_DEVTYPE_SEQUENCER As Integer = 523
'    Public Const MCI_DEVTYPE_FIRST As Integer = 513
'    Public Const MCI_DEVTYPE_LAST As Integer = 523
'    Public Const MCI_DEVTYPE_FIRST_USER As Integer = &H1000
'    Public Const MCI_MODE_NOT_READY As Integer = 512 + 12
'    Public Const MCI_MODE_STOP As Integer = 512 + 13
'    Public Const MCI_MODE_PLAY As Integer = 512 + 14
'    Public Const MCI_MODE_RECORD As Integer = 512 + 15
'    Public Const MCI_MODE_SEEK As Integer = 512 + 16
'    Public Const MCI_MODE_PAUSE As Integer = 512 + 17
'    Public Const MCI_MODE_OPEN As Integer = 512 + 18
'    Public Const MCI_FORMAT_MILLISECONDS As Integer = 0
'    Public Const MCI_FORMAT_HMS As Integer = 1
'    Public Const MCI_FORMAT_MSF As Integer = 2
'    Public Const MCI_FORMAT_FRAMES As Integer = 3
'    Public Const MCI_FORMAT_SMPTE_24 As Integer = 4
'    Public Const MCI_FORMAT_SMPTE_25 As Integer = 5
'    Public Const MCI_FORMAT_SMPTE_30 As Integer = 6
'    Public Const MCI_FORMAT_SMPTE_30DROP As Integer = 7
'    Public Const MCI_FORMAT_BYTES As Integer = 8
'    Public Const MCI_FORMAT_SAMPLES As Integer = 9
'    Public Const MCI_FORMAT_TMSF As Integer = 10
'    Public Const MCI_NOTIFY_SUCCESSFUL As Integer = &H1
'    Public Const MCI_NOTIFY_SUPERSEDED As Integer = &H2
'    Public Const MCI_NOTIFY_ABORTED As Integer = &H4
'    Public Const MCI_NOTIFY_FAILURE As Integer = &H8
'    Public Const MCI_NOTIFY As Integer = &H1
'    Public Const MCI_WAIT As Integer = &H2
'    Public Const MCI_FROM As Integer = &H4
'    Public Const MCI_TO As Integer = &H8
'    Public Const MCI_TRACK As Integer = &H10
'    Public Const MCI_OPEN_SHAREABLE As Integer = &H100
'    Public Const MCI_OPEN_ELEMENT As Integer = &H200
'    Public Const MCI_OPEN_ALIAS As Integer = &H400
'    Public Const MCI_OPEN_ELEMENT_ID As Integer = &H800
'    Public Const MCI_OPEN_TYPE_ID As Integer = &H1000
'    Public Const MCI_OPEN_TYPE As Integer = &H2000
'    Public Const MCI_SEEK_TO_START As Integer = &H100
'    Public Const MCI_SEEK_TO_END As Integer = &H200
'    Public Const MCI_STATUS_ITEM As Integer = &H100
'    Public Const MCI_STATUS_START As Integer = &H200
'    Public Const MCI_STATUS_LENGTH As Integer = &H1
'    Public Const MCI_STATUS_POSITION As Integer = &H2
'    Public Const MCI_STATUS_NUMBER_OF_TRACKS As Integer = &H3
'    Public Const MCI_STATUS_MODE As Integer = &H4
'    Public Const MCI_STATUS_MEDIA_PRESENT As Integer = &H5
'    Public Const MCI_STATUS_TIME_FORMAT As Integer = &H6
'    Public Const MCI_STATUS_READY As Integer = &H7
'    Public Const MCI_STATUS_CURRENT_TRACK As Integer = &H8
'    Public Const MCI_INFO_PRODUCT As Integer = &H100
'    Public Const MCI_INFO_FILE As Integer = &H200
'    Public Const MCI_INFO_MEDIA_UPC As Integer = &H400
'    Public Const MCI_INFO_MEDIA_IDENTITY As Integer = &H800
'    Public Const MCI_INFO_NAME As Integer = &H1000
'    Public Const MCI_INFO_COPYRIGHT As Integer = &H2000
'    Public Const MCI_GETDEVCAPS_ITEM As Integer = &H100
'    Public Const MCI_GETDEVCAPS_CAN_RECORD As Integer = &H1
'    Public Const MCI_GETDEVCAPS_HAS_AUDIO As Integer = &H2
'    Public Const MCI_GETDEVCAPS_HAS_VIDEO As Integer = &H3
'    Public Const MCI_GETDEVCAPS_DEVICE_TYPE As Integer = &H4
'    Public Const MCI_GETDEVCAPS_USES_FILES As Integer = &H5
'    Public Const MCI_GETDEVCAPS_COMPOUND_DEVICE As Integer = &H6
'    Public Const MCI_GETDEVCAPS_CAN_EJECT As Integer = &H7
'    Public Const MCI_GETDEVCAPS_CAN_PLAY As Integer = &H8
'    Public Const MCI_GETDEVCAPS_CAN_SAVE As Integer = &H9
'    Public Const MCI_SYSINFO_QUANTITY As Integer = &H100
'    Public Const MCI_SYSINFO_OPEN As Integer = &H200
'    Public Const MCI_SYSINFO_NAME As Integer = &H400
'    Public Const MCI_SYSINFO_INSTALLNAME As Integer = &H800
'    Public Const MCI_SET_DOOR_OPEN As Integer = &H100
'    Public Const MCI_SET_DOOR_CLOSED As Integer = &H200
'    Public Const MCI_SET_TIME_FORMAT As Integer = &H400
'    Public Const MCI_SET_AUDIO As Integer = &H800
'    Public Const MCI_SET_VIDEO As Integer = &H1000
'    Public Const MCI_SET_ON As Integer = &H2000
'    Public Const MCI_SET_OFF As Integer = &H4000
'    Public Const MCI_SET_AUDIO_ALL As Integer = &H0
'    Public Const MCI_SET_AUDIO_LEFT As Integer = &H1
'    Public Const MCI_SET_AUDIO_RIGHT As Integer = &H2
'    Public Const MCI_BREAK_KEY As Integer = &H100
'    Public Const MCI_BREAK_HWND As Integer = &H200
'    Public Const MCI_BREAK_OFF As Integer = &H400
'    Public Const MCI_RECORD_INSERT As Integer = &H100
'    Public Const MCI_RECORD_OVERWRITE As Integer = &H200
'    Public Const MCI_SAVE_FILE As Integer = &H100
'    Public Const MCI_LOAD_FILE As Integer = &H100
'    Public Const MCI_VD_MODE_PARK As Integer = 1024 + 1
'    Public Const MCI_VD_MEDIA_CLV As Integer = 1024 + 2
'    Public Const MCI_VD_MEDIA_CAV As Integer = 1024 + 3
'    Public Const MCI_VD_MEDIA_OTHER As Integer = 1024 + 4
'    Public Const MCI_VD_FORMAT_TRACK As Integer = &H4001
'    Public Const MCI_VD_PLAY_REVERSE As Integer = &H10000
'    Public Const MCI_VD_PLAY_FAST As Integer = &H20000
'    Public Const MCI_VD_PLAY_SPEED As Integer = &H40000
'    Public Const MCI_VD_PLAY_SCAN As Integer = &H80000
'    Public Const MCI_VD_PLAY_SLOW As Integer = &H100000
'    Public Const MCI_VD_SEEK_REVERSE As Integer = &H10000
'    Public Const MCI_VD_STATUS_SPEED As Integer = &H4002
'    Public Const MCI_VD_STATUS_FORWARD As Integer = &H4003
'    Public Const MCI_VD_STATUS_MEDIA_TYPE As Integer = &H4004
'    Public Const MCI_VD_STATUS_SIDE As Integer = &H4005
'    Public Const MCI_VD_STATUS_DISC_SIZE As Integer = &H4006
'    Public Const MCI_VD_GETDEVCAPS_CLV As Integer = &H10000
'    Public Const MCI_VD_GETDEVCAPS_CAV As Integer = &H20000
'    Public Const MCI_VD_SPIN_UP As Integer = &H10000
'    Public Const MCI_VD_SPIN_DOWN As Integer = &H20000
'    Public Const MCI_VD_GETDEVCAPS_CAN_REVERSE As Integer = &H4002
'    Public Const MCI_VD_GETDEVCAPS_FAST_RATE As Integer = &H4003
'    Public Const MCI_VD_GETDEVCAPS_SLOW_RATE As Integer = &H4004
'    Public Const MCI_VD_GETDEVCAPS_NORMAL_RATE As Integer = &H4005
'    Public Const MCI_VD_STEP_FRAMES As Integer = &H10000
'    Public Const MCI_VD_STEP_REVERSE As Integer = &H20000
'    Public Const MCI_VD_ESCAPE_STRING As Integer = &H100
'    Public Const MCI_CDA_STATUS_TYPE_TRACK As Integer = &H4001
'    Public Const MCI_CDA_TRACK_AUDIO As Integer = 1088 + 0
'    Public Const MCI_CDA_TRACK_OTHER As Integer = 1088 + 1
'    Public Const MCI_WAVE_PCM As Integer = 1152 + 0
'    Public Const MCI_WAVE_MAPPER As Integer = 1152 + 1
'    Public Const MCI_WAVE_OPEN_BUFFER As Integer = &H10000
'    Public Const MCI_WAVE_SET_FORMATTAG As Integer = &H10000
'    Public Const MCI_WAVE_SET_CHANNELS As Integer = &H20000
'    Public Const MCI_WAVE_SET_SAMPLESPERSEC As Integer = &H40000
'    Public Const MCI_WAVE_SET_AVGBYTESPERSEC As Integer = &H80000
'    Public Const MCI_WAVE_SET_BLOCKALIGN As Integer = &H100000
'    Public Const MCI_WAVE_SET_BITSPERSAMPLE As Integer = &H200000
'    Public Const MCI_WAVE_INPUT As Integer = &H400000
'    Public Const MCI_WAVE_OUTPUT As Integer = &H800000
'    Public Const MCI_WAVE_STATUS_FORMATTAG As Integer = &H4001
'    Public Const MCI_WAVE_STATUS_CHANNELS As Integer = &H4002
'    Public Const MCI_WAVE_STATUS_SAMPLESPERSEC As Integer = &H4003
'    Public Const MCI_WAVE_STATUS_AVGBYTESPERSEC As Integer = &H4004
'    Public Const MCI_WAVE_STATUS_BLOCKALIGN As Integer = &H4005
'    Public Const MCI_WAVE_STATUS_BITSPERSAMPLE As Integer = &H4006
'    Public Const MCI_WAVE_STATUS_LEVEL As Integer = &H4007
'    Public Const MCI_WAVE_SET_ANYINPUT As Integer = &H4000000
'    Public Const MCI_WAVE_SET_ANYOUTPUT As Integer = &H8000000
'    Public Const MCI_WAVE_GETDEVCAPS_INPUTS As Integer = &H4001
'    Public Const MCI_WAVE_GETDEVCAPS_OUTPUTS As Integer = &H4002
'    Public Const MCI_SEQ_DIV_PPQN As Integer = 0 + 1216
'    Public Const MCI_SEQ_DIV_SMPTE_24 As Integer = 1 + 1216
'    Public Const MCI_SEQ_DIV_SMPTE_25 As Integer = 2 + 1216
'    Public Const MCI_SEQ_DIV_SMPTE_30DROP As Integer = 3 + 1216
'    Public Const MCI_SEQ_DIV_SMPTE_30 As Integer = 4 + 1216
'    Public Const MCI_SEQ_FORMAT_SONGPTR As Integer = &H4001
'    Public Const MCI_SEQ_FILE As Integer = &H4002
'    Public Const MCI_SEQ_MIDI As Integer = &H4003
'    Public Const MCI_SEQ_SMPTE As Integer = &H4004
'    Public Const MCI_SEQ_NONE As Integer = 65533
'    Public Const MCI_SEQ_MAPPER As Integer = 65535
'    Public Const MCI_SEQ_STATUS_TEMPO As Integer = &H4002
'    Public Const MCI_SEQ_STATUS_PORT As Integer = &H4003
'    Public Const MCI_SEQ_STATUS_SLAVE As Integer = &H4007
'    Public Const MCI_SEQ_STATUS_MASTER As Integer = &H4008
'    Public Const MCI_SEQ_STATUS_OFFSET As Integer = &H4009
'    Public Const MCI_SEQ_STATUS_DIVTYPE As Integer = &H400A
'    Public Const MCI_SEQ_STATUS_NAME As Integer = &H400B
'    Public Const MCI_SEQ_STATUS_COPYRIGHT As Integer = &H400C
'    Public Const MCI_SEQ_SET_TEMPO As Integer = &H10000
'    Public Const MCI_SEQ_SET_PORT As Integer = &H20000
'    Public Const MCI_SEQ_SET_SLAVE As Integer = &H40000
'    Public Const MCI_SEQ_SET_MASTER As Integer = &H80000
'    Public Const MCI_SEQ_SET_OFFSET As Integer = &H1000000
'    Public Const MCI_ANIM_OPEN_WS As Integer = &H10000
'    Public Const MCI_ANIM_OPEN_PARENT As Integer = &H20000
'    Public Const MCI_ANIM_OPEN_NOSTATIC As Integer = &H40000
'    Public Const MCI_ANIM_PLAY_SPEED As Integer = &H10000
'    Public Const MCI_ANIM_PLAY_REVERSE As Integer = &H20000
'    Public Const MCI_ANIM_PLAY_FAST As Integer = &H40000
'    Public Const MCI_ANIM_PLAY_SLOW As Integer = &H80000
'    Public Const MCI_ANIM_PLAY_SCAN As Integer = &H100000
'    Public Const MCI_ANIM_STEP_REVERSE As Integer = &H10000
'    Public Const MCI_ANIM_STEP_FRAMES As Integer = &H20000
'    Public Const MCI_ANIM_STATUS_SPEED As Integer = &H4001
'    Public Const MCI_ANIM_STATUS_FORWARD As Integer = &H4002
'    Public Const MCI_ANIM_STATUS_HWND As Integer = &H4003
'    Public Const MCI_ANIM_STATUS_HPAL As Integer = &H4004
'    Public Const MCI_ANIM_STATUS_STRETCH As Integer = &H4005
'    Public Const MCI_ANIM_INFO_TEXT As Integer = &H10000
'    Public Const MCI_ANIM_GETDEVCAPS_CAN_REVERSE As Integer = &H4001
'    Public Const MCI_ANIM_GETDEVCAPS_FAST_RATE As Integer = &H4002
'    Public Const MCI_ANIM_GETDEVCAPS_SLOW_RATE As Integer = &H4003
'    Public Const MCI_ANIM_GETDEVCAPS_NORMAL_RATE As Integer = &H4004
'    Public Const MCI_ANIM_GETDEVCAPS_PALETTES As Integer = &H4006
'    Public Const MCI_ANIM_GETDEVCAPS_CAN_STRETCH As Integer = &H4007
'    Public Const MCI_ANIM_GETDEVCAPS_MAX_WINDOWS As Integer = &H4008
'    Public Const MCI_ANIM_REALIZE_NORM As Integer = &H10000
'    Public Const MCI_ANIM_REALIZE_BKGD As Integer = &H20000
'    Public Const MCI_ANIM_WINDOW_HWND As Integer = &H10000
'    Public Const MCI_ANIM_WINDOW_STATE As Integer = &H40000
'    Public Const MCI_ANIM_WINDOW_TEXT As Integer = &H80000
'    Public Const MCI_ANIM_WINDOW_ENABLE_STRETCH As Integer = &H100000
'    Public Const MCI_ANIM_WINDOW_DISABLE_STRETCH As Integer = &H200000
'    Public Const MCI_ANIM_WINDOW_DEFAULT As Integer = &H0
'    Public Const MCI_ANIM_RECT As Integer = &H10000
'    Public Const MCI_ANIM_PUT_SOURCE As Integer = &H20000
'    Public Const MCI_ANIM_PUT_DESTINATION As Integer = &H40000
'    Public Const MCI_ANIM_WHERE_SOURCE As Integer = &H20000
'    Public Const MCI_ANIM_WHERE_DESTINATION As Integer = &H40000
'    Public Const MCI_ANIM_UPDATE_HDC As Integer = &H20000
'    Public Const MCI_OVLY_OPEN_WS As Integer = &H10000
'    Public Const MCI_OVLY_OPEN_PARENT As Integer = &H20000
'    Public Const MCI_OVLY_STATUS_HWND As Integer = &H4001
'    Public Const MCI_OVLY_STATUS_STRETCH As Integer = &H4002
'    Public Const MCI_OVLY_INFO_TEXT As Integer = &H10000
'    Public Const MCI_OVLY_GETDEVCAPS_CAN_STRETCH As Integer = &H4001
'    Public Const MCI_OVLY_GETDEVCAPS_CAN_FREEZE As Integer = &H4002
'    Public Const MCI_OVLY_GETDEVCAPS_MAX_WINDOWS As Integer = &H4003
'    Public Const MCI_OVLY_WINDOW_HWND As Integer = &H10000
'    Public Const MCI_OVLY_WINDOW_STATE As Integer = &H40000
'    Public Const MCI_OVLY_WINDOW_TEXT As Integer = &H80000
'    Public Const MCI_OVLY_WINDOW_ENABLE_STRETCH As Integer = &H100000
'    Public Const MCI_OVLY_WINDOW_DISABLE_STRETCH As Integer = &H200000
'    Public Const MCI_OVLY_WINDOW_DEFAULT As Integer = &H0
'    Public Const MCI_OVLY_RECT As Integer = &H10000
'    Public Const MCI_OVLY_PUT_SOURCE As Integer = &H20000
'    Public Const MCI_OVLY_PUT_DESTINATION As Integer = &H40000
'    Public Const MCI_OVLY_PUT_FRAME As Integer = &H80000
'    Public Const MCI_OVLY_PUT_VIDEO As Integer = &H100000
'    Public Const MCI_OVLY_WHERE_SOURCE As Integer = &H20000
'    Public Const MCI_OVLY_WHERE_DESTINATION As Integer = &H40000
'    Public Const MCI_OVLY_WHERE_FRAME As Integer = &H80000
'    Public Const MCI_OVLY_WHERE_VIDEO As Integer = &H100000
'    Public Const MAX_LANA As Integer = 254
'    Public Const MARSHALINTERFACE_MIN As Integer = 500
'    Public Const MEMBERID_NIL As Integer = - 1
'    Public Const MK_ALT As Integer = &H20
'    Public Const MAXPROPPAGES As Integer = 100
'    Public Const MARKPARITY As Integer = 3
'    Public Const MS_CTS_ON As Integer = &H10
'    Public Const MS_DSR_ON As Integer = &H20
'    Public Const MS_RING_ON As Integer = &H40
'    Public Const MS_RLSD_ON As Integer = &H80
'    Public Const MAXINTATOM As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                             MS_RLSD_ON = (0x0080),
'    '                                                          MAXINTATOM = unchecked((int)0xC000),
'    '------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const MOVEFILE_REPLACE_EXISTING As Integer = &H1
'    Public Const MOVEFILE_COPY_ALLOWED As Integer = &H2
'    Public Const MOVEFILE_DELAY_UNTIL_REBOOT As Integer = &H4
'    Public Const MOVEFILE_WRITE_THROUGH As Integer = &H8
'    Public Const MAX_COMPUTERNAME_LENGTH As Integer = 15
'    Public Const MAX_PROFILE_LEN As Integer = 80
'    Public Const MOUSE_MOVED As Integer = &H1
'    Public Const MOUSE_EVENT As Integer = &H2
'    Public Const MENU_EVENT As Integer = &H8
'    Public Const MAXUIDLEN As Integer = 64
     Public Const MAX_PATH As Integer = 260
'    Public Const MARSHAL_E_FIRST As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        MAX_PATH = 260,
'    '        MARSHAL_E_FIRST = unchecked((int)0x80040120),
'    '---------------------------^--- GenCode(token): unexpected token type
'    Public Const MARSHAL_E_LAST As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        MARSHAL_E_FIRST = unchecked((int)0x80040120),
'    '        MARSHAL_E_LAST = unchecked((int)0x8004012F),
'    '--------------------------^--- GenCode(token): unexpected token type
'    Public Const MARSHAL_S_FIRST As Integer = &H40120
'    Public Const MARSHAL_S_LAST As Integer = &H4012F
'    Public Const MK_E_FIRST As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        MARSHAL_S_LAST = 0x0004012F,
'    '        MK_E_FIRST = unchecked((int)0x800401E0),
'    '----------------------^--- GenCode(token): unexpected token type
'    Public Const MK_E_LAST As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        MK_E_FIRST = unchecked((int)0x800401E0),
'    '        MK_E_LAST = unchecked((int)0x800401EF),
'    '---------------------^--- GenCode(token): unexpected token type
'    Public Const MK_S_FIRST As Integer = &H401E0
'    Public Const MK_S_LAST As Integer = &H401EF
'    Public Const MK_E_CONNECTMANUALLY As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        MK_S_LAST = 0x000401EF,
'    '        MK_E_CONNECTMANUALLY = unchecked((int)0x800401E0),
'    '--------------------------------^--- GenCode(token): unexpected token type
'    Public Const MK_E_EXCEEDEDDEADLINE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        MK_E_CONNECTMANUALLY = unchecked((int)0x800401E0),
'    '        MK_E_EXCEEDEDDEADLINE = unchecked((int)0x800401E1),
'    '---------------------------------^--- GenCode(token): unexpected token type
'    Public Const MK_E_NEEDGENERIC As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        MK_E_EXCEEDEDDEADLINE = unchecked((int)0x800401E1),
'    '        MK_E_NEEDGENERIC = unchecked((int)0x800401E2),
'    '----------------------------^--- GenCode(token): unexpected token type
'    Public Const MK_E_UNAVAILABLE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        MK_E_NEEDGENERIC = unchecked((int)0x800401E2),
'    '        MK_E_UNAVAILABLE = unchecked((int)0x800401E3),
'    '----------------------------^--- GenCode(token): unexpected token type
'    Public Const MK_E_SYNTAX As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        MK_E_UNAVAILABLE = unchecked((int)0x800401E3),
'    '        MK_E_SYNTAX = unchecked((int)0x800401E4),
'    '-----------------------^--- GenCode(token): unexpected token type
'    Public Const MK_E_NOOBJECT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        MK_E_SYNTAX = unchecked((int)0x800401E4),
'    '        MK_E_NOOBJECT = unchecked((int)0x800401E5),
'    '-------------------------^--- GenCode(token): unexpected token type
'    Public Const MK_E_INVALIDEXTENSION As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        MK_E_NOOBJECT = unchecked((int)0x800401E5),
'    '        MK_E_INVALIDEXTENSION = unchecked((int)0x800401E6),
'    '---------------------------------^--- GenCode(token): unexpected token type
'    Public Const MK_E_INTERMEDIATEINTERFACENOTSUPPORTED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        MK_E_INVALIDEXTENSION = unchecked((int)0x800401E6),
'    '        MK_E_INTERMEDIATEINTERFACENOTSUPPORTED = unchecked((int)0x800401E7),
'    '--------------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const MK_E_NOTBINDABLE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        MK_E_INTERMEDIATEINTERFACENOTSUPPORTED = unchecked((int)0x800401E7),
'    '        MK_E_NOTBINDABLE = unchecked((int)0x800401E8),
'    '----------------------------^--- GenCode(token): unexpected token type
'    Public Const MK_E_NOTBOUND As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        MK_E_NOTBINDABLE = unchecked((int)0x800401E8),
'    '        MK_E_NOTBOUND = unchecked((int)0x800401E9),
'    '-------------------------^--- GenCode(token): unexpected token type
'    Public Const MK_E_CANTOPENFILE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        MK_E_NOTBOUND = unchecked((int)0x800401E9),
'    '        MK_E_CANTOPENFILE = unchecked((int)0x800401EA),
'    '-----------------------------^--- GenCode(token): unexpected token type
'    Public Const MK_E_MUSTBOTHERUSER As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        MK_E_CANTOPENFILE = unchecked((int)0x800401EA),
'    '        MK_E_MUSTBOTHERUSER = unchecked((int)0x800401EB),
'    '-------------------------------^--- GenCode(token): unexpected token type
'    Public Const MK_E_NOINVERSE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        MK_E_MUSTBOTHERUSER = unchecked((int)0x800401EB),
'    '        MK_E_NOINVERSE = unchecked((int)0x800401EC),
'    '--------------------------^--- GenCode(token): unexpected token type
'    Public Const MK_E_NOSTORAGE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        MK_E_NOINVERSE = unchecked((int)0x800401EC),
'    '        MK_E_NOSTORAGE = unchecked((int)0x800401ED),
'    '--------------------------^--- GenCode(token): unexpected token type
'    Public Const MK_E_NOPREFIX As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        MK_E_NOSTORAGE = unchecked((int)0x800401ED),
'    '        MK_E_NOPREFIX = unchecked((int)0x800401EE),
'    '-------------------------^--- GenCode(token): unexpected token type
'    Public Const MK_E_ENUMERATION_FAILED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        MK_E_NOPREFIX = unchecked((int)0x800401EE),
'    '        MK_E_ENUMERATION_FAILED = unchecked((int)0x800401EF),
'    '-----------------------------------^--- GenCode(token): unexpected token type
'    Public Const MK_S_REDUCED_TO_SELF As Integer = &H401E2
'    Public Const MK_S_ME As Integer = &H401E4
'    Public Const MK_S_HIM As Integer = &H401E5
'    Public Const MK_S_US As Integer = &H401E6
'    Public Const MK_S_MONIKERALREADYREGISTERED As Integer = &H401E7
'    Public Const MK_E_NO_NORMALIZED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        MK_S_MONIKERALREADYREGISTERED = 0x000401E7,
'    '        MK_E_NO_NORMALIZED = unchecked((int)0x80080007),
'    '------------------------------^--- GenCode(token): unexpected token type
'    Public Const MEM_E_INVALID_ROOT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        MK_E_NO_NORMALIZED = unchecked((int)0x80080007),
'    '        MEM_E_INVALID_ROOT = unchecked((int)0x80080009),
'    '------------------------------^--- GenCode(token): unexpected token type
'    Public Const MEM_E_INVALID_LINK As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        MEM_E_INVALID_ROOT = unchecked((int)0x80080009),
'    '        MEM_E_INVALID_LINK = unchecked((int)0x80080010),
'    '------------------------------^--- GenCode(token): unexpected token type
'    Public Const MEM_E_INVALID_SIZE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        MEM_E_INVALID_LINK = unchecked((int)0x80080010),
'    '        MEM_E_INVALID_SIZE = unchecked((int)0x80080011),
'    '------------------------------^--- GenCode(token): unexpected token type
'    Public Const MAXSTRETCHBLTMODE As Integer = 4
'    Public Const META_SETBKCOLOR As Integer = &H201
'    Public Const META_SETBKMODE As Integer = &H102
'    Public Const META_SETMAPMODE As Integer = &H103
'    Public Const META_SETROP2 As Integer = &H104
'    Public Const META_SETRELABS As Integer = &H105
'    Public Const META_SETPOLYFILLMODE As Integer = &H106
'    Public Const META_SETSTRETCHBLTMODE As Integer = &H107
'    Public Const META_SETTEXTCHAREXTRA As Integer = &H108
'    Public Const META_SETTEXTCOLOR As Integer = &H209
'    Public Const META_SETTEXTJUSTIFICATION As Integer = &H20A
'    Public Const META_SETWINDOWORG As Integer = &H20B
'    Public Const META_SETWINDOWEXT As Integer = &H20C
'    Public Const META_SETVIEWPORTORG As Integer = &H20D
'    Public Const META_SETVIEWPORTEXT As Integer = &H20E
'    Public Const META_OFFSETWINDOWORG As Integer = &H20F
'    Public Const META_SCALEWINDOWEXT As Integer = &H410
'    Public Const META_OFFSETVIEWPORTORG As Integer = &H211
'    Public Const META_SCALEVIEWPORTEXT As Integer = &H412
'    Public Const META_LINETO As Integer = &H213
'    Public Const META_MOVETO As Integer = &H214
'    Public Const META_EXCLUDECLIPRECT As Integer = &H415
'    Public Const META_INTERSECTCLIPRECT As Integer = &H416
'    Public Const META_ARC As Integer = &H817
'    Public Const META_ELLIPSE As Integer = &H418
'    Public Const META_FLOODFILL As Integer = &H419
'    Public Const META_PIE As Integer = &H81A
'    Public Const META_RECTANGLE As Integer = &H41B
'    Public Const META_ROUNDRECT As Integer = &H61C
'    Public Const META_PATBLT As Integer = &H61D
'    Public Const META_SAVEDC As Integer = &H1E
'    Public Const META_SETPIXEL As Integer = &H41F
'    Public Const META_OFFSETCLIPRGN As Integer = &H220
'    Public Const META_TEXTOUT As Integer = &H521
'    Public Const META_BITBLT As Integer = &H922
'    Public Const META_STRETCHBLT As Integer = &HB23
'    Public Const META_POLYGON As Integer = &H324
'    Public Const META_POLYLINE As Integer = &H325
'    Public Const META_ESCAPE As Integer = &H626
'    Public Const META_RESTOREDC As Integer = &H127
'    Public Const META_FILLREGION As Integer = &H228
'    Public Const META_FRAMEREGION As Integer = &H429
'    Public Const META_INVERTREGION As Integer = &H12A
'    Public Const META_PAINTREGION As Integer = &H12B
'    Public Const META_SELECTCLIPREGION As Integer = &H12C
'    Public Const META_SELECTOBJECT As Integer = &H12D
'    Public Const META_SETTEXTALIGN As Integer = &H12E
'    Public Const META_CHORD As Integer = &H830
'    Public Const META_SETMAPPERFLAGS As Integer = &H231
'    Public Const META_EXTTEXTOUT As Integer = &HA32
'    Public Const META_SETDIBTODEV As Integer = &HD33
'    Public Const META_SELECTPALETTE As Integer = &H234
'    Public Const META_REALIZEPALETTE As Integer = &H35
'    Public Const META_ANIMATEPALETTE As Integer = &H436
'    Public Const META_SETPALENTRIES As Integer = &H37
'    Public Const META_POLYPOLYGON As Integer = &H538
'    Public Const META_RESIZEPALETTE As Integer = &H139
'    Public Const META_DIBBITBLT As Integer = &H940
'    Public Const META_DIBSTRETCHBLT As Integer = &HB41
'    Public Const META_DIBCREATEPATTERNBRUSH As Integer = &H142
'    Public Const META_STRETCHDIB As Integer = &HF43
'    Public Const META_EXTFLOODFILL As Integer = &H548
'    Public Const META_DELETEOBJECT As Integer = &H1F0
'    Public Const META_CREATEPALETTE As Integer = &HF7
'    Public Const META_CREATEPATTERNBRUSH As Integer = &H1F9
'    Public Const META_CREATEPENINDIRECT As Integer = &H2FA
'    Public Const META_CREATEFONTINDIRECT As Integer = &H2FB
'    Public Const META_CREATEBRUSHINDIRECT As Integer = &H2FC
'    Public Const META_CREATEREGION As Integer = &H6FF
'    Public Const MFCOMMENT As Integer = 15
'    Public Const MOUSETRAILS As Integer = 39
'    Public Const MWT_IDENTITY As Integer = 1
'    Public Const MWT_LEFTMULTIPLY As Integer = 2
'    Public Const MWT_RIGHTMULTIPLY As Integer = 3
'    Public Const MWT_MIN As Integer = 1
'    Public Const MWT_MAX As Integer = 3
'    Public Const MONO_FONT As Integer = 8
'    Public Const MAC_CHARSET As Integer = 77
'    Public Const MM_TEXT As Integer = 1
'    Public Const MM_LOMETRIC As Integer = 2
'    Public Const MM_HIMETRIC As Integer = 3
'    Public Const MM_LOENGLISH As Integer = 4
'    Public Const MM_HIENGLISH As Integer = 5
'    Public Const MM_TWIPS As Integer = 6
'    Public Const MM_ISOTROPIC As Integer = 7
'    Public Const MM_ANISOTROPIC As Integer = 8
'    Public Const MM_MIN As Integer = 1
'    Public Const MM_MAX As Integer = 8
'    Public Const MM_MAX_FIXEDSCALE As Integer = 6
'    Public Const MAX_LEADBYTES As Integer = 12
'    Public Const MAX_DEFAULTCHAR As Integer = 2
'    Public Const MB_PRECOMPOSED As Integer = &H1
'    Public Const MB_COMPOSITE As Integer = &H2
'    Public Const MB_USEGLYPHCHARS As Integer = &H4
'    Public Const MB_ERR_INVALID_CHARS As Integer = &H8
'    Public Const MAP_FOLDCZONE As Integer = &H10
'    Public Const MAP_PRECOMPOSED As Integer = &H20
'    Public Const MAP_COMPOSITE As Integer = &H40
'    Public Const MAP_FOLDDIGITS As Integer = &H80
'    Public Const MAXLONGLONG As Long = __unknown
'    Public Const MINCHAR As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        public const long MAXLONGLONG = (0x7fffffffffffffffL);
'    '        public const int MINCHAR = unchecked((int)0x80),
'    '------------------------------------^--- GenCode(token): unexpected token type
'    Public Const MAXCHAR As Integer = &H7F
'    Public Const MINSHORT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        MAXCHAR = 0x7f,
'    '        MINSHORT = unchecked((int)0x8000),
'    '--------------------^--- GenCode(token): unexpected token type
'    Public Const MAXSHORT As Integer = &H7FFF
'    Public Const MINLONG As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        MAXSHORT = 0x7fff,
'    '        MINLONG = unchecked((int)0x80000000),
'    '-------------------^--- GenCode(token): unexpected token type
'    Public Const MAXLONG As Integer = &H7FFFFFFF
'    Public Const MAXBYTE As Integer = &HFF
'    Public Const MAXWORD As Integer = &HFFFF
'    Public Const MAXDWORD As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        MAXWORD = 0xffff,
'    '        MAXDWORD = unchecked((int)0xFfffffff),
'    '--------------------^--- GenCode(token): unexpected token type
'    Public Const MAXIMUM_WAIT_OBJECTS As Integer = 64
'    Public Const MAXIMUM_SUSPEND_COUNT As Integer = &H7F
'    Public Const MAXIMUM_PROCESSORS As Integer = 32
'    Public Const MUTANT_QUERY_STATE As Integer = &H1
'    Public Const MEM_COMMIT As Integer = &H1000
'    Public Const MEM_RESERVE As Integer = &H2000
'    Public Const MEM_DECOMMIT As Integer = &H4000
'    Public Const MEM_RELEASE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        MEM_DECOMMIT = 0x4000,
'    '        MEM_RELEASE = unchecked((int)0x8000),
'    '-----------------------^--- GenCode(token): unexpected token type
'    Public Const MEM_FREE As Integer = &H10000
'    Public Const MEM_PRIVATE As Integer = &H20000
'    Public Const MEM_MAPPED As Integer = &H40000
'    Public Const MEM_RESET As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        MEM_MAPPED = 0x40000,
'    '        MEM_RESET = unchecked((int)0x80000),
'    '---------------------^--- GenCode(token): unexpected token type
'    Public Const MEM_TOP_DOWN As Integer = &H100000
'    Public Const MEM_IMAGE As Integer = &H1000000
'    Public Const MAILSLOT_NO_MESSAGE As Integer = - 1
'    Public Const MAILSLOT_WAIT_FOREVER As Integer = - 1
'    Public Const MAXIMUM_ALLOWED As Integer = &H2000000
'    Public Const MESSAGE_RESOURCE_UNICODE As Integer = &H1
'    Public Const MAX_PRIORITY As Integer = 99
'    Public Const MIN_PRIORITY As Integer = 1
'    Public Const MSGF_DIALOGBOX As Integer = 0
'    Public Const MSGF_MESSAGEBOX As Integer = 1
'    Public Const MSGF_MENU As Integer = 2
'    Public Const MSGF_MOVE As Integer = 3
'    Public Const MSGF_SIZE As Integer = 4
'    Public Const MSGF_SCROLLBAR As Integer = 5
'    Public Const MSGF_NEXTWINDOW As Integer = 6
'    Public Const MSGF_MAINLOOP As Integer = 8
'    Public Const MSGF_MAX As Integer = 8
'    Public Const MSGF_USER As Integer = 4096
'    Public Const MENULOOP_WINDOW As Integer = 0
'    Public Const MENULOOP_POPUP As Integer = 1
'    Public Const MA_ACTIVATE As Integer = 1
'    Public Const MA_ACTIVATEANDEAT As Integer = 2
'    Public Const MA_NOACTIVATE As Integer = 3
'    Public Const MA_NOACTIVATEANDEAT As Integer = 4
'    Public Const MK_LBUTTON As Integer = &H1
'    Public Const MK_RBUTTON As Integer = &H2
'    Public Const MK_SHIFT As Integer = &H4
'    Public Const MK_CONTROL As Integer = &H8
'    Public Const MK_MBUTTON As Integer = &H10
'    Public Const MOD_WIN As Integer = &H8
'    Public Const MOUSEEVENTF_MOVE As Integer = &H1
'    Public Const MOUSEEVENTF_LEFTDOWN As Integer = &H2
'    Public Const MOUSEEVENTF_LEFTUP As Integer = &H4
'    Public Const MOUSEEVENTF_RIGHTDOWN As Integer = &H8
'    Public Const MOUSEEVENTF_RIGHTUP As Integer = &H10
'    Public Const MOUSEEVENTF_MIDDLEDOWN As Integer = &H20
'    Public Const MOUSEEVENTF_MIDDLEUP As Integer = &H40
'    Public Const MOUSEEVENTF_WHEEL As Integer = &H800
'    Public Const MOUSEEVENTF_ABSOLUTE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        MOUSEEVENTF_WHEEL = 0x0800,
'    '        MOUSEEVENTF_ABSOLUTE = unchecked((int)0x8000),
'    '--------------------------------^--- GenCode(token): unexpected token type
'    Public Const MWMO_WAITALL As Integer = &H1
'    Public Const MWMO_ALERTABLE As Integer = &H2
'    Public Const MNC_IGNORE As Integer = 0
'    Public Const MNC_CLOSE As Integer = 1
'    Public Const MNC_EXECUTE As Integer = 2
'    Public Const MNC_SELECT As Integer = 3
'    Public Const MIIM_STATE As Integer = &H1
'    Public Const MIIM_ID As Integer = &H2
'    Public Const MIIM_SUBMENU As Integer = &H4
'    Public Const MIIM_CHECKMARKS As Integer = &H8
'    Public Const MIIM_TYPE As Integer = &H10
'    Public Const MIIM_DATA As Integer = &H20
'    Public Const MB_OK As Integer = &H0
'    Public Const MB_OKCANCEL As Integer = &H1
'    Public Const MB_ABORTRETRYIGNORE As Integer = &H2
'    Public Const MB_YESNOCANCEL As Integer = &H3
'    Public Const MB_YESNO As Integer = &H4
'    Public Const MB_RETRYCANCEL As Integer = &H5
'    Public Const MB_ICONHAND As Integer = &H10
'    Public Const MB_ICONQUESTION As Integer = &H20
'    Public Const MB_ICONEXCLAMATION As Integer = &H30
'    Public Const MB_ICONASTERISK As Integer = &H40
'    Public Const MB_USERICON As Integer = &H80
'    Public Const MB_ICONWARNING As Integer = &H30
'    Public Const MB_ICONERROR As Integer = &H10
'    Public Const MB_ICONINFORMATION As Integer = &H40
'    Public Const MB_DEFBUTTON1 As Integer = &H0
'    Public Const MB_DEFBUTTON2 As Integer = &H100
'    Public Const MB_DEFBUTTON3 As Integer = &H200
'    Public Const MB_DEFBUTTON4 As Integer = &H300
'    Public Const MB_APPLMODAL As Integer = &H0
'    Public Const MB_SYSTEMMODAL As Integer = &H1000
'    Public Const MB_TASKMODAL As Integer = &H2000
'    Public Const MB_HELP As Integer = &H4000
'    Public Const MB_NOFOCUS As Integer = &H8000
'    Public Const MB_SETFOREGROUND As Integer = &H10000
'    Public Const MB_DEFAULT_DESKTOP_ONLY As Integer = &H20000
'    Public Const MB_TOPMOST As Integer = &H40000
'    Public Const MB_RIGHT As Integer = &H80000
'    Public Const MB_RTLREADING As Integer = &H100000
'    Public Const MB_SERVICE_NOTIFICATION As Integer = &H200000 '
'    'Note:  Error processing original source shown below
'    '        public const long MAXLONGLONG = (0x7fffffffffffffffL);
'    '        public const int MINCHAR = unchecked((int)0x80),
'    '--------------------------------------------------------------^--- Numeric constant overflow
'    ' MB_SERVICE_NOTIFICATION = 0x00040000;
'    Public Const MB_SERVICE_NOTIFICATION_NT3X As Integer = &H40000
'    Public Const MB_TYPEMASK As Integer = &HF
'    Public Const MB_ICONMASK As Integer = &HF0
'    Public Const MB_DEFMASK As Integer = &HF00
'    Public Const MB_MODEMASK As Integer = &H3000
'    Public Const MB_MISCMASK As Integer = &HC000
'    Public Const MF_INSERT As Integer = &H0
'    Public Const MF_CHANGE As Integer = &H80
'    Public Const MF_APPEND As Integer = &H100
'    Public Const MF_DELETE As Integer = &H200
'    Public Const MF_REMOVE As Integer = &H1000
'    Public Const MF_BYCOMMAND As Integer = &H0
'    Public Const MF_BYPOSITION As Integer = &H400
'    Public Const MF_SEPARATOR As Integer = &H800
'    Public Const MF_ENABLED As Integer = &H0
'    Public Const MF_GRAYED As Integer = &H1
'    Public Const MF_DISABLED As Integer = &H2
'    Public Const MF_UNCHECKED As Integer = &H0
'    Public Const MF_CHECKED As Integer = &H8
'    Public Const MF_USECHECKBITMAPS As Integer = &H200
'    Public Const MF_STRING As Integer = &H0
'    Public Const MF_BITMAP As Integer = &H4
'    Public Const MF_OWNERDRAW As Integer = &H100
'    Public Const MF_POPUP As Integer = &H10
'    Public Const MF_MENUBARBREAK As Integer = &H20
'    Public Const MF_MENUBREAK As Integer = &H40
'    Public Const MF_UNHILITE As Integer = &H0
'    Public Const MF_HILITE As Integer = &H80
'    Public Const MF_DEFAULT As Integer = &H1000
'    Public Const MF_SYSMENU As Integer = &H2000
'    Public Const MF_HELP As Integer = &H4000
'    Public Const MF_RIGHTJUSTIFY As Integer = &H4000
'    Public Const MF_MOUSESELECT As Integer = &H8000
'    Public Const MF_END As Integer = &H80
'    Public Const MFT_STRING As Integer = &H0
'    Public Const MFT_BITMAP As Integer = &H4
'    Public Const MFT_MENUBARBREAK As Integer = &H20
'    Public Const MFT_MENUBREAK As Integer = &H40
'    Public Const MFT_OWNERDRAW As Integer = &H100
'    Public Const MFT_RADIOCHECK As Integer = &H200
'    Public Const MFT_SEPARATOR As Integer = &H800
'    Public Const MFT_RIGHTORDER As Integer = &H2000
'    Public Const MFT_RIGHTJUSTIFY As Integer = &H4000
'    Public Const MFS_GRAYED As Integer = &H3
'    Public Const MFS_DISABLED As Integer = &H3
'    Public Const MFS_CHECKED As Integer = &H8
'    Public Const MFS_HILITE As Integer = &H80
'    Public Const MFS_ENABLED As Integer = &H0
'    Public Const MFS_UNCHECKED As Integer = &H0
'    Public Const MFS_UNHILITE As Integer = &H0
'    Public Const MFS_DEFAULT As Integer = &H1000
'    Public Const MDIS_ALLCHILDSTYLES As Integer = &H1
'    Public Const MDITILE_VERTICAL As Integer = &H0
'    Public Const MDITILE_HORIZONTAL As Integer = &H1
'    Public Const MDITILE_SKIPDISABLED As Integer = &H2
'    Public Const METRICS_USEDEFAULT As Integer = - 1
'    Public Const MKF_MOUSEKEYSON As Integer = &H1
'    Public Const MKF_AVAILABLE As Integer = &H2
'    Public Const MKF_HOTKEYACTIVE As Integer = &H4
'    Public Const MKF_CONFIRMHOTKEY As Integer = &H8
'    Public Const MKF_HOTKEYSOUND As Integer = &H10
'    Public Const MKF_INDICATOR As Integer = &H20
'    Public Const MKF_MODIFIERS As Integer = &H40
'    Public Const MKF_REPLACENUMBERS As Integer = &H80
'    Public Const MCN_FIRST As Integer = 0 - 750
'    Public Const MCN_LAST As Integer = 0 - 759
'    Public Const MSGF_COMMCTRL_BEGINDRAG As Integer = &H4200
'    Public Const MSGF_COMMCTRL_SIZEHEADER As Integer = &H4201
'    Public Const MSGF_COMMCTRL_DRAGSELECT As Integer = &H4202
'    Public Const MSGF_COMMCTRL_TOOLBARCUST As Integer = &H4203
'    Public Const MINSYSCOMMAND As Integer = &HF000
'    Public Const MCM_FIRST As Integer = &H1000
'    Public Const MCM_GETCURSEL As Integer = &H1000 + 1
'    Public Const MCM_SETCURSEL As Integer = &H1000 + 2
'    Public Const MCM_GETMAXSELCOUNT As Integer = &H1000 + 3
'    Public Const MCM_SETMAXSELCOUNT As Integer = &H1000 + 4
'    Public Const MCM_GETSELRANGE As Integer = &H1000 + 5
'    Public Const MCM_SETSELRANGE As Integer = &H1000 + 6
'    Public Const MCM_GETMONTHRANGE As Integer = &H1000 + 7
'    Public Const MCM_SETDAYSTATE As Integer = &H1000 + 8
'    Public Const MCM_GETMINREQRECT As Integer = &H1000 + 9
'    Public Const MCM_SETCOLOR As Integer = &H1000 + 10
'    Public Const MCM_GETCOLOR As Integer = &H1000 + 11
'    Public Const MCM_SETTODAY As Integer = &H1000 + 12
'    Public Const MCM_GETTODAY As Integer = &H1000 + 13
'    Public Const MCM_HITTEST As Integer = &H1000 + 14
'    Public Const MCM_SETFIRSTDAYOFWEEK As Integer = &H1000 + 15
'    Public Const MCM_GETFIRSTDAYOFWEEK As Integer = &H1000 + 16
'    Public Const MCM_GETRANGE As Integer = &H1000 + 17
'    Public Const MCM_SETRANGE As Integer = &H1000 + 18
'    Public Const MCM_GETMONTHDELTA As Integer = &H1000 + 19
'    Public Const MCM_SETMONTHDELTA As Integer = &H1000 + 20
'    Public Const MCM_GETMAXTODAYWIDTH As Integer = &H1000 + 21
'    Public Const MCHT_TITLE As Integer = &H10000
'    Public Const MCHT_CALENDAR As Integer = &H20000
'    Public Const MCHT_TODAYLINK As Integer = &H30000
'    Public Const MCHT_NEXT As Integer = &H1000000
'    Public Const MCHT_PREV As Integer = &H2000000
'    Public Const MCHT_NOWHERE As Integer = &H0
'    Public Const MCHT_TITLEBK As Integer = &H10000
'    Public Const MCHT_TITLEMONTH As Integer = &H10000 Or &H1
'    Public Const MCHT_TITLEYEAR As Integer = &H10000 Or &H2
'    Public Const MCHT_TITLEBTNNEXT As Integer = &H10000 Or &H1000000 Or &H3
'    Public Const MCHT_TITLEBTNPREV As Integer = &H10000 Or &H2000000 Or &H3
'    Public Const MCHT_CALENDARBK As Integer = &H20000
'    Public Const MCHT_CALENDARDATE As Integer = &H20000 Or &H1
'    Public Const MCHT_CALENDARDATENEXT As Integer = &H20000 Or &H1 Or &H1000000
'    Public Const MCHT_CALENDARDATEPREV As Integer = &H20000 Or &H1 Or &H2000000
'    Public Const MCHT_CALENDARDAY As Integer = &H20000 Or &H2
'    Public Const MCHT_CALENDARWEEKNUM As Integer = &H20000 Or &H3
'    Public Const MCSC_BACKGROUND As Integer = 0
'    Public Const MCSC_TEXT As Integer = 1
'    Public Const MCSC_TITLEBK As Integer = 2
'    Public Const MCSC_TITLETEXT As Integer = 3
'    Public Const MCSC_MONTHBK As Integer = 4
'    Public Const MCSC_TRAILINGTEXT As Integer = 5
'    Public Const MCN_SELCHANGE As Integer = 0 - 750 + 1
'    Public Const MCN_GETDAYSTATE As Integer = 0 - 750 + 3
'    Public Const MCN_SELECT As Integer = 0 - 750 + 4
'    Public Const MCS_DAYSTATE As Integer = &H1
'    Public Const MCS_MULTISELECT As Integer = &H2
'    Public Const MCS_WEEKNUMBERS As Integer = &H4
'    Public Const MCS_NOTODAYCIRCLE As Integer = &H8
'    Public Const MUTEX_MODIFY_STATE As Integer = &H1
'    Public Const MERGECOPY As Integer = &HC000CA
'    Public Const MERGEPAINT As Integer = &HBB0226
    
    
    
    
    
'    Public Const NEWFILEOPENORD As Integer = 1547
'    Public Const NI_OPENCANDIDATE As Integer = &H10
'    Public Const NI_CLOSECANDIDATE As Integer = &H11
'    Public Const NI_SELECTCANDIDATESTR As Integer = &H12
'    Public Const NI_CHANGECANDIDATELIST As Integer = &H13
'    Public Const NI_FINALIZECONVERSIONRESULT As Integer = &H14
'    Public Const NI_COMPOSITIONSTR As Integer = &H15
'    Public Const NI_SETCANDIDATE_PAGESTART As Integer = &H16
'    Public Const NI_SETCANDIDATE_PAGESIZE As Integer = &H17
'    Public Const NEWTRANSPARENT As Integer = 3
'    Public Const NCBNAMSZ As Integer = 16
'    Public Const NAME_FLAGS_MASK As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        NCBNAMSZ = 16,
'    '        NAME_FLAGS_MASK = unchecked((int)0x87),
'    '---------------------------^--- GenCode(token): unexpected token type
'    Public Const NCBCALL As Integer = &H10
'    Public Const NCBLISTEN As Integer = &H11
'    Public Const NCBHANGUP As Integer = &H12
'    Public Const NCBSEND As Integer = &H14
'    Public Const NCBRECV As Integer = &H15
'    Public Const NCBRECVANY As Integer = &H16
'    Public Const NCBCHAINSEND As Integer = &H17
'    Public Const NCBDGSEND As Integer = &H20
'    Public Const NCBDGRECV As Integer = &H21
'    Public Const NCBDGSENDBC As Integer = &H22
'    Public Const NCBDGRECVBC As Integer = &H23
'    Public Const NCBADDNAME As Integer = &H30
'    Public Const NCBDELNAME As Integer = &H31
'    Public Const NCBRESET As Integer = &H32
'    Public Const NCBASTAT As Integer = &H33
'    Public Const NCBSSTAT As Integer = &H34
'    Public Const NCBCANCEL As Integer = &H35
'    Public Const NCBADDGRNAME As Integer = &H36
'    Public Const NCBENUM As Integer = &H37
'    Public Const NCBUNLINK As Integer = &H70
'    Public Const NCBSENDNA As Integer = &H71
'    Public Const NCBCHAINSENDNA As Integer = &H72
'    Public Const NCBLANSTALERT As Integer = &H73
'    Public Const NCBACTION As Integer = &H77
'    Public Const NCBFINDNAME As Integer = &H78
'    Public Const NCBTRACE As Integer = &H79
'    Public Const NRC_GOODRET As Integer = &H0
'    Public Const NRC_BUFLEN As Integer = &H1
'    Public Const NRC_ILLCMD As Integer = &H3
'    Public Const NRC_CMDTMO As Integer = &H5
'    Public Const NRC_INCOMP As Integer = &H6
'    Public Const NRC_BADDR As Integer = &H7
'    Public Const NRC_SNUMOUT As Integer = &H8
'    Public Const NRC_NORES As Integer = &H9
'    Public Const NRC_SCLOSED As Integer = &HA
'    Public Const NRC_CMDCAN As Integer = &HB
'    Public Const NRC_DUPNAME As Integer = &HD
'    Public Const NRC_NAMTFUL As Integer = &HE
'    Public Const NRC_ACTSES As Integer = &HF
'    Public Const NRC_LOCTFUL As Integer = &H11
'    Public Const NRC_REMTFUL As Integer = &H12
'    Public Const NRC_ILLNN As Integer = &H13
'    Public Const NRC_NOCALL As Integer = &H14
'    Public Const NRC_NOWILD As Integer = &H15
'    Public Const NRC_INUSE As Integer = &H16
'    Public Const NRC_NAMERR As Integer = &H17
'    Public Const NRC_SABORT As Integer = &H18
'    Public Const NRC_NAMCONF As Integer = &H19
'    Public Const NRC_IFBUSY As Integer = &H21
'    Public Const NRC_TOOMANY As Integer = &H22
'    Public Const NRC_BRIDGE As Integer = &H23
'    Public Const NRC_CANOCCR As Integer = &H24
'    Public Const NRC_CANCEL As Integer = &H26
'    Public Const NRC_DUPENV As Integer = &H30
'    Public Const NRC_ENVNOTDEF As Integer = &H34
'    Public Const NRC_OSRESNOTAV As Integer = &H35
'    Public Const NRC_MAXAPPS As Integer = &H36
'    Public Const NRC_NOSAPS As Integer = &H37
'    Public Const NRC_NORESOURCES As Integer = &H38
'    Public Const NRC_INVADDRESS As Integer = &H39
'    Public Const NRC_INVDDID As Integer = &H3B
'    Public Const NRC_LOCKFAIL As Integer = &H3C
'    Public Const NRC_OPENERR As Integer = &H3F
'    Public Const NRC_SYSTEM As Integer = &H40
'    Public Const NRC_PENDING As Integer = &HFF
'    Public Const NUMPRS_LEADING_WHITE As Integer = &H1
'    Public Const NUMPRS_TRAILING_WHITE As Integer = &H2
'    Public Const NUMPRS_LEADING_PLUS As Integer = &H4
'    Public Const NUMPRS_TRAILING_PLUS As Integer = &H8
'    Public Const NUMPRS_LEADING_MINUS As Integer = &H10
'    Public Const NUMPRS_TRAILING_MINUS As Integer = &H20
'    Public Const NUMPRS_HEX_OCT As Integer = &H40
'    Public Const NUMPRS_PARENS As Integer = &H80
'    Public Const NUMPRS_DECIMAL As Integer = &H100
'    Public Const NUMPRS_THOUSANDS As Integer = &H200
'    Public Const NUMPRS_CURRENCY As Integer = &H400
'    Public Const NUMPRS_EXPONENT As Integer = &H800
'    Public Const NUMPRS_USE_ALL As Integer = &H1000
'    Public Const NUMPRS_STD As Integer = &H1FFF
'    Public Const NUMPRS_NEG As Integer = &H10000
'    Public Const NUMPRS_INEXACT As Integer = &H20000
'    Public Const NT351_INTERFACE_SIZE As Integer = &H40
'    Public Const NIM_ADD As Integer = &H0
'    Public Const NIM_MODIFY As Integer = &H1
'    Public Const NIM_DELETE As Integer = &H2
'    Public Const NIF_MESSAGE As Integer = &H1
'    Public Const NIF_ICON As Integer = &H2
'    Public Const NIF_TIP As Integer = &H4
'    Public Const NONZEROLHND As Integer = &H2
'    Public Const NONZEROLPTR As Integer = &H0
'    Public Const NORMAL_PRIORITY_Class As Integer = &H20
'    Public Const NOPARITY As Integer = 0
'    Public Const NMPWAIT_WAIT_FOREVER As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        NOPARITY = 0,
'    '        NMPWAIT_WAIT_FOREVER = unchecked((int)0xFfffffff),
'    '--------------------------------^--- GenCode(token): unexpected token type
'    Public Const NMPWAIT_NOWAIT As Integer = &H1
'    Public Const NMPWAIT_USE_DEFAULT_WAIT As Integer = &H0
'    Public Const NUMLOCK_ON As Integer = &H20
'    Public Const NULL As Integer = 0
'    Public Const NO_ERROR As Integer = 0
'    Public Const NOERROR As Integer = 0
'    Public Const NTE_BAD_UID As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        NOERROR = 0,
'    '        NTE_BAD_UID = unchecked((int)0x80090001),
'    '-----------------------^--- GenCode(token): unexpected token type
'    Public Const NTE_BAD_HASH As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        NTE_BAD_UID = unchecked((int)0x80090001),
'    '        NTE_BAD_HASH = unchecked((int)0x80090002),
'    '------------------------^--- GenCode(token): unexpected token type
'    Public Const NTE_BAD_KEY As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        NTE_BAD_HASH = unchecked((int)0x80090002),
'    '        NTE_BAD_KEY = unchecked((int)0x80090003),
'    '-----------------------^--- GenCode(token): unexpected token type
'    Public Const NTE_BAD_LEN As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        NTE_BAD_KEY = unchecked((int)0x80090003),
'    '        NTE_BAD_LEN = unchecked((int)0x80090004),
'    '-----------------------^--- GenCode(token): unexpected token type
'    Public Const NTE_BAD_DATA As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        NTE_BAD_LEN = unchecked((int)0x80090004),
'    '        NTE_BAD_DATA = unchecked((int)0x80090005),
'    '------------------------^--- GenCode(token): unexpected token type
'    Public Const NTE_BAD_SIGNATURE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        NTE_BAD_DATA = unchecked((int)0x80090005),
'    '        NTE_BAD_SIGNATURE = unchecked((int)0x80090006),
'    '-----------------------------^--- GenCode(token): unexpected token type
'    Public Const NTE_BAD_VER As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        NTE_BAD_SIGNATURE = unchecked((int)0x80090006),
'    '        NTE_BAD_VER = unchecked((int)0x80090007),
'    '-----------------------^--- GenCode(token): unexpected token type
'    Public Const NTE_BAD_ALGID As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        NTE_BAD_VER = unchecked((int)0x80090007),
'    '        NTE_BAD_ALGID = unchecked((int)0x80090008),
'    '-------------------------^--- GenCode(token): unexpected token type
'    Public Const NTE_BAD_FLAGS As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        NTE_BAD_ALGID = unchecked((int)0x80090008),
'    '        NTE_BAD_FLAGS = unchecked((int)0x80090009),
'    '-------------------------^--- GenCode(token): unexpected token type
'    Public Const NTE_BAD_TYPE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        NTE_BAD_FLAGS = unchecked((int)0x80090009),
'    '        NTE_BAD_TYPE = unchecked((int)0x8009000A),
'    '------------------------^--- GenCode(token): unexpected token type
'    Public Const NTE_BAD_KEY_STATE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        NTE_BAD_TYPE = unchecked((int)0x8009000A),
'    '        NTE_BAD_KEY_STATE = unchecked((int)0x8009000B),
'    '-----------------------------^--- GenCode(token): unexpected token type
'    Public Const NTE_BAD_HASH_STATE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        NTE_BAD_KEY_STATE = unchecked((int)0x8009000B),
'    '        NTE_BAD_HASH_STATE = unchecked((int)0x8009000C),
'    '------------------------------^--- GenCode(token): unexpected token type
'    Public Const NTE_NO_KEY As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        NTE_BAD_HASH_STATE = unchecked((int)0x8009000C),
'    '        NTE_NO_KEY = unchecked((int)0x8009000D),
'    '----------------------^--- GenCode(token): unexpected token type
'    Public Const NTE_NO_MEMORY As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        NTE_NO_KEY = unchecked((int)0x8009000D),
'    '        NTE_NO_MEMORY = unchecked((int)0x8009000E),
'    '-------------------------^--- GenCode(token): unexpected token type
'    Public Const NTE_EXISTS As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        NTE_NO_MEMORY = unchecked((int)0x8009000E),
'    '        NTE_EXISTS = unchecked((int)0x8009000F),
'    '----------------------^--- GenCode(token): unexpected token type
'    Public Const NTE_PERM As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        NTE_EXISTS = unchecked((int)0x8009000F),
'    '        NTE_PERM = unchecked((int)0x80090010),
'    '--------------------^--- GenCode(token): unexpected token type
'    Public Const NTE_NOT_FOUND As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        NTE_PERM = unchecked((int)0x80090010),
'    '        NTE_NOT_FOUND = unchecked((int)0x80090011),
'    '-------------------------^--- GenCode(token): unexpected token type
'    Public Const NTE_DOUBLE_ENCRYPT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        NTE_NOT_FOUND = unchecked((int)0x80090011),
'    '        NTE_DOUBLE_ENCRYPT = unchecked((int)0x80090012),
'    '------------------------------^--- GenCode(token): unexpected token type
'    Public Const NTE_BAD_PROVIDER As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        NTE_DOUBLE_ENCRYPT = unchecked((int)0x80090012),
'    '        NTE_BAD_PROVIDER = unchecked((int)0x80090013),
'    '----------------------------^--- GenCode(token): unexpected token type
'    Public Const NTE_BAD_PROV_TYPE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        NTE_BAD_PROVIDER = unchecked((int)0x80090013),
'    '        NTE_BAD_PROV_TYPE = unchecked((int)0x80090014),
'    '-----------------------------^--- GenCode(token): unexpected token type
'    Public Const NTE_BAD_PUBLIC_KEY As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        NTE_BAD_PROV_TYPE = unchecked((int)0x80090014),
'    '        NTE_BAD_PUBLIC_KEY = unchecked((int)0x80090015),
'    '------------------------------^--- GenCode(token): unexpected token type
'    Public Const NTE_BAD_KEYSET As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        NTE_BAD_PUBLIC_KEY = unchecked((int)0x80090015),
'    '        NTE_BAD_KEYSET = unchecked((int)0x80090016),
'    '--------------------------^--- GenCode(token): unexpected token type
'    Public Const NTE_PROV_TYPE_NOT_DEF As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        NTE_BAD_KEYSET = unchecked((int)0x80090016),
'    '        NTE_PROV_TYPE_NOT_DEF = unchecked((int)0x80090017),
'    '---------------------------------^--- GenCode(token): unexpected token type
'    Public Const NTE_PROV_TYPE_ENTRY_BAD As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        NTE_PROV_TYPE_NOT_DEF = unchecked((int)0x80090017),
'    '        NTE_PROV_TYPE_ENTRY_BAD = unchecked((int)0x80090018),
'    '-----------------------------------^--- GenCode(token): unexpected token type
'    Public Const NTE_KEYSET_NOT_DEF As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        NTE_PROV_TYPE_ENTRY_BAD = unchecked((int)0x80090018),
'    '        NTE_KEYSET_NOT_DEF = unchecked((int)0x80090019),
'    '------------------------------^--- GenCode(token): unexpected token type
'    Public Const NTE_KEYSET_ENTRY_BAD As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        NTE_KEYSET_NOT_DEF = unchecked((int)0x80090019),
'    '        NTE_KEYSET_ENTRY_BAD = unchecked((int)0x8009001A),
'    '--------------------------------^--- GenCode(token): unexpected token type
'    Public Const NTE_PROV_TYPE_NO_MATCH As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        NTE_KEYSET_ENTRY_BAD = unchecked((int)0x8009001A),
'    '        NTE_PROV_TYPE_NO_MATCH = unchecked((int)0x8009001B),
'    '----------------------------------^--- GenCode(token): unexpected token type
'    Public Const NTE_SIGNATURE_FILE_BAD As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        NTE_PROV_TYPE_NO_MATCH = unchecked((int)0x8009001B),
'    '        NTE_SIGNATURE_FILE_BAD = unchecked((int)0x8009001C),
'    '----------------------------------^--- GenCode(token): unexpected token type
'    Public Const NTE_PROVIDER_DLL_FAIL As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        NTE_SIGNATURE_FILE_BAD = unchecked((int)0x8009001C),
'    '        NTE_PROVIDER_DLL_FAIL = unchecked((int)0x8009001D),
'    '---------------------------------^--- GenCode(token): unexpected token type
'    Public Const NTE_PROV_DLL_NOT_FOUND As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        NTE_PROVIDER_DLL_FAIL = unchecked((int)0x8009001D),
'    '        NTE_PROV_DLL_NOT_FOUND = unchecked((int)0x8009001E),
'    '----------------------------------^--- GenCode(token): unexpected token type
'    Public Const NTE_BAD_KEYSET_PARAM As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        NTE_PROV_DLL_NOT_FOUND = unchecked((int)0x8009001E),
'    '        NTE_BAD_KEYSET_PARAM = unchecked((int)0x8009001F),
'    '--------------------------------^--- GenCode(token): unexpected token type
'    Public Const NTE_FAIL As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        NTE_BAD_KEYSET_PARAM = unchecked((int)0x8009001F),
'    '        NTE_FAIL = unchecked((int)0x80090020),
'    '--------------------^--- GenCode(token): unexpected token type
'    Public Const NTE_SYS_ERR As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        NTE_FAIL = unchecked((int)0x80090020),
'    '        NTE_SYS_ERR = unchecked((int)0x80090021),
'    '-----------------------^--- GenCode(token): unexpected token type
'    Public Const NTE_OP_OK As Integer = 0
'    Public Const NULLREGION As Integer = 1
'    Public Const NEWFRAME As Integer = 1
'    Public Const NEXTBAND As Integer = 3
'    Public Const NTM_REGULAR As Integer = &H40
'    Public Const NTM_BOLD As Integer = &H20
'    Public Const NTM_ITALIC As Integer = &H1
'    Public Const NONANTIALIASED_QUALITY As Integer = 3
'    Public Const NULL_BRUSH As Integer = 5
'    Public Const NULL_PEN As Integer = 8
'    Public Const NUMBRUSHES As Integer = 16
'    Public Const NUMPENS As Integer = 18
'    Public Const NUMMARKERS As Integer = 20
'    Public Const NUMFONTS As Integer = 22
'    Public Const NUMCOLORS As Integer = 24
'    Public Const NUMRESERVED As Integer = 106
'    Public Const NETPROPERTY_PERSISTENT As Integer = 1
'    Public Const NETINFO_DLL16 As Integer = &H1
'    Public Const NETINFO_DISKRED As Integer = &H4
'    Public Const NETINFO_PRINTERRED As Integer = &H8
'    Public Const NORM_IGNORECASE As Integer = &H1
'    Public Const NORM_IGNORENONSPACE As Integer = &H2
'    Public Const NORM_IGNORESYMBOLS As Integer = &H4
'    Public Const NORM_IGNOREKANATYPE As Integer = &H10000
'    Public Const NORM_IGNOREWIDTH As Integer = &H20000
'    Public Const NLS_VALID_LOCALE_MASK As Integer = &HFFFFF
'    Public Const NO_PROPAGATE_INHERIT_ACE As Integer = &H4
'    Public Const N_BTMASK As Integer = &HF
'    Public Const N_TMASK As Integer = &H30
'    Public Const N_TMASK1 As Integer = &HC0
'    Public Const N_TMASK2 As Integer = &HF0
'    Public Const N_BTSHFT As Integer = 4
'    Public Const N_TSHIFT As Integer = 2
'    Public Const NO_PRIORITY As Integer = 0
'    Public Const NFR_ANSI As Integer = 1
'    Public Const NFR_UNICODE As Integer = 2
'    Public Const NF_QUERY As Integer = 3
'    Public Const NF_REQUERY As Integer = 4
'    Public Const NM_FIRST As Integer = 0 - 0
'    Public Const NM_LAST As Integer = 0 - 99
'    Public Const NM_OUTOFMEMORY As Integer = 0 - 0 - 1
'    Public Const NM_CLICK As Integer = 0 - 0 - 2
'    Public Const NM_DBLCLK As Integer = 0 - 0 - 3
'    Public Const NM_RETURN As Integer = 0 - 0 - 4
'    Public Const NM_RCLICK As Integer = 0 - 0 - 5
'    Public Const NM_RDBLCLK As Integer = 0 - 0 - 6
'    Public Const NM_SETFOCUS As Integer = 0 - 0 - 7
'    Public Const NM_KILLFOCUS As Integer = 0 - 0 - 8
'    Public Const NM_CUSTOMDRAW As Integer = 0 - 0 - 12
'    Public Const NM_HOVER As Integer = 0 - 0 - 13
'    Public Const NM_RELEASEDCAPTURE As Integer = 0 - 0 - 16
'    Public Const NOTSRCCOPY As Integer = &H330008
'    Public Const NOTSRCERASE As Integer = &H1100A6
    
    
    
'    Public Const OFN_READONLY As Integer = &H1
'    Public Const OFN_OVERWRITEPROMPT As Integer = &H2
'    Public Const OFN_HIDEREADONLY As Integer = &H4
'    Public Const OFN_NOCHANGEDIR As Integer = &H8
'    Public Const OFN_SHOWHELP As Integer = &H10
'    Public Const OFN_ENABLEHOOK As Integer = &H20
'    Public Const OFN_ENABLETEMPLATE As Integer = &H40
'    Public Const OFN_ENABLETEMPLATEHANDLE As Integer = &H80
'    Public Const OFN_NOVALIDATE As Integer = &H100
'    Public Const OFN_ALLOWMULTISELECT As Integer = &H200
'    Public Const OFN_EXTENSIONDIFFERENT As Integer = &H400
'    Public Const OFN_PATHMUSTEXIST As Integer = &H800
'    Public Const OFN_FILEMUSTEXIST As Integer = &H1000
'    Public Const OFN_CREATEPROMPT As Integer = &H2000
'    Public Const OFN_SHAREAWARE As Integer = &H4000
'    Public Const OFN_NOREADONLYRETURN As Integer = &H8000
'    Public Const OFN_NOTESTFILECREATE As Integer = &H10000
'    Public Const OFN_NONETWORKBUTTON As Integer = &H20000
'    Public Const OFN_NOLONGNAMES As Integer = &H40000
'    Public Const OFN_EXPLORER As Integer = &H80000
'    Public Const OFN_NODEREFERENCELINKS As Integer = &H100000
'    Public Const OFN_ENABLEINCLUDENOTIFY As Integer = &H400000
'    Public Const OFN_ENABLESIZING As Integer = &H800000
'    Public Const OFN_LONGNAMES As Integer = &H200000
'    Public Const OFN_SHAREFALLTHROUGH As Integer = 2
'    Public Const OFN_SHARENOWARN As Integer = 1
'    Public Const OFN_SHAREWARN As Integer = 0
'    Public Const OFN_USESHELLITEM As Integer = &H1000000
'    Public Const OFN_DONTADDTORECENT As Integer = &H2000000
'    Public Const OFN_FORCESHOWHIDDEN As Integer = &H10000000
'    Public Const OLEIVERB_PRIMARY As Integer = 0
'    Public Const OLEIVERB_SHOW As Integer = - 1
'    Public Const OLEIVERB_OPEN As Integer = - 2
'    Public Const OLEIVERB_HIDE As Integer = - 3
'    Public Const OLEIVERB_UIACTIVATE As Integer = - 4
'    Public Const OLEIVERB_INPLACEACTIVATE As Integer = - 5
'    Public Const OLEIVERB_DISCARDUNDOSTATE As Integer = - 6
'    Public Const OLEIVERB_PROPERTIES As Integer = - 7
'    Public Const OLECREATE_LEAVERUNNING As Integer = &H1
'    Public Const OPEN_EXISTING As Integer = 3
'    Public Const OPEN_ALWAYS As Integer = 4
'    Public Const OUTPUT_DEBUG_STRING_EVENT As Integer = 8
'    Public Const ODDPARITY As Integer = 1
'    Public Const ONESTOPBIT As Integer = 0
'    Public Const ONE5STOPBITS As Integer = 1
'    Public Const OF_READ As Integer = &H0
'    Public Const OF_WRITE As Integer = &H1
'    Public Const OF_READWRITE As Integer = &H2
'    Public Const OF_SHARE_COMPAT As Integer = &H0
'    Public Const OF_SHARE_EXCLUSIVE As Integer = &H10
'    Public Const OF_SHARE_DENY_WRITE As Integer = &H20
'    Public Const OF_SHARE_DENY_READ As Integer = &H30
'    Public Const OF_SHARE_DENY_NONE As Integer = &H40
'    Public Const OF_PARSE As Integer = &H100
'    Public Const OF_DELETE As Integer = &H200
'    Public Const OF_VERIFY As Integer = &H400
'    Public Const OF_CANCEL As Integer = &H800
'    Public Const OF_CREATE As Integer = &H1000
'    Public Const OF_PROMPT As Integer = &H2000
'    Public Const OF_EXIST As Integer = &H4000
'    Public Const OF_REOPEN As Integer = &H8000
'    Public Const OFS_MAXPATHNAME As Integer = 128
'    Public Const OR_INVALID_OXID As Integer = 1910
'    Public Const OR_INVALID_OID As Integer = 1911
'    Public Const OR_INVALID_SET As Integer = 1912
'    Public Const OLE_E_OLEVERB As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        OR_INVALID_SET = 1912,
'    '        OLE_E_OLEVERB = unchecked((int)0x80040000),
'    '-------------------------^--- GenCode(token): unexpected token type
'    Public Const OLE_E_ADVF As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        OLE_E_OLEVERB = unchecked((int)0x80040000),
'    '        OLE_E_ADVF = unchecked((int)0x80040001),
'    '----------------------^--- GenCode(token): unexpected token type
'    Public Const OLE_E_ENUM_NOMORE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        OLE_E_ADVF = unchecked((int)0x80040001),
'    '        OLE_E_ENUM_NOMORE = unchecked((int)0x80040002),
'    '-----------------------------^--- GenCode(token): unexpected token type
'    Public Const OLE_E_ADVISENOTSUPPORTED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        OLE_E_ENUM_NOMORE = unchecked((int)0x80040002),
'    '        OLE_E_ADVISENOTSUPPORTED = unchecked((int)0x80040003),
'    '------------------------------------^--- GenCode(token): unexpected token type
'    Public Const OLE_E_NOCONNECTION As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        OLE_E_ADVISENOTSUPPORTED = unchecked((int)0x80040003),
'    '        OLE_E_NOCONNECTION = unchecked((int)0x80040004),
'    '------------------------------^--- GenCode(token): unexpected token type
'    Public Const OLE_E_NOTRUNNING As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        OLE_E_NOCONNECTION = unchecked((int)0x80040004),
'    '        OLE_E_NOTRUNNING = unchecked((int)0x80040005),
'    '----------------------------^--- GenCode(token): unexpected token type
'    Public Const OLE_E_NOCACHE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        OLE_E_NOTRUNNING = unchecked((int)0x80040005),
'    '        OLE_E_NOCACHE = unchecked((int)0x80040006),
'    '-------------------------^--- GenCode(token): unexpected token type
'    Public Const OLE_E_BLANK As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        OLE_E_NOCACHE = unchecked((int)0x80040006),
'    '        OLE_E_BLANK = unchecked((int)0x80040007),
'    '-----------------------^--- GenCode(token): unexpected token type
'    Public Const OLE_E_CLASSDIFF As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        OLE_E_BLANK = unchecked((int)0x80040007),
'    '        OLE_E_CLASSDIFF = unchecked((int)0x80040008),
'    '---------------------------^--- GenCode(token): unexpected token type
'    Public Const OLE_E_CANT_GETMONIKER As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        OLE_E_CLASSDIFF = unchecked((int)0x80040008),
'    '        OLE_E_CANT_GETMONIKER = unchecked((int)0x80040009),
'    '---------------------------------^--- GenCode(token): unexpected token type
'    Public Const OLE_E_CANT_BINDTOSOURCE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        OLE_E_CANT_GETMONIKER = unchecked((int)0x80040009),
'    '        OLE_E_CANT_BINDTOSOURCE = unchecked((int)0x8004000A),
'    '-----------------------------------^--- GenCode(token): unexpected token type
'    Public Const OLE_E_STATIC As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        OLE_E_CANT_BINDTOSOURCE = unchecked((int)0x8004000A),
'    '        OLE_E_STATIC = unchecked((int)0x8004000B),
'    '------------------------^--- GenCode(token): unexpected token type
'    Public Const OLE_E_PROMPTSAVECANCELLED As Integer = 
'    '
'    'Note:  Error processing original source shown below
        '    '        OLE_E_STATIC = unchecked((int)0x8004000B),

        Public Const OLE_E_PROMPTSAVECANCELLED As Integer = &H8004000C

'    '-------------------------------------^--- GenCode(token): unexpected token type
'    Public Const OLE_E_INVALIDRECT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        OLE_E_PROMPTSAVECANCELLED = unchecked((int)0x8004000C),
'    '        OLE_E_INVALIDRECT = unchecked((int)0x8004000D),
'    '-----------------------------^--- GenCode(token): unexpected token type
'    Public Const OLE_E_WRONGCOMPOBJ As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        OLE_E_INVALIDRECT = unchecked((int)0x8004000D),
'    '        OLE_E_WRONGCOMPOBJ = unchecked((int)0x8004000E),
'    '------------------------------^--- GenCode(token): unexpected token type
'    Public Const OLE_E_INVALIDHWND As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        OLE_E_WRONGCOMPOBJ = unchecked((int)0x8004000E),
'    '        OLE_E_INVALIDHWND = unchecked((int)0x8004000F),
'    '-----------------------------^--- GenCode(token): unexpected token type
'    Public Const OLE_E_NOT_INPLACEACTIVE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        OLE_E_INVALIDHWND = unchecked((int)0x8004000F),
'    '        OLE_E_NOT_INPLACEACTIVE = unchecked((int)0x80040010),
'    '-----------------------------------^--- GenCode(token): unexpected token type
'    Public Const OLE_E_CANTCONVERT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        OLE_E_NOT_INPLACEACTIVE = unchecked((int)0x80040010),
'    '        OLE_E_CANTCONVERT = unchecked((int)0x80040011),
'    '-----------------------------^--- GenCode(token): unexpected token type
'    Public Const OLE_E_NOSTORAGE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        OLE_E_CANTCONVERT = unchecked((int)0x80040011),
'    '        OLE_E_NOSTORAGE = unchecked((int)0x80040012),
'    '---------------------------^--- GenCode(token): unexpected token type
     Public Const OLECMDERR_E_NOTSUPPORTED As Integer = &H80040100
'    '
'    'Note:  Error processing original source shown below
'    '        OLE_E_NOSTORAGE = unchecked((int)0x80040012),
'    '        OLECMDERR_E_NOTSUPPORTED = unchecked((int)0x80040100),
'    '------------------------------------^--- GenCode(token): unexpected token type
'    Public Const OLECMDERR_E_DISABLED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        OLECMDERR_E_NOTSUPPORTED = unchecked((int)0x80040100),
'    '        OLECMDERR_E_DISABLED  = unchecked((int)0x80040101),
'    '---------------------------------^--- GenCode(token): unexpected token type
'    Public Const OLECMDERR_E_NOHELP As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        OLECMDERR_E_DISABLED  = unchecked((int)0x80040101),
'    '        OLECMDERR_E_NOHELP  = unchecked((int)0x80040102),
'    '-------------------------------^--- GenCode(token): unexpected token type
'    Public Const OLECMDERR_E_CANCELED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        OLECMDERR_E_NOHELP  = unchecked((int)0x80040102),
'    '        OLECMDERR_E_CANCELED  = unchecked((int)0x80040103),
'    '---------------------------------^--- GenCode(token): unexpected token type
'    Public Const OLECMDERR_E_UNKNOWNGROUP As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        OLECMDERR_E_CANCELED  = unchecked((int)0x80040103),
'    '        OLECMDERR_E_UNKNOWNGROUP  = unchecked((int)0x80040104),
'    '-------------------------------------^--- GenCode(token): unexpected token type
'    Public Const OLEMISC_RECOMPOSEONRESIZE As Integer = &H1
'    Public Const OLEMISC_ONLYICONIC As Integer = &H2
'    Public Const OLEMISC_INSERTNOTREPLACE As Integer = &H4
'    Public Const OLEMISC_STATIC As Integer = &H8
'    Public Const OLEMISC_CANTLINKINSIDE As Integer = &H10
'    Public Const OLEMISC_CANLINKBYOLE1 As Integer = &H20
'    Public Const OLEMISC_ISLINKOBJECT As Integer = &H40
'    Public Const OLEMISC_INSIDEOUT As Integer = &H80
'    Public Const OLEMISC_ACTIVATEWHENVISIBLE As Integer = &H100
'    Public Const OLEMISC_RENDERINGISDEVICEINDEPENDENT As Integer = &H200
'    Public Const OLEMISC_INVISIBLEATRUNTIME As Integer = &H400
'    Public Const OLEMISC_ALWAYSRUN As Integer = &H800
'    Public Const OLEMISC_ACTSLIKEBUTTON As Integer = &H1000
'    Public Const OLEMISC_ACTSLIKELABEL As Integer = &H2000
'    Public Const OLEMISC_NOUIACTIVATE As Integer = &H4000
'    Public Const OLEMISC_ALIGNABLE As Integer = &H8000
'    Public Const OLEMISC_SIMPLEFRAME As Integer = &H10000
'    Public Const OLEMISC_SETCLIENTSITEFIRST As Integer = &H20000
'    Public Const OLEMISC_IMEMODE As Integer = &H40000
'    Public Const OLEOBJ_E_FIRST As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        OLEMISC_IMEMODE = 0x00040000,
'    '        OLEOBJ_E_FIRST = unchecked((int)0x80040180),
'    '--------------------------^--- GenCode(token): unexpected token type
'    Public Const OLEOBJ_E_LAST As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        OLEOBJ_E_FIRST = unchecked((int)0x80040180),
'    '        OLEOBJ_E_LAST = unchecked((int)0x8004018F),
'    '-------------------------^--- GenCode(token): unexpected token type
'    Public Const OLEOBJ_S_FIRST As Integer = &H40180
'    Public Const OLEOBJ_S_LAST As Integer = &H4018F
'    Public Const OLEOBJ_E_NOVERBS As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        OLEOBJ_S_LAST = 0x0004018F,
'    '        OLEOBJ_E_NOVERBS = unchecked((int)0x80040180),
'    '----------------------------^--- GenCode(token): unexpected token type
'    Public Const OLEOBJ_E_INVALIDVERB As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        OLEOBJ_E_NOVERBS = unchecked((int)0x80040180),
'    '        OLEOBJ_E_INVALIDVERB = unchecked((int)0x80040181),
'    '--------------------------------^--- GenCode(token): unexpected token type
'    Public Const OLE_S_USEREG As Integer = &H40000
'    Public Const OLE_S_STATIC As Integer = &H40001
'    Public Const OLE_S_MAC_CLIPFORMAT As Integer = &H40002
'    Public Const OLEOBJ_S_INVALIDVERB As Integer = &H40180
'    Public Const OLEOBJ_S_CANNOT_DOVERB_NOW As Integer = &H40181
'    Public Const OLEOBJ_S_INVALIDHWND As Integer = &H40182
'    Public Const OPENCHANNEL As Integer = 4110
'    Public Const OBJ_PEN As Integer = 1
'    Public Const OBJ_BRUSH As Integer = 2
'    Public Const OBJ_DC As Integer = 3
'    Public Const OBJ_METADC As Integer = 4
'    Public Const OBJ_PAL As Integer = 5
'    Public Const OBJ_FONT As Integer = 6
'    Public Const OBJ_BITMAP As Integer = 7
'    Public Const OBJ_REGION As Integer = 8
'    Public Const OBJ_METAFILE As Integer = 9
'    Public Const OBJ_MEMDC As Integer = 10
'    Public Const OBJ_EXTPEN As Integer = 11
'    Public Const OBJ_ENHMETADC As Integer = 12
'    Public Const OBJ_ENHMETAFILE As Integer = 13
'    Public Const OUT_DEFAULT_PRECIS As Integer = 0
'    Public Const OUT_STRING_PRECIS As Integer = 1
'    Public Const OUT_CHARACTER_PRECIS As Integer = 2
'    Public Const OUT_STROKE_PRECIS As Integer = 3
'    Public Const OUT_TT_PRECIS As Integer = 4
'    Public Const OUT_DEVICE_PRECIS As Integer = 5
'    Public Const OUT_RASTER_PRECIS As Integer = 6
'    Public Const OUT_TT_ONLY_PRECIS As Integer = 7
'    Public Const OUT_OUTLINE_PRECIS As Integer = 8
'    Public Const OUT_SCREEN_OUTLINE_PRECIS As Integer = 9
'    Public Const OEM_CHARSET As Integer = 255
'    Public Const OPAQUE As Integer = 2
'    Public Const OEM_FIXED_FONT As Integer = 10
'    Public Const OBJECT_INHERIT_ACE As Integer = &H1
'    Public Const OWNER_SECURITY_INFORMATION As Integer = 0
'    Private __unknown As X00000001
'    Private __unknown As __unknown
'    Private ODT_MENU As __unknown = 1
'    Private ODT_LISTBOX As __unknown = 2
'    Private ODT_COMBOBOX As __unknown = 3
'    Private ODT_BUTTON As __unknown = 4
'    Private ODT_STATIC As __unknown = 5
'    Private ODA_DRAWENTIRE As __unknown = &H1
'    Private ODA_SELECT As __unknown = &H2
'    Private ODA_FOCUS As __unknown = &H4
'    Private ODS_CHECKED As __unknown = &H8
'    Private ODS_COMBOBOXEDIT As __unknown = &H1000
'    Private ODS_DEFAULT As __unknown = &H20
'    Private ODS_DISABLED As __unknown = &H4
'    Private ODS_FOCUS As __unknown = &H10
'    Private ODS_GRAYED As __unknown = &H2
'    Private ODS_HOTLIGHT As __unknown = &H40
'    Private ODS_INACTIVE As __unknown = &H80
'    Private ODS_NOACCEL As __unknown = &H100
'    Private ODS_NOFOCUSRECT As __unknown = &H200
'    Private ODS_SELECTED As __unknown = &H1
'    Private OBM_CLOSE As __unknown = 32754
'    Private OBM_UPARROW As __unknown = 32753
'    Private OBM_DNARROW As __unknown = 32752
'    Private OBM_RGARROW As __unknown = 32751
'    Private OBM_LFARROW As __unknown = 32750
'    Private OBM_REDUCE As __unknown = 32749
'    Private OBM_ZOOM As __unknown = 32748
'    Private OBM_RESTORE As __unknown = 32747
'    Private OBM_REDUCED As __unknown = 32746
'    Private OBM_ZOOMD As __unknown = 32745
'    Private OBM_RESTORED As __unknown = 32744
'    Private OBM_UPARROWD As __unknown = 32743
'    Private OBM_DNARROWD As __unknown = 32742
'    Private OBM_RGARROWD As __unknown = 32741
'    Private OBM_LFARROWD As __unknown = 32740
'    Private OBM_MNARROW As __unknown = 32739
'    Private OBM_COMBO As __unknown = 32738
'    Private OBM_UPARROWI As __unknown = 32737
'    Private OBM_DNARROWI As __unknown = 32736
'    Private OBM_RGARROWI As __unknown = 32735
'    Private OBM_LFARROWI As __unknown = 32734
'    Private OBM_OLD_CLOSE As __unknown = 32767
'    Private OBM_SIZE As __unknown = 32766
'    Private OBM_OLD_UPARROW As __unknown = 32765
'    Private OBM_OLD_DNARROW As __unknown = 32764
'    Private OBM_OLD_RGARROW As __unknown = 32763
'    Private OBM_OLD_LFARROW As __unknown = 32762
'    Private OBM_BTSIZE As __unknown = 32761
'    Private OBM_CHECK As __unknown = 32760
'    Private OBM_CHECKBOXES As __unknown = 32759
'    Private OBM_BTNCORNERS As __unknown = 32758
'    Private OBM_OLD_REDUCE As __unknown = 32757
'    Private OBM_OLD_ZOOM As __unknown = 32756
'    Private OBM_OLD_RESTORE As __unknown = 32755
'    Private OCR_NORMAL As __unknown = 32512
'    Private OCR_IBEAM As __unknown = 32513
'    Private OCR_WAIT As __unknown = 32514
'    Private OCR_CROSS As __unknown = 32515
'    Private OCR_UP As __unknown = 32516
'    Private OCR_SIZE As __unknown = 32640
'    Private OCR_ICON As __unknown = 32641
'    Private OCR_SIZENWSE As __unknown = 32642
'    Private OCR_SIZENESW As __unknown = 32643
'    Private OCR_SIZEWE As __unknown = 32644
'    Private OCR_SIZENS As __unknown = 32645
'    Private OCR_SIZEALL As __unknown = 32646
'    Private OCR_ICOCUR As __unknown = 32647
'    Private OCR_NO As __unknown = 32648
'    Private OCR_APPSTARTING As __unknown = 32650
'    Private OIC_SAMPLE As __unknown = 32512
'    Private OIC_HAND As __unknown = 32513
'    Private OIC_QUES As __unknown = 32514
'    Private OIC_BANG As __unknown = 32515
'    Private OIC_NOTE As __unknown = 32516
'    Private OIC_WINLOGO As __unknown = 32517
'    Private OIC_WARNING As __unknown = 32515
'    Private OIC_ERROR As __unknown = 32513
'    Private OIC_INFORMATION As __unknown = 32516
'    Private ORD_LANGDRIVER As __unknown = 1
'    Private ODT_HEADER As __unknown = 100
'    Private ODT_TAB As __unknown = 101
'    Private ODT_LISTVIEW As __unknown = 102
'    Private OLECLOSE_SAVEIFDIRTY As __unknown = 0
'    Private OLECLOSE_NOSAVE As __unknown = 1
'    Private OLECLOSE_PROMPTSAVE As __unknown = 2
'    Private __unknown As __unknown
    
    
    
    
    
    
    
    
    
    
'    Public Const PDERR_PRINTERCODES As Integer = &H1000
'    Public Const PDERR_SETUPFAILURE As Integer = &H1001
'    Public Const PDERR_PARSEFAILURE As Integer = &H1002
'    Public Const PDERR_RETDEFFAILURE As Integer = &H1003
'    Public Const PDERR_LOADDRVFAILURE As Integer = &H1004
'    Public Const PDERR_GETDEVMODEFAIL As Integer = &H1005
'    Public Const PDERR_INITFAILURE As Integer = &H1006
'    Public Const PDERR_NODEVICES As Integer = &H1007
'    Public Const PDERR_NODEFAULTPRN As Integer = &H1008
'    Public Const PDERR_DNDMMISMATCH As Integer = &H1009
'    Public Const PDERR_CREATEICFAILURE As Integer = &H100A
'    Public Const PDERR_PRINTERNOTFOUND As Integer = &H100B
'    Public Const PDERR_DEFAULTDIFFERENT As Integer = &H100C
'    Public Const PRINTER_FONTTYPE As Integer = &H4000
'    Public Const PD_ALLPAGES As Integer = &H0
'    Public Const PD_SELECTION As Integer = &H1
'    Public Const PD_PAGENUMS As Integer = &H2
'    Public Const PD_CURRENTPAGE As Integer = &H400000
'    Public Const PD_NOSELECTION As Integer = &H4
'    Public Const PD_NOPAGENUMS As Integer = &H8
'    Public Const PD_NOCURRENTPAGE As Integer = &H800000
'    Public Const PD_COLLATE As Integer = &H10
'    Public Const PD_PRINTTOFILE As Integer = &H20
'    Public Const PD_PRINTSETUP As Integer = &H40
'    Public Const PD_NOWARNING As Integer = &H80
'    Public Const PD_RETURNDC As Integer = &H100
'    Public Const PD_RETURNIC As Integer = &H200
'    Public Const PD_RETURNDEFAULT As Integer = &H400
'    Public Const PD_SHOWHELP As Integer = &H800
'    Public Const PD_ENABLEPRINTHOOK As Integer = &H1000
'    Public Const PD_ENABLESETUPHOOK As Integer = &H2000
'    Public Const PD_ENABLEPRINTTEMPLATE As Integer = &H4000
'    Public Const PD_ENABLESETUPTEMPLATE As Integer = &H8000
'    Public Const PD_ENABLEPRINTTEMPLATEHANDLE As Integer = &H10000
'    Public Const PD_ENABLESETUPTEMPLATEHANDLE As Integer = &H20000
'    Public Const PD_USEDEVMODECOPIES As Integer = &H40000
'    Public Const PD_USEDEVMODECOPIESANDCOLLATE As Integer = &H40000
'    Public Const PD_DISABLEPRINTTOFILE As Integer = &H80000
'    Public Const PD_HIDEPRINTTOFILE As Integer = &H100000
'    Public Const PD_NONETWORKBUTTON As Integer = &H200000
'    Public Const PSD_DEFAULTMINMARGINS As Integer = &H0
'    Public Const PSD_INWININIINTLMEASURE As Integer = &H0
'    Public Const PSD_MINMARGINS As Integer = &H1
'    Public Const PSD_MARGINS As Integer = &H2
'    Public Const PSD_INTHOUSANDTHSOFINCHES As Integer = &H4
'    Public Const PSD_INHUNDREDTHSOFMILLIMETERS As Integer = &H8
'    Public Const PSD_DISABLEMARGINS As Integer = &H10
'    Public Const PSD_DISABLEPRINTER As Integer = &H20
'    Public Const PSD_NOWARNING As Integer = &H80
'    Public Const PSD_DISABLEORIENTATION As Integer = &H100
'    Public Const PSD_RETURNDEFAULT As Integer = &H400
'    Public Const PSD_DISABLEPAPER As Integer = &H200
'    Public Const PSD_SHOWHELP As Integer = &H800
'    Public Const PSD_ENABLEPAGESETUPHOOK As Integer = &H2000
'    Public Const PSD_ENABLEPAGESETUPTEMPLATE As Integer = &H8000
'    Public Const PSD_ENABLEPAGESETUPTEMPLATEHANDLE As Integer = &H20000
'    Public Const PSD_ENABLEPAGEPAINTHOOK As Integer = &H40000
'    Public Const PSD_DISABLEPAGEPAINTING As Integer = &H80000
'    Public Const PSD_NONETWORKBUTTON As Integer = &H200000
'    Public Const psh1 As Integer = &H400
'    Public Const psh2 As Integer = &H401
'    Public Const psh3 As Integer = &H402
'    Public Const psh4 As Integer = &H403
'    Public Const psh5 As Integer = &H404
'    Public Const psh6 As Integer = &H405
'    Public Const psh7 As Integer = &H406
'    Public Const psh8 As Integer = &H407
'    Public Const psh9 As Integer = &H408
'    Public Const psh10 As Integer = &H409
'    Public Const psh11 As Integer = &H40A
'    Public Const psh12 As Integer = &H40B
'    Public Const psh13 As Integer = &H40C
'    Public Const psh14 As Integer = &H40D
'    Public Const psh15 As Integer = &H40E
'    Public Const pshHelp As Integer = &H40E
'    Public Const psh16 As Integer = &H40F
'    Public Const PRINTDLGORD As Integer = 1538
'    Public Const PRNSETUPDLGORD As Integer = 1539
'    Public Const PAGESETUPDLGORD As Integer = 1546
'    Public Const PARAMFLAG_NONE As Integer = 0
'    Public Const PARAMFLAG_FIN As Integer = &H1
'    Public Const PARAMFLAG_FOUT As Integer = &H2
'    Public Const PARAMFLAG_FLCID As Integer = &H4
'    Public Const PARAMFLAG_FRETVAL As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                          PARAMFLAG_FLCID = (0x4),
'    '                                                                            PARAMFLAG_FRETVAL = (unchecked((int)0x8)),
'    '--------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const PARAMFLAG_FOPT As Integer = &H10
'    Public Const PARAMFLAG_FHASDEFAULT As Integer = &H20
'    Public Const PROPSETFLAG_DEFAULT As Integer = 0
'    Public Const PROPSETFLAG_NONSIMPLE As Integer = 1
'    Public Const PROPSETFLAG_ANSI As Integer = 2
'    Public Const PID_DICTIONARY As Integer = 0
'    Public Const PID_CODEPAGE As Integer = &H1
'    Public Const PID_FIRST_USABLE As Integer = &H2
'    Public Const PID_FIRST_NAME_DEFAULT As Integer = &HFFF
'    Public Const PID_LOCALE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                                                                                                                             PID_FIRST_NAME_DEFAULT = (0xfff),
'    '                                                                                                                                                                                                                                                                                      PID_LOCALE = (unchecked((int)0x80000000)),
'    '-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const PID_MODIFY_TIME As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                                                                                                                                                      PID_LOCALE = (unchecked((int)0x80000000)),
'    '                                                                                                                                                                                                                                                                                                   PID_MODIFY_TIME = (unchecked((int)0x80000001)),
'    '-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const PID_SECURITY As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                                                                                                                                                                   PID_MODIFY_TIME = (unchecked((int)0x80000001)),
'    '                                                                                                                                                                                                                                                                                                                     PID_SECURITY = (unchecked((int)0x80000002)),
'    '--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const PID_ILLEGAL As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                                                                                                                                                                                     PID_SECURITY = (unchecked((int)0x80000002)),
'    '                                                                                                                                                                                                                                                                                                                                    PID_ILLEGAL = (unchecked((int)0xFfffffff)),
'    '----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const PIDSI_TITLE As Integer = &H2
'    Public Const PIDSI_SUBJECT As Integer = &H3
'    Public Const PIDSI_AUTHOR As Integer = &H4
'    Public Const PIDSI_KEYWORDS As Integer = &H5
'    Public Const PIDSI_COMMENTS As Integer = &H6
'    Public Const PIDSI_TEMPLATE As Integer = &H7
'    Public Const PIDSI_LASTAUTHOR As Integer = &H8
'    Public Const PIDSI_REVNUMBER As Integer = &H9
'    Public Const PIDSI_EDITTIME As Integer = &HA
'    Public Const PIDSI_LASTPRINTED As Integer = &HB
'    Public Const PIDSI_CREATE_DTM As Integer = &HC
'    Public Const PIDSI_LASTSAVE_DTM As Integer = &HD
'    Public Const PIDSI_PAGECOUNT As Integer = &HE
'    Public Const PIDSI_WORDCOUNT As Integer = &HF
'    Public Const PIDSI_CHARCOUNT As Integer = &H10
'    Public Const PIDSI_THUMBNAIL As Integer = &H11
'    Public Const PIDSI_APPNAME As Integer = &H12
'    Public Const PIDSI_DOC_SECURITY As Integer = &H13
'    Public Const PRSPEC_INVALID As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        PIDSI_DOC_SECURITY = 0x00000013,
'    '        PRSPEC_INVALID = (unchecked((int)0xFfffffff)),
'    '---------------------------^--- GenCode(token): unexpected token type
'    Public Const PRSPEC_LPWSTR As Integer = 0
'    Public Const PRSPEC_PROPID As Integer = 1
'    Public Const PROPSETHDR_OSVERSION_UNKNOWN As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                         PRSPEC_PROPID = (1),
'    '                                                         PROPSETHDR_OSVERSION_UNKNOWN = unchecked((int)0xFFFFFFFF),
'    '-----------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const PSP_DEFAULT As Integer = &H0
'    Public Const PSP_DLGINDIRECT As Integer = &H1
'    Public Const PSP_USEHICON As Integer = &H2
'    Public Const PSP_USEICONID As Integer = &H4
'    Public Const PSP_USETITLE As Integer = &H8
'    Public Const PSP_RTLREADING As Integer = &H10
'    Public Const PSP_HASHELP As Integer = &H20
'    Public Const PSP_USEREFPARENT As Integer = &H40
'    Public Const PSP_USECALLBACK As Integer = &H80
'    Public Const PSPCB_RELEASE As Integer = 1
'    Public Const PSPCB_CREATE As Integer = 2
'    Public Const PSH_DEFAULT As Integer = &H0
'    Public Const PSH_PROPTITLE As Integer = &H1
'    Public Const PSH_USEHICON As Integer = &H2
'    Public Const PSH_USEICONID As Integer = &H4
'    Public Const PSH_PROPSHEETPAGE As Integer = &H8
'    Public Const PSH_WIZARD As Integer = &H20
'    Public Const PSH_USEPSTARTPAGE As Integer = &H40
'    Public Const PSH_NOAPPLYNOW As Integer = &H80
'    Public Const PSH_USECALLBACK As Integer = &H100
'    Public Const PSH_HASHELP As Integer = &H200
'    Public Const PSH_MODELESS As Integer = &H400
'    Public Const PSH_RTLREADING As Integer = &H800
'    Public Const PSCB_INITIALIZED As Integer = 1
'    Public Const PSCB_PRECREATE As Integer = 2
'    Public Const PSN_FIRST As Integer = 0 - 200
'    Public Const PSN_LAST As Integer = 0 - 299
'    Public Const PSN_SETACTIVE As Integer = 0 - 200 - 0
'    Public Const PSN_KILLACTIVE As Integer = 0 - 200 - 1
'    Public Const PSN_APPLY As Integer = 0 - 200 - 2
'    Public Const PSN_RESET As Integer = 0 - 200 - 3
'    Public Const PSN_HELP As Integer = 0 - 200 - 5
'    Public Const PSN_WIZBACK As Integer = 0 - 200 - 6
'    Public Const PSN_WIZNEXT As Integer = 0 - 200 - 7
'    Public Const PSN_WIZFINISH As Integer = 0 - 200 - 8
'    Public Const PSN_QUERYCANCEL As Integer = 0 - 200 - 9
'    Public Const PSNRET_NOERROR As Integer = 0
'    Public Const PSNRET_INVALID As Integer = 1
'    Public Const PSNRET_INVALID_NOCHANGEPAGE As Integer = 2
'    Public Const PSWIZB_BACK As Integer = &H1
'    Public Const PSWIZB_NEXT As Integer = &H2
'    Public Const PSWIZB_FINISH As Integer = &H4
'    Public Const PSWIZB_DISABLEDFINISH As Integer = &H8
'    Public Const PSBTN_BACK As Integer = 0
'    Public Const PSBTN_NEXT As Integer = 1
'    Public Const PSBTN_FINISH As Integer = 2
'    Public Const PSBTN_OK As Integer = 3
'    Public Const PSBTN_APPLYNOW As Integer = 4
'    Public Const PSBTN_CANCEL As Integer = 5
'    Public Const PSBTN_HELP As Integer = 6
'    Public Const PSBTN_MAX As Integer = 6
'    Public Const PROP_SM_CXDLG As Integer = 212
'    Public Const PROP_SM_CYDLG As Integer = 188
'    Public Const PROP_MED_CXDLG As Integer = 227
'    Public Const PROP_MED_CYDLG As Integer = 215
'    Public Const PROP_LG_CXDLG As Integer = 252
'    Public Const PROP_LG_CYDLG As Integer = 218
'    Public Const PO_DELETE As Integer = &H13
'    Public Const PO_RENAME As Integer = &H14
'    Public Const PO_PORTCHANGE As Integer = &H20
'    Public Const PO_REN_PORT As Integer = &H34
'    Public Const PROGRESS_CONTINUE As Integer = 0
'    Public Const PROGRESS_CANCEL As Integer = 1
'    Public Const PROGRESS_STOP As Integer = 2
'    Public Const PROGRESS_QUIET As Integer = 3
'    Public Const PIPE_ACCESS_INBOUND As Integer = &H1
'    Public Const PIPE_ACCESS_OUTBOUND As Integer = &H2
'    Public Const PIPE_ACCESS_DUPLEX As Integer = &H3
'    Public Const PIPE_CLIENT_END As Integer = &H0
'    Public Const PIPE_SERVER_END As Integer = &H1
'    Public Const PIPE_WAIT As Integer = &H0
'    Public Const PIPE_NOWAIT As Integer = &H1
'    Public Const PIPE_READMODE_BYTE As Integer = &H0
'    Public Const PIPE_READMODE_MESSAGE As Integer = &H2
'    Public Const PIPE_TYPE_BYTE As Integer = &H0
'    Public Const PIPE_TYPE_MESSAGE As Integer = &H4
'    Public Const PIPE_UNLIMITED_INSTANCES As Integer = 255
'    Public Const PST_UNSPECIFIED As Integer = &H0
'    Public Const PST_RS232 As Integer = &H1
'    Public Const PST_PARALLELPORT As Integer = &H2
'    Public Const PST_RS422 As Integer = &H3
'    Public Const PST_RS423 As Integer = &H4
'    Public Const PST_RS449 As Integer = &H5
'    Public Const PST_MODEM As Integer = &H6
'    Public Const PST_FAX As Integer = &H21
'    Public Const PST_SCANNER As Integer = &H22
'    Public Const PST_NETWORK_BRIDGE As Integer = &H100
'    Public Const PST_LAT As Integer = &H101
'    Public Const PST_TCPIP_TELNET As Integer = &H102
'    Public Const PST_X25 As Integer = &H103
'    Public Const PCF_DTRDSR As Integer = &H1
'    Public Const PCF_RTSCTS As Integer = &H2
'    Public Const PCF_RLSD As Integer = &H4
'    Public Const PCF_PARITY_CHECK As Integer = &H8
'    Public Const PCF_XONXOFF As Integer = &H10
'    Public Const PCF_SETXCHAR As Integer = &H20
'    Public Const PCF_TOTALTIMEOUTS As Integer = &H40
'    Public Const PCF_INTTIMEOUTS As Integer = &H80
'    Public Const PCF_SPECIALCHARS As Integer = &H100
'    Public Const PCF_16BITMODE As Integer = &H200
'    Public Const PARITY_NONE As Integer = &H100
'    Public Const PARITY_ODD As Integer = &H200
'    Public Const PARITY_EVEN As Integer = &H400
'    Public Const PARITY_MARK As Integer = &H800
'    Public Const PARITY_SPACE As Integer = &H1000
'    Public Const PROFILE_USER As Integer = &H10000000
'    Public Const PROFILE_KERNEL As Integer = &H20000000
'    Public Const PROFILE_SERVER As Integer = &H40000000
'    Public Const PURGE_TXABORT As Integer = &H1
'    Public Const PURGE_RXABORT As Integer = &H2
'    Public Const PURGE_TXCLEAR As Integer = &H4
'    Public Const PURGE_RXCLEAR As Integer = &H8
'    Public Const PROCESS_HEAP_REGION As Integer = &H1
'    Public Const PROCESS_HEAP_UNCOMMITTED_RANGE As Integer = &H2
'    Public Const PROCESS_HEAP_ENTRY_BUSY As Integer = &H4
'    Public Const PROCESS_HEAP_ENTRY_MOVEABLE As Integer = &H10
'    Public Const PROCESS_HEAP_ENTRY_DDESHARE As Integer = &H20
'    Public Const PUBLICKEYBLOB As Integer = &H6
'    Public Const PRIVATEKEYBLOB As Integer = &H7
'    Public Const PKCS5_PADDING As Integer = 1
'    Public Const PP_ENUMALGS As Integer = 1
'    Public Const PP_ENUMCONTAINERS As Integer = 2
'    Public Const PP_IMPTYPE As Integer = 3
'    Public Const PP_NAME As Integer = 4
'    Public Const PP_VERSION As Integer = 5
'    Public Const PP_CONTAINER As Integer = 6
'    Public Const PP_CLIENT_HWND As Integer = 1
'    Public Const PROV_RSA_FULL As Integer = 1
'    Public Const PROV_RSA_SIG As Integer = 2
'    Public Const PROV_DSS As Integer = 3
'    Public Const PROV_FORTEZZA As Integer = 4
'    Public Const PROV_MS_EXCHANGE As Integer = 5
'    Public Const PROV_SSL As Integer = 6
'    Public Const PROV_STT_MER As Integer = 7
'    Public Const PROV_STT_ACQ As Integer = 8
'    Public Const PROV_STT_BRND As Integer = 9
'    Public Const PROV_STT_ROOT As Integer = 10
'    Public Const PROV_STT_ISS As Integer = 11
'    Public Const PERSIST_E_SIZEDEFINITE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        PROV_STT_ISS = 11,
'    '        PERSIST_E_SIZEDEFINITE = unchecked((int)0x800B0009),
'    '----------------------------------^--- GenCode(token): unexpected token type
'    Public Const PERSIST_E_SIZEINDEFINITE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        PERSIST_E_SIZEDEFINITE = unchecked((int)0x800B0009),
'    '        PERSIST_E_SIZEINDEFINITE = unchecked((int)0x800B000A),
'    '------------------------------------^--- GenCode(token): unexpected token type
'    Public Const PERSIST_E_NOTSELFSIZING As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        PERSIST_E_SIZEINDEFINITE = unchecked((int)0x800B000A),
'    '        PERSIST_E_NOTSELFSIZING = unchecked((int)0x800B000B),
'    '-----------------------------------^--- GenCode(token): unexpected token type
'    Public Const POLYFILL_LAST As Integer = 2
'    Public Const PASSTHROUGH As Integer = 19
'    Public Const POSTSCRIPT_DATA As Integer = 37
'    Public Const POSTSCRIPT_IGNORE As Integer = 38
'    Public Const POSTSCRIPT_PASSTHROUGH As Integer = 4115
'    Public Const PR_JOBSTATUS As Integer = &H0
'    Public Const PROOF_QUALITY As Integer = 2
'    Public Const PANOSE_COUNT As Integer = 10
'    Public Const PAN_FAMILYTYPE_INDEX As Integer = 0
'    Public Const PAN_SERIFSTYLE_INDEX As Integer = 1
'    Public Const PAN_WEIGHT_INDEX As Integer = 2
'    Public Const PAN_PROPORTION_INDEX As Integer = 3
'    Public Const PAN_CONTRAST_INDEX As Integer = 4
'    Public Const PAN_STROKEVARIATION_INDEX As Integer = 5
'    Public Const PAN_ARMSTYLE_INDEX As Integer = 6
'    Public Const PAN_LETTERFORM_INDEX As Integer = 7
'    Public Const PAN_MIDLINE_INDEX As Integer = 8
'    Public Const PAN_XHEIGHT_INDEX As Integer = 9
'    Public Const PAN_CULTURE_LATIN As Integer = 0
'    Public Const PAN_ANY As Integer = 0
'    Public Const PAN_NO_FIT As Integer = 1
'    Public Const PAN_FAMILY_TEXT_DISPLAY As Integer = 2
'    Public Const PAN_FAMILY_SCRIPT As Integer = 3
'    Public Const PAN_FAMILY_DECORATIVE As Integer = 4
'    Public Const PAN_FAMILY_PICTORIAL As Integer = 5
'    Public Const PAN_SERIF_COVE As Integer = 2
'    Public Const PAN_SERIF_OBTUSE_COVE As Integer = 3
'    Public Const PAN_SERIF_SQUARE_COVE As Integer = 4
'    Public Const PAN_SERIF_OBTUSE_SQUARE_COVE As Integer = 5
'    Public Const PAN_SERIF_SQUARE As Integer = 6
'    Public Const PAN_SERIF_THIN As Integer = 7
'    Public Const PAN_SERIF_BONE As Integer = 8
'    Public Const PAN_SERIF_EXAGGERATED As Integer = 9
'    Public Const PAN_SERIF_TRIANGLE As Integer = 10
'    Public Const PAN_SERIF_NORMAL_SANS As Integer = 11
'    Public Const PAN_SERIF_OBTUSE_SANS As Integer = 12
'    Public Const PAN_SERIF_PERP_SANS As Integer = 13
'    Public Const PAN_SERIF_FLARED As Integer = 14
'    Public Const PAN_SERIF_ROUNDED As Integer = 15
'    Public Const PAN_WEIGHT_VERY_LIGHT As Integer = 2
'    Public Const PAN_WEIGHT_LIGHT As Integer = 3
'    Public Const PAN_WEIGHT_THIN As Integer = 4
'    Public Const PAN_WEIGHT_BOOK As Integer = 5
'    Public Const PAN_WEIGHT_MEDIUM As Integer = 6
'    Public Const PAN_WEIGHT_DEMI As Integer = 7
'    Public Const PAN_WEIGHT_BOLD As Integer = 8
'    Public Const PAN_WEIGHT_HEAVY As Integer = 9
'    Public Const PAN_WEIGHT_BLACK As Integer = 10
'    Public Const PAN_WEIGHT_NORD As Integer = 11
'    Public Const PAN_PROP_OLD_STYLE As Integer = 2
'    Public Const PAN_PROP_MODERN As Integer = 3
'    Public Const PAN_PROP_EVEN_WIDTH As Integer = 4
'    Public Const PAN_PROP_EXPANDED As Integer = 5
'    Public Const PAN_PROP_CONDENSED As Integer = 6
'    Public Const PAN_PROP_VERY_EXPANDED As Integer = 7
'    Public Const PAN_PROP_VERY_CONDENSED As Integer = 8
'    Public Const PAN_PROP_MONOSPACED As Integer = 9
'    Public Const PAN_CONTRAST_NONE As Integer = 2
'    Public Const PAN_CONTRAST_VERY_LOW As Integer = 3
'    Public Const PAN_CONTRAST_LOW As Integer = 4
'    Public Const PAN_CONTRAST_MEDIUM_LOW As Integer = 5
'    Public Const PAN_CONTRAST_MEDIUM As Integer = 6
'    Public Const PAN_CONTRAST_MEDIUM_HIGH As Integer = 7
'    Public Const PAN_CONTRAST_HIGH As Integer = 8
'    Public Const PAN_CONTRAST_VERY_HIGH As Integer = 9
'    Public Const PAN_STROKE_GRADUAL_DIAG As Integer = 2
'    Public Const PAN_STROKE_GRADUAL_TRAN As Integer = 3
'    Public Const PAN_STROKE_GRADUAL_VERT As Integer = 4
'    Public Const PAN_STROKE_GRADUAL_HORZ As Integer = 5
'    Public Const PAN_STROKE_RAPID_VERT As Integer = 6
'    Public Const PAN_STROKE_RAPID_HORZ As Integer = 7
'    Public Const PAN_STROKE_INSTANT_VERT As Integer = 8
'    Public Const PAN_STRAIGHT_ARMS_HORZ As Integer = 2
'    Public Const PAN_STRAIGHT_ARMS_WEDGE As Integer = 3
'    Public Const PAN_STRAIGHT_ARMS_VERT As Integer = 4
'    Public Const PAN_STRAIGHT_ARMS_SINGLE_SERIF As Integer = 5
'    Public Const PAN_STRAIGHT_ARMS_DOUBLE_SERIF As Integer = 6
'    Public Const PAN_BENT_ARMS_HORZ As Integer = 7
'    Public Const PAN_BENT_ARMS_WEDGE As Integer = 8
'    Public Const PAN_BENT_ARMS_VERT As Integer = 9
'    Public Const PAN_BENT_ARMS_SINGLE_SERIF As Integer = 10
'    Public Const PAN_BENT_ARMS_DOUBLE_SERIF As Integer = 11
'    Public Const PAN_LETT_NORMAL_CONTACT As Integer = 2
'    Public Const PAN_LETT_NORMAL_WEIGHTED As Integer = 3
'    Public Const PAN_LETT_NORMAL_BOXED As Integer = 4
'    Public Const PAN_LETT_NORMAL_FLATTENED As Integer = 5
'    Public Const PAN_LETT_NORMAL_ROUNDED As Integer = 6
'    Public Const PAN_LETT_NORMAL_OFF_CENTER As Integer = 7
'    Public Const PAN_LETT_NORMAL_SQUARE As Integer = 8
'    Public Const PAN_LETT_OBLIQUE_CONTACT As Integer = 9
'    Public Const PAN_LETT_OBLIQUE_WEIGHTED As Integer = 10
'    Public Const PAN_LETT_OBLIQUE_BOXED As Integer = 11
'    Public Const PAN_LETT_OBLIQUE_FLATTENED As Integer = 12
'    Public Const PAN_LETT_OBLIQUE_ROUNDED As Integer = 13
'    Public Const PAN_LETT_OBLIQUE_OFF_CENTER As Integer = 14
'    Public Const PAN_LETT_OBLIQUE_SQUARE As Integer = 15
'    Public Const PAN_MIDLINE_STANDARD_TRIMMED As Integer = 2
'    Public Const PAN_MIDLINE_STANDARD_POINTED As Integer = 3
'    Public Const PAN_MIDLINE_STANDARD_SERIFED As Integer = 4
'    Public Const PAN_MIDLINE_HIGH_TRIMMED As Integer = 5
'    Public Const PAN_MIDLINE_HIGH_POINTED As Integer = 6
'    Public Const PAN_MIDLINE_HIGH_SERIFED As Integer = 7
'    Public Const PAN_MIDLINE_LOW_TRIMMED As Integer = 11
'    Public Const PAN_MIDLINE_LOW_POINTED As Integer = 12
'    Public Const PAN_MIDLINE_LOW_SERIFED As Integer = 13
'    Public Const PAN_XHEIGHT_DUCKING_SMALL As Integer = 5
'    Public Const PAN_XHEIGHT_DUCKING_STD As Integer = 6
'    Public Const PAN_XHEIGHT_DUCKING_LARGE As Integer = 7
'    Public Const PC_RESERVED As Integer = &H1
'    Public Const PC_EXPLICIT As Integer = &H2
'    Public Const PC_NOCOLLAPSE As Integer = &H4
'    Public Const PT_CLOSEFIGURE As Integer = &H1
'    Public Const PT_LINETO As Integer = &H2
'    Public Const PT_BEZIERTO As Integer = &H4
'    Public Const PT_MOVETO As Integer = &H6
'    Public Const PS_SOLID As Integer = 0
'    Public Const PS_DASH As Integer = 1
'    Public Const PS_DOT As Integer = 2
'    Public Const PS_DASHDOT As Integer = 3
'    Public Const PS_DASHDOTDOT As Integer = 4
'    Public Const PS_NULL As Integer = 5
'    Public Const PS_INSIDEFRAME As Integer = 6
'    Public Const PS_USERSTYLE As Integer = 7
'    Public Const PS_ALTERNATE As Integer = 8
'    Public Const PS_STYLE_MASK As Integer = &HF
'    Public Const PS_ENDCAP_ROUND As Integer = &H0
'    Public Const PS_ENDCAP_SQUARE As Integer = &H100
'    Public Const PS_ENDCAP_FLAT As Integer = &H200
'    Public Const PS_ENDCAP_MASK As Integer = &HF00
'    Public Const PS_JOIN_ROUND As Integer = &H0
'    Public Const PS_JOIN_BEVEL As Integer = &H1000
'    Public Const PS_JOIN_MITER As Integer = &H2000
'    Public Const PS_JOIN_MASK As Integer = &HF000
'    Public Const PS_COSMETIC As Integer = &H0
'    Public Const PS_GEOMETRIC As Integer = &H10000
'    Public Const PS_TYPE_MASK As Integer = &HF0000
'    Public Const PLANES As Integer = 14
'    Public Const PDEVICESIZE As Integer = 26
'    Public Const POLYGONALCAPS As Integer = 32
'    Public Const PHYSICALWIDTH As Integer = 110
'    Public Const PHYSICALHEIGHT As Integer = 111
'    Public Const PHYSICALOFFSETX As Integer = 112
'    Public Const PHYSICALOFFSETY As Integer = 113
'    Public Const PC_NONE As Integer = 0
'    Public Const PC_POLYGON As Integer = 1
'    Public Const PC_RECTANGLE As Integer = 2
'    Public Const PC_WINDPOLYGON As Integer = 4
'    Public Const PC_TRAPEZOID As Integer = 4
'    Public Const PC_SCANLINE As Integer = 8
'    Public Const PC_WIDE As Integer = 16
'    Public Const PC_STYLED As Integer = 32
'    Public Const PC_WIDESTYLED As Integer = 64
'    Public Const PC_INTERIORS As Integer = 128
'    Public Const PC_POLYPOLYGON As Integer = 256
'    Public Const PC_PATHS As Integer = 512
'    Public Const PFD_TYPE_RGBA As Integer = 0
'    Public Const PFD_TYPE_COLORINDEX As Integer = 1
'    Public Const PFD_MAIN_PLANE As Integer = 0
'    Public Const PFD_OVERLAY_PLANE As Integer = 1
'    Public Const PFD_UNDERLAY_PLANE As Integer = - 1
'    Public Const PFD_DOUBLEBUFFER As Integer = &H1
'    Public Const PFD_STEREO As Integer = &H2
'    Public Const PFD_DRAW_TO_WINDOW As Integer = &H4
'    Public Const PFD_DRAW_TO_BITMAP As Integer = &H8
'    Public Const PFD_SUPPORT_GDI As Integer = &H10
'    Public Const PFD_SUPPORT_OPENGL As Integer = &H20
'    Public Const PFD_GENERIC_FORMAT As Integer = &H40
'    Public Const PFD_NEED_PALETTE As Integer = &H80
'    Public Const PFD_NEED_SYSTEM_PALETTE As Integer = &H100
'    Public Const PFD_SWAP_EXCHANGE As Integer = &H200
'    Public Const PFD_SWAP_COPY As Integer = &H400
'    Public Const PFD_SWAP_LAYER_BUFFERS As Integer = &H800
'    Public Const PFD_GENERIC_ACCELERATED As Integer = &H1000
'    Public Const PFD_DEPTH_DONTCARE As Integer = &H20000000
'    Public Const PFD_DOUBLEBUFFER_DONTCARE As Integer = &H40000000
'    Public Const PFD_STEREO_DONTCARE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        PFD_DOUBLEBUFFER_DONTCARE = 0x40000000,
'    '        PFD_STEREO_DONTCARE = unchecked((int)0x80000000),
'    '-------------------------------^--- GenCode(token): unexpected token type
'    Public Const PP_DISPLAYERRORS As Integer = &H1
'    Public Const PROCESS_TERMINATE As Integer = &H1
'    Public Const PROCESS_CREATE_THREAD As Integer = &H2
'    Public Const PROCESS_VM_OPERATION As Integer = &H8
'    Public Const PROCESS_VM_READ As Integer = &H10
'    Public Const PROCESS_VM_WRITE As Integer = &H20
'    Public Const PROCESS_DUP_HANDLE As Integer = &H40
'    Public Const PROCESS_CREATE_PROCESS As Integer = &H80
'    Public Const PROCESS_SET_QUOTA As Integer = &H100
'    Public Const PROCESS_SET_INFORMATION As Integer = &H200
'    Public Const PROCESS_QUERY_INFORMATION As Integer = &H400
'    Public Const PROCESSOR_INTEL_386 As Integer = 386
'    Public Const PROCESSOR_INTEL_486 As Integer = 486
'    Public Const PROCESSOR_INTEL_PENTIUM As Integer = 586
'    Public Const PROCESSOR_MIPS_R4000 As Integer = 4000
'    Public Const PROCESSOR_ALPHA_21064 As Integer = 21064
'    Public Const PROCESSOR_ARCHITECTURE_INTEL As Integer = 0
'    Public Const PROCESSOR_ARCHITECTURE_MIPS As Integer = 1
'    Public Const PROCESSOR_ARCHITECTURE_ALPHA As Integer = 2
'    Public Const PROCESSOR_ARCHITECTURE_PPC As Integer = 3
'    Public Const PROCESSOR_ARCHITECTURE_UNKNOWN As Integer = &HFFFF
'    Public Const PF_FLOATING_POINT_PRECISION_ERRATA As Integer = 0
'    Public Const PF_FLOATING_POINT_EMULATED As Integer = 1
'    Public Const PF_COMPARE_EXCHANGE_DOUBLE As Integer = 2
'    Public Const PF_MMX_INSTRUCTIONS_AVAILABLE As Integer = 3
'    Public Const PAGE_NOACCESS As Integer = &H1
'    Public Const PAGE_READONLY As Integer = &H2
'    Public Const PAGE_READWRITE As Integer = &H4
'    Public Const PAGE_WRITECOPY As Integer = &H8
'    Public Const PAGE_EXECUTE As Integer = &H10
'    Public Const PAGE_EXECUTE_READ As Integer = &H20
'    Public Const PAGE_EXECUTE_READWRITE As Integer = &H40
'    Public Const PAGE_EXECUTE_WRITECOPY As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        PAGE_EXECUTE_READWRITE = 0x40,
'    '        PAGE_EXECUTE_WRITECOPY = unchecked((int)0x80),
'    '----------------------------------^--- GenCode(token): unexpected token type
'    Public Const PAGE_GUARD As Integer = &H100
'    Public Const PAGE_NOCACHE As Integer = &H200
'    Public Const PRIVILEGE_SET_ALL_NECESSARY As Integer = 1
'    Public Const PERF_DATA_VERSION As Integer = 1
'    Public Const PERF_DATA_REVISION As Integer = 1
'    Public Const PERF_NO_INSTANCES As Integer = - 1
'    Public Const PERF_SIZE_DWORD As Integer = &H0
'    Public Const PERF_SIZE_LARGE As Integer = &H100
'    Public Const PERF_SIZE_ZERO As Integer = &H200
'    Public Const PERF_SIZE_VARIABLE_LEN As Integer = &H300
'    Public Const PERF_TYPE_NUMBER As Integer = &H0
'    Public Const PERF_TYPE_COUNTER As Integer = &H400
'    Public Const PERF_TYPE_TEXT As Integer = &H800
'    Public Const PERF_TYPE_ZERO As Integer = &HC00
'    Public Const PERF_NUMBER_HEX As Integer = &H0
'    Public Const PERF_NUMBER_DECIMAL As Integer = &H10000
'    Public Const PERF_NUMBER_DEC_1000 As Integer = &H20000
'    Public Const PERF_COUNTER_VALUE As Integer = &H0
'    Public Const PERF_COUNTER_RATE As Integer = &H10000
'    Public Const PERF_COUNTER_FRACTION As Integer = &H20000
'    Public Const PERF_COUNTER_BASE As Integer = &H30000
'    Public Const PERF_COUNTER_ELAPSED As Integer = &H40000
'    Public Const PERF_COUNTER_QUEUELEN As Integer = &H50000
'    Public Const PERF_COUNTER_HISTOGRAM As Integer = &H60000
'    Public Const PERF_TEXT_UNICODE As Integer = &H0
'    Public Const PERF_TEXT_ASCII As Integer = &H10000
'    Public Const PERF_TIMER_TICK As Integer = &H0
'    Public Const PERF_TIMER_100NS As Integer = &H100000
'    Public Const PERF_OBJECT_TIMER As Integer = &H200000
'    Public Const PERF_DELTA_COUNTER As Integer = &H400000
'    Public Const PERF_DELTA_BASE As Integer = &H800000
'    Public Const PERF_INVERSE_COUNTER As Integer = &H1000000
'    Public Const PERF_MULTI_COUNTER As Integer = &H2000000
'    Public Const PERF_DISPLAY_NO_SUFFIX As Integer = &H0
'    Public Const PERF_DISPLAY_PER_SEC As Integer = &H10000000
'    Public Const PERF_DISPLAY_PERCENT As Integer = &H20000000
'    Public Const PERF_DISPLAY_SECONDS As Integer = &H30000000
'    Public Const PERF_DISPLAY_NOSHOW As Integer = &H40000000
'    Public Const PERF_COUNTER_HISTOGRAM_TYPE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        PERF_DISPLAY_NOSHOW = 0x40000000,
'    '        PERF_COUNTER_HISTOGRAM_TYPE = unchecked((int)0x80000000),
'    '---------------------------------------^--- GenCode(token): unexpected token type
'    Public Const PERF_DETAIL_NOVICE As Integer = 100
'    Public Const PERF_DETAIL_ADVANCED As Integer = 200
'    Public Const PERF_DETAIL_EXPERT As Integer = 300
'    Public Const PERF_DETAIL_WIZARD As Integer = 400
'    Public Const PERF_NO_UNIQUE_ID As Integer = - 1
'    Public Const PROVIDER_KEEPS_VALUE_LENGTH As Integer = &H1
'    Public Const PRINTER_CONTROL_PAUSE As Integer = 1
'    Public Const PRINTER_CONTROL_RESUME As Integer = 2
'    Public Const PRINTER_CONTROL_PURGE As Integer = 3
'    Public Const PRINTER_CONTROL_SET_STATUS As Integer = 4
'    Public Const PRINTER_STATUS_PAUSED As Integer = &H1
'    Public Const PRINTER_STATUS_ERROR As Integer = &H2
'    Public Const PRINTER_STATUS_PENDING_DELETION As Integer = &H4
'    Public Const PRINTER_STATUS_PAPER_JAM As Integer = &H8
'    Public Const PRINTER_STATUS_PAPER_OUT As Integer = &H10
'    Public Const PRINTER_STATUS_MANUAL_FEED As Integer = &H20
'    Public Const PRINTER_STATUS_PAPER_PROBLEM As Integer = &H40
'    Public Const PRINTER_STATUS_OFFLINE As Integer = &H80
'    Public Const PRINTER_STATUS_IO_ACTIVE As Integer = &H100
'    Public Const PRINTER_STATUS_BUSY As Integer = &H200
'    Public Const PRINTER_STATUS_PRINTING As Integer = &H400
'    Public Const PRINTER_STATUS_OUTPUT_BIN_FULL As Integer = &H800
'    Public Const PRINTER_STATUS_NOT_AVAILABLE As Integer = &H1000
'    Public Const PRINTER_STATUS_WAITING As Integer = &H2000
'    Public Const PRINTER_STATUS_PROCESSING As Integer = &H4000
'    Public Const PRINTER_STATUS_INITIALIZING As Integer = &H8000
'    Public Const PRINTER_STATUS_WARMING_UP As Integer = &H10000
'    Public Const PRINTER_STATUS_TONER_LOW As Integer = &H20000
'    Public Const PRINTER_STATUS_NO_TONER As Integer = &H40000
'    Public Const PRINTER_STATUS_PAGE_PUNT As Integer = &H80000
'    Public Const PRINTER_STATUS_USER_INTERVENTION As Integer = &H100000
'    Public Const PRINTER_STATUS_OUT_OF_MEMORY As Integer = &H200000
'    Public Const PRINTER_STATUS_DOOR_OPEN As Integer = &H400000
'    Public Const PRINTER_STATUS_SERVER_UNKNOWN As Integer = &H800000
'    Public Const PRINTER_STATUS_POWER_SAVE As Integer = &H1000000
'    Public Const PRINTER_ATTRIBUTE_QUEUED As Integer = &H1
'    Public Const PRINTER_ATTRIBUTE_DIRECT As Integer = &H2
'    Public Const PRINTER_ATTRIBUTE_DEFAULT As Integer = &H4
'    Public Const PRINTER_ATTRIBUTE_SHARED As Integer = &H8
'    Public Const PRINTER_ATTRIBUTE_NETWORK As Integer = &H10
'    Public Const PRINTER_ATTRIBUTE_HIDDEN As Integer = &H20
'    Public Const PRINTER_ATTRIBUTE_LOCAL As Integer = &H40
'    Public Const PRINTER_ATTRIBUTE_ENABLE_DEVQ As Integer = &H80
'    Public Const PRINTER_ATTRIBUTE_KEEPPRINTEDJOBS As Integer = &H100
'    Public Const PRINTER_ATTRIBUTE_DO_COMPLETE_FIRST As Integer = &H200
'    Public Const PRINTER_ATTRIBUTE_WORK_OFFLINE As Integer = &H400
'    Public Const PRINTER_ATTRIBUTE_ENABLE_BIDI As Integer = &H800
'    Public Const PRINTER_ATTRIBUTE_RAW_ONLY As Integer = &H1000
'    Public Const PORT_TYPE_WRITE As Integer = &H1
'    Public Const PORT_TYPE_READ As Integer = &H2
'    Public Const PORT_TYPE_REDIRECTED As Integer = &H4
'    Public Const PORT_TYPE_NET_ATTACHED As Integer = &H8
'    Public Const PORT_STATUS_TYPE_ERROR As Integer = 1
'    Public Const PORT_STATUS_TYPE_WARNING As Integer = 2
'    Public Const PORT_STATUS_TYPE_INFO As Integer = 3
'    Public Const PORT_STATUS_OFFLINE As Integer = 1
'    Public Const PORT_STATUS_PAPER_JAM As Integer = 2
'    Public Const PORT_STATUS_PAPER_OUT As Integer = 3
'    Public Const PORT_STATUS_OUTPUT_BIN_FULL As Integer = 4
'    Public Const PORT_STATUS_PAPER_PROBLEM As Integer = 5
'    Public Const PORT_STATUS_NO_TONER As Integer = 6
'    Public Const PORT_STATUS_DOOR_OPEN As Integer = 7
'    Public Const PORT_STATUS_USER_INTERVENTION As Integer = 8
'    Public Const PORT_STATUS_OUT_OF_MEMORY As Integer = 9
'    Public Const PORT_STATUS_TONER_LOW As Integer = 10
'    Public Const PORT_STATUS_WARMING_UP As Integer = 11
'    Public Const PORT_STATUS_POWER_SAVE As Integer = 12
'    Public Const PRINTER_ENUM_DEFAULT As Integer = &H1
'    Public Const PRINTER_ENUM_LOCAL As Integer = &H2
'    Public Const PRINTER_ENUM_CONNECTIONS As Integer = &H4
'    Public Const PRINTER_ENUM_FAVORITE As Integer = &H4
'    Public Const PRINTER_ENUM_NAME As Integer = &H8
'    Public Const PRINTER_ENUM_REMOTE As Integer = &H10
'    Public Const PRINTER_ENUM_SHARED As Integer = &H20
'    Public Const PRINTER_ENUM_NETWORK As Integer = &H40
'    Public Const PRINTER_ENUM_EXPAND As Integer = &H4000
'    Public Const PRINTER_ENUM_CONTAINER As Integer = &H8000
'    Public Const PRINTER_ENUM_ICONMASK As Integer = &HFF0000
'    Public Const PRINTER_ENUM_ICON1 As Integer = &H10000
'    Public Const PRINTER_ENUM_ICON2 As Integer = &H20000
'    Public Const PRINTER_ENUM_ICON3 As Integer = &H40000
'    Public Const PRINTER_ENUM_ICON4 As Integer = &H80000
'    Public Const PRINTER_ENUM_ICON5 As Integer = &H100000
'    Public Const PRINTER_ENUM_ICON6 As Integer = &H200000
'    Public Const PRINTER_ENUM_ICON7 As Integer = &H400000
'    Public Const PRINTER_ENUM_ICON8 As Integer = &H800000
'    Public Const PRINTER_NOTIFY_TYPE As Integer = &H0
'    Public Const PRINTER_NOTIFY_FIELD_SERVER_NAME As Integer = &H0
'    Public Const PRINTER_NOTIFY_FIELD_PRINTER_NAME As Integer = &H1
'    Public Const PRINTER_NOTIFY_FIELD_SHARE_NAME As Integer = &H2
'    Public Const PRINTER_NOTIFY_FIELD_PORT_NAME As Integer = &H3
'    Public Const PRINTER_NOTIFY_FIELD_DRIVER_NAME As Integer = &H4
'    Public Const PRINTER_NOTIFY_FIELD_COMMENT As Integer = &H5
'    Public Const PRINTER_NOTIFY_FIELD_LOCATION As Integer = &H6
'    Public Const PRINTER_NOTIFY_FIELD_DEVMODE As Integer = &H7
'    Public Const PRINTER_NOTIFY_FIELD_SEPFILE As Integer = &H8
'    Public Const PRINTER_NOTIFY_FIELD_PRINT_PROCESSOR As Integer = &H9
'    Public Const PRINTER_NOTIFY_FIELD_PARAMETERS As Integer = &HA
'    Public Const PRINTER_NOTIFY_FIELD_DATATYPE As Integer = &HB
'    Public Const PRINTER_NOTIFY_FIELD_SECURITY_DESCRIPTOR As Integer = &HC
'    Public Const PRINTER_NOTIFY_FIELD_ATTRIBUTES As Integer = &HD
'    Public Const PRINTER_NOTIFY_FIELD_PRIORITY As Integer = &HE
'    Public Const PRINTER_NOTIFY_FIELD_DEFAULT_PRIORITY As Integer = &HF
'    Public Const PRINTER_NOTIFY_FIELD_START_TIME As Integer = &H10
'    Public Const PRINTER_NOTIFY_FIELD_UNTIL_TIME As Integer = &H11
'    Public Const PRINTER_NOTIFY_FIELD_STATUS As Integer = &H12
'    Public Const PRINTER_NOTIFY_FIELD_STATUS_STRING As Integer = &H13
'    Public Const PRINTER_NOTIFY_FIELD_CJOBS As Integer = &H14
'    Public Const PRINTER_NOTIFY_FIELD_AVERAGE_PPM As Integer = &H15
'    Public Const PRINTER_NOTIFY_FIELD_TOTAL_PAGES As Integer = &H16
'    Public Const PRINTER_NOTIFY_FIELD_PAGES_PRINTED As Integer = &H17
'    Public Const PRINTER_NOTIFY_FIELD_TOTAL_BYTES As Integer = &H18
'    Public Const PRINTER_NOTIFY_FIELD_BYTES_PRINTED As Integer = &H19
'    Public Const PRINTER_NOTIFY_OPTIONS_REFRESH As Integer = &H1
'    Public Const PRINTER_NOTIFY_INFO_DISCARDED As Integer = &H1
'    Public Const PRINTER_CHANGE_ADD_PRINTER As Integer = &H1
'    Public Const PRINTER_CHANGE_SET_PRINTER As Integer = &H2
'    Public Const PRINTER_CHANGE_DELETE_PRINTER As Integer = &H4
'    Public Const PRINTER_CHANGE_FAILED_CONNECTION_PRINTER As Integer = &H8
'    Public Const PRINTER_CHANGE_PRINTER As Integer = &HFF
'    Public Const PRINTER_CHANGE_ADD_JOB As Integer = &H100
'    Public Const PRINTER_CHANGE_SET_JOB As Integer = &H200
'    Public Const PRINTER_CHANGE_DELETE_JOB As Integer = &H400
'    Public Const PRINTER_CHANGE_WRITE_JOB As Integer = &H800
'    Public Const PRINTER_CHANGE_JOB As Integer = &HFF00
'    Public Const PRINTER_CHANGE_ADD_FORM As Integer = &H10000
'    Public Const PRINTER_CHANGE_SET_FORM As Integer = &H20000
'    Public Const PRINTER_CHANGE_DELETE_FORM As Integer = &H40000
'    Public Const PRINTER_CHANGE_FORM As Integer = &H70000
'    Public Const PRINTER_CHANGE_ADD_PORT As Integer = &H100000
'    Public Const PRINTER_CHANGE_CONFIGURE_PORT As Integer = &H200000
'    Public Const PRINTER_CHANGE_DELETE_PORT As Integer = &H400000
'    Public Const PRINTER_CHANGE_PORT As Integer = &H700000
'    Public Const PRINTER_CHANGE_ADD_PRINT_PROCESSOR As Integer = &H1000000
'    Public Const PRINTER_CHANGE_DELETE_PRINT_PROCESSOR As Integer = &H4000000
'    Public Const PRINTER_CHANGE_PRINT_PROCESSOR As Integer = &H7000000
'    Public Const PRINTER_CHANGE_ADD_PRINTER_DRIVER As Integer = &H10000000
'    Public Const PRINTER_CHANGE_SET_PRINTER_DRIVER As Integer = &H20000000
'    Public Const PRINTER_CHANGE_DELETE_PRINTER_DRIVER As Integer = &H40000000
'    Public Const PRINTER_CHANGE_PRINTER_DRIVER As Integer = &H70000000
'    Public Const PRINTER_CHANGE_TIMEOUT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        PRINTER_CHANGE_PRINTER_DRIVER = 0x70000000,
'    '        PRINTER_CHANGE_TIMEOUT = unchecked((int)0x80000000),
'    '----------------------------------^--- GenCode(token): unexpected token type
'    Public Const PRINTER_CHANGE_ALL As Integer = &H7777FFFF
'    Public Const PRINTER_ERROR_INFORMATION As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        PRINTER_CHANGE_ALL = 0x7777FFFF,
'    '        PRINTER_ERROR_INFORMATION = unchecked((int)0x80000000),
'    '-------------------------------------^--- GenCode(token): unexpected token type
'    Public Const PRINTER_ERROR_WARNING As Integer = &H40000000
'    Public Const PRINTER_ERROR_SEVERE As Integer = &H20000000
'    Public Const PRINTER_ERROR_OUTOFPAPER As Integer = &H1
'    Public Const PRINTER_ERROR_JAM As Integer = &H2
'    Public Const PRINTER_ERROR_OUTOFTONER As Integer = &H4
'    Public Const PRINTER_ACCESS_ADMINISTER As Integer = &H4
'    Public Const PRINTER_ACCESS_USE As Integer = &H8
'    Public Const PWR_OK As Integer = 1
'    Public Const PWR_FAIL As Integer = - 1
'    Public Const PWR_SUSPENDREQUEST As Integer = 1
'    Public Const PWR_SUSPENDRESUME As Integer = 2
'    Public Const PWR_CRITICALRESUME As Integer = 3
'    Public Const PRF_CHECKVISIBLE As Integer = &H1
'    Public Const PRF_NONCLIENT As Integer = &H2
'    Public Const PRF_CLIENT As Integer = &H4
'    Public Const PRF_ERASEBKGND As Integer = &H8
'    Public Const PRF_CHILDREN As Integer = &H10
'    Public Const PRF_OWNED As Integer = &H20
'    Public Const PM_NOREMOVE As Integer = &H0
'    Public Const PM_REMOVE As Integer = &H1
'    Public Const PM_NOYIELD As Integer = &H2
'    Public Const PSM_PAGEINFO As Integer = &H400 + 100
'    Public Const PSM_SHEETINFO As Integer = &H400 + 101
'    Public Const PSI_SETACTIVE As Integer = &H1
'    Public Const PSI_KILLACTIVE As Integer = &H2
'    Public Const PSI_APPLY As Integer = &H3
'    Public Const PSI_RESET As Integer = &H4
'    Public Const PSI_HASHELP As Integer = &H5
'    Public Const PSI_HELP As Integer = &H6
'    Public Const PSI_CHANGED As Integer = &H1
'    Public Const PSI_GUISTART As Integer = &H2
'    Public Const PSI_REBOOT As Integer = &H3
'    Public Const PSI_GETSIBLINGS As Integer = &H4
'    Public Const PBS_SMOOTH As Integer = &H1
'    Public Const PBS_VERTICAL As Integer = &H4
'    Public Const PBM_SETRANGE As Integer = &H400 + 1
'    Public Const PBM_SETPOS As Integer = &H400 + 2
'    Public Const PBM_DELTAPOS As Integer = &H400 + 3
'    Public Const PBM_SETSTEP As Integer = &H400 + 4
'    Public Const PBM_STEPIT As Integer = &H400 + 5
'    Public Const PBM_SETRANGE32 As Integer = &H400 + 6
'    Public Const PBM_GETRANGE As Integer = &H400 + 7
'    Public Const PBM_GETPOS As Integer = &H400 + 8
'    Public Const PSM_SETCURSEL As Integer = &H400 + 101
'    Public Const PSM_REMOVEPAGE As Integer = &H400 + 102
'    Public Const PSM_ADDPAGE As Integer = &H400 + 103
'    Public Const PSM_CHANGED As Integer = &H400 + 104
'    Public Const PSM_RESTARTWINDOWS As Integer = &H400 + 105
'    Public Const PSM_REBOOTSYSTEM As Integer = &H400 + 106
'    Public Const PSM_CANCELTOCLOSE As Integer = &H400 + 107
'    Public Const PSM_QUERYSIBLINGS As Integer = &H400 + 108
'    Public Const PSM_UNCHANGED As Integer = &H400 + 109
'    Public Const PSM_APPLY As Integer = &H400 + 110
'    Public Const PSM_SETTITLEA As Integer = &H400 + 111
'    Public Const PSM_SETTITLEW As Integer = &H400 + 120
'    Public Const PSM_SETWIZBUTTONS As Integer = &H400 + 112
'    Public Const PSM_PRESSBUTTON As Integer = &H400 + 113
'    Public Const PSM_SETCURSELID As Integer = &H400 + 114
'    Public Const PSM_SETFINISHTEXTA As Integer = &H400 + 115
'    Public Const PSM_SETFINISHTEXTW As Integer = &H400 + 121
'    Public Const PSM_GETTABCONTROL As Integer = &H400 + 116
'    Public Const PSM_ISDIALOGMESSAGE As Integer = &H400 + 117
'    Public Const PSM_GETCURRENTPAGEHWND As Integer = &H400 + 118
'    Public Const PATCOPY As Integer = &HF00021
'    Public Const PATPAINT As Integer = &HFB0A09
'    Public Const PATINVERT As Integer = &H5A0049
'    Public Const PGN_FIRST As Integer = 0 - 900
'    Public Const PGN_LAST As Integer = 0 - 950
    
    
    
'    Public Const QID_SYNC As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '
'    '        public const int QID_SYNC = unchecked((int)0xFFFFFFFF),
'    '-------------------------------------^--- GenCode(token): unexpected token type
'    Public Const QUERYROPSUPPORT As Integer = 40
'    Public Const QUERYESCSUPPORT As Integer = 8
'    Public Const QUERYDIBSUPPORT As Integer = 3073
'    Public Const QDI_SETDIBITS As Integer = 1
'    Public Const QDI_GETDIBITS As Integer = 2
'    Public Const QDI_DIBTOSCREEN As Integer = 4
'    Public Const QDI_STRETCHDIB As Integer = 8
        Public Const QS_KEY As Integer = &H1
        Public Const QS_MOUSEMOVE As Integer = &H2
        Public Const QS_MOUSEBUTTON As Integer = &H4
        Public Const QS_POSTMESSAGE As Integer = &H8
        Public Const QS_TIMER As Integer = &H10
        Public Const QS_PAINT As Integer = &H20
        Public Const QS_SENDMESSAGE As Integer = &H40
        Public Const QS_HOTKEY As Integer = &H80
        Public Const QS_ALLPOSTMESSAGE As Integer = &H100
        Public Const QS_MOUSE As Integer = QS_MOUSEMOVE Or QS_MOUSEBUTTON
        Public Const QS_INPUT As Integer = QS_MOUSE Or QS_KEY
        Public Const QS_ALLEVENTS As Integer = QS_INPUT Or QS_POSTMESSAGE Or QS_TIMER Or QS_PAINT Or QS_HOTKEY
        Public Const QS_ALLINPUT As Integer = QS_INPUT Or QS_POSTMESSAGE Or QS_TIMER Or QS_PAINT Or QS_HOTKEY Or QS_SENDMESSAGE
    
'    Public Const REGULAR_FONTTYPE As Integer = &H400
'    Public Const rad1 As Integer = &H420
'    Public Const rad2 As Integer = &H421
'    Public Const rad3 As Integer = &H422
'    Public Const rad4 As Integer = &H423
'    Public Const rad5 As Integer = &H424
'    Public Const rad6 As Integer = &H425
'    Public Const rad7 As Integer = &H426
'    Public Const rad8 As Integer = &H427
'    Public Const rad9 As Integer = &H428
'    Public Const rad10 As Integer = &H429
'    Public Const rad11 As Integer = &H42A
'    Public Const rad12 As Integer = &H42B
'    Public Const rad13 As Integer = &H42C
'    Public Const rad14 As Integer = &H42D
'    Public Const rad15 As Integer = &H42E
'    Public Const rad16 As Integer = &H42F
'    Public Const rct1 As Integer = &H438
'    Public Const rct2 As Integer = &H439
'    Public Const rct3 As Integer = &H43A
'    Public Const rct4 As Integer = &H43B
'    Public Const REPLACEDLGORD As Integer = 1541
'    Public Const REGISTERING As Integer = &H0
'    Public Const REGISTERED As Integer = &H4
'    Public Const RPC_C_BINDING_INFINITE_TIMEOUT As Integer = 10
'    Public Const RPC_C_BINDING_MIN_TIMEOUT As Integer = 0
'    Public Const RPC_C_BINDING_DEFAULT_TIMEOUT As Integer = 5
'    Public Const RPC_C_BINDING_MAX_TIMEOUT As Integer = 9
'    Public Const RPC_C_CANCEL_INFINITE_TIMEOUT As Integer = - 1
'    Public Const RPC_C_LISTEN_MAX_CALLS_DEFAULT As Integer = 1234
'    Public Const RPC_C_PROTSEQ_MAX_REQS_DEFAULT As Integer = 10
'    Public Const RPC_C_BIND_TO_ALL_NICS As Integer = 1
'    Public Const RPC_C_USE_INTERNET_PORT As Integer = 1
'    Public Const RPC_C_USE_INTRANET_PORT As Integer = 2
'    Public Const RPC_C_STATS_CALLS_IN As Integer = 0
'    Public Const RPC_C_STATS_CALLS_OUT As Integer = 1
'    Public Const RPC_C_STATS_PKTS_IN As Integer = 2
'    Public Const RPC_C_STATS_PKTS_OUT As Integer = 3
'    Public Const RPC_C_AUTHN_LEVEL_DEFAULT As Integer = 0
'    Public Const RPC_C_AUTHN_LEVEL_NONE As Integer = 1
'    Public Const RPC_C_AUTHN_LEVEL_CONNECT As Integer = 2
'    Public Const RPC_C_AUTHN_LEVEL_CALL As Integer = 3
'    Public Const RPC_C_AUTHN_LEVEL_PKT As Integer = 4
'    Public Const RPC_C_AUTHN_LEVEL_PKT_INTEGRITY As Integer = 5
'    Public Const RPC_C_AUTHN_LEVEL_PKT_PRIVACY As Integer = 6
'    Public Const RPC_C_IMP_LEVEL_ANONYMOUS As Integer = 1
'    Public Const RPC_C_IMP_LEVEL_IDENTIFY As Integer = 2
'    Public Const RPC_C_IMP_LEVEL_IMPERSONATE As Integer = 3
'    Public Const RPC_C_IMP_LEVEL_DELEGATE As Integer = 4
'    Public Const RPC_C_QOS_IDENTITY_STATIC As Integer = 0
'    Public Const RPC_C_QOS_IDENTITY_DYNAMIC As Integer = 1
'    Public Const RPC_C_QOS_CAPABILITIES_DEFAULT As Integer = 0
'    Public Const RPC_C_QOS_CAPABILITIES_MUTUAL_AUTH As Integer = 1
'    Public Const RPC_C_PROTECT_LEVEL_DEFAULT As Integer = 0
'    Public Const RPC_C_PROTECT_LEVEL_NONE As Integer = 1
'    Public Const RPC_C_PROTECT_LEVEL_CONNECT As Integer = 2
'    Public Const RPC_C_PROTECT_LEVEL_CALL As Integer = 3
'    Public Const RPC_C_PROTECT_LEVEL_PKT As Integer = 4
'    Public Const RPC_C_PROTECT_LEVEL_PKT_INTEGRITY As Integer = 5
'    Public Const RPC_C_PROTECT_LEVEL_PKT_PRIVACY As Integer = 6
'    Public Const RPC_C_AUTHN_NONE As Integer = 0
'    Public Const RPC_C_AUTHN_DCE_PRIVATE As Integer = 1
'    Public Const RPC_C_AUTHN_DCE_PUBLIC As Integer = 2
'    Public Const RPC_C_AUTHN_DEC_PUBLIC As Integer = 4
'    Public Const RPC_C_AUTHN_WINNT As Integer = 10
'    Public Const RPC_C_AUTHN_DEFAULT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_C_AUTHN_WINNT = 10,
'    '        RPC_C_AUTHN_DEFAULT = unchecked((int)0xFFFFFFFF),
'    '-------------------------------^--- GenCode(token): unexpected token type
'    Public Const RPC_C_SECURITY_QOS_VERSION As Integer = 1
'    Public Const RPC_C_AUTHZ_NONE As Integer = 0
'    Public Const RPC_C_AUTHZ_NAME As Integer = 1
'    Public Const RPC_C_AUTHZ_DCE As Integer = 2
'    Public Const RPC_C_EP_ALL_ELTS As Integer = 0
'    Public Const RPC_C_EP_MATCH_BY_IF As Integer = 1
'    Public Const RPC_C_EP_MATCH_BY_OBJ As Integer = 2
'    Public Const RPC_C_EP_MATCH_BY_BOTH As Integer = 3
'    Public Const RPC_C_VERS_ALL As Integer = 1
'    Public Const RPC_C_VERS_COMPATIBLE As Integer = 2
'    Public Const RPC_C_VERS_EXACT As Integer = 3
'    Public Const RPC_C_VERS_MAJOR_ONLY As Integer = 4
'    Public Const RPC_C_VERS_UPTO As Integer = 5
'    Public Const RPC_C_MGMT_INQ_IF_IDS As Integer = 0
'    Public Const RPC_C_MGMT_INQ_PRINC_NAME As Integer = 1
'    Public Const RPC_C_MGMT_INQ_STATS As Integer = 2
'    Public Const RPC_C_MGMT_IS_SERVER_LISTEN As Integer = 3
'    Public Const RPC_C_MGMT_STOP_SERVER_LISTEN As Integer = 4
'    Public Const RPC_C_PARM_MAX_PACKET_LENGTH As Integer = 1
'    Public Const RPC_C_PARM_BUFFER_LENGTH As Integer = 2
'    Public Const RPC_IF_AUTOLISTEN As Integer = &H1
'    Public Const RPC_IF_OLE As Integer = &H2
'    Public Const RPC_NCA_FLAGS_DEFAULT As Integer = &H0
'    Public Const RPC_NCA_FLAGS_IDEMPOTENT As Integer = &H1
'    Public Const RPC_NCA_FLAGS_BROADCAST As Integer = &H2
'    Public Const RPC_NCA_FLAGS_MAYBE As Integer = &H4
'    Public Const RPC_BUFFER_COMPLETE As Integer = &H1000
'    Public Const RPC_BUFFER_PARTIAL As Integer = &H2000
'    Public Const RPC_BUFFER_EXTRA As Integer = &H4000
'    Public Const RPCFLG_NON_NDR As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_BUFFER_EXTRA = 0x00004000,
'    '        RPCFLG_NON_NDR = unchecked((int)0x80000000),
'    '--------------------------^--- GenCode(token): unexpected token type
'    Public Const RPCFLG_ASYNCHRONOUS As Integer = &H40000000
'    Public Const RPCFLG_INPUT_SYNCHRONOUS As Integer = &H20000000
'    Public Const RPCFLG_LOCAL_CALL As Integer = &H10000000
'    Public Const RPC_FLAGS_VALID_BIT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPCFLG_LOCAL_CALL = 0x10000000,
'    '        RPC_FLAGS_VALID_BIT = unchecked((int)0x8000);
'    '-------------------------------^--- GenCode(token): unexpected token type
'    ' RPC_FLAGS_VALID_BIT = 0x00008000;
'    Public Const RPC_INTERFACE_HAS_PIPES As Integer = &H1
'    Public Const RPC_C_NS_SYNTAX_DEFAULT As Integer = 0
'    Public Const RPC_C_NS_SYNTAX_DCE As Integer = 3
'    Public Const RPC_C_PROFILE_DEFAULT_ELT As Integer = 0
'    Public Const RPC_C_PROFILE_ALL_ELT As Integer = 1
'    Public Const RPC_C_PROFILE_MATCH_BY_IF As Integer = 2
'    Public Const RPC_C_PROFILE_MATCH_BY_MBR As Integer = 3
'    Public Const RPC_C_PROFILE_MATCH_BY_BOTH As Integer = 4
'    Public Const RPC_C_NS_DEFAULT_EXP_AGE As Integer = - 1
'    Public Const RTS_CONTROL_DISABLE As Integer = &H0
'    Public Const RTS_CONTROL_ENABLE As Integer = &H1
'    Public Const RTS_CONTROL_HANDSHAKE As Integer = &H2
'    Public Const RTS_CONTROL_TOGGLE As Integer = &H3
'    Public Const REALTIME_PRIORITY_Class As Integer = &H100
'    Public Const RIP_EVENT As Integer = 9
'    Public Const RESETDEV As Integer = 7
'    Public Const RIGHT_ALT_PRESSED As Integer = &H1
'    Public Const RIGHT_CTRL_PRESSED As Integer = &H4
'    Public Const RIGHTMOST_BUTTON_PRESSED As Integer = &H2
'    Public Const RPC_S_INVALID_STRING_BINDING As Integer = 1700
'    Public Const RPC_S_WRONG_KIND_OF_BINDING As Integer = 1701
'    Public Const RPC_S_INVALID_BINDING As Integer = 1702
'    Public Const RPC_S_PROTSEQ_NOT_SUPPORTED As Integer = 1703
'    Public Const RPC_S_INVALID_RPC_PROTSEQ As Integer = 1704
'    Public Const RPC_S_INVALID_STRING_UUID As Integer = 1705
'    Public Const RPC_S_INVALID_ENDPOINT_FORMAT As Integer = 1706
'    Public Const RPC_S_INVALID_NET_ADDR As Integer = 1707
'    Public Const RPC_S_NO_ENDPOINT_FOUND As Integer = 1708
'    Public Const RPC_S_INVALID_TIMEOUT As Integer = 1709
'    Public Const RPC_S_OBJECT_NOT_FOUND As Integer = 1710
'    Public Const RPC_S_ALREADY_REGISTERED As Integer = 1711
'    Public Const RPC_S_TYPE_ALREADY_REGISTERED As Integer = 1712
'    Public Const RPC_S_ALREADY_LISTENING As Integer = 1713
'    Public Const RPC_S_NO_PROTSEQS_REGISTERED As Integer = 1714
'    Public Const RPC_S_NOT_LISTENING As Integer = 1715
'    Public Const RPC_S_UNKNOWN_MGR_TYPE As Integer = 1716
'    Public Const RPC_S_UNKNOWN_IF As Integer = 1717
'    Public Const RPC_S_NO_BINDINGS As Integer = 1718
'    Public Const RPC_S_NO_PROTSEQS As Integer = 1719
'    Public Const RPC_S_CANT_CREATE_ENDPOINT As Integer = 1720
'    Public Const RPC_S_OUT_OF_RESOURCES As Integer = 1721
'    Public Const RPC_S_SERVER_UNAVAILABLE As Integer = 1722
'    Public Const RPC_S_SERVER_TOO_BUSY As Integer = 1723
'    Public Const RPC_S_INVALID_NETWORK_OPTIONS As Integer = 1724
'    Public Const RPC_S_NO_CALL_ACTIVE As Integer = 1725
'    Public Const RPC_S_CALL_FAILED As Integer = 1726
'    Public Const RPC_S_CALL_FAILED_DNE As Integer = 1727
'    Public Const RPC_S_PROTOCOL_ERROR As Integer = 1728
'    Public Const RPC_S_UNSUPPORTED_TRANS_SYN As Integer = 1730
'    Public Const RPC_S_UNSUPPORTED_TYPE As Integer = 1732
'    Public Const RPC_S_INVALID_TAG As Integer = 1733
'    Public Const RPC_S_INVALID_BOUND As Integer = 1734
'    Public Const RPC_S_NO_ENTRY_NAME As Integer = 1735
'    Public Const RPC_S_INVALID_NAME_SYNTAX As Integer = 1736
'    Public Const RPC_S_UNSUPPORTED_NAME_SYNTAX As Integer = 1737
'    Public Const RPC_S_UUID_NO_ADDRESS As Integer = 1739
'    Public Const RPC_S_DUPLICATE_ENDPOINT As Integer = 1740
'    Public Const RPC_S_UNKNOWN_AUTHN_TYPE As Integer = 1741
'    Public Const RPC_S_MAX_CALLS_TOO_SMALL As Integer = 1742
'    Public Const RPC_S_STRING_TOO_LONG As Integer = 1743
'    Public Const RPC_S_PROTSEQ_NOT_FOUND As Integer = 1744
'    Public Const RPC_S_PROCNUM_OUT_OF_RANGE As Integer = 1745
'    Public Const RPC_S_BINDING_HAS_NO_AUTH As Integer = 1746
'    Public Const RPC_S_UNKNOWN_AUTHN_SERVICE As Integer = 1747
'    Public Const RPC_S_UNKNOWN_AUTHN_LEVEL As Integer = 1748
'    Public Const RPC_S_INVALID_AUTH_IDENTITY As Integer = 1749
'    Public Const RPC_S_UNKNOWN_AUTHZ_SERVICE As Integer = 1750
'    Public Const RPC_S_NOTHING_TO_EXPORT As Integer = 1754
'    Public Const RPC_S_INCOMPLETE_NAME As Integer = 1755
'    Public Const RPC_S_INVALID_VERS_OPTION As Integer = 1756
'    Public Const RPC_S_NO_MORE_MEMBERS As Integer = 1757
'    Public Const RPC_S_NOT_ALL_OBJS_UNEXPORTED As Integer = 1758
'    Public Const RPC_S_INTERFACE_NOT_FOUND As Integer = 1759
'    Public Const RPC_S_ENTRY_ALREADY_EXISTS As Integer = 1760
'    Public Const RPC_S_ENTRY_NOT_FOUND As Integer = 1761
'    Public Const RPC_S_NAME_SERVICE_UNAVAILABLE As Integer = 1762
'    Public Const RPC_S_INVALID_NAF_ID As Integer = 1763
'    Public Const RPC_S_CANNOT_SUPPORT As Integer = 1764
'    Public Const RPC_S_NO_CONTEXT_AVAILABLE As Integer = 1765
'    Public Const RPC_S_INTERNAL_ERROR As Integer = 1766
'    Public Const RPC_S_ZERO_DIVIDE As Integer = 1767
'    Public Const RPC_S_ADDRESS_ERROR As Integer = 1768
'    Public Const RPC_S_FP_DIV_ZERO As Integer = 1769
'    Public Const RPC_S_FP_UNDERFLOW As Integer = 1770
'    Public Const RPC_S_FP_OVERFLOW As Integer = 1771
'    Public Const RPC_X_NO_MORE_ENTRIES As Integer = 1772
'    Public Const RPC_X_SS_CHAR_TRANS_OPEN_FAIL As Integer = 1773
'    Public Const RPC_X_SS_CHAR_TRANS_SHORT_FILE As Integer = 1774
'    Public Const RPC_X_SS_IN_NULL_CONTEXT As Integer = 1775
'    Public Const RPC_X_SS_CONTEXT_DAMAGED As Integer = 1777
'    Public Const RPC_X_SS_HANDLES_MISMATCH As Integer = 1778
'    Public Const RPC_X_SS_CANNOT_GET_CALL_HANDLE As Integer = 1779
'    Public Const RPC_X_NULL_REF_POINTER As Integer = 1780
'    Public Const RPC_X_ENUM_VALUE_OUT_OF_RANGE As Integer = 1781
'    Public Const RPC_X_BYTE_COUNT_TOO_SMALL As Integer = 1782
'    Public Const RPC_X_BAD_STUB_DATA As Integer = 1783
'    Public Const RPC_S_CALL_IN_PROGRESS As Integer = 1791
'    Public Const RPC_S_NO_MORE_BINDINGS As Integer = 1806
'    Public Const RPC_S_NO_INTERFACES As Integer = 1817
'    Public Const RPC_S_CALL_CANCELLED As Integer = 1818
'    Public Const RPC_S_BINDING_INCOMPLETE As Integer = 1819
'    Public Const RPC_S_COMM_FAILURE As Integer = 1820
'    Public Const RPC_S_UNSUPPORTED_AUTHN_LEVEL As Integer = 1821
'    Public Const RPC_S_NO_PRINC_NAME As Integer = 1822
'    Public Const RPC_S_NOT_RPC_ERROR As Integer = 1823
'    Public Const RPC_S_UUID_LOCAL_ONLY As Integer = 1824
'    Public Const RPC_S_SEC_PKG_ERROR As Integer = 1825
'    Public Const RPC_S_NOT_CANCELLED As Integer = 1826
'    Public Const RPC_X_INVALID_ES_ACTION As Integer = 1827
'    Public Const RPC_X_WRONG_ES_VERSION As Integer = 1828
'    Public Const RPC_X_WRONG_STUB_VERSION As Integer = 1829
'    Public Const RPC_X_INVALID_PIPE_OBJECT As Integer = 1830
'    Public Const RPC_X_INVALID_PIPE_OPERATION As Integer = 1831
'    Public Const RPC_X_WRONG_PIPE_VERSION As Integer = 1832
'    Public Const RPC_S_GROUP_MEMBER_NOT_FOUND As Integer = 1898
'    Public Const RPC_S_INVALID_OBJECT As Integer = 1900
'    Public Const RPC_S_SEND_INCOMPLETE As Integer = 1913
'    Public Const REGDB_E_FIRST As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_S_SEND_INCOMPLETE = 1913,
'    '        REGDB_E_FIRST = unchecked((int)0x80040150),
'    '-------------------------^--- GenCode(token): unexpected token type
'    Public Const REGDB_E_LAST As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        REGDB_E_FIRST = unchecked((int)0x80040150),
'    '        REGDB_E_LAST = unchecked((int)0x8004015F),
'    '------------------------^--- GenCode(token): unexpected token type
'    Public Const REGDB_S_FIRST As Integer = &H40150
'    Public Const REGDB_S_LAST As Integer = &H4015F
'    Public Const REGDB_E_READREGDB As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        REGDB_S_LAST = 0x0004015F,
'    '        REGDB_E_READREGDB = unchecked((int)0x80040150),
'    '-----------------------------^--- GenCode(token): unexpected token type
'    Public Const REGDB_E_WRITEREGDB As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        REGDB_E_READREGDB = unchecked((int)0x80040150),
'    '        REGDB_E_WRITEREGDB = unchecked((int)0x80040151),
'    '------------------------------^--- GenCode(token): unexpected token type
'    Public Const REGDB_E_KEYMISSING As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        REGDB_E_WRITEREGDB = unchecked((int)0x80040151),
'    '        REGDB_E_KEYMISSING = unchecked((int)0x80040152),
'    '------------------------------^--- GenCode(token): unexpected token type
'    Public Const REGDB_E_INVALIDVALUE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        REGDB_E_KEYMISSING = unchecked((int)0x80040152),
'    '        REGDB_E_INVALIDVALUE = unchecked((int)0x80040153),
'    '--------------------------------^--- GenCode(token): unexpected token type
'    Public Const REGDB_E_CLASSNOTREG As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        REGDB_E_INVALIDVALUE = unchecked((int)0x80040153),
'    '        REGDB_E_CLASSNOTREG = unchecked((int)0x80040154),
'    '-------------------------------^--- GenCode(token): unexpected token type
'    Public Const REGDB_E_IIDNOTREG As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        REGDB_E_CLASSNOTREG = unchecked((int)0x80040154),
'    '        REGDB_E_IIDNOTREG = unchecked((int)0x80040155),
'    '-----------------------------^--- GenCode(token): unexpected token type
'    Public Const RPC_E_CALL_REJECTED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        REGDB_E_IIDNOTREG = unchecked((int)0x80040155),
'    '        RPC_E_CALL_REJECTED = unchecked((int)0x80010001),
'    '-------------------------------^--- GenCode(token): unexpected token type
'    Public Const RPC_E_CALL_CANCELED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_CALL_REJECTED = unchecked((int)0x80010001),
'    '        RPC_E_CALL_CANCELED = unchecked((int)0x80010002),
'    '-------------------------------^--- GenCode(token): unexpected token type
'    Public Const RPC_E_CANTPOST_INSENDCALL As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_CALL_CANCELED = unchecked((int)0x80010002),
'    '        RPC_E_CANTPOST_INSENDCALL = unchecked((int)0x80010003),
'    '-------------------------------------^--- GenCode(token): unexpected token type
'    Public Const RPC_E_CANTCALLOUT_INASYNCCALL As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_CANTPOST_INSENDCALL = unchecked((int)0x80010003),
'    '        RPC_E_CANTCALLOUT_INASYNCCALL = unchecked((int)0x80010004),
'    '-----------------------------------------^--- GenCode(token): unexpected token type
'    Public Const RPC_E_CONNECTION_TERMINATED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_CANTCALLOUT_INASYNCCALL = unchecked((int)0x80010004),
'    '        RPC_E_CONNECTION_TERMINATED = unchecked((int)0x80010006),
'    '---------------------------------------^--- GenCode(token): unexpected token type
'    Public Const RPC_E_SERVER_DIED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_CONNECTION_TERMINATED = unchecked((int)0x80010006),
'    '        RPC_E_SERVER_DIED = unchecked((int)0x80010007),
'    '-----------------------------^--- GenCode(token): unexpected token type
'    Public Const RPC_E_CLIENT_DIED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_SERVER_DIED = unchecked((int)0x80010007),
'    '        RPC_E_CLIENT_DIED = unchecked((int)0x80010008),
'    '-----------------------------^--- GenCode(token): unexpected token type
'    Public Const RPC_E_INVALID_DATAPACKET As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_CLIENT_DIED = unchecked((int)0x80010008),
'    '        RPC_E_INVALID_DATAPACKET = unchecked((int)0x80010009),
'    '------------------------------------^--- GenCode(token): unexpected token type
'    Public Const RPC_E_CANTTRANSMIT_CALL As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_INVALID_DATAPACKET = unchecked((int)0x80010009),
'    '        RPC_E_CANTTRANSMIT_CALL = unchecked((int)0x8001000A),
'    '-----------------------------------^--- GenCode(token): unexpected token type
'    Public Const RPC_E_CLIENT_CANTMARSHAL_DATA As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_CANTTRANSMIT_CALL = unchecked((int)0x8001000A),
'    '        RPC_E_CLIENT_CANTMARSHAL_DATA = unchecked((int)0x8001000B),
'    '-----------------------------------------^--- GenCode(token): unexpected token type
'    Public Const RPC_E_CLIENT_CANTUNMARSHAL_DATA As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_CLIENT_CANTMARSHAL_DATA = unchecked((int)0x8001000B),
'    '        RPC_E_CLIENT_CANTUNMARSHAL_DATA = unchecked((int)0x8001000C),
'    '-------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const RPC_E_SERVER_CANTMARSHAL_DATA As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_CLIENT_CANTUNMARSHAL_DATA = unchecked((int)0x8001000C),
'    '        RPC_E_SERVER_CANTMARSHAL_DATA = unchecked((int)0x8001000D),
'    '-----------------------------------------^--- GenCode(token): unexpected token type
'    Public Const RPC_E_SERVER_CANTUNMARSHAL_DATA As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_SERVER_CANTMARSHAL_DATA = unchecked((int)0x8001000D),
'    '        RPC_E_SERVER_CANTUNMARSHAL_DATA = unchecked((int)0x8001000E),
'    '-------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const RPC_E_INVALID_DATA As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_SERVER_CANTUNMARSHAL_DATA = unchecked((int)0x8001000E),
'    '        RPC_E_INVALID_DATA = unchecked((int)0x8001000F),
'    '------------------------------^--- GenCode(token): unexpected token type
'    Public Const RPC_E_INVALID_PARAMETER As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_INVALID_DATA = unchecked((int)0x8001000F),
'    '        RPC_E_INVALID_PARAMETER = unchecked((int)0x80010010),
'    '-----------------------------------^--- GenCode(token): unexpected token type
'    Public Const RPC_E_CANTCALLOUT_AGAIN As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_INVALID_PARAMETER = unchecked((int)0x80010010),
'    '        RPC_E_CANTCALLOUT_AGAIN = unchecked((int)0x80010011),
'    '-----------------------------------^--- GenCode(token): unexpected token type
'    Public Const RPC_E_SERVER_DIED_DNE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_CANTCALLOUT_AGAIN = unchecked((int)0x80010011),
'    '        RPC_E_SERVER_DIED_DNE = unchecked((int)0x80010012),
'    '---------------------------------^--- GenCode(token): unexpected token type
'    Public Const RPC_E_SYS_CALL_FAILED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_SERVER_DIED_DNE = unchecked((int)0x80010012),
'    '        RPC_E_SYS_CALL_FAILED = unchecked((int)0x80010100),
'    '---------------------------------^--- GenCode(token): unexpected token type
'    Public Const RPC_E_OUT_OF_RESOURCES As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_SYS_CALL_FAILED = unchecked((int)0x80010100),
'    '        RPC_E_OUT_OF_RESOURCES = unchecked((int)0x80010101),
'    '----------------------------------^--- GenCode(token): unexpected token type
'    Public Const RPC_E_ATTEMPTED_MULTITHREAD As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_OUT_OF_RESOURCES = unchecked((int)0x80010101),
'    '        RPC_E_ATTEMPTED_MULTITHREAD = unchecked((int)0x80010102),
'    '---------------------------------------^--- GenCode(token): unexpected token type
'    Public Const RPC_E_NOT_REGISTERED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_ATTEMPTED_MULTITHREAD = unchecked((int)0x80010102),
'    '        RPC_E_NOT_REGISTERED = unchecked((int)0x80010103),
'    '--------------------------------^--- GenCode(token): unexpected token type
'    Public Const RPC_E_FAULT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_NOT_REGISTERED = unchecked((int)0x80010103),
'    '        RPC_E_FAULT = unchecked((int)0x80010104),
'    '-----------------------^--- GenCode(token): unexpected token type
'    Public Const RPC_E_SERVERFAULT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_FAULT = unchecked((int)0x80010104),
'    '        RPC_E_SERVERFAULT = unchecked((int)0x80010105),
'    '-----------------------------^--- GenCode(token): unexpected token type
'    Public Const RPC_E_CHANGED_MODE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_SERVERFAULT = unchecked((int)0x80010105),
'    '        RPC_E_CHANGED_MODE = unchecked((int)0x80010106),
'    '------------------------------^--- GenCode(token): unexpected token type
'    Public Const RPC_E_INVALIDMETHOD As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_CHANGED_MODE = unchecked((int)0x80010106),
'    '        RPC_E_INVALIDMETHOD = unchecked((int)0x80010107),
'    '-------------------------------^--- GenCode(token): unexpected token type
'    Public Const RPC_E_DISCONNECTED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_INVALIDMETHOD = unchecked((int)0x80010107),
'    '        RPC_E_DISCONNECTED = unchecked((int)0x80010108),
'    '------------------------------^--- GenCode(token): unexpected token type
'    Public Const RPC_E_RETRY As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_DISCONNECTED = unchecked((int)0x80010108),
'    '        RPC_E_RETRY = unchecked((int)0x80010109),
'    '-----------------------^--- GenCode(token): unexpected token type
'    Public Const RPC_E_SERVERCALL_RETRYLATER As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_RETRY = unchecked((int)0x80010109),
'    '        RPC_E_SERVERCALL_RETRYLATER = unchecked((int)0x8001010A),
'    '---------------------------------------^--- GenCode(token): unexpected token type
'    Public Const RPC_E_SERVERCALL_REJECTED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_SERVERCALL_RETRYLATER = unchecked((int)0x8001010A),
'    '        RPC_E_SERVERCALL_REJECTED = unchecked((int)0x8001010B),
'    '-------------------------------------^--- GenCode(token): unexpected token type
'    Public Const RPC_E_INVALID_CALLDATA As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_SERVERCALL_REJECTED = unchecked((int)0x8001010B),
'    '        RPC_E_INVALID_CALLDATA = unchecked((int)0x8001010C),
'    '----------------------------------^--- GenCode(token): unexpected token type
'    Public Const RPC_E_CANTCALLOUT_ININPUTSYNCCALL As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_INVALID_CALLDATA = unchecked((int)0x8001010C),
'    '        RPC_E_CANTCALLOUT_ININPUTSYNCCALL = unchecked((int)0x8001010D),
'    '---------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const RPC_E_WRONG_THREAD As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_CANTCALLOUT_ININPUTSYNCCALL = unchecked((int)0x8001010D),
'    '        RPC_E_WRONG_THREAD = unchecked((int)0x8001010E),
'    '------------------------------^--- GenCode(token): unexpected token type
'    Public Const RPC_E_THREAD_NOT_INIT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_WRONG_THREAD = unchecked((int)0x8001010E),
'    '        RPC_E_THREAD_NOT_INIT = unchecked((int)0x8001010F),
'    '---------------------------------^--- GenCode(token): unexpected token type
'    Public Const RPC_E_VERSION_MISMATCH As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_THREAD_NOT_INIT = unchecked((int)0x8001010F),
'    '        RPC_E_VERSION_MISMATCH = unchecked((int)0x80010110),
'    '----------------------------------^--- GenCode(token): unexpected token type
'    Public Const RPC_E_INVALID_HEADER As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_VERSION_MISMATCH = unchecked((int)0x80010110),
'    '        RPC_E_INVALID_HEADER = unchecked((int)0x80010111),
'    '--------------------------------^--- GenCode(token): unexpected token type
'    Public Const RPC_E_INVALID_EXTENSION As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_INVALID_HEADER = unchecked((int)0x80010111),
'    '        RPC_E_INVALID_EXTENSION = unchecked((int)0x80010112),
'    '-----------------------------------^--- GenCode(token): unexpected token type
'    Public Const RPC_E_INVALID_IPID As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_INVALID_EXTENSION = unchecked((int)0x80010112),
'    '        RPC_E_INVALID_IPID = unchecked((int)0x80010113),
'    '------------------------------^--- GenCode(token): unexpected token type
'    Public Const RPC_E_INVALID_OBJECT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_INVALID_IPID = unchecked((int)0x80010113),
'    '        RPC_E_INVALID_OBJECT = unchecked((int)0x80010114),
'    '--------------------------------^--- GenCode(token): unexpected token type
'    Public Const RPC_S_CALLPENDING As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_INVALID_OBJECT = unchecked((int)0x80010114),
'    '        RPC_S_CALLPENDING = unchecked((int)0x80010115),
'    '-----------------------------^--- GenCode(token): unexpected token type
'    Public Const RPC_S_WAITONTIMER As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_S_CALLPENDING = unchecked((int)0x80010115),
'    '        RPC_S_WAITONTIMER = unchecked((int)0x80010116),
'    '-----------------------------^--- GenCode(token): unexpected token type
'    Public Const RPC_E_CALL_COMPLETE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_S_WAITONTIMER = unchecked((int)0x80010116),
'    '        RPC_E_CALL_COMPLETE = unchecked((int)0x80010117),
'    '-------------------------------^--- GenCode(token): unexpected token type
'    Public Const RPC_E_UNSECURE_CALL As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_CALL_COMPLETE = unchecked((int)0x80010117),
'    '        RPC_E_UNSECURE_CALL = unchecked((int)0x80010118),
'    '-------------------------------^--- GenCode(token): unexpected token type
'    Public Const RPC_E_TOO_LATE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_UNSECURE_CALL = unchecked((int)0x80010118),
'    '        RPC_E_TOO_LATE = unchecked((int)0x80010119),
'    '--------------------------^--- GenCode(token): unexpected token type
'    Public Const RPC_E_NO_GOOD_SECURITY_PACKAGES As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_TOO_LATE = unchecked((int)0x80010119),
'    '        RPC_E_NO_GOOD_SECURITY_PACKAGES = unchecked((int)0x8001011A),
'    '-------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const RPC_E_ACCESS_DENIED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_NO_GOOD_SECURITY_PACKAGES = unchecked((int)0x8001011A),
'    '        RPC_E_ACCESS_DENIED = unchecked((int)0x8001011B),
'    '-------------------------------^--- GenCode(token): unexpected token type
'    Public Const RPC_E_REMOTE_DISABLED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_ACCESS_DENIED = unchecked((int)0x8001011B),
'    '        RPC_E_REMOTE_DISABLED = unchecked((int)0x8001011C),
'    '---------------------------------^--- GenCode(token): unexpected token type
'    Public Const RPC_E_INVALID_OBJREF As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_REMOTE_DISABLED = unchecked((int)0x8001011C),
'    '        RPC_E_INVALID_OBJREF = unchecked((int)0x8001011D),
'    '--------------------------------^--- GenCode(token): unexpected token type
'    Public Const RPC_E_UNEXPECTED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_INVALID_OBJREF = unchecked((int)0x8001011D),
'    '        RPC_E_UNEXPECTED = unchecked((int)0x8001FFFF),
'    '----------------------------^--- GenCode(token): unexpected token type
'    Public Const R2_BLACK As Integer = 1
'    Public Const R2_NOTMERGEPEN As Integer = 2
'    Public Const R2_MASKNOTPEN As Integer = 3
'    Public Const R2_NOTCOPYPEN As Integer = 4
'    Public Const R2_MASKPENNOT As Integer = 5
'    Public Const R2_NOT As Integer = 6
'    Public Const R2_XORPEN As Integer = 7
'    Public Const R2_NOTMASKPEN As Integer = 8
'    Public Const R2_MASKPEN As Integer = 9
'    Public Const R2_NOTXORPEN As Integer = 10
'    Public Const R2_NOP As Integer = 11
'    Public Const R2_MERGENOTPEN As Integer = 12
'    Public Const R2_COPYPEN As Integer = 13
'    Public Const R2_MERGEPENNOT As Integer = 14
'    Public Const R2_MERGEPEN As Integer = 15
'    Public Const R2_WHITE As Integer = 16
'    Public Const R2_LAST As Integer = 16
'    Public Const RGN_ERROR As Integer = 0
'    Public Const RGN_AND As Integer = 1
'    Public Const RGN_OR As Integer = 2
'    Public Const RGN_XOR As Integer = 3
'    Public Const RGN_DIFF As Integer = 4
'    Public Const RGN_COPY As Integer = 5
'    Public Const RGN_MIN As Integer = 1
'    Public Const RGN_MAX As Integer = 5
'    Public Const RESTORE_CTM As Integer = 4100
'    Public Const RUSSIAN_CHARSET As Integer = 204
'    Public Const RASTER_FONTTYPE As Integer = &H1
'    Public Const RELATIVE As Integer = 2
'    Public Const RASTERCAPS As Integer = 38
'    Public Const RC_BITBLT As Integer = 1
'    Public Const RC_BANDING As Integer = 2
'    Public Const RC_SCALING As Integer = 4
'    Public Const RC_BITMAP64 As Integer = 8
'    Public Const RC_GDI20_OUTPUT As Integer = &H10
'    Public Const RC_GDI20_STATE As Integer = &H20
'    Public Const RC_SAVEBITMAP As Integer = &H40
'    Public Const RC_DI_BITMAP As Integer = &H80
'    Public Const RC_PALETTE As Integer = &H100
'    Public Const RC_DIBTODEV As Integer = &H200
'    Public Const RC_BIGFONT As Integer = &H400
'    Public Const RC_STRETCHBLT As Integer = &H800
'    Public Const RC_FLOODFILL As Integer = &H1000
'    Public Const RC_STRETCHDIB As Integer = &H2000
'    Public Const RC_OP_DX_OUTPUT As Integer = &H4000
'    Public Const RC_DEVBITS As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RC_OP_DX_OUTPUT = 0x4000,
'    '        RC_DEVBITS = unchecked((int)0x8000),
'    '----------------------^--- GenCode(token): unexpected token type
'    Public Const RDH_RECTANGLES As Integer = 1
'    Public Const RESOURCE_CONNECTED As Integer = &H1
'    Public Const RESOURCE_GLOBALNET As Integer = &H2
'    Public Const RESOURCE_REMEMBERED As Integer = &H3
'    Public Const RESOURCE_RECENT As Integer = &H4
'    Public Const RESOURCE_CONTEXT As Integer = &H5
'    Public Const RESOURCETYPE_ANY As Integer = &H0
'    Public Const RESOURCETYPE_DISK As Integer = &H1
'    Public Const RESOURCETYPE_PRINT As Integer = &H2
'    Public Const RESOURCETYPE_RESERVED As Integer = &H8
'    Public Const RESOURCETYPE_UNKNOWN As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RESOURCETYPE_RESERVED = 0x00000008,
'    '        RESOURCETYPE_UNKNOWN = unchecked((int)0xFFFFFFFF),
'    '--------------------------------^--- GenCode(token): unexpected token type
'    Public Const RESOURCEUSAGE_CONNECTABLE As Integer = &H1
'    Public Const RESOURCEUSAGE_CONTAINER As Integer = &H2
'    Public Const RESOURCEUSAGE_NOLOCALDEVICE As Integer = &H4
'    Public Const RESOURCEUSAGE_SIBLING As Integer = &H8
'    Public Const RESOURCEUSAGE_ATTACHED As Integer = &H10
'    Public Const RESOURCEUSAGE_ALL As Integer = &H1 Or &H2 Or &H10
'    Public Const RESOURCEUSAGE_RESERVED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RESOURCEUSAGE_ALL = (0x00000001|0x00000002|0x00000010),
'    '                            RESOURCEUSAGE_RESERVED = unchecked((int)0x80000000),
'    '------------------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const RESOURCEDISPLAYTYPE_GENERIC As Integer = &H0
'    Public Const RESOURCEDISPLAYTYPE_DOMAIN As Integer = &H1
'    Public Const RESOURCEDISPLAYTYPE_SERVER As Integer = &H2
'    Public Const RESOURCEDISPLAYTYPE_SHARE As Integer = &H3
'    Public Const RESOURCEDISPLAYTYPE_FILE As Integer = &H4
'    Public Const RESOURCEDISPLAYTYPE_GROUP As Integer = &H5
'    Public Const RESOURCEDISPLAYTYPE_NETWORK As Integer = &H6
'    Public Const RESOURCEDISPLAYTYPE_ROOT As Integer = &H7
'    Public Const RESOURCEDISPLAYTYPE_SHAREADMIN As Integer = &H8
'    Public Const RESOURCEDISPLAYTYPE_DIRECTORY As Integer = &H9
'    Public Const RESOURCEDISPLAYTYPE_TREE As Integer = &HA
'    Public Const RESOURCEDISPLAYTYPE_NDSCONTAINER As Integer = &HB
'    Public Const REMOTE_NAME_INFO_LEVEL As Integer = &H2
'    Public Const RP_LOGON As Integer = &H1
'    Public Const RP_INIFILE As Integer = &H2
'    Public Const READ_CONTROL As Integer = &H20000
'    Public Const RTL_CRITSECT_TYPE As Integer = 0
'    Public Const RTL_RESOURCE_TYPE As Integer = 1
'    Public Const REG_OPTION_RESERVED As Integer = &H0
'    Public Const REG_OPTION_NON_VOLATILE As Integer = &H0
'    Public Const REG_OPTION_VOLATILE As Integer = &H1
'    Public Const REG_OPTION_CREATE_LINK As Integer = &H2
'    Public Const REG_OPTION_BACKUP_RESTORE As Integer = &H4
'    Public Const REG_OPTION_OPEN_LINK As Integer = &H8
'    Public Const REG_CREATED_NEW_KEY As Integer = &H1
'    Public Const REG_OPENED_EXISTING_KEY As Integer = &H2
'    Public Const REG_WHOLE_HIVE_VOLATILE As Integer = &H1
'    Public Const REG_REFRESH_HIVE As Integer = &H2
'    Public Const REG_NO_LAZY_FLUSH As Integer = &H4
'    Public Const REG_NOTIFY_CHANGE_NAME As Integer = &H1
'    Public Const REG_NOTIFY_CHANGE_ATTRIBUTES As Integer = &H2
'    Public Const REG_NOTIFY_CHANGE_LAST_SET As Integer = &H4
'    Public Const REG_NOTIFY_CHANGE_SECURITY As Integer = &H8
'    Public Const REG_NONE As Integer = 0
'    Public Const REG_SZ As Integer = 1
'    Public Const REG_EXPAND_SZ As Integer = 2
'    Public Const REG_BINARY As Integer = 3
'    Public Const REG_DWORD As Integer = 4
'    Public Const REG_DWORD_LITTLE_ENDIAN As Integer = 4
'    Public Const REG_DWORD_BIG_ENDIAN As Integer = 5
'    Public Const REG_LINK As Integer = 6
'    Public Const REG_MULTI_SZ As Integer = 7
'    Public Const REG_RESOURCE_LIST As Integer = 8
'    Public Const REG_FULL_RESOURCE_DESCRIPTOR As Integer = 9
'    Public Const REG_RESOURCE_REQUIREMENTS_LIST As Integer = 10
'    Public Const RT_CURSOR As Integer = 1
'    Public Const RT_BITMAP As Integer = 2
'    Public Const RT_ICON As Integer = 3
'    Public Const RT_MENU As Integer = 4
'    Public Const RT_DIALOG As Integer = 5
'    Public Const RT_STRING As Integer = 6
'    Public Const RT_FONTDIR As Integer = 7
'    Public Const RT_FONT As Integer = 8
'    Public Const RT_ACCELERATOR As Integer = 9
'    Public Const RT_RCDATA As Integer = 10
'    Public Const RT_MESSAGETABLE As Integer = 11
'    Public Const RT_GROUP_CURSOR As Integer = 1 + 11
'    Public Const RT_GROUP_ICON As Integer = 3 + 11
'    Public Const RT_VERSION As Integer = 16
'    Public Const RT_DLGINCLUDE As Integer = 17
'    Public Const RT_PLUGPLAY As Integer = 19
'    Public Const RT_VXD As Integer = 20
'    Public Const RT_ANICURSOR As Integer = 21
'    Public Const RT_ANIICON As Integer = 22
'    Public Const RDW_INVALIDATE As Integer = &H1
'    Public Const RDW_INTERNALPAINT As Integer = &H2
'    Public Const RDW_ERASE As Integer = &H4
'    Public Const RDW_VALIDATE As Integer = &H8
'    Public Const RDW_NOINTERNALPAINT As Integer = &H10
'    Public Const RDW_NOERASE As Integer = &H20
'    Public Const RDW_NOCHILDREN As Integer = &H40
'    Public Const RDW_ALLCHILDREN As Integer = &H80
'    Public Const RDW_UPDATENOW As Integer = &H100
'    Public Const RDW_ERASENOW As Integer = &H200
'    Public Const RDW_FRAME As Integer = &H400
'    Public Const RDW_NOFRAME As Integer = &H800
'    Public Const RES_ICON As Integer = 1
'    Public Const RES_CURSOR As Integer = 2
'    Public Const ROTFLAGS_REGISTRATIONKEEPSALIVE As Integer = &H1
'    Public Const ROTFLAGS_ALLOWANYCLIENT As Integer = &H2
'    Public Const ROT_COMPARE_MAX As Integer = 2048
'    Public Const RBN_FIRST As Integer = 0 - 831
'    Public Const RBN_LAST As Integer = 0 - 859
'    Public Const RBNM_ID As Integer = &H1
'    Public Const RBNM_STYLE As Integer = &H2
'    Public Const RBNM_LPARAM As Integer = &H4
'    Public Const RBIM_IMAGELIST As Integer = &H1
'    Public Const RBS_TOOLTIPS As Integer = &H100
'    Public Const RBS_VARHEIGHT As Integer = &H200
'    Public Const RBS_BANDBORDERS As Integer = &H400
'    Public Const RBS_FIXEDORDER As Integer = &H800
'    Public Const RBS_REGISTERDROP As Integer = &H1000
'    Public Const RBS_AUTOSIZE As Integer = &H2000
'    Public Const RBS_VERTICALGRIPPER As Integer = &H4000
'    Public Const RBS_DBLCLKTOGGLE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RBS_VERTICALGRIPPER = 0x4000,
'    '        RBS_DBLCLKTOGGLE = unchecked((int)0x8000),
'    '----------------------------^--- GenCode(token): unexpected token type
'    Public Const RBBS_BREAK As Integer = &H1
'    Public Const RBBS_FIXEDSIZE As Integer = &H2
'    Public Const RBBS_CHILDEDGE As Integer = &H4
'    Public Const RBBS_HIDDEN As Integer = &H8
'    Public Const RBBS_NOVERT As Integer = &H10
'    Public Const RBBS_FIXEDBMP As Integer = &H20
'    Public Const RBBS_VARIABLEHEIGHT As Integer = &H40
'    Public Const RBBS_GRIPPERALWAYS As Integer = &H80
'    Public Const RBBS_NOGRIPPER As Integer = &H100
'    Public Const RBBIM_STYLE As Integer = &H1
'    Public Const RBBIM_COLORS As Integer = &H2
'    Public Const RBBIM_TEXT As Integer = &H4
'    Public Const RBBIM_IMAGE As Integer = &H8
'    Public Const RBBIM_CHILD As Integer = &H10
'    Public Const RBBIM_CHILDSIZE As Integer = &H20
'    Public Const RBBIM_SIZE As Integer = &H40
'    Public Const RBBIM_BACKGROUND As Integer = &H80
'    Public Const RBBIM_ID As Integer = &H100
'    Public Const RBBIM_IDEALSIZE As Integer = &H200
'    Public Const RBBIM_LPARAM As Integer = &H400
'    Public Const RBBIM_HEADERSIZE As Integer = &H800
'    Public Const RB_INSERTBANDA As Integer = &H400 + 1
'    Public Const RB_DELETEBAND As Integer = &H400 + 2
'    Public Const RB_GETBARINFO As Integer = &H400 + 3
'    Public Const RB_SETBARINFO As Integer = &H400 + 4
'    Public Const RB_GETBANDINFO_OLD As Integer = &H400 + 5
'    Public Const RB_SETBANDINFOA As Integer = &H400 + 6
'    Public Const RB_SETPARENT As Integer = &H400 + 7
'    Public Const RB_HITTEST As Integer = &H400 + 8
'    Public Const RB_GETRECT As Integer = &H400 + 9
'    Public Const RB_INSERTBANDW As Integer = &H400 + 10
'    Public Const RB_SETBANDINFOW As Integer = &H400 + 11
'    Public Const RB_GETBANDCOUNT As Integer = &H400 + 12
'    Public Const RB_GETROWCOUNT As Integer = &H400 + 13
'    Public Const RB_GETROWHEIGHT As Integer = &H400 + 14
'    Public Const RB_IDTOINDEX As Integer = &H400 + 16
'    Public Const RB_GETTOOLTIPS As Integer = &H400 + 17
'    Public Const RB_SETTOOLTIPS As Integer = &H400 + 18
'    Public Const RB_SETBKCOLOR As Integer = &H400 + 19
'    Public Const RB_GETBKCOLOR As Integer = &H400 + 20
'    Public Const RB_SETTEXTCOLOR As Integer = &H400 + 21
'    Public Const RB_GETTEXTCOLOR As Integer = &H400 + 22
'    Public Const RB_SIZETORECT As Integer = &H400 + 23
'    Public Const RB_BEGINDRAG As Integer = &H400 + 24
'    Public Const RB_ENDDRAG As Integer = &H400 + 25
'    Public Const RB_DRAGMOVE As Integer = &H400 + 26
'    Public Const RB_GETBARHEIGHT As Integer = &H400 + 27
'    Public Const RB_GETBANDINFOW As Integer = &H400 + 28
'    Public Const RB_GETBANDINFOA As Integer = &H400 + 29
'    Public Const RB_MINIMIZEBAND As Integer = &H400 + 30
'    Public Const RB_MAXIMIZEBAND As Integer = &H400 + 31
'    Public Const RB_GETBANDBORDERS As Integer = &H400 + 34
'    Public Const RB_SHOWBAND As Integer = &H400 + 35
'    Public Const RB_SETPALETTE As Integer = &H400 + 37
'    Public Const RB_GETPALETTE As Integer = &H400 + 38
'    Public Const RB_MOVEBAND As Integer = &H400 + 39
'    Public Const RB_SETCOLORSCHEME As Integer = win.CCM_SETCOLORSCHEME
'    Public Const RB_GETCOLORSCHEME As Integer = win.CCM_GETCOLORSCHEME
'    Public Const RB_GETDROPTARGET As Integer = win.CCM_GETDROPTARGET
'    Public Const RB_SETUNICODEFORMAT As Integer = win.CCM_SETUNICODEFORMAT
'    Public Const RB_GETUNICODEFORMAT As Integer = win.CCM_GETUNICODEFORMAT
'    Public Const RBN_HEIGHTCHANGE As Integer = 0 - 831 - 0
'    Public Const RBN_GETOBJECT As Integer = 0 - 831 - 1
'    Public Const RBN_LAYOUTCHANGED As Integer = 0 - 831 - 2
'    Public Const RBN_AUTOSIZE As Integer = 0 - 831 - 3
'    Public Const RBN_BEGINDRAG As Integer = 0 - 831 - 4
'    Public Const RBN_ENDDRAG As Integer = 0 - 831 - 5
'    Public Const RBN_DELETINGBAND As Integer = 0 - 831 - 6
'    Public Const RBN_DELETEDBAND As Integer = 0 - 831 - 7
'    Public Const RBN_CHILDSIZE As Integer = 0 - 831 - 8
'    Public Const RBHT_NOWHERE As Integer = &H1
'    Public Const RBHT_CAPTION As Integer = &H2
'    Public Const RBHT_CLIENT As Integer = &H3
'    Public Const RBHT_GRABBER As Integer = &H4
'    Public Const RPC_S_OK As Integer = 0
'    Public Const RPC_S_INVALID_ARG As Integer = 87
'    Public Const RPC_S_OUT_OF_MEMORY As Integer = 14
'    Public Const RPC_S_OUT_OF_THREADS As Integer = 164
'    Public Const RPC_S_INVALID_LEVEL As Integer = 87
'    Public Const RPC_S_BUFFER_TOO_SMALL As Integer = 122
'    Public Const RPC_S_INVALID_SECURITY_DESC As Integer = 1338
'    Public Const RPC_S_ACCESS_DENIED As Integer = 5
'    Public Const RPC_S_SERVER_OUT_OF_MEMORY As Integer = 1130
'    Public Const RPC_X_NO_MEMORY As Integer = 14
'    Public Const RPC_X_INVALID_BOUND As Integer = 1734
'    Public Const RPC_X_INVALID_TAG As Integer = 1733
'    Public Const RPC_X_ENUM_VALUE_TOO_LARGE As Integer = 1781
'    Public Const RPC_X_SS_CONTEXT_MISMATCH As Integer = 6
'    Public Const RPC_X_INVALID_BUFFER As Integer = 1784
    
    
    
    
    
    
    
'    Public Const SIMULATED_FONTTYPE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '
'    '        public const int SIMULATED_FONTTYPE = unchecked((int)0x8000),
'    '-----------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const SCREEN_FONTTYPE As Integer = &H2000
'    Public Const ST_CONNECTED As Integer = &H1
'    Public Const ST_ADVISE As Integer = &H2
'    Public Const ST_ISLOCAL As Integer = &H4
'    Public Const ST_BLOCKED As Integer = &H8
'    Public Const ST_CLIENT As Integer = &H10
'    Public Const ST_TERMINATED As Integer = &H20
'    Public Const ST_INLIST As Integer = &H40
'    Public Const ST_BLOCKNEXT As Integer = &H80
'    Public Const ST_ISSELF As Integer = &H100
'    Public Const stc1 As Integer = &H440
'    Public Const stc2 As Integer = &H441
'    Public Const stc3 As Integer = &H442
'    Public Const stc4 As Integer = &H443
'    Public Const stc5 As Integer = &H444
'    Public Const stc6 As Integer = &H445
'    Public Const stc7 As Integer = &H446
'    Public Const stc8 As Integer = &H447
'    Public Const stc9 As Integer = &H448
'    Public Const stc10 As Integer = &H449
'    Public Const stc11 As Integer = &H44A
'    Public Const stc12 As Integer = &H44B
'    Public Const stc13 As Integer = &H44C
'    Public Const stc14 As Integer = &H44D
'    Public Const stc15 As Integer = &H44E
'    Public Const stc16 As Integer = &H44F
'    Public Const stc17 As Integer = &H450
'    Public Const stc18 As Integer = &H451
'    Public Const stc19 As Integer = &H452
'    Public Const stc20 As Integer = &H453
'    Public Const stc21 As Integer = &H454
'    Public Const stc22 As Integer = &H455
'    Public Const stc23 As Integer = &H456
'    Public Const stc24 As Integer = &H457
'    Public Const stc25 As Integer = &H458
'    Public Const stc26 As Integer = &H459
'    Public Const stc27 As Integer = &H45A
'    Public Const stc28 As Integer = &H45B
'    Public Const stc29 As Integer = &H45C
'    Public Const stc30 As Integer = &H45D
'    Public Const stc31 As Integer = &H45E
'    Public Const stc32 As Integer = &H45F
'    Public Const scr1 As Integer = &H490
'    Public Const scr2 As Integer = &H491
'    Public Const scr3 As Integer = &H492
'    Public Const scr4 As Integer = &H493
'    Public Const scr5 As Integer = &H494
'    Public Const scr6 As Integer = &H495
'    Public Const scr7 As Integer = &H496
'    Public Const scr8 As Integer = &H497
'    Public Const STYLE_DESCRIPTION_SIZE As Integer = 32
'    Public Const SCS_CAP_COMPSTR As Integer = &H1
'    Public Const SCS_CAP_MAKEREAD As Integer = &H2
'    Public Const SELECT_CAP_CONVERSION As Integer = &H1
'    Public Const SELECT_CAP_SENTENCE As Integer = &H2
'    Public Const SCS_SETSTR As Integer = &H1 Or &H8
'    Public Const SCS_CHANGEATTR As Integer = &H2 Or &H10
'    Public Const SCS_CHANGECLAUSE As Integer = &H4 Or &H20
'    Public Const SOFTKEYBOARD_TYPE_T1 As Integer = &H1
'    Public Const SOFTKEYBOARD_TYPE_C1 As Integer = &H2
'    Public Const SND_SYNC As Integer = &H0
'    Public Const SND_ASYNC As Integer = &H1
'    Public Const SND_NODEFAULT As Integer = &H2
'    Public Const SND_MEMORY As Integer = &H4
'    Public Const SND_LOOP As Integer = &H8
'    Public Const SND_NOSTOP As Integer = &H10
'    Public Const SND_NOWAIT As Integer = &H2000
'    Public Const SND_ALIAS As Integer = &H10000
'    Public Const SND_ALIAS_ID As Integer = &H110000
'    Public Const SND_FILENAME As Integer = &H20000
'    Public Const SND_RESOURCE As Integer = &H40004
'    Public Const SND_PURGE As Integer = &H40
'    Public Const SND_APPLICATION As Integer = &H80
'    Public Const SND_ALIAS_START As Integer = 0
'    Public Const SEEK_SET As Integer = 0
'    Public Const SEEK_CUR As Integer = 1
'    Public Const SEEK_END As Integer = 2
'    Public Const SELECTDIB As Integer = 41
'    Public Const SC_SCREENSAVE As Integer = &HF140
'    Public Const SO_CONNDATA As Integer = &H7000
'    Public Const SO_CONNOPT As Integer = &H7001
'    Public Const SO_DISCDATA As Integer = &H7002
'    Public Const SO_DISCOPT As Integer = &H7003
'    Public Const SO_CONNDATALEN As Integer = &H7004
'    Public Const SO_CONNOPTLEN As Integer = &H7005
'    Public Const SO_DISCDATALEN As Integer = &H7006
'    Public Const SO_DISCOPTLEN As Integer = &H7007
'    Public Const SO_OPENTYPE As Integer = &H7008
'    Public Const SO_SYNCHRONOUS_ALERT As Integer = &H10
'    Public Const SO_SYNCHRONOUS_NONALERT As Integer = &H20
'    Public Const SO_MAXDG As Integer = &H7009
'    Public Const SO_MAXPATHDG As Integer = &H700A
'    Public Const SO_UPDATE_ACCEPT_CONTEXT As Integer = &H700B
'    Public Const SO_CONNECT_TIME As Integer = &H700C
'    Public Const SESSION_ESTABLISHED As Integer = &H3
'    Public Const SESSION_ABORTED As Integer = &H6
'    Public Const STGM_DIRECT As Integer = &H0
'    Public Const STGM_TRANSACTED As Integer = &H10000
'    Public Const STGM_SIMPLE As Integer = &H8000000
'    Public Const STGM_READ As Integer = &H0
'    Public Const STGM_WRITE As Integer = &H1
'    Public Const STGM_READWRITE As Integer = &H2
'    Public Const STGM_SHARE_DENY_NONE As Integer = &H40
'    Public Const STGM_SHARE_DENY_READ As Integer = &H30
'    Public Const STGM_SHARE_DENY_WRITE As Integer = &H20
'    Public Const STGM_SHARE_EXCLUSIVE As Integer = &H10
'    Public Const STGM_PRIORITY As Integer = &H40000
'    Public Const STGM_DELETEONRELEASE As Integer = &H4000000
'    Public Const STGM_NOSCRATCH As Integer = &H100000
'    Public Const STGM_CREATE As Integer = &H1000
'    Public Const STGM_CONVERT As Integer = &H20000
'    Public Const STGM_FAILIFTHERE As Integer = &H0
'    Public Const STGM_NOSNAPSHOT As Integer = &H200000
'    Public Const STGTY_REPEAT As Integer = &H100
'    Public Const STG_TOEND As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        STGTY_REPEAT = 0x00000100,
'    '        STG_TOEND = unchecked((int)0xFFFFFFFF),
'    '---------------------^--- GenCode(token): unexpected token type
'    Public Const STG_LAYOUT_SEQUENTIAL As Integer = &H0
'    Public Const STG_LAYOUT_INTERLEAVED As Integer = &H1
'    Public Const STDOLE_MAJORVERNUM As Integer = &H1
'    Public Const STDOLE_MINORVERNUM As Integer = &H0
'    Public Const STDOLE_LCID As Integer = &H0
'    Public Const STDOLE2_MAJORVERNUM As Integer = &H2
'    Public Const STDOLE2_MINORVERNUM As Integer = &H0
'    Public Const STDOLE2_LCID As Integer = &H0
'    Public Const SEC_WINNT_AUTH_IDENTITY_ANSI As Integer = &H1
'    Public Const SEC_WINNT_AUTH_IDENTITY_UNICODE As Integer = &H2
'    Public Const SE_ERR_FNF As Integer = 2
'    Public Const SE_ERR_PNF As Integer = 3
'    Public Const SE_ERR_ACCESSDENIED As Integer = 5
'    Public Const SE_ERR_OOM As Integer = 8
'    Public Const SE_ERR_DLLNOTFOUND As Integer = 32
'    Public Const SE_ERR_SHARE As Integer = 26
'    Public Const SE_ERR_ASSOCINCOMPLETE As Integer = 27
'    Public Const SE_ERR_DDETIMEOUT As Integer = 28
'    Public Const SE_ERR_DDEFAIL As Integer = 29
'    Public Const SE_ERR_DDEBUSY As Integer = 30
'    Public Const SE_ERR_NOASSOC As Integer = 31
'    Public Const SEE_MASK_CLASSNAME As Integer = &H1
'    Public Const SEE_MASK_CLASSKEY As Integer = &H3
'    Public Const SEE_MASK_IDLIST As Integer = &H4
'    Public Const SEE_MASK_INVOKEIDLIST As Integer = &HC
'    Public Const SEE_MASK_ICON As Integer = &H10
'    Public Const SEE_MASK_HOTKEY As Integer = &H20
'    Public Const SEE_MASK_NOCLOSEPROCESS As Integer = &H40
'    Public Const SEE_MASK_CONNECTNETDRV As Integer = &H80
'    Public Const SEE_MASK_FLAG_DDEWAIT As Integer = &H100
'    Public Const SEE_MASK_DOENVSUBST As Integer = &H200
'    Public Const SEE_MASK_FLAG_NO_UI As Integer = &H400
'    Public Const SEE_MASK_UNICODE As Integer = &H4000
'    Public Const SEE_MASK_NO_CONSOLE As Integer = &H8000
'    Public Const SEE_MASK_ASYNCOK As Integer = &H100000
'    Public Const SHGFI_ICON As Long = &H100L
'    Public Const SHGFI_DISPLAYNAME As Long = &H200L
'    Public Const SHGFI_TYPENAME As Long = &H400L
'    Public Const SHGFI_ATTRIBUTES As Long = &H800L
'    Public Const SHGFI_ICONLOCATION As Long = &H1000L
'    Public Const SHGFI_EXETYPE As Long = &H2000L
'    Public Const SHGFI_SYSICONINDEX As Long = &H4000L
'    Public Const SHGFI_LINKOVERLAY As Long = &H8000L
'    Public Const SHGFI_SELECTED As Long = &H10000L
'    Public Const SHGFI_LARGEICON As Long = &H0L
'    Public Const SHGFI_SMALLICON As Long = &H1L
'    Public Const SHGFI_OPENICON As Long = &H2L
'    Public Const SHGFI_SHELLICONSIZE As Long = &H4L
'    Public Const SHGFI_PIDL As Long = &H8L
'    Public Const SHGFI_USEFILEATTRIBUTES As Long = &H10L
'    Public Const SHGNLI_PIDL As Long = &H1L
'    Public Const SHGNLI_PREFIXNAME As Long = &H2L
'    Public Const SHGNLI_NOUNIQUE As Long = &H4L
'    Public Const SECURITY_CONTEXT_TRACKING As Integer = &H40000
'    Public Const SECURITY_EFFECTIVE_ONLY As Integer = &H80000
'    Public Const SECURITY_SQOS_PRESENT As Integer = &H100000
'    Public Const SECURITY_VALID_SQOS_FLAGS As Integer = &H1F0000
'    Public Const SP_SERIALCOMM As Integer = &H1
'    Public Const SP_PARITY As Integer = &H1
'    Public Const SP_BAUD As Integer = &H2
'    Public Const SP_DATABITS As Integer = &H4
'    Public Const SP_STOPBITS As Integer = &H8
'    Public Const SP_HANDSHAKING As Integer = &H10
'    Public Const SP_PARITY_CHECK As Integer = &H20
'    Public Const SP_RLSD As Integer = &H40
'    Public Const STOPBITS_10 As Integer = &H1
'    Public Const STOPBITS_15 As Integer = &H2
'    Public Const STOPBITS_20 As Integer = &H4
'    Public Const SPACEPARITY As Integer = 4
'    Public Const SETXOFF As Integer = 1
'    Public Const SETXON As Integer = 2
'    Public Const SETRTS As Integer = 3
'    Public Const SETDTR As Integer = 5
'    Public Const SETBREAK As Integer = 8
'    Public Const S_QUEUEEMPTY As Integer = 0
'    Public Const S_THRESHOLD As Integer = 1
'    Public Const S_ALLTHRESHOLD As Integer = 2
'    Public Const S_NORMAL As Integer = 0
'    Public Const S_LEGATO As Integer = 1
'    Public Const S_STACCATO As Integer = 2
'    Public Const S_PERIOD512 As Integer = 0
'    Public Const S_PERIOD1024 As Integer = 1
'    Public Const S_PERIOD2048 As Integer = 2
'    Public Const S_PERIODVOICE As Integer = 3
'    Public Const S_WHITE512 As Integer = 4
'    Public Const S_WHITE1024 As Integer = 5
'    Public Const S_WHITE2048 As Integer = 6
'    Public Const S_WHITEVOICE As Integer = 7
'    Public Const S_SERDVNA As Integer = - 1
'    Public Const S_SEROFM As Integer = - 2
'    Public Const S_SERMACT As Integer = - 3
'    Public Const S_SERQFUL As Integer = - 4
'    Public Const S_SERBDNT As Integer = - 5
'    Public Const S_SERDLN As Integer = - 6
'    Public Const S_SERDCC As Integer = - 7
'    Public Const S_SERDTP As Integer = - 8
'    Public Const S_SERDVL As Integer = - 9
'    Public Const S_SERDMD As Integer = - 10
'    Public Const S_SERDSH As Integer = - 11
'    Public Const S_SERDPT As Integer = - 12
'    Public Const S_SERDFQ As Integer = - 13
'    Public Const S_SERDDR As Integer = - 14
'    Public Const S_SERDSR As Integer = - 15
'    Public Const S_SERDST As Integer = - 16
'    Public Const SCS_32BIT_BINARY As Integer = 0
'    Public Const SCS_DOS_BINARY As Integer = 1
'    Public Const SCS_WOW_BINARY As Integer = 2
'    Public Const SCS_PIF_BINARY As Integer = 3
'    Public Const SCS_POSIX_BINARY As Integer = 4
'    Public Const SCS_OS216_BINARY As Integer = 5
'    Public Const SEM_FAILCRITICALERRORS As Integer = &H1
'    Public Const SEM_NOGPFAULTERRORBOX As Integer = &H2
'    Public Const SEM_NOALIGNMENTFAULTEXCEPT As Integer = &H4
'    Public Const SEM_NOOPENFILEERRORBOX As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        SEM_NOALIGNMENTFAULTEXCEPT = 0x0004,
'    '        SEM_NOOPENFILEERRORBOX = unchecked((int)0x8000),
'    '----------------------------------^--- GenCode(token): unexpected token type
'    Public Const SET_TAPE_MEDIA_INFORMATION As Integer = 0
'    Public Const SET_TAPE_DRIVE_INFORMATION As Integer = 1
'    Public Const STREAM_NORMAL_ATTRIBUTE As Integer = &H0
'    Public Const STREAM_MODIFIED_WHEN_READ As Integer = &H1
'    Public Const STREAM_CONTAINS_SECURITY As Integer = &H2
'    Public Const STREAM_CONTAINS_PROPERTIES As Integer = &H4
'    Public Const STARTF_USESHOWWINDOW As Integer = &H1
'    Public Const STARTF_USESIZE As Integer = &H2
'    Public Const STARTF_USEPOSITION As Integer = &H4
'    Public Const STARTF_USECOUNTCHARS As Integer = &H8
'    Public Const STARTF_USEFILLATTRIBUTE As Integer = &H10
'    Public Const STARTF_RUNFULLSCREEN As Integer = &H20
'    Public Const STARTF_FORCEONFEEDBACK As Integer = &H40
'    Public Const STARTF_FORCEOFFFEEDBACK As Integer = &H80
'    Public Const STARTF_USESTDHANDLES As Integer = &H100
'    Public Const STARTF_USEHOTKEY As Integer = &H200
'    Public Const SHUTDOWN_NORETRY As Integer = &H1
'    Public Const SHIFT_PRESSED As Integer = &H10
'    Public Const SCROLLLOCK_ON As Integer = &H40
'    Public Const SIMPLEBLOB As Integer = &H1
'    Public Const SEVERITY_SUCCESS As Integer = 0
'    Public Const SEVERITY_ERROR As Integer = 1
'    Public Const STG_E_INVALIDFUNCTION As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        SEVERITY_ERROR = 1,
'    '        STG_E_INVALIDFUNCTION = unchecked((int)0x80030001),
'    '---------------------------------^--- GenCode(token): unexpected token type
'    Public Const STG_E_FILENOTFOUND As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        STG_E_INVALIDFUNCTION = unchecked((int)0x80030001),
'    '        STG_E_FILENOTFOUND = unchecked((int)0x80030002),
'    '------------------------------^--- GenCode(token): unexpected token type
'    Public Const STG_E_PATHNOTFOUND As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        STG_E_FILENOTFOUND = unchecked((int)0x80030002),
'    '        STG_E_PATHNOTFOUND = unchecked((int)0x80030003),
'    '------------------------------^--- GenCode(token): unexpected token type
'    Public Const STG_E_TOOMANYOPENFILES As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        STG_E_PATHNOTFOUND = unchecked((int)0x80030003),
'    '        STG_E_TOOMANYOPENFILES = unchecked((int)0x80030004),
'    '----------------------------------^--- GenCode(token): unexpected token type
'    Public Const STG_E_ACCESSDENIED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        STG_E_TOOMANYOPENFILES = unchecked((int)0x80030004),
'    '        STG_E_ACCESSDENIED = unchecked((int)0x80030005),
'    '------------------------------^--- GenCode(token): unexpected token type
'    Public Const STG_E_INVALIDHANDLE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        STG_E_ACCESSDENIED = unchecked((int)0x80030005),
'    '        STG_E_INVALIDHANDLE = unchecked((int)0x80030006),
'    '-------------------------------^--- GenCode(token): unexpected token type
'    Public Const STG_E_INSUFFICIENTMEMORY As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        STG_E_INVALIDHANDLE = unchecked((int)0x80030006),
'    '        STG_E_INSUFFICIENTMEMORY = unchecked((int)0x80030008),
'    '------------------------------------^--- GenCode(token): unexpected token type
'    Public Const STG_E_INVALIDPOINTER As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        STG_E_INSUFFICIENTMEMORY = unchecked((int)0x80030008),
'    '        STG_E_INVALIDPOINTER = unchecked((int)0x80030009),
'    '--------------------------------^--- GenCode(token): unexpected token type
'    Public Const STG_E_NOMOREFILES As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        STG_E_INVALIDPOINTER = unchecked((int)0x80030009),
'    '        STG_E_NOMOREFILES = unchecked((int)0x80030012),
'    '-----------------------------^--- GenCode(token): unexpected token type
'    Public Const STG_E_DISKISWRITEPROTECTED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        STG_E_NOMOREFILES = unchecked((int)0x80030012),
'    '        STG_E_DISKISWRITEPROTECTED = unchecked((int)0x80030013),
'    '--------------------------------------^--- GenCode(token): unexpected token type
'    Public Const STG_E_SEEKERROR As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        STG_E_DISKISWRITEPROTECTED = unchecked((int)0x80030013),
'    '        STG_E_SEEKERROR = unchecked((int)0x80030019),
'    '---------------------------^--- GenCode(token): unexpected token type
'    Public Const STG_E_WRITEFAULT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        STG_E_SEEKERROR = unchecked((int)0x80030019),
'    '        STG_E_WRITEFAULT = unchecked((int)0x8003001D),
'    '----------------------------^--- GenCode(token): unexpected token type
'    Public Const STG_E_READFAULT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        STG_E_WRITEFAULT = unchecked((int)0x8003001D),
'    '        STG_E_READFAULT = unchecked((int)0x8003001E),
'    '---------------------------^--- GenCode(token): unexpected token type
'    Public Const STG_E_SHAREVIOLATION As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        STG_E_READFAULT = unchecked((int)0x8003001E),
'    '        STG_E_SHAREVIOLATION = unchecked((int)0x80030020),
'    '--------------------------------^--- GenCode(token): unexpected token type
'    Public Const STG_E_LOCKVIOLATION As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        STG_E_SHAREVIOLATION = unchecked((int)0x80030020),
'    '        STG_E_LOCKVIOLATION = unchecked((int)0x80030021),
'    '-------------------------------^--- GenCode(token): unexpected token type
'    Public Const STG_E_FILEALREADYEXISTS As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        STG_E_LOCKVIOLATION = unchecked((int)0x80030021),
'    '        STG_E_FILEALREADYEXISTS = unchecked((int)0x80030050),
'    '-----------------------------------^--- GenCode(token): unexpected token type
'    Public Const STG_E_INVALIDPARAMETER As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        STG_E_FILEALREADYEXISTS = unchecked((int)0x80030050),
'    '        STG_E_INVALIDPARAMETER = unchecked((int)0x80030057),
'    '----------------------------------^--- GenCode(token): unexpected token type
'    Public Const STG_E_MEDIUMFULL As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        STG_E_INVALIDPARAMETER = unchecked((int)0x80030057),
'    '        STG_E_MEDIUMFULL = unchecked((int)0x80030070),
'    '----------------------------^--- GenCode(token): unexpected token type
'    Public Const STG_E_PROPSETMISMATCHED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        STG_E_MEDIUMFULL = unchecked((int)0x80030070),
'    '        STG_E_PROPSETMISMATCHED = unchecked((int)0x800300F0),
'    '-----------------------------------^--- GenCode(token): unexpected token type
'    Public Const STG_E_ABNORMALAPIEXIT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        STG_E_PROPSETMISMATCHED = unchecked((int)0x800300F0),
'    '        STG_E_ABNORMALAPIEXIT = unchecked((int)0x800300FA),
'    '---------------------------------^--- GenCode(token): unexpected token type
'    Public Const STG_E_INVALIDHEADER As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        STG_E_ABNORMALAPIEXIT = unchecked((int)0x800300FA),
'    '        STG_E_INVALIDHEADER = unchecked((int)0x800300FB),
'    '-------------------------------^--- GenCode(token): unexpected token type
'    Public Const STG_E_INVALIDNAME As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        STG_E_INVALIDHEADER = unchecked((int)0x800300FB),
'    '        STG_E_INVALIDNAME = unchecked((int)0x800300FC),
'    '-----------------------------^--- GenCode(token): unexpected token type
'    Public Const STG_E_UNKNOWN As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        STG_E_INVALIDNAME = unchecked((int)0x800300FC),
'    '        STG_E_UNKNOWN = unchecked((int)0x800300FD),
'    '-------------------------^--- GenCode(token): unexpected token type
'    Public Const STG_E_UNIMPLEMENTEDFUNCTION As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        STG_E_UNKNOWN = unchecked((int)0x800300FD),
'    '        STG_E_UNIMPLEMENTEDFUNCTION = unchecked((int)0x800300FE),
'    '---------------------------------------^--- GenCode(token): unexpected token type
'    Public Const STG_E_INVALIDFLAG As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        STG_E_UNIMPLEMENTEDFUNCTION = unchecked((int)0x800300FE),
'    '        STG_E_INVALIDFLAG = unchecked((int)0x800300FF),
'    '-----------------------------^--- GenCode(token): unexpected token type
'    Public Const STG_E_INUSE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        STG_E_INVALIDFLAG = unchecked((int)0x800300FF),
'    '        STG_E_INUSE = unchecked((int)0x80030100),
'    '-----------------------^--- GenCode(token): unexpected token type
'    Public Const STG_E_NOTCURRENT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        STG_E_INUSE = unchecked((int)0x80030100),
'    '        STG_E_NOTCURRENT = unchecked((int)0x80030101),
'    '----------------------------^--- GenCode(token): unexpected token type
'    Public Const STG_E_REVERTED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        STG_E_NOTCURRENT = unchecked((int)0x80030101),
'    '        STG_E_REVERTED = unchecked((int)0x80030102),
'    '--------------------------^--- GenCode(token): unexpected token type
'    Public Const STG_E_CANTSAVE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        STG_E_REVERTED = unchecked((int)0x80030102),
'    '        STG_E_CANTSAVE = unchecked((int)0x80030103),
'    '--------------------------^--- GenCode(token): unexpected token type
'    Public Const STG_E_OLDFORMAT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        STG_E_CANTSAVE = unchecked((int)0x80030103),
'    '        STG_E_OLDFORMAT = unchecked((int)0x80030104),
'    '---------------------------^--- GenCode(token): unexpected token type
'    Public Const STG_E_OLDDLL As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        STG_E_OLDFORMAT = unchecked((int)0x80030104),
'    '        STG_E_OLDDLL = unchecked((int)0x80030105),
'    '------------------------^--- GenCode(token): unexpected token type
'    Public Const STG_E_SHAREREQUIRED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        STG_E_OLDDLL = unchecked((int)0x80030105),
'    '        STG_E_SHAREREQUIRED = unchecked((int)0x80030106),
'    '-------------------------------^--- GenCode(token): unexpected token type
'    Public Const STG_E_NOTFILEBASEDSTORAGE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        STG_E_SHAREREQUIRED = unchecked((int)0x80030106),
'    '        STG_E_NOTFILEBASEDSTORAGE = unchecked((int)0x80030107),
'    '-------------------------------------^--- GenCode(token): unexpected token type
'    Public Const STG_E_EXTANTMARSHALLINGS As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        STG_E_NOTFILEBASEDSTORAGE = unchecked((int)0x80030107),
'    '        STG_E_EXTANTMARSHALLINGS = unchecked((int)0x80030108),
'    '------------------------------------^--- GenCode(token): unexpected token type
'    Public Const STG_E_DOCFILECORRUPT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        STG_E_EXTANTMARSHALLINGS = unchecked((int)0x80030108),
'    '        STG_E_DOCFILECORRUPT = unchecked((int)0x80030109),
'    '--------------------------------^--- GenCode(token): unexpected token type
'    Public Const STG_E_BADBASEADDRESS As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        STG_E_DOCFILECORRUPT = unchecked((int)0x80030109),
'    '        STG_E_BADBASEADDRESS = unchecked((int)0x80030110),
'    '--------------------------------^--- GenCode(token): unexpected token type
'    Public Const STG_E_INCOMPLETE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        STG_E_BADBASEADDRESS = unchecked((int)0x80030110),
'    '        STG_E_INCOMPLETE = unchecked((int)0x80030201),
'    '----------------------------^--- GenCode(token): unexpected token type
'    Public Const STG_E_TERMINATED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        STG_E_INCOMPLETE = unchecked((int)0x80030201),
'    '        STG_E_TERMINATED = unchecked((int)0x80030202),
'    '----------------------------^--- GenCode(token): unexpected token type
'    Public Const STG_S_CONVERTED As Integer = &H30200
'    Public Const STG_S_BLOCK As Integer = &H30201
'    Public Const STG_S_RETRYNOW As Integer = &H30202
'    Public Const STG_S_MONITORING As Integer = &H30203
'    Public Const SIMPLEREGION As Integer = 2
'    Public Const STRETCH_ANDSCANS As Integer = 1
'    Public Const STRETCH_ORSCANS As Integer = 2
'    Public Const STRETCH_DELETESCANS As Integer = 3
'    Public Const STRETCH_HALFTONE As Integer = 4
'    Public Const SETCOLORTABLE As Integer = 4
'    Public Const SETABORTPROC As Integer = 9
'    Public Const STARTDOC As Integer = 10
'    Public Const SETCOPYCOUNT As Integer = 17
'    Public Const SELECTPAPERSOURCE As Integer = 18
'    Public Const SETLINECAP As Integer = 21
'    Public Const SETLINEJOIN As Integer = 22
'    Public Const SETMITERLIMIT As Integer = 23
'    Public Const SETDIBSCALING As Integer = 32
'    Public Const SETKERNTRACK As Integer = 770
'    Public Const SETALLJUSTVALUES As Integer = 771
'    Public Const SETCHARSET As Integer = 772
'    Public Const STRETCHBLT As Integer = 2048
'    Public Const SAVE_CTM As Integer = 4101
'    Public Const SET_ARC_DIRECTION As Integer = 4102
'    Public Const SET_BACKGROUND_COLOR As Integer = 4103
'    Public Const SET_POLY_MODE As Integer = 4104
'    Public Const SET_SCREEN_ANGLE As Integer = 4105
'    Public Const SET_SPREAD As Integer = 4106
'    Public Const SET_CLIP_BOX As Integer = 4108
'    Public Const SET_BOUNDS As Integer = 4109
'    Public Const SET_MIRROR_MODE As Integer = 4110
'    Public Const SP_NOTREPORTED As Integer = &H4000
'    Public Const SP_ERROR As Integer = - 1
'    Public Const SP_APPABORT As Integer = - 2
'    Public Const SP_USERABORT As Integer = - 3
'    Public Const SP_OUTOFDISK As Integer = - 4
'    Public Const SP_OUTOFMEMORY As Integer = - 5
'    Public Const SYMBOL_CHARSET As Integer = 2
'    Public Const SHIFTJIS_CHARSET As Integer = 128
'    Public Const SYSTEM_FONT As Integer = 13
'    Public Const SYSTEM_FIXED_FONT As Integer = 16
'    Public Const STOCK_LAST As Integer = 17
'    ' STOCK_LAST = 16;
'    Public Const SIZEPALETTE As Integer = 104
'    Public Const SCALINGFACTORX As Integer = 114
'    Public Const SCALINGFACTORY As Integer = 115
'    Public Const SYSPAL_ERROR As Integer = 0
'    Public Const SYSPAL_STATIC As Integer = 1
'    Public Const SYSPAL_NOSTATIC As Integer = 2
'    Public Const SORT_STRINGSORT As Integer = &H1000
'    Public Const SUBLANG_NEUTRAL As Integer = &H0
'    Public Const SUBLANG_DEFAULT As Integer = &H1
'    Public Const SUBLANG_SYS_DEFAULT As Integer = &H2
'    Public Const SUBLANG_ARABIC_SAUDI_ARABIA As Integer = &H1
'    Public Const SUBLANG_ARABIC_IRAQ As Integer = &H2
'    Public Const SUBLANG_ARABIC_EGYPT As Integer = &H3
'    Public Const SUBLANG_ARABIC_LIBYA As Integer = &H4
'    Public Const SUBLANG_ARABIC_ALGERIA As Integer = &H5
'    Public Const SUBLANG_ARABIC_MOROCCO As Integer = &H6
'    Public Const SUBLANG_ARABIC_TUNISIA As Integer = &H7
'    Public Const SUBLANG_ARABIC_OMAN As Integer = &H8
'    Public Const SUBLANG_ARABIC_YEMEN As Integer = &H9
'    Public Const SUBLANG_ARABIC_SYRIA As Integer = &HA
'    Public Const SUBLANG_ARABIC_JORDAN As Integer = &HB
'    Public Const SUBLANG_ARABIC_LEBANON As Integer = &HC
'    Public Const SUBLANG_ARABIC_KUWAIT As Integer = &HD
'    Public Const SUBLANG_ARABIC_UAE As Integer = &HE
'    Public Const SUBLANG_ARABIC_BAHRAIN As Integer = &HF
'    Public Const SUBLANG_ARABIC_QATAR As Integer = &H10
'    Public Const SUBLANG_CHINESE_TRADITIONAL As Integer = &H1
'    Public Const SUBLANG_CHINESE_SIMPLIFIED As Integer = &H2
'    Public Const SUBLANG_CHINESE_HONGKONG As Integer = &H3
'    Public Const SUBLANG_CHINESE_SINGAPORE As Integer = &H4
'    Public Const SUBLANG_DUTCH As Integer = &H1
'    Public Const SUBLANG_DUTCH_BELGIAN As Integer = &H2
'    Public Const SUBLANG_ENGLISH_US As Integer = &H1
'    Public Const SUBLANG_ENGLISH_UK As Integer = &H2
'    Public Const SUBLANG_ENGLISH_AUS As Integer = &H3
'    Public Const SUBLANG_ENGLISH_CAN As Integer = &H4
'    Public Const SUBLANG_ENGLISH_NZ As Integer = &H5
'    Public Const SUBLANG_ENGLISH_EIRE As Integer = &H6
'    Public Const SUBLANG_ENGLISH_SOUTH_AFRICA As Integer = &H7
'    Public Const SUBLANG_ENGLISH_JAMAICA As Integer = &H8
'    Public Const SUBLANG_ENGLISH_CARIBBEAN As Integer = &H9
'    Public Const SUBLANG_ENGLISH_BELIZE As Integer = &HA
'    Public Const SUBLANG_ENGLISH_TRINIDAD As Integer = &HB
'    Public Const SUBLANG_FRENCH As Integer = &H1
'    Public Const SUBLANG_FRENCH_BELGIAN As Integer = &H2
'    Public Const SUBLANG_FRENCH_CANADIAN As Integer = &H3
'    Public Const SUBLANG_FRENCH_SWISS As Integer = &H4
'    Public Const SUBLANG_FRENCH_LUXEMBOURG As Integer = &H5
'    Public Const SUBLANG_GERMAN As Integer = &H1
'    Public Const SUBLANG_GERMAN_SWISS As Integer = &H2
'    Public Const SUBLANG_GERMAN_AUSTRIAN As Integer = &H3
'    Public Const SUBLANG_GERMAN_LUXEMBOURG As Integer = &H4
'    Public Const SUBLANG_GERMAN_LIECHTENSTEIN As Integer = &H5
'    Public Const SUBLANG_ITALIAN As Integer = &H1
'    Public Const SUBLANG_ITALIAN_SWISS As Integer = &H2
'    Public Const SUBLANG_KOREAN As Integer = &H1
'    Public Const SUBLANG_KOREAN_JOHAB As Integer = &H2
'    Public Const SUBLANG_NORWEGIAN_BOKMAL As Integer = &H1
'    Public Const SUBLANG_NORWEGIAN_NYNORSK As Integer = &H2
'    Public Const SUBLANG_PORTUGUESE As Integer = &H2
'    Public Const SUBLANG_PORTUGUESE_BRAZILIAN As Integer = &H1
'    Public Const SUBLANG_SERBIAN_LATIN As Integer = &H2
'    Public Const SUBLANG_SERBIAN_CYRILLIC As Integer = &H3
'    Public Const SUBLANG_SPANISH As Integer = &H1
'    Public Const SUBLANG_SPANISH_MEXICAN As Integer = &H2
'    Public Const SUBLANG_SPANISH_MODERN As Integer = &H3
'    Public Const SUBLANG_SPANISH_GUATEMALA As Integer = &H4
'    Public Const SUBLANG_SPANISH_COSTA_RICA As Integer = &H5
'    Public Const SUBLANG_SPANISH_PANAMA As Integer = &H6
'    Public Const SUBLANG_SPANISH_DOMINICAN_REPUBLIC As Integer = &H7
'    Public Const SUBLANG_SPANISH_VENEZUELA As Integer = &H8
'    Public Const SUBLANG_SPANISH_COLOMBIA As Integer = &H9
'    Public Const SUBLANG_SPANISH_PERU As Integer = &HA
'    Public Const SUBLANG_SPANISH_ARGENTINA As Integer = &HB
'    Public Const SUBLANG_SPANISH_ECUADOR As Integer = &HC
'    Public Const SUBLANG_SPANISH_CHILE As Integer = &HD
'    Public Const SUBLANG_SPANISH_URUGUAY As Integer = &HE
'    Public Const SUBLANG_SPANISH_PARAGUAY As Integer = &HF
'    Public Const SUBLANG_SPANISH_BOLIVIA As Integer = &H10
'    Public Const SUBLANG_SPANISH_EL_SALVADOR As Integer = &H11
'    Public Const SUBLANG_SPANISH_HONDURAS As Integer = &H12
'    Public Const SUBLANG_SPANISH_NICARAGUA As Integer = &H13
'    Public Const SUBLANG_SPANISH_PUERTO_RICO As Integer = &H14
'    Public Const SUBLANG_SWEDISH As Integer = &H1
'    Public Const SUBLANG_SWEDISH_FINLAND As Integer = &H2
'    Public Const SORT_DEFAULT As Integer = &H0
'    Public Const SORT_JAPANESE_XJIS As Integer = &H0
'    Public Const SORT_JAPANESE_UNICODE As Integer = &H1
'    Public Const SORT_CHINESE_BIG5 As Integer = &H0
'    Public Const SORT_CHINESE_PRCP As Integer = &H0
'    Public Const SORT_CHINESE_UNICODE As Integer = &H1
'    Public Const SORT_CHINESE_PRC As Integer = &H2
'    Public Const SORT_KOREAN_KSC As Integer = &H0
'    Public Const SORT_KOREAN_UNICODE As Integer = &H1
'    Public Const SORT_GERMAN_PHONE_BOOK As Integer = &H1
'    Public Const STATUS_WAIT_0 As Integer = &H0
'    Public Const STATUS_ABANDONED_WAIT_0 As Integer = &H80
'    Public Const STATUS_USER_APC As Integer = &HC0
'    Public Const STATUS_TIMEOUT As Integer = &H102
'    Public Const STATUS_PENDING As Integer = &H103
'    Public Const STATUS_SEGMENT_NOTIFICATION As Integer = &H40000005
'    Public Const STATUS_GUARD_PAGE_VIOLATION As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                      STATUS_SEGMENT_NOTIFICATION = (0x40000005),
'    '                                                                                                                                    STATUS_GUARD_PAGE_VIOLATION = (unchecked((int)0x80000001)),
'    '--------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const STATUS_DATATYPE_MISALIGNMENT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                    STATUS_GUARD_PAGE_VIOLATION = (unchecked((int)0x80000001)),
'    '                                                                                                                                                                  STATUS_DATATYPE_MISALIGNMENT = (unchecked((int)0x80000002)),
'    '---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const STATUS_BREAKPOINT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                                  STATUS_DATATYPE_MISALIGNMENT = (unchecked((int)0x80000002)),
'    '                                                                                                                                                                                                 STATUS_BREAKPOINT = (unchecked((int)0x80000003)),
'    '-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const STATUS_SINGLE_STEP As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                                                                 STATUS_BREAKPOINT = (unchecked((int)0x80000003)),
'    '                                                                                                                                                                                                                     STATUS_SINGLE_STEP = (unchecked((int)0x80000004)),
'    '--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const STATUS_ACCESS_VIOLATION As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                                                                                     STATUS_SINGLE_STEP = (unchecked((int)0x80000004)),
'    '                                                                                                                                                                                                                                          STATUS_ACCESS_VIOLATION = (unchecked((int)0xC0000005)),
'    '----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const STATUS_IN_PAGE_ERROR As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                                                                                                          STATUS_ACCESS_VIOLATION = (unchecked((int)0xC0000005)),
'    '                                                                                                                                                                                                                                                                    STATUS_IN_PAGE_ERROR = (unchecked((int)0xC0000006)),
'    '---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const STATUS_INVALID_HANDLE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                                                                                                                                    STATUS_IN_PAGE_ERROR = (unchecked((int)0xC0000006)),
'    '                                                                                                                                                                                                                                                                                           STATUS_INVALID_HANDLE = (unchecked((int)0xC0000008)),
'    '---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const STATUS_NO_MEMORY As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                                                                                                                                                           STATUS_INVALID_HANDLE = (unchecked((int)0xC0000008)),
'    '                                                                                                                                                                                                                                                                                                                   STATUS_NO_MEMORY = (unchecked((int)0xC0000017)),
'    '----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const STATUS_ILLEGAL_INSTRUCTION As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                                                                                                                                                                                   STATUS_NO_MEMORY = (unchecked((int)0xC0000017)),
'    '                                                                                                                                                                                                                                                                                                                                      STATUS_ILLEGAL_INSTRUCTION = (unchecked((int)0xC000001D)),
'    '---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const STATUS_NONCONTINUABLE_EXCEPTION As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                                                                                                                                                                                                      STATUS_ILLEGAL_INSTRUCTION = (unchecked((int)0xC000001D)),
'    '                                                                                                                                                                                                                                                                                                                                                                   STATUS_NONCONTINUABLE_EXCEPTION = (unchecked((int)0xC0000025)),
'    '-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const STATUS_INVALID_DISPOSITION As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                                                                                                                                                                                                                                   STATUS_NONCONTINUABLE_EXCEPTION = (unchecked((int)0xC0000025)),
'    '                                                                                                                                                                                                                                                                                                                                                                                                     STATUS_INVALID_DISPOSITION = (unchecked((int)0xC0000026)),
'    '------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const STATUS_ARRAY_BOUNDS_EXCEEDED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                                                                                                                                                                                                                                                                     STATUS_INVALID_DISPOSITION = (unchecked((int)0xC0000026)),
'    '                                                                                                                                                                                                                                                                                                                                                                                                                                  STATUS_ARRAY_BOUNDS_EXCEEDED = (unchecked((int)0xC000008C)),
'    '-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const STATUS_FLOAT_DENORMAL_OPERAND As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                                                                                                                                                                                                                                                                                                  STATUS_ARRAY_BOUNDS_EXCEEDED = (unchecked((int)0xC000008C)),
'    '                                                                                                                                                                                                                                                                                                                                                                                                                                                                 STATUS_FLOAT_DENORMAL_OPERAND = (unchecked((int)0xC000008D)),
'    '---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const STATUS_FLOAT_DIVIDE_BY_ZERO As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                                                                                                                                                                                                                                                                                                                                 STATUS_FLOAT_DENORMAL_OPERAND = (unchecked((int)0xC000008D)),
'    '                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                 STATUS_FLOAT_DIVIDE_BY_ZERO = (unchecked((int)0xC000008E)),
'    '---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const STATUS_FLOAT_INEXACT_RESULT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                 STATUS_FLOAT_DIVIDE_BY_ZERO = (unchecked((int)0xC000008E)),
'    '                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                               STATUS_FLOAT_INEXACT_RESULT = (unchecked((int)0xC000008F)),
'    '---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const STATUS_FLOAT_INVALID_OPERATION As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                               STATUS_FLOAT_INEXACT_RESULT = (unchecked((int)0xC000008F)),
'    '                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                             STATUS_FLOAT_INVALID_OPERATION = (unchecked((int)0xC0000090)),
'    '------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const STATUS_FLOAT_OVERFLOW As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                             STATUS_FLOAT_INVALID_OPERATION = (unchecked((int)0xC0000090)),
'    '                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                              STATUS_FLOAT_OVERFLOW = (unchecked((int)0xC0000091)),
'    '------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const STATUS_FLOAT_STACK_CHECK As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                              STATUS_FLOAT_OVERFLOW = (unchecked((int)0xC0000091)),
'    '                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                      STATUS_FLOAT_STACK_CHECK = (unchecked((int)0xC0000092)),
'    '---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const STATUS_FLOAT_UNDERFLOW As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                      STATUS_FLOAT_STACK_CHECK = (unchecked((int)0xC0000092)),
'    '                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                 STATUS_FLOAT_UNDERFLOW = (unchecked((int)0xC0000093)),
'    '----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const STATUS_INTEGER_DIVIDE_BY_ZERO As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                 STATUS_FLOAT_UNDERFLOW = (unchecked((int)0xC0000093)),
'    '                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                          STATUS_INTEGER_DIVIDE_BY_ZERO = (unchecked((int)0xC0000094)),
'    '------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const STATUS_INTEGER_OVERFLOW As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                          STATUS_INTEGER_DIVIDE_BY_ZERO = (unchecked((int)0xC0000094)),
'    '                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                          STATUS_INTEGER_OVERFLOW = (unchecked((int)0xC0000095)),
'    '--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const STATUS_PRIVILEGED_INSTRUCTION As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                          STATUS_INTEGER_OVERFLOW = (unchecked((int)0xC0000095)),
'    '                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    STATUS_PRIVILEGED_INSTRUCTION = (unchecked((int)0xC0000096)),
'    '----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const STATUS_STACK_OVERFLOW As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    STATUS_PRIVILEGED_INSTRUCTION = (unchecked((int)0xC0000096)),
'    '                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    STATUS_STACK_OVERFLOW = (unchecked((int)0xC00000FD)),
'    '----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const STATUS_CONTROL_C_EXIT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    STATUS_STACK_OVERFLOW = (unchecked((int)0xC00000FD)),
'    '                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                            STATUS_CONTROL_C_EXIT = (unchecked((int)0xC000013A)),
'    '----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const SIZE_OF_80387_REGISTERS As Integer = 80
'    Public Const SEMAPHORE_MODIFY_STATE As Integer = &H2
'    Public Const SECTION_QUERY As Integer = &H1
'    Public Const SECTION_MAP_WRITE As Integer = &H2
'    Public Const SECTION_MAP_READ As Integer = &H4
'    Public Const SECTION_MAP_EXECUTE As Integer = &H8
'    Public Const SECTION_EXTEND_SIZE As Integer = &H10
'    Public Const SEC_FILE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        SECTION_EXTEND_SIZE = 0x0010,
'    '        SEC_FILE = unchecked((int)0x800000),
'    '--------------------^--- GenCode(token): unexpected token type
'    Public Const SEC_IMAGE As Integer = &H1000000
'    Public Const SEC_RESERVE As Integer = &H4000000
'    Public Const SEC_COMMIT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        SEC_RESERVE = 0x4000000,
'    '        SEC_COMMIT = unchecked((int)0x8000000),
'    '----------------------^--- GenCode(token): unexpected token type
'    Public Const SEC_NOCACHE As Integer = &H10000000
'    Public Const SYNCHRONIZE As Integer = &H100000
'    Public Const STANDARD_RIGHTS_REQUIRED As Integer = &HF0000
'    Public Const STANDARD_RIGHTS_READ As Integer = &H20000
'    Public Const STANDARD_RIGHTS_WRITE As Integer = &H20000
'    Public Const STANDARD_RIGHTS_EXECUTE As Integer = &H20000
'    Public Const STANDARD_RIGHTS_ALL As Integer = &H1F0000
'    Public Const SPECIFIC_RIGHTS_ALL As Integer = &HFFFF
'    Public Const SID_REVISION As Integer = 1
'    Public Const SID_MAX_SUB_AUTHORITIES As Integer = 15
'    Public Const SID_RECOMMENDED_SUB_AUTHORITIES As Integer = 1
'    Public Const SECURITY_NULL_RID As Integer = &H0
'    Public Const SECURITY_WORLD_RID As Integer = &H0
'    Public Const SECURITY_LOCAL_RID As Integer = 0
'    Private __unknown As X00000000
'    Private __unknown As __unknown
'    Private SECURITY_CREATOR_OWNER_RID As __unknown = &H0
'    Private SECURITY_CREATOR_GROUP_RID As __unknown = &H1
'    Private SECURITY_CREATOR_OWNER_SERVER_RID As __unknown = &H2
'    Private SECURITY_CREATOR_GROUP_SERVER_RID As __unknown = &H3
'    Private SECURITY_DIALUP_RID As __unknown = &H1
'    Private SECURITY_NETWORK_RID As __unknown = &H2
'    Private SECURITY_BATCH_RID As __unknown = &H3
'    Private SECURITY_INTERACTIVE_RID As __unknown = &H4
'    Private SECURITY_SERVICE_RID As __unknown = &H6
'    Private SECURITY_ANONYMOUS_LOGON_RID As __unknown = &H7
'    Private SECURITY_PROXY_RID As __unknown = &H8
'    Private SECURITY_SERVER_LOGON_RID As __unknown = &H9
'    Private SECURITY_LOGON_IDS_RID As __unknown = &H5
'    Private SECURITY_LOGON_IDS_RID_COUNT As __unknown = 3
'    Private SECURITY_LOCAL_SYSTEM_RID As __unknown = &H12
'    Private SECURITY_NT_NON_UNIQUE As __unknown = &H15
'    Private SECURITY_BUILTIN_DOMAIN_RID As __unknown = &H20
'    Private SE_GROUP_MANDATORY As __unknown = &H1
'    Private SE_GROUP_ENABLED_BY_DEFAULT As __unknown = &H2
'    Private SE_GROUP_ENABLED As __unknown = &H4
'    Private SE_GROUP_OWNER As __unknown = &H8
'    Private SE_GROUP_LOGON_ID As __unknown = 
'    '
'    'Note:  Error processing original source shown below
'    '        SE_GROUP_OWNER = (0x00000008),
'    '        SE_GROUP_LOGON_ID = (unchecked((int)0xC0000000)),
'    '------------------------------^--- GenCode(token): unexpected token type
'    Private SYSTEM_AUDIT_ACE_TYPE As __unknown = &H2
'    Private SYSTEM_ALARM_ACE_TYPE As __unknown = &H3
'    Private SUCCESSFUL_ACCESS_ACE_FLAG As __unknown = &H40
'    Private SECURITY_DESCRIPTOR_REVISION As __unknown = 1
'    Private SECURITY_DESCRIPTOR_REVISION1 As __unknown = 1
'    Private SECURITY_DESCRIPTOR_MIN_LENGTH As __unknown = 20
'    Private SE_OWNER_DEFAULTED As __unknown = &H1
'    Private SE_GROUP_DEFAULTED As __unknown = &H2
'    Private SE_DACL_PRESENT As __unknown = &H4
'    Private SE_DACL_DEFAULTED As __unknown = &H8
'    Private SE_SACL_PRESENT As __unknown = &H10
'    Private SE_SACL_DEFAULTED As __unknown = &H20
'    Private SE_SELF_RELATIVE As __unknown = 
'    '
'    'Note:  Error processing original source shown below
'    '        SE_SACL_DEFAULTED = (0x0020),
'    '        SE_SELF_RELATIVE = (unchecked((int)0x8000)),
'    '-----------------------------^--- GenCode(token): unexpected token type
'    Private SE_PRIVILEGE_ENABLED_BY_DEFAULT As __unknown = &H1
'    Private SE_PRIVILEGE_ENABLED As __unknown = &H2
'    Private SE_PRIVILEGE_USED_FOR_ACCESS As __unknown = 
'    '
'    'Note:  Error processing original source shown below
'    '        SE_PRIVILEGE_ENABLED = (0x00000002),
'    '        SE_PRIVILEGE_USED_FOR_ACCESS = (unchecked((int)0x80000000));
'    '-----------------------------------------^--- GenCode(token): unexpected token type
'    Private __unknown As __unknown
'    Public Const SECURITY_DYNAMIC_TRACKING As Boolean = True
'    Public Const SECURITY_STATIC_TRACKING As Boolean = False
'    Public Const SACL_SECURITY_INFORMATION As Integer = 0
'    Private __unknown As X00000008
'    Private __unknown As __unknown
'    Private SIZEOF_RFPO_DATA As __unknown = 16
'    Private SERVICE_KERNEL_DRIVER As __unknown = &H1
'    Private SERVICE_FILE_SYSTEM_DRIVER As __unknown = &H2
'    Private SERVICE_ADAPTER As __unknown = &H4
'    Private SERVICE_RECOGNIZER_DRIVER As __unknown = &H8
'    Private SERVICE_WIN32_OWN_PROCESS As __unknown = &H10
'    Private SERVICE_WIN32_SHARE_PROCESS As __unknown = &H20
'    Private SERVICE_INTERACTIVE_PROCESS As __unknown = &H100
'    Private SERVICE_BOOT_START As __unknown = &H0
'    Private SERVICE_SYSTEM_START As __unknown = &H1
'    Private SERVICE_AUTO_START As __unknown = &H2
'    Private SERVICE_DEMAND_START As __unknown = &H3
'    Private SERVICE_DISABLED As __unknown = &H4
'    Private SERVICE_ERROR_IGNORE As __unknown = &H0
'    Private SERVICE_ERROR_NORMAL As __unknown = &H1
'    Private SERVICE_ERROR_SEVERE As __unknown = &H2
'    Private SERVICE_ERROR_CRITICAL As __unknown = &H3
'    Private SERVER_ACCESS_ADMINISTER As __unknown = &H1
'    Private SERVER_ACCESS_ENUMERATE As __unknown = &H2
'    Private __unknown As __unknown
'    Public Const SC_GROUP_IDENTIFIERW As Char = "+"c
'    Public Const SC_GROUP_IDENTIFIERA As Char = "+"c
'    Public Const SERVICE_NO_CHANGE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        SC_GROUP_IDENTIFIERA = '+';
'    '        public const int SERVICE_NO_CHANGE = unchecked((int)0xFfffffff),
'    '----------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const SERVICE_ACTIVE As Integer = &H1
'    Public Const SERVICE_INACTIVE As Integer = &H2
'    Public Const SERVICE_CONTROL_STOP As Integer = &H1
'    Public Const SERVICE_CONTROL_PAUSE As Integer = &H2
'    Public Const SERVICE_CONTROL_CONTINUE As Integer = &H3
'    Public Const SERVICE_CONTROL_INTERROGATE As Integer = &H4
'    Public Const SERVICE_CONTROL_SHUTDOWN As Integer = &H5
'    Public Const SERVICE_STOPPED As Integer = &H1
'    Public Const SERVICE_START_PENDING As Integer = &H2
'    Public Const SERVICE_STOP_PENDING As Integer = &H3
'    Public Const SERVICE_RUNNING As Integer = &H4
'    Public Const SERVICE_CONTINUE_PENDING As Integer = &H5
'    Public Const SERVICE_PAUSE_PENDING As Integer = &H6
'    Public Const SERVICE_PAUSED As Integer = &H7
'    Public Const SERVICE_ACCEPT_STOP As Integer = &H1
'    Public Const SERVICE_ACCEPT_PAUSE_CONTINUE As Integer = &H2
'    Public Const SERVICE_ACCEPT_SHUTDOWN As Integer = &H4
'    Public Const SC_MANAGER_CONNECT As Integer = &H1
'    Public Const SC_MANAGER_CREATE_SERVICE As Integer = &H2
'    Public Const SC_MANAGER_ENUMERATE_SERVICE As Integer = &H4
'    Public Const SC_MANAGER_LOCK As Integer = &H8
'    Public Const SC_MANAGER_QUERY_LOCK_STATUS As Integer = &H10
'    Public Const SC_MANAGER_MODIFY_BOOT_CONFIG As Integer = &H20
'    Public Const SERVICE_QUERY_CONFIG As Integer = &H1
'    Public Const SERVICE_CHANGE_CONFIG As Integer = &H2
'    Public Const SERVICE_QUERY_STATUS As Integer = &H4
'    Public Const SERVICE_ENUMERATE_DEPENDENTS As Integer = &H8
'    Public Const SERVICE_START As Integer = &H10
'    Public Const SERVICE_STOP As Integer = &H20
'    Public Const SERVICE_PAUSE_CONTINUE As Integer = &H40
'    Public Const SERVICE_INTERROGATE As Integer = &H80
'    Public Const SERVICE_USER_DEFINED_CONTROL As Integer = &H100
'    Public Const SB_HORZ As Integer = 0
'    Public Const SB_VERT As Integer = 1
'    Public Const SB_CTL As Integer = 2
'    Public Const SB_BOTH As Integer = 3
'    Public Const SB_LINEUP As Integer = 0
'    Public Const SB_LINELEFT As Integer = 0
'    Public Const SB_LINEDOWN As Integer = 1
'    Public Const SB_LINERIGHT As Integer = 1
'    Public Const SB_PAGEUP As Integer = 2
'    Public Const SB_PAGELEFT As Integer = 2
'    Public Const SB_PAGEDOWN As Integer = 3
'    Public Const SB_PAGERIGHT As Integer = 3
'    Public Const SB_THUMBPOSITION As Integer = 4
'    Public Const SB_THUMBTRACK As Integer = 5
'    Public Const SB_TOP As Integer = 6
'    Public Const SB_LEFT As Integer = 6
'    Public Const SB_BOTTOM As Integer = 7
'    Public Const SB_RIGHT As Integer = 7
'    Public Const SB_ENDSCROLL As Integer = 8
'    Public Const SW_HIDE As Integer = 0
'    Public Const SW_SHOWNORMAL As Integer = 1
'    Public Const SW_NORMAL As Integer = 1
'    Public Const SW_SHOWMINIMIZED As Integer = 2
'    Public Const SW_SHOWMAXIMIZED As Integer = 3
'    Public Const SW_MAXIMIZE As Integer = 3
'    Public Const SW_SHOWNOACTIVATE As Integer = 4
'    Public Const SW_SHOW As Integer = 5
'    Public Const SW_MINIMIZE As Integer = 6
'    Public Const SW_SHOWMINNOACTIVE As Integer = 7
'    Public Const SW_SHOWNA As Integer = 8
'    Public Const SW_RESTORE As Integer = 9
'    Public Const SW_SHOWDEFAULT As Integer = 10
'    Public Const SW_MAX As Integer = 10
'    Public Const SHOW_OPENWINDOW As Integer = 1
'    Public Const SHOW_ICONWINDOW As Integer = 2
'    Public Const SHOW_FULLSCREEN As Integer = 3
'    Public Const SHOW_OPENNOACTIVATE As Integer = 4
'    Public Const SW_PARENTCLOSING As Integer = 1
'    Public Const SW_OTHERZOOM As Integer = 2
'    Public Const SW_PARENTOPENING As Integer = 3
'    Public Const SW_OTHERUNZOOM As Integer = 4
'    Public Const ST_BEGINSWP As Integer = 0
'    Public Const ST_ENDSWP As Integer = 1
'    Public Const SMTO_NORMAL As Integer = &H0
'    Public Const SMTO_BLOCK As Integer = &H1
'    Public Const SMTO_ABORTIFHUNG As Integer = &H2
'    Public Const SIZE_RESTORED As Integer = 0
'    Public Const SIZE_MINIMIZED As Integer = 1
'    Public Const SIZE_MAXIMIZED As Integer = 2
'    Public Const SIZE_MAXSHOW As Integer = 3
'    Public Const SIZE_MAXHIDE As Integer = 4
'    Public Const SIZENORMAL As Integer = 0
'    Public Const SIZEICONIC As Integer = 1
'    Public Const SIZEFULLSCREEN As Integer = 2
'    Public Const SIZEZOOMSHOW As Integer = 3
'    Public Const SIZEZOOMHIDE As Integer = 4
'    Public Const SWP_NOSIZE As Integer = &H1
'    Public Const SWP_NOMOVE As Integer = &H2
'    Public Const SWP_NOZORDER As Integer = &H4
'    Public Const SWP_NOREDRAW As Integer = &H8
'    Public Const SWP_NOACTIVATE As Integer = &H10
'    Public Const SWP_FRAMECHANGED As Integer = &H20
'    Public Const SWP_SHOWWINDOW As Integer = &H40
'    Public Const SWP_HIDEWINDOW As Integer = &H80
'    Public Const SWP_NOCOPYBITS As Integer = &H100
'    Public Const SWP_NOOWNERZORDER As Integer = &H200
'    Public Const SWP_NOSENDCHANGING As Integer = &H400
'    Public Const SWP_DRAWFRAME As Integer = &H20
'    Public Const SWP_NOREPOSITION As Integer = &H200
'    Public Const SWP_DEFERERASE As Integer = &H2000
'    Public Const SWP_ASYNCWINDOWPOS As Integer = &H4000
'    Public Const SM_CXSCREEN As Integer = 0
'    Public Const SM_CYSCREEN As Integer = 1
'    Public Const SM_CXVSCROLL As Integer = 2
'    Public Const SM_CYHSCROLL As Integer = 3
'    Public Const SM_CYCAPTION As Integer = 4
'    Public Const SM_CXBORDER As Integer = 5
'    Public Const SM_CYBORDER As Integer = 6
'    Public Const SM_CXDLGFRAME As Integer = 7
'    Public Const SM_CYDLGFRAME As Integer = 8
'    Public Const SM_CYVTHUMB As Integer = 9
'    Public Const SM_CXHTHUMB As Integer = 10
'    Public Const SM_CXICON As Integer = 11
'    Public Const SM_CYICON As Integer = 12
'    Public Const SM_CXCURSOR As Integer = 13
'    Public Const SM_CYCURSOR As Integer = 14
'    Public Const SM_CYMENU As Integer = 15
'    Public Const SM_CXFULLSCREEN As Integer = 16
'    Public Const SM_CYFULLSCREEN As Integer = 17
'    Public Const SM_CYKANJIWINDOW As Integer = 18
'    Public Const SM_MOUSEPRESENT As Integer = 19
'    Public Const SM_CYVSCROLL As Integer = 20
'    Public Const SM_CXHSCROLL As Integer = 21
'    Public Const SM_DEBUG As Integer = 22
'    Public Const SM_SWAPBUTTON As Integer = 23
'    Public Const SM_RESERVED1 As Integer = 24
'    Public Const SM_RESERVED2 As Integer = 25
'    Public Const SM_RESERVED3 As Integer = 26
'    Public Const SM_RESERVED4 As Integer = 27
'    Public Const SM_CXMIN As Integer = 28
'    Public Const SM_CYMIN As Integer = 29
'    Public Const SM_CXSIZE As Integer = 30
'    Public Const SM_CYSIZE As Integer = 31
'    Public Const SM_CXFRAME As Integer = 32
'    Public Const SM_CYFRAME As Integer = 33
'    Public Const SM_CXMINTRACK As Integer = 34
'    Public Const SM_CYMINTRACK As Integer = 35
'    Public Const SM_CXDOUBLECLK As Integer = 36
'    Public Const SM_CYDOUBLECLK As Integer = 37
'    Public Const SM_CXICONSPACING As Integer = 38
'    Public Const SM_CYICONSPACING As Integer = 39
'    Public Const SM_MENUDROPALIGNMENT As Integer = 40
'    Public Const SM_PENWINDOWS As Integer = 41
'    Public Const SM_DBCSENABLED As Integer = 42
'    Public Const SM_CMOUSEBUTTONS As Integer = 43
'    Public Const SM_CXFIXEDFRAME As Integer = 7
'    Public Const SM_CYFIXEDFRAME As Integer = 8
'    Public Const SM_CXSIZEFRAME As Integer = 32
'    Public Const SM_CYSIZEFRAME As Integer = 33
'    Public Const SM_SECURE As Integer = 44
'    Public Const SM_CXEDGE As Integer = 45
'    Public Const SM_CYEDGE As Integer = 46
'    Public Const SM_CXMINSPACING As Integer = 47
'    Public Const SM_CYMINSPACING As Integer = 48
'    Public Const SM_CXSMICON As Integer = 49
'    Public Const SM_CYSMICON As Integer = 50
'    Public Const SM_CYSMCAPTION As Integer = 51
'    Public Const SM_CXSMSIZE As Integer = 52
'    Public Const SM_CYSMSIZE As Integer = 53
'    Public Const SM_CXMENUSIZE As Integer = 54
'    Public Const SM_CYMENUSIZE As Integer = 55
'    Public Const SM_ARRANGE As Integer = 56
'    Public Const SM_CXMINIMIZED As Integer = 57
'    Public Const SM_CYMINIMIZED As Integer = 58
'    Public Const SM_CXMAXTRACK As Integer = 59
'    Public Const SM_CYMAXTRACK As Integer = 60
'    Public Const SM_CXMAXIMIZED As Integer = 61
'    Public Const SM_CYMAXIMIZED As Integer = 62
'    Public Const SM_NETWORK As Integer = 63
'    Public Const SM_CLEANBOOT As Integer = 67
'    Public Const SM_CXDRAG As Integer = 68
'    Public Const SM_CYDRAG As Integer = 69
'    Public Const SM_SHOWSOUNDS As Integer = 70
'    Public Const SM_CXMENUCHECK As Integer = 71
'    Public Const SM_CYMENUCHECK As Integer = 72
'    Public Const SM_SLOWMACHINE As Integer = 73
'    Public Const SM_MIDEASTENABLED As Integer = 74
'    Public Const SM_MOUSEWHEELPRESENT As Integer = 75
'    Public Const SM_XVIRTUALSCREEN As Integer = 76
'    Public Const SM_YVIRTUALSCREEN As Integer = 77
'    Public Const SM_CXVIRTUALSCREEN As Integer = 78
'    Public Const SM_CYVIRTUALSCREEN As Integer = 79
'    Public Const SM_CMONITORS As Integer = 80
'    Public Const SM_SAMEDISPLAYFORMAT As Integer = 81
'    Public Const SM_CMETRICS As Integer = 83
'    Public Const SW_SCROLLCHILDREN As Integer = &H1
'    Public Const SW_INVALIDATE As Integer = &H2
'    Public Const SW_ERASE As Integer = &H4
'    Public Const SC_SIZE As Integer = &HF000
'    Public Const SC_MOVE As Integer = &HF010
'    Public Const SC_MINIMIZE As Integer = &HF020
'    Public Const SC_MAXIMIZE As Integer = &HF030
'    Public Const SC_NEXTWINDOW As Integer = &HF040
'    Public Const SC_PREVWINDOW As Integer = &HF050
'    Public Const SC_CLOSE As Integer = &HF060
'    Public Const SC_VSCROLL As Integer = &HF070
'    Public Const SC_HSCROLL As Integer = &HF080
'    Public Const SC_MOUSEMENU As Integer = &HF090
'    Public Const SC_KEYMENU As Integer = &HF100
'    Public Const SC_ARRANGE As Integer = &HF110
'    Public Const SC_RESTORE As Integer = &HF120
'    Public Const SC_TASKLIST As Integer = &HF130
'    Public Const SC_HOTKEY As Integer = &HF150
'    Public Const SC_DEFAULT As Integer = &HF160
'    Public Const SC_MONITORPOWER As Integer = &HF170
        Public Const SC_CONTEXTHELP As Integer = &HF180
'    Public Const SC_SEPARATOR As Integer = &HF00F
'    Public Const SC_ICON As Integer = &HF020
'    Public Const SC_ZOOM As Integer = &HF030
'    Public Const SS_LEFT As Integer = &H0
'    Public Const SS_CENTER As Integer = &H1
'    Public Const SS_RIGHT As Integer = &H2
'    Public Const SS_ICON As Integer = &H3
'    Public Const SS_BLACKRECT As Integer = &H4
'    Public Const SS_GRAYRECT As Integer = &H5
'    Public Const SS_WHITERECT As Integer = &H6
'    Public Const SS_BLACKFRAME As Integer = &H7
'    Public Const SS_GRAYFRAME As Integer = &H8
'    Public Const SS_WHITEFRAME As Integer = &H9
'    Public Const SS_USERITEM As Integer = &HA
'    Public Const SS_SIMPLE As Integer = &HB
'    Public Const SS_LEFTNOWORDWRAP As Integer = &HC
'    Public Const SS_OWNERDRAW As Integer = &HD
'    Public Const SS_BITMAP As Integer = &HE
'    Public Const SS_ENHMETAFILE As Integer = &HF
'    Public Const SS_ETCHEDHORZ As Integer = &H10
'    Public Const SS_ETCHEDVERT As Integer = &H11
'    Public Const SS_ETCHEDFRAME As Integer = &H12
'    Public Const SS_TYPEMASK As Integer = &H1F
'    Public Const SS_NOPREFIX As Integer = &H80
'    Public Const SS_NOTIFY As Integer = &H100
'    Public Const SS_CENTERIMAGE As Integer = &H200
'    Public Const SS_RIGHTJUST As Integer = &H400
'    Public Const SS_REALSIZEIMAGE As Integer = &H800
'    Public Const SS_SUNKEN As Integer = &H1000
'    Public Const SS_ENDELLIPSIS As Integer = &H4000
'    Public Const SS_PATHELLIPSIS As Integer = &H8000
'    Public Const SS_WORDELLIPSIS As Integer = &HC000
'    Public Const SS_ELLIPSISMASK As Integer = &HC000
'    Public Const STM_SETICON As Integer = &H170
'    Public Const STM_GETICON As Integer = &H171
'    Public Const STM_SETIMAGE As Integer = &H172
'    Public Const STM_GETIMAGE As Integer = &H173
'    Public Const STN_CLICKED As Integer = 0
'    Public Const STN_DBLCLK As Integer = 1
'    Public Const STN_ENABLE As Integer = 2
'    Public Const STN_DISABLE As Integer = 3
'    Public Const STM_MSGMAX As Integer = &H174
'    Public Const SBS_HORZ As Integer = &H0
'    Public Const SBS_VERT As Integer = &H1
'    Public Const SBS_TOPALIGN As Integer = &H2
'    Public Const SBS_LEFTALIGN As Integer = &H2
'    Public Const SBS_BOTTOMALIGN As Integer = &H4
'    Public Const SBS_RIGHTALIGN As Integer = &H4
'    Public Const SBS_SIZEBOXTOPLEFTALIGN As Integer = &H2
'    Public Const SBS_SIZEBOXBOTTOMRIGHTALIGN As Integer = &H4
'    Public Const SBS_SIZEBOX As Integer = &H8
'    Public Const SBS_SIZEGRIP As Integer = &H10
'    Public Const SBM_SETPOS As Integer = &HE0
'    Public Const SBM_GETPOS As Integer = &HE1
'    Public Const SBM_SETRANGE As Integer = &HE2
'    Public Const SBM_SETRANGEREDRAW As Integer = &HE6
'    Public Const SBM_GETRANGE As Integer = &HE3
'    Public Const SBM_ENABLE_ARROWS As Integer = &HE4
'    Public Const SBM_SETSCROLLINFO As Integer = &HE9
'    Public Const SBM_GETSCROLLINFO As Integer = &HEA
'    Public Const SIF_RANGE As Integer = &H1
'    Public Const SIF_PAGE As Integer = &H2
'    Public Const SIF_POS As Integer = &H4
'    Public Const SIF_DISABLENOSCROLL As Integer = &H8
'    Public Const SIF_TRACKPOS As Integer = &H10
'    Public Const SIF_ALL As Integer = &H1 Or &H2 Or &H4 Or &H10
'    Public Const SPI_GETBEEP As Integer = 1
'    Public Const SPI_SETBEEP As Integer = 2
'    Public Const SPI_GETMOUSE As Integer = 3
'    Public Const SPI_SETMOUSE As Integer = 4
'    Public Const SPI_GETBORDER As Integer = 5
'    Public Const SPI_SETBORDER As Integer = 6
'    Public Const SPI_GETKEYBOARDSPEED As Integer = 10
'    Public Const SPI_SETKEYBOARDSPEED As Integer = 11
'    Public Const SPI_LANGDRIVER As Integer = 12
'    Public Const SPI_ICONHORIZONTALSPACING As Integer = 13
'    Public Const SPI_GETSCREENSAVETIMEOUT As Integer = 14
'    Public Const SPI_SETSCREENSAVETIMEOUT As Integer = 15
'    Public Const SPI_GETSCREENSAVEACTIVE As Integer = 16
'    Public Const SPI_SETSCREENSAVEACTIVE As Integer = 17
'    Public Const SPI_GETGRIDGRANULARITY As Integer = 18
'    Public Const SPI_SETGRIDGRANULARITY As Integer = 19
'    Public Const SPI_SETDESKWALLPAPER As Integer = 20
'    Public Const SPI_SETDESKPATTERN As Integer = 21
'    Public Const SPI_GETKEYBOARDDELAY As Integer = 22
'    Public Const SPI_SETKEYBOARDDELAY As Integer = 23
'    Public Const SPI_ICONVERTICALSPACING As Integer = 24
'    Public Const SPI_GETICONTITLEWRAP As Integer = 25
'    Public Const SPI_SETICONTITLEWRAP As Integer = 26
'    Public Const SPI_GETMENUDROPALIGNMENT As Integer = 27
'    Public Const SPI_SETMENUDROPALIGNMENT As Integer = 28
'    Public Const SPI_SETDOUBLECLKWIDTH As Integer = 29
'    Public Const SPI_SETDOUBLECLKHEIGHT As Integer = 30
'    Public Const SPI_GETICONTITLELOGFONT As Integer = 31
'    Public Const SPI_SETDOUBLECLICKTIME As Integer = 32
'    Public Const SPI_SETMOUSEBUTTONSWAP As Integer = 33
'    Public Const SPI_SETICONTITLELOGFONT As Integer = 34
'    Public Const SPI_GETFASTTASKSWITCH As Integer = 35
'    Public Const SPI_SETFASTTASKSWITCH As Integer = 36
'    Public Const SPI_SETDRAGFULLWINDOWS As Integer = 37
'    Public Const SPI_GETDRAGFULLWINDOWS As Integer = 38
'    Public Const SPI_GETNONCLIENTMETRICS As Integer = 41
'    Public Const SPI_SETNONCLIENTMETRICS As Integer = 42
'    Public Const SPI_GETMINIMIZEDMETRICS As Integer = 43
'    Public Const SPI_SETMINIMIZEDMETRICS As Integer = 44
'    Public Const SPI_GETICONMETRICS As Integer = 45
'    Public Const SPI_SETICONMETRICS As Integer = 46
'    Public Const SPI_SETWORKAREA As Integer = 47
'    Public Const SPI_GETWORKAREA As Integer = 48
'    Public Const SPI_SETPENWINDOWS As Integer = 49
'    Public Const SPI_GETHIGHCONTRAST As Integer = 66
'    Public Const SPI_SETHIGHCONTRAST As Integer = 67
'    Public Const SPI_GETKEYBOARDPREF As Integer = 68
'    Public Const SPI_SETKEYBOARDPREF As Integer = 69
     Public Const SPI_GETSCREENREADER As Integer = 70
'    Public Const SPI_SETSCREENREADER As Integer = 71
'    Public Const SPI_GETANIMATION As Integer = 72
'    Public Const SPI_SETANIMATION As Integer = 73
'    Public Const SPI_GETFONTSMOOTHING As Integer = 74
'    Public Const SPI_SETFONTSMOOTHING As Integer = 75
'    Public Const SPI_SETDRAGWIDTH As Integer = 76
'    Public Const SPI_SETDRAGHEIGHT As Integer = 77
'    Public Const SPI_SETHANDHELD As Integer = 78
'    Public Const SPI_GETLOWPOWERTIMEOUT As Integer = 79
'    Public Const SPI_GETPOWEROFFTIMEOUT As Integer = 80
'    Public Const SPI_SETLOWPOWERTIMEOUT As Integer = 81
'    Public Const SPI_SETPOWEROFFTIMEOUT As Integer = 82
'    Public Const SPI_GETLOWPOWERACTIVE As Integer = 83
'    Public Const SPI_GETPOWEROFFACTIVE As Integer = 84
'    Public Const SPI_SETLOWPOWERACTIVE As Integer = 85
'    Public Const SPI_SETPOWEROFFACTIVE As Integer = 86
'    Public Const SPI_SETCURSORS As Integer = 87
'    Public Const SPI_SETICONS As Integer = 88
'    Public Const SPI_GETDEFAULTINPUTLANG As Integer = 89
'    Public Const SPI_SETDEFAULTINPUTLANG As Integer = 90
'    Public Const SPI_SETLANGTOGGLE As Integer = 91
'    Public Const SPI_GETWINDOWSEXTENSION As Integer = 92
'    Public Const SPI_SETMOUSETRAILS As Integer = 93
'    Public Const SPI_GETMOUSETRAILS As Integer = 94
'    Public Const SPI_SCREENSAVERRUNNING As Integer = 97
'    Public Const SPI_GETFILTERKEYS As Integer = 50
'    Public Const SPI_SETFILTERKEYS As Integer = 51
'    Public Const SPI_GETTOGGLEKEYS As Integer = 52
'    Public Const SPI_SETTOGGLEKEYS As Integer = 53
'    Public Const SPI_GETMOUSEKEYS As Integer = 54
'    Public Const SPI_SETMOUSEKEYS As Integer = 55
'    Public Const SPI_GETSHOWSOUNDS As Integer = 56
'    Public Const SPI_SETSHOWSOUNDS As Integer = 57
'    Public Const SPI_GETSTICKYKEYS As Integer = 58
'    Public Const SPI_SETSTICKYKEYS As Integer = 59
'    Public Const SPI_GETACCESSTIMEOUT As Integer = 60
'    Public Const SPI_SETACCESSTIMEOUT As Integer = 61
'    Public Const SPI_GETSERIALKEYS As Integer = 62
'    Public Const SPI_SETSERIALKEYS As Integer = 63
'    Public Const SPI_GETSOUNDSENTRY As Integer = 64
'    Public Const SPI_SETSOUNDSENTRY As Integer = 65
'    Public Const SPI_GETSNAPTODEFBUTTON As Integer = 95
'    Public Const SPI_SETSNAPTODEFBUTTON As Integer = 96
'    Public Const SPI_GETMOUSEHOVERWIDTH As Integer = 98
'    Public Const SPI_SETMOUSEHOVERWIDTH As Integer = 99
'    Public Const SPI_GETMOUSEHOVERHEIGHT As Integer = 100
'    Public Const SPI_SETMOUSEHOVERHEIGHT As Integer = 101
'    Public Const SPI_GETMOUSEHOVERTIME As Integer = 102
'    Public Const SPI_SETMOUSEHOVERTIME As Integer = 103
'    Public Const SPI_GETWHEELSCROLLLINES As Integer = 104
'    Public Const SPI_SETWHEELSCROLLLINES As Integer = 105
'    Public Const SPI_GETKEYBOARDCUES As Integer = &H100A
'    Public Const SPI_GETMENUUNDERLINES As Integer = SPI_GETKEYBOARDCUES
'    Public Const SPIF_UPDATEINIFILE As Integer = &H1
'    Public Const SPIF_SENDWININICHANGE As Integer = &H2
'    Public Const SPIF_SENDCHANGE As Integer = &H2
'    Public Const SERKF_SERIALKEYSON As Integer = &H1
'    Public Const SERKF_AVAILABLE As Integer = &H2
'    Public Const SERKF_INDICATOR As Integer = &H4
'    Public Const SKF_STICKYKEYSON As Integer = &H1
'    Public Const SKF_AVAILABLE As Integer = &H2
'    Public Const SKF_HOTKEYACTIVE As Integer = &H4
'    Public Const SKF_CONFIRMHOTKEY As Integer = &H8
'    Public Const SKF_HOTKEYSOUND As Integer = &H10
'    Public Const SKF_INDICATOR As Integer = &H20
'    Public Const SKF_AUDIBLEFEEDBACK As Integer = &H40
'    Public Const SKF_TRISTATE As Integer = &H80
'    Public Const SKF_TWOKEYSOFF As Integer = &H100
'    Public Const SSGF_NONE As Integer = 0
'    Public Const SSGF_DISPLAY As Integer = 3
'    Public Const SSTF_NONE As Integer = 0
'    Public Const SSTF_CHARS As Integer = 1
'    Public Const SSTF_BORDER As Integer = 2
'    Public Const SSTF_DISPLAY As Integer = 3
'    Public Const SSWF_NONE As Integer = 0
'    Public Const SSWF_TITLE As Integer = 1
'    Public Const SSWF_WINDOW As Integer = 2
'    Public Const SSWF_DISPLAY As Integer = 3
'    Public Const SSWF_CUSTOM As Integer = 4
'    Public Const SSF_SOUNDSENTRYON As Integer = &H1
'    Public Const SSF_AVAILABLE As Integer = &H2
'    Public Const SSF_INDICATOR As Integer = &H4
'    Public Const SLE_ERROR As Integer = &H1
'    Public Const SLE_MINORERROR As Integer = &H2
'    Public Const SLE_WARNING As Integer = &H3
'    Public Const STD_CUT As Integer = 0
'    Public Const STD_COPY As Integer = 1
'    Public Const STD_PASTE As Integer = 2
'    Public Const STD_UNDO As Integer = 3
'    Public Const STD_REDOW As Integer = 4
'    Public Const STD_DELETE As Integer = 5
'    Public Const STD_FILENEW As Integer = 6
'    Public Const STD_FILEOPEN As Integer = 7
'    Public Const STD_FILESAVE As Integer = 8
'    Public Const STD_PRINTPRE As Integer = 9
'    Public Const STD_PROPERTIES As Integer = 10
'    Public Const STD_HELP As Integer = 11
'    Public Const STD_FIND As Integer = 12
'    Public Const STD_REPLACE As Integer = 13
'    Public Const STD_PRINT As Integer = 14
'    Public Const SBARS_SIZEGRIP As Integer = &H100
'    Public Const SB_SETTEXTA As Integer = &H400 + 1
'    Public Const SB_SETTEXTW As Integer = &H400 + 11
'    Public Const SB_GETTEXTA As Integer = &H400 + 2
'    Public Const SB_GETTEXTW As Integer = &H400 + 13
'    Public Const SB_GETTEXTLENGTHA As Integer = &H400 + 3
'    Public Const SB_GETTEXTLENGTHW As Integer = &H400 + 12
'    Public Const SB_SETPARTS As Integer = &H400 + 4
'    Public Const SB_GETPARTS As Integer = &H400 + 6
'    Public Const SB_GETBORDERS As Integer = &H400 + 7
'    Public Const SB_SETMINHEIGHT As Integer = &H400 + 8
'    Public Const SB_SIMPLE As Integer = &H400 + 9
'    Public Const SB_GETRECT As Integer = &H400 + 10
'    Public Const SB_ISSIMPLE As Integer = &H400 + 14
'    Public Const SB_SETICON As Integer = &H400 + 15
'    Public Const SB_SETTIPTEXTA As Integer = &H400 + 16
'    Public Const SB_SETTIPTEXTW As Integer = &H400 + 17
'    Public Const SB_GETTIPTEXTA As Integer = &H400 + 18
'    Public Const SB_GETTIPTEXTW As Integer = &H400 + 19
'    Public Const SB_GETICON As Integer = &H400 + 20
'    Public Const SB_SETBKCOLOR As Integer = win.CCM_SETBKCOLOR
'    Public Const SB_SETUNICODEFORMAT As Integer = win.CCM_SETUNICODEFORMAT
'    Public Const SB_GETUNICODEFORMAT As Integer = win.CCM_GETUNICODEFORMAT
'    Public Const SBT_OWNERDRAW As Integer = &H1000
'    Public Const SBT_NOBORDERS As Integer = &H100
'    Public Const SBT_POPOUT As Integer = &H200
'    Public Const SBT_RTLREADING As Integer = &H400
'    Public Const SBT_TOOLTIPS As Integer = &H800
'    Public Const SC_GROUP_IDENTIFIER As Char = "+"c
'    Public Const SYSRGN As Integer = 4
'    Public Const STILL_ACTIVE As Integer = &H103
'    Public Const SEMAPHORE_ALL_ACCESS As Integer = &HF0000 Or &H100000 Or &H3
'    Public Const SRCCOPY As Integer = &HCC0020
'    Public Const SRCPAINT As Integer = &HEE0086
'    Public Const SRCAND As Integer = &H8800C6
'    Public Const SRCINVERT As Integer = &H660046
'    Public Const SRCERASE As Integer = &H440328
'    Public Const STD_INPUT_HANDLE As Integer = - 10
'    Public Const STD_OUTPUT_HANDLE As Integer = - 11
'    Public Const STD_ERROR_HANDLE As Integer = - 12
'    Public Const SBN_FIRST As Integer = 0 - 880
'    Public Const SBN_LAST As Integer = 0 - 899
'    Public Const SBN_SIMPLEMODECHANGE As Integer = SBN_FIRST - 0
    
'    Public Const S_OK As Integer = &H0
'    Public Const S_FALSE As Integer = &H1
    
    
        '    Public  Shared Function Succeeded(hr As Integer) As Boolean
'        Return hr >= 0
'    End Function 'Succeeded
    
    
        '    Public  Shared Function Failed(hr As Integer) As Boolean
'        Return hr < 0
'    End Function 'Failed
    
    
'    Public Const [TRUE] As Boolean = True
'    Public Const TIMEOUT_ASYNC As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        public const bool TRUE = true;
'    '        public const int TIMEOUT_ASYNC = unchecked((int)0xFFFFFFFF),
'    '------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const TIME_MS As Integer = &H1
'    Public Const TIME_SAMPLES As Integer = &H2
'    Public Const TIME_BYTES As Integer = &H4
'    Public Const TIME_SMPTE As Integer = &H8
'    Public Const TIME_MIDI As Integer = &H10
'    Public Const TIME_TICKS As Integer = &H20
'    Public Const TIMERR_BASE As Integer = 96
'    Public Const TIMERR_NOERROR As Integer = 0
'    Public Const TIMERR_NOCANDO As Integer = 96 + 1
'    Public Const TIMERR_STRUCT As Integer = 96 + 33
'    Public Const TIME_ONESHOT As Integer = &H0
'    Public Const TIME_PERIODIC As Integer = &H1
'    Public Const TIME_CALLBACK_FUNCTION As Integer = &H0
'    Public Const TIME_CALLBACK_EVENT_SET As Integer = &H10
'    Public Const TIME_CALLBACK_EVENT_PULSE As Integer = &H20
'    Public Const TCP_BSDURGENT As Integer = &H7000
'    Public Const TF_DISCONNECT As Integer = &H1
'    Public Const TF_REUSE_SOCKET As Integer = &H2
'    Public Const TF_WRITE_BEHIND As Integer = &H4
'    Public Const TRANSPORT_TYPE_CN As Integer = &H1
'    Public Const TRANSPORT_TYPE_DG As Integer = &H2
'    Public Const TRANSPORT_TYPE_LPC As Integer = &H4
'    Public Const TRANSPORT_TYPE_WMSG As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TRANSPORT_TYPE_LPC = 0x4,
'    '        TRANSPORT_TYPE_WMSG = unchecked((int)0x8);
'    '-------------------------------^--- GenCode(token): unexpected token type
'    Public Const TRUNCATE_EXISTING As Integer = 5
'    Public Const THREAD_PRIORITY_NORMAL As Integer = 0
'    Public Const TWOSTOPBITS As Integer = 2
'    Public Const TC_NORMAL As Integer = 0
'    Public Const TC_HARDERR As Integer = 1
'    Public Const TC_GP_TRAP As Integer = 2
'    Public Const TC_SIGNAL As Integer = 3
     Public Const [TRUE] As Integer = 1
'    Public Const TYPE_E_BUFFERTOOSMALL As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        // TRUE = 1;
'    '        public const int TYPE_E_BUFFERTOOSMALL = unchecked((int)0x80028016),
'    '--------------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const TYPE_E_INVDATAREAD As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        public const int TYPE_E_BUFFERTOOSMALL = unchecked((int)0x80028016),
'    '        TYPE_E_INVDATAREAD = unchecked((int)0x80028018),
'    '------------------------------^--- GenCode(token): unexpected token type
'    Public Const TYPE_E_UNSUPFORMAT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TYPE_E_INVDATAREAD = unchecked((int)0x80028018),
'    '        TYPE_E_UNSUPFORMAT = unchecked((int)0x80028019),
'    '------------------------------^--- GenCode(token): unexpected token type
'    Public Const TYPE_E_REGISTRYACCESS As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TYPE_E_UNSUPFORMAT = unchecked((int)0x80028019),
'    '        TYPE_E_REGISTRYACCESS = unchecked((int)0x8002801C),
'    '---------------------------------^--- GenCode(token): unexpected token type
'    Public Const TYPE_E_LIBNOTREGISTERED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TYPE_E_REGISTRYACCESS = unchecked((int)0x8002801C),
'    '        TYPE_E_LIBNOTREGISTERED = unchecked((int)0x8002801D),
'    '-----------------------------------^--- GenCode(token): unexpected token type
'    Public Const TYPE_E_UNDEFINEDTYPE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TYPE_E_LIBNOTREGISTERED = unchecked((int)0x8002801D),
'    '        TYPE_E_UNDEFINEDTYPE = unchecked((int)0x80028027),
'    '--------------------------------^--- GenCode(token): unexpected token type
'    Public Const TYPE_E_QUALIFIEDNAMEDISALLOWED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TYPE_E_UNDEFINEDTYPE = unchecked((int)0x80028027),
'    '        TYPE_E_QUALIFIEDNAMEDISALLOWED = unchecked((int)0x80028028),
'    '------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const TYPE_E_INVALIDSTATE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TYPE_E_QUALIFIEDNAMEDISALLOWED = unchecked((int)0x80028028),
'    '        TYPE_E_INVALIDSTATE = unchecked((int)0x80028029),
'    '-------------------------------^--- GenCode(token): unexpected token type
'    Public Const TYPE_E_WRONGTYPEKIND As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TYPE_E_INVALIDSTATE = unchecked((int)0x80028029),
'    '        TYPE_E_WRONGTYPEKIND = unchecked((int)0x8002802A),
'    '--------------------------------^--- GenCode(token): unexpected token type
'    Public Const TYPE_E_ELEMENTNOTFOUND As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TYPE_E_WRONGTYPEKIND = unchecked((int)0x8002802A),
'    '        TYPE_E_ELEMENTNOTFOUND = unchecked((int)0x8002802B),
'    '----------------------------------^--- GenCode(token): unexpected token type
'    Public Const TYPE_E_AMBIGUOUSNAME As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TYPE_E_ELEMENTNOTFOUND = unchecked((int)0x8002802B),
'    '        TYPE_E_AMBIGUOUSNAME = unchecked((int)0x8002802C),
'    '--------------------------------^--- GenCode(token): unexpected token type
'    Public Const TYPE_E_NAMECONFLICT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TYPE_E_AMBIGUOUSNAME = unchecked((int)0x8002802C),
'    '        TYPE_E_NAMECONFLICT = unchecked((int)0x8002802D),
'    '-------------------------------^--- GenCode(token): unexpected token type
'    Public Const TYPE_E_UNKNOWNLCID As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TYPE_E_NAMECONFLICT = unchecked((int)0x8002802D),
'    '        TYPE_E_UNKNOWNLCID = unchecked((int)0x8002802E),
'    '------------------------------^--- GenCode(token): unexpected token type
'    Public Const TYPE_E_DLLFUNCTIONNOTFOUND As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TYPE_E_UNKNOWNLCID = unchecked((int)0x8002802E),
'    '        TYPE_E_DLLFUNCTIONNOTFOUND = unchecked((int)0x8002802F),
'    '--------------------------------------^--- GenCode(token): unexpected token type
'    Public Const TYPE_E_BADMODULEKIND As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TYPE_E_DLLFUNCTIONNOTFOUND = unchecked((int)0x8002802F),
'    '        TYPE_E_BADMODULEKIND = unchecked((int)0x800288BD),
'    '--------------------------------^--- GenCode(token): unexpected token type
'    Public Const TYPE_E_SIZETOOBIG As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TYPE_E_BADMODULEKIND = unchecked((int)0x800288BD),
'    '        TYPE_E_SIZETOOBIG = unchecked((int)0x800288C5),
'    '-----------------------------^--- GenCode(token): unexpected token type
'    Public Const TYPE_E_DUPLICATEID As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TYPE_E_SIZETOOBIG = unchecked((int)0x800288C5),
'    '        TYPE_E_DUPLICATEID = unchecked((int)0x800288C6),
'    '------------------------------^--- GenCode(token): unexpected token type
'    Public Const TYPE_E_INVALIDID As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TYPE_E_DUPLICATEID = unchecked((int)0x800288C6),
'    '        TYPE_E_INVALIDID = unchecked((int)0x800288CF),
'    '----------------------------^--- GenCode(token): unexpected token type
'    Public Const TYPE_E_TYPEMISMATCH As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TYPE_E_INVALIDID = unchecked((int)0x800288CF),
'    '        TYPE_E_TYPEMISMATCH = unchecked((int)0x80028CA0),
'    '-------------------------------^--- GenCode(token): unexpected token type
'    Public Const TYPE_E_OUTOFBOUNDS As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TYPE_E_TYPEMISMATCH = unchecked((int)0x80028CA0),
'    '        TYPE_E_OUTOFBOUNDS = unchecked((int)0x80028CA1),
'    '------------------------------^--- GenCode(token): unexpected token type
'    Public Const TYPE_E_IOERROR As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TYPE_E_OUTOFBOUNDS = unchecked((int)0x80028CA1),
'    '        TYPE_E_IOERROR = unchecked((int)0x80028CA2),
'    '--------------------------^--- GenCode(token): unexpected token type
'    Public Const TYPE_E_CANTCREATETMPFILE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TYPE_E_IOERROR = unchecked((int)0x80028CA2),
'    '        TYPE_E_CANTCREATETMPFILE = unchecked((int)0x80028CA3),
'    '------------------------------------^--- GenCode(token): unexpected token type
'    Public Const TYPE_E_CANTLOADLIBRARY As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TYPE_E_CANTCREATETMPFILE = unchecked((int)0x80028CA3),
'    '        TYPE_E_CANTLOADLIBRARY = unchecked((int)0x80029C4A),
'    '----------------------------------^--- GenCode(token): unexpected token type
'    Public Const TYPE_E_INCONSISTENTPROPFUNCS As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TYPE_E_CANTLOADLIBRARY = unchecked((int)0x80029C4A),
'    '        TYPE_E_INCONSISTENTPROPFUNCS = unchecked((int)0x80029C83),
'    '----------------------------------------^--- GenCode(token): unexpected token type
'    Public Const TYPE_E_CIRCULARTYPE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TYPE_E_INCONSISTENTPROPFUNCS = unchecked((int)0x80029C83),
'    '        TYPE_E_CIRCULARTYPE = unchecked((int)0x80029C84),
'    '-------------------------------^--- GenCode(token): unexpected token type
'    Public Const TRUST_E_PROVIDER_UNKNOWN As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TYPE_E_CIRCULARTYPE = unchecked((int)0x80029C84),
'    '        TRUST_E_PROVIDER_UNKNOWN = unchecked((int)0x800B0001),
'    '------------------------------------^--- GenCode(token): unexpected token type
'    Public Const TRUST_E_ACTION_UNKNOWN As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TRUST_E_PROVIDER_UNKNOWN = unchecked((int)0x800B0001),
'    '        TRUST_E_ACTION_UNKNOWN = unchecked((int)0x800B0002),
'    '----------------------------------^--- GenCode(token): unexpected token type
'    Public Const TRUST_E_SUBJECT_FORM_UNKNOWN As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TRUST_E_ACTION_UNKNOWN = unchecked((int)0x800B0002),
'    '        TRUST_E_SUBJECT_FORM_UNKNOWN = unchecked((int)0x800B0003),
'    '----------------------------------------^--- GenCode(token): unexpected token type
'    Public Const TRUST_E_SUBJECT_NOT_TRUSTED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TRUST_E_SUBJECT_FORM_UNKNOWN = unchecked((int)0x800B0003),
'    '        TRUST_E_SUBJECT_NOT_TRUSTED = unchecked((int)0x800B0004),
'    '---------------------------------------^--- GenCode(token): unexpected token type
'    Public Const TRUST_E_NOSIGNATURE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TRUST_E_SUBJECT_NOT_TRUSTED = unchecked((int)0x800B0004),
'    '        TRUST_E_NOSIGNATURE = unchecked((int)0x800B0100),
'    '-------------------------------^--- GenCode(token): unexpected token type
'    Public Const TA_NOUPDATECP As Integer = 0
'    Public Const TA_UPDATECP As Integer = 1
'    Public Const TA_LEFT As Integer = 0
'    Public Const TA_RIGHT As Integer = 2
'    Public Const TA_CENTER As Integer = 6
'    Public Const TA_TOP As Integer = 0
'    Public Const TA_BOTTOM As Integer = 8
'    Public Const TA_BASELINE As Integer = 24
'    Public Const TA_RTLREADING As Integer = 256
'    Public Const TA_MASK As Integer = 24 + 6 + 1 + 256
'    ' TA_MASK = (24+6+1);
'    Public Const TRANSFORM_CTM As Integer = 4107
'    Public Const TCI_SRCCHARSET As Integer = 1
'    Public Const TCI_SRCCODEPAGE As Integer = 2
'    Public Const TCI_SRCFONTSIG As Integer = 3
'    Public Const TMPF_FIXED_PITCH As Integer = &H1
'    Public Const TMPF_VECTOR As Integer = &H2
'    Public Const TMPF_DEVICE As Integer = &H8
'    Public Const TMPF_TRUETYPE As Integer = &H4
'    Public Const TURKISH_CHARSET As Integer = 162
'    Public Const THAI_CHARSET As Integer = 222
'    Public Const TRUETYPE_FONTTYPE As Integer = &H4
'    Public Const TRANSPARENT As Integer = 1
'    Public Const TECHNOLOGY As Integer = 2
'    Public Const TEXTCAPS As Integer = 34
'    Public Const TC_OP_CHARACTER As Integer = &H1
'    Public Const TC_OP_STROKE As Integer = &H2
'    Public Const TC_CP_STROKE As Integer = &H4
'    Public Const TC_CR_90 As Integer = &H8
'    Public Const TC_CR_ANY As Integer = &H10
'    Public Const TC_SF_X_YINDEP As Integer = &H20
'    Public Const TC_SA_DOUBLE As Integer = &H40
'    Public Const TC_SA_INTEGER As Integer = &H80
'    Public Const TC_SA_CONTIN As Integer = &H100
'    Public Const TC_EA_DOUBLE As Integer = &H200
'    Public Const TC_IA_ABLE As Integer = &H400
'    Public Const TC_UA_ABLE As Integer = &H800
'    Public Const TC_SO_ABLE As Integer = &H1000
'    Public Const TC_RA_ABLE As Integer = &H2000
'    Public Const TC_VA_ABLE As Integer = &H4000
'    Public Const TC_RESERVED As Integer = &H8000
'    Public Const TC_SCROLLBLT As Integer = &H10000
'    Public Const TT_POLYGON_TYPE As Integer = 24
'    Public Const TT_PRIM_LINE As Integer = 1
'    Public Const TT_PRIM_QSPLINE As Integer = 2
'    Public Const TT_AVAILABLE As Integer = &H1
'    Public Const TT_ENABLED As Integer = &H2
'    Public Const TIME_NOMINUTESORSECONDS As Integer = &H1
'    Public Const TIME_NOSECONDS As Integer = &H2
'    Public Const TIME_NOTIMEMARKER As Integer = &H4
'    Public Const TIME_FORCE24HOURFORMAT As Integer = &H8
'    Public Const THREAD_TERMINATE As Integer = &H1
'    Public Const THREAD_SUSPEND_RESUME As Integer = &H2
'    Public Const THREAD_GET_CONTEXT As Integer = &H8
'    Public Const THREAD_SET_CONTEXT As Integer = &H10
'    Public Const THREAD_SET_INFORMATION As Integer = &H20
'    Public Const THREAD_QUERY_INFORMATION As Integer = &H40
'    Public Const THREAD_SET_THREAD_TOKEN As Integer = &H80
'    Public Const THREAD_IMPERSONATE As Integer = &H100
'    Public Const THREAD_DIRECT_IMPERSONATION As Integer = &H200
'    Public Const TLS_MINIMUM_AVAILABLE As Integer = 64
'    Public Const THREAD_BASE_PRIORITY_LOWRT As Integer = 15
'    Public Const THREAD_BASE_PRIORITY_MAX As Integer = 2
'    Public Const THREAD_BASE_PRIORITY_MIN As Integer = - 2
'    Public Const THREAD_BASE_PRIORITY_IDLE As Integer = - 15
'    Public Const TIME_ZONE_ID_UNKNOWN As Integer = 0
'    Public Const TIME_ZONE_ID_STANDARD As Integer = 1
'    Public Const TIME_ZONE_ID_DAYLIGHT As Integer = 2
'    Public Const TOKEN_ASSIGN_PRIMARY As Integer = &H1
'    Public Const TOKEN_DUPLICATE As Integer = &H2
'    Public Const TOKEN_IMPERSONATE As Integer = &H4
'    Public Const TOKEN_QUERY As Integer = &H8
'    Public Const TOKEN_QUERY_SOURCE As Integer = &H10
'    Public Const TOKEN_ADJUST_PRIVILEGES As Integer = &H20
'    Public Const TOKEN_ADJUST_GROUPS As Integer = &H40
'    Public Const TOKEN_ADJUST_DEFAULT As Integer = &H80
'    Public Const TOKEN_EXECUTE As Integer = &H20000
'    Public Const TOKEN_SOURCE_LENGTH As Integer = 8
'    Public Const TAPE_ERASE_SHORT As Integer = 0
'    Public Const TAPE_ERASE_LONG As Integer = 1
'    Public Const TAPE_LOAD As Integer = 0
'    Public Const TAPE_UNLOAD As Integer = 1
'    Public Const TAPE_TENSION As Integer = 2
'    Public Const TAPE_LOCK As Integer = 3
'    Public Const TAPE_UNLOCK As Integer = 4
'    Public Const TAPE_FORMAT As Integer = 5
'    Public Const TAPE_SETMARKS As Integer = 0
'    Public Const TAPE_FILEMARKS As Integer = 1
'    Public Const TAPE_SHORT_FILEMARKS As Integer = 2
'    Public Const TAPE_LONG_FILEMARKS As Integer = 3
'    Public Const TAPE_ABSOLUTE_POSITION As Integer = 0
'    Public Const TAPE_LOGICAL_POSITION As Integer = 1
'    Public Const TAPE_PSEUDO_LOGICAL_POSITION As Integer = 2
'    Public Const TAPE_REWIND As Integer = 0
'    Public Const TAPE_ABSOLUTE_BLOCK As Integer = 1
'    Public Const TAPE_LOGICAL_BLOCK As Integer = 2
'    Public Const TAPE_PSEUDO_LOGICAL_BLOCK As Integer = 3
'    Public Const TAPE_SPACE_END_OF_DATA As Integer = 4
'    Public Const TAPE_SPACE_RELATIVE_BLOCKS As Integer = 5
'    Public Const TAPE_SPACE_FILEMARKS As Integer = 6
'    Public Const TAPE_SPACE_SEQUENTIAL_FMKS As Integer = 7
'    Public Const TAPE_SPACE_SETMARKS As Integer = 8
'    Public Const TAPE_SPACE_SEQUENTIAL_SMKS As Integer = 9
'    Public Const TAPE_DRIVE_FIXED As Integer = &H1
'    Public Const TAPE_DRIVE_SELECT As Integer = &H2
'    Public Const TAPE_DRIVE_INITIATOR As Integer = &H4
'    Public Const TAPE_DRIVE_ERASE_SHORT As Integer = &H10
'    Public Const TAPE_DRIVE_ERASE_LONG As Integer = &H20
'    Public Const TAPE_DRIVE_ERASE_BOP_ONLY As Integer = &H40
'    Public Const TAPE_DRIVE_ERASE_IMMEDIATE As Integer = &H80
'    Public Const TAPE_DRIVE_TAPE_CAPACITY As Integer = &H100
'    Public Const TAPE_DRIVE_TAPE_REMAINING As Integer = &H200
'    Public Const TAPE_DRIVE_FIXED_BLOCK As Integer = &H400
'    Public Const TAPE_DRIVE_VARIABLE_BLOCK As Integer = &H800
'    Public Const TAPE_DRIVE_WRITE_PROTECT As Integer = &H1000
'    Public Const TAPE_DRIVE_EOT_WZ_SIZE As Integer = &H2000
'    Public Const TAPE_DRIVE_ECC As Integer = &H10000
'    Public Const TAPE_DRIVE_COMPRESSION As Integer = &H20000
'    Public Const TAPE_DRIVE_PADDING As Integer = &H40000
'    Public Const TAPE_DRIVE_REPORT_SMKS As Integer = &H80000
'    Public Const TAPE_DRIVE_GET_ABSOLUTE_BLK As Integer = &H100000
'    Public Const TAPE_DRIVE_GET_LOGICAL_BLK As Integer = &H200000
'    Public Const TAPE_DRIVE_SET_EOT_WZ_SIZE As Integer = &H400000
'    Public Const TAPE_DRIVE_EJECT_MEDIA As Integer = &H1000000
'    Public Const TAPE_DRIVE_RESERVED_BIT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TAPE_DRIVE_EJECT_MEDIA = 0x01000000,
'    '        TAPE_DRIVE_RESERVED_BIT = unchecked((int)0x80000000),
'    '-----------------------------------^--- GenCode(token): unexpected token type
'    Public Const TAPE_DRIVE_LOAD_UNLOAD As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TAPE_DRIVE_RESERVED_BIT = unchecked((int)0x80000000),
'    '        TAPE_DRIVE_LOAD_UNLOAD = unchecked((int)0x80000001),
'    '----------------------------------^--- GenCode(token): unexpected token type
'    Public Const TAPE_DRIVE_TENSION As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TAPE_DRIVE_LOAD_UNLOAD = unchecked((int)0x80000001),
'    '        TAPE_DRIVE_TENSION = unchecked((int)0x80000002),
'    '------------------------------^--- GenCode(token): unexpected token type
'    Public Const TAPE_DRIVE_LOCK_UNLOCK As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TAPE_DRIVE_TENSION = unchecked((int)0x80000002),
'    '        TAPE_DRIVE_LOCK_UNLOCK = unchecked((int)0x80000004),
'    '----------------------------------^--- GenCode(token): unexpected token type
'    Public Const TAPE_DRIVE_REWIND_IMMEDIATE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TAPE_DRIVE_LOCK_UNLOCK = unchecked((int)0x80000004),
'    '        TAPE_DRIVE_REWIND_IMMEDIATE = unchecked((int)0x80000008),
'    '---------------------------------------^--- GenCode(token): unexpected token type
'    Public Const TAPE_DRIVE_SET_BLOCK_SIZE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TAPE_DRIVE_REWIND_IMMEDIATE = unchecked((int)0x80000008),
'    '        TAPE_DRIVE_SET_BLOCK_SIZE = unchecked((int)0x80000010),
'    '-------------------------------------^--- GenCode(token): unexpected token type
'    Public Const TAPE_DRIVE_LOAD_UNLD_IMMED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TAPE_DRIVE_SET_BLOCK_SIZE = unchecked((int)0x80000010),
'    '        TAPE_DRIVE_LOAD_UNLD_IMMED = unchecked((int)0x80000020),
'    '--------------------------------------^--- GenCode(token): unexpected token type
'    Public Const TAPE_DRIVE_TENSION_IMMED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TAPE_DRIVE_LOAD_UNLD_IMMED = unchecked((int)0x80000020),
'    '        TAPE_DRIVE_TENSION_IMMED = unchecked((int)0x80000040),
'    '------------------------------------^--- GenCode(token): unexpected token type
'    Public Const TAPE_DRIVE_LOCK_UNLK_IMMED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TAPE_DRIVE_TENSION_IMMED = unchecked((int)0x80000040),
'    '        TAPE_DRIVE_LOCK_UNLK_IMMED = unchecked((int)0x80000080),
'    '--------------------------------------^--- GenCode(token): unexpected token type
'    Public Const TAPE_DRIVE_SET_ECC As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TAPE_DRIVE_LOCK_UNLK_IMMED = unchecked((int)0x80000080),
'    '        TAPE_DRIVE_SET_ECC = unchecked((int)0x80000100),
'    '------------------------------^--- GenCode(token): unexpected token type
'    Public Const TAPE_DRIVE_SET_COMPRESSION As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TAPE_DRIVE_SET_ECC = unchecked((int)0x80000100),
'    '        TAPE_DRIVE_SET_COMPRESSION = unchecked((int)0x80000200),
'    '--------------------------------------^--- GenCode(token): unexpected token type
'    Public Const TAPE_DRIVE_SET_PADDING As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TAPE_DRIVE_SET_COMPRESSION = unchecked((int)0x80000200),
'    '        TAPE_DRIVE_SET_PADDING = unchecked((int)0x80000400),
'    '----------------------------------^--- GenCode(token): unexpected token type
'    Public Const TAPE_DRIVE_SET_REPORT_SMKS As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TAPE_DRIVE_SET_PADDING = unchecked((int)0x80000400),
'    '        TAPE_DRIVE_SET_REPORT_SMKS = unchecked((int)0x80000800),
'    '--------------------------------------^--- GenCode(token): unexpected token type
'    Public Const TAPE_DRIVE_ABSOLUTE_BLK As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TAPE_DRIVE_SET_REPORT_SMKS = unchecked((int)0x80000800),
'    '        TAPE_DRIVE_ABSOLUTE_BLK = unchecked((int)0x80001000),
'    '-----------------------------------^--- GenCode(token): unexpected token type
'    Public Const TAPE_DRIVE_ABS_BLK_IMMED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TAPE_DRIVE_ABSOLUTE_BLK = unchecked((int)0x80001000),
'    '        TAPE_DRIVE_ABS_BLK_IMMED = unchecked((int)0x80002000),
'    '------------------------------------^--- GenCode(token): unexpected token type
'    Public Const TAPE_DRIVE_LOGICAL_BLK As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TAPE_DRIVE_ABS_BLK_IMMED = unchecked((int)0x80002000),
'    '        TAPE_DRIVE_LOGICAL_BLK = unchecked((int)0x80004000),
'    '----------------------------------^--- GenCode(token): unexpected token type
'    Public Const TAPE_DRIVE_LOG_BLK_IMMED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TAPE_DRIVE_LOGICAL_BLK = unchecked((int)0x80004000),
'    '        TAPE_DRIVE_LOG_BLK_IMMED = unchecked((int)0x80008000),
'    '------------------------------------^--- GenCode(token): unexpected token type
'    Public Const TAPE_DRIVE_END_OF_DATA As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TAPE_DRIVE_LOG_BLK_IMMED = unchecked((int)0x80008000),
'    '        TAPE_DRIVE_END_OF_DATA = unchecked((int)0x80010000),
'    '----------------------------------^--- GenCode(token): unexpected token type
'    Public Const TAPE_DRIVE_RELATIVE_BLKS As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TAPE_DRIVE_END_OF_DATA = unchecked((int)0x80010000),
'    '        TAPE_DRIVE_RELATIVE_BLKS = unchecked((int)0x80020000),
'    '------------------------------------^--- GenCode(token): unexpected token type
'    Public Const TAPE_DRIVE_FILEMARKS As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TAPE_DRIVE_RELATIVE_BLKS = unchecked((int)0x80020000),
'    '        TAPE_DRIVE_FILEMARKS = unchecked((int)0x80040000),
'    '--------------------------------^--- GenCode(token): unexpected token type
'    Public Const TAPE_DRIVE_SEQUENTIAL_FMKS As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TAPE_DRIVE_FILEMARKS = unchecked((int)0x80040000),
'    '        TAPE_DRIVE_SEQUENTIAL_FMKS = unchecked((int)0x80080000),
'    '--------------------------------------^--- GenCode(token): unexpected token type
'    Public Const TAPE_DRIVE_SETMARKS As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TAPE_DRIVE_SEQUENTIAL_FMKS = unchecked((int)0x80080000),
'    '        TAPE_DRIVE_SETMARKS = unchecked((int)0x80100000),
'    '-------------------------------^--- GenCode(token): unexpected token type
'    Public Const TAPE_DRIVE_SEQUENTIAL_SMKS As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TAPE_DRIVE_SETMARKS = unchecked((int)0x80100000),
'    '        TAPE_DRIVE_SEQUENTIAL_SMKS = unchecked((int)0x80200000),
'    '--------------------------------------^--- GenCode(token): unexpected token type
'    Public Const TAPE_DRIVE_REVERSE_POSITION As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TAPE_DRIVE_SEQUENTIAL_SMKS = unchecked((int)0x80200000),
'    '        TAPE_DRIVE_REVERSE_POSITION = unchecked((int)0x80400000),
'    '---------------------------------------^--- GenCode(token): unexpected token type
'    Public Const TAPE_DRIVE_SPACE_IMMEDIATE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TAPE_DRIVE_REVERSE_POSITION = unchecked((int)0x80400000),
'    '        TAPE_DRIVE_SPACE_IMMEDIATE = unchecked((int)0x80800000),
'    '--------------------------------------^--- GenCode(token): unexpected token type
'    Public Const TAPE_DRIVE_WRITE_SETMARKS As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TAPE_DRIVE_SPACE_IMMEDIATE = unchecked((int)0x80800000),
'    '        TAPE_DRIVE_WRITE_SETMARKS = unchecked((int)0x81000000),
'    '-------------------------------------^--- GenCode(token): unexpected token type
'    Public Const TAPE_DRIVE_WRITE_FILEMARKS As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TAPE_DRIVE_WRITE_SETMARKS = unchecked((int)0x81000000),
'    '        TAPE_DRIVE_WRITE_FILEMARKS = unchecked((int)0x82000000),
'    '--------------------------------------^--- GenCode(token): unexpected token type
'    Public Const TAPE_DRIVE_WRITE_SHORT_FMKS As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TAPE_DRIVE_WRITE_FILEMARKS = unchecked((int)0x82000000),
'    '        TAPE_DRIVE_WRITE_SHORT_FMKS = unchecked((int)0x84000000),
'    '---------------------------------------^--- GenCode(token): unexpected token type
'    Public Const TAPE_DRIVE_WRITE_LONG_FMKS As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TAPE_DRIVE_WRITE_SHORT_FMKS = unchecked((int)0x84000000),
'    '        TAPE_DRIVE_WRITE_LONG_FMKS = unchecked((int)0x88000000),
'    '--------------------------------------^--- GenCode(token): unexpected token type
'    Public Const TAPE_DRIVE_WRITE_MARK_IMMED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TAPE_DRIVE_WRITE_LONG_FMKS = unchecked((int)0x88000000),
'    '        TAPE_DRIVE_WRITE_MARK_IMMED = unchecked((int)0x90000000),
'    '---------------------------------------^--- GenCode(token): unexpected token type
'    Public Const TAPE_DRIVE_FORMAT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TAPE_DRIVE_WRITE_MARK_IMMED = unchecked((int)0x90000000),
'    '        TAPE_DRIVE_FORMAT = unchecked((int)0xA0000000),
'    '-----------------------------^--- GenCode(token): unexpected token type
'    Public Const TAPE_DRIVE_FORMAT_IMMEDIATE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TAPE_DRIVE_FORMAT = unchecked((int)0xA0000000),
'    '        TAPE_DRIVE_FORMAT_IMMEDIATE = unchecked((int)0xC0000000),
'    '---------------------------------------^--- GenCode(token): unexpected token type
'    Public Const TAPE_DRIVE_HIGH_FEATURES As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TAPE_DRIVE_FORMAT_IMMEDIATE = unchecked((int)0xC0000000),
'    '        TAPE_DRIVE_HIGH_FEATURES = unchecked((int)0x80000000),
'    '------------------------------------^--- GenCode(token): unexpected token type
'    Public Const TAPE_FIXED_PARTITIONS As Integer = 0
'    Public Const TAPE_SELECT_PARTITIONS As Integer = 1
'    Public Const TAPE_INITIATOR_PARTITIONS As Integer = 2
'    Public Const TME_HOVER As Integer = &H1
'    Public Const TME_LEAVE As Integer = &H2
'    Public Const TME_QUERY As Integer = &H40000000
'    Public Const TME_CANCEL As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TME_QUERY = 0x40000000,
'    '        TME_CANCEL = unchecked((int)0x80000000),
'    '----------------------^--- GenCode(token): unexpected token type
'    Public Const TPM_LEFTBUTTON As Integer = &H0
'    Public Const TPM_RIGHTBUTTON As Integer = &H2
'    Public Const TPM_LEFTALIGN As Integer = &H0
'    Public Const TPM_CENTERALIGN As Integer = &H4
'    Public Const TPM_RIGHTALIGN As Integer = &H8
'    Public Const TPM_TOPALIGN As Integer = &H0
'    Public Const TPM_VCENTERALIGN As Integer = &H10
'    Public Const TPM_BOTTOMALIGN As Integer = &H20
'    Public Const TPM_HORIZONTAL As Integer = &H0
'    Public Const TPM_VERTICAL As Integer = &H40
'    Public Const TPM_NONOTIFY As Integer = &H80
'    Public Const TPM_RETURNCMD As Integer = &H100
'    Public Const TKF_TOGGLEKEYSON As Integer = &H1
'    Public Const TKF_AVAILABLE As Integer = &H2
'    Public Const TKF_HOTKEYACTIVE As Integer = &H4
'    Public Const TKF_CONFIRMHOTKEY As Integer = &H8
'    Public Const TKF_HOTKEYSOUND As Integer = &H10
'    Public Const TKF_INDICATOR As Integer = &H20
'    Public Const TV_FIRST As Integer = &H1100
'    Public Const TVN_FIRST As Integer = 0 - 400
'    Public Const TVN_LAST As Integer = 0 - 499
'    Public Const TTN_FIRST As Integer = 0 - 520
'    Public Const TTN_LAST As Integer = 0 - 549
'    Public Const TCN_FIRST As Integer = 0 - 550
'    Public Const TCN_LAST As Integer = 0 - 580
'    Public Const TBN_FIRST As Integer = 0 - 700
'    Public Const TBN_LAST As Integer = 0 - 720
'    Public Const TBSTATE_CHECKED As Integer = &H1
'    Public Const TBSTATE_PRESSED As Integer = &H2
'    Public Const TBSTATE_ENABLED As Integer = &H4
'    Public Const TBSTATE_HIDDEN As Integer = &H8
'    Public Const TBSTATE_INDETERMINATE As Integer = &H10
'    Public Const TBSTATE_WRAP As Integer = &H20
'    Public Const TBSTATE_ELLIPSES As Integer = &H40
'    Public Const TBSTATE_MARKED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TBSTATE_ELLIPSES = 0x40,
'    '        TBSTATE_MARKED = unchecked((int)0x80),
'    '--------------------------^--- GenCode(token): unexpected token type
'    Public Const TBSTYLE_BUTTON As Integer = &H0
'    Public Const TBSTYLE_SEP As Integer = &H1
'    Public Const TBSTYLE_CHECK As Integer = &H2
'    Public Const TBSTYLE_GROUP As Integer = &H4
'    Public Const TBSTYLE_CHECKGROUP As Integer = &H4 Or &H2
'    Public Const TBSTYLE_DROPDOWN As Integer = &H8
'    Public Const TBSTYLE_AUTOSIZE As Integer = &H10
'    Public Const TBSTYLE_NOPREFIX As Integer = &H20
'    Public Const TBSTYLE_TOOLTIPS As Integer = &H100
'    Public Const TBSTYLE_WRAPABLE As Integer = &H200
'    Public Const TBSTYLE_ALTDRAG As Integer = &H400
'    Public Const TBSTYLE_FLAT As Integer = &H800
'    Public Const TBSTYLE_LIST As Integer = &H1000
'    Public Const TBSTYLE_CUSTOMERASE As Integer = &H2000
'    Public Const TBSTYLE_REGISTERDROP As Integer = &H4000
'    Public Const TBSTYLE_TRANSPARENT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TBSTYLE_REGISTERDROP = 0x4000,
'    '        TBSTYLE_TRANSPARENT = unchecked((int)0x8000),
'    '-------------------------------^--- GenCode(token): unexpected token type
'    Public Const TBSTYLE_EX_DRAWDDARROWS As Integer = &H1
'    Public Const TB_ENABLEBUTTON As Integer = &H400 + 1
'    Public Const TB_CHECKBUTTON As Integer = &H400 + 2
'    Public Const TB_PRESSBUTTON As Integer = &H400 + 3
'    Public Const TB_HIDEBUTTON As Integer = &H400 + 4
'    Public Const TB_INDETERMINATE As Integer = &H400 + 5
'    Public Const TB_MARKBUTTON As Integer = &H400 + 6
'    Public Const TB_ISBUTTONENABLED As Integer = &H400 + 9
'    Public Const TB_ISBUTTONCHECKED As Integer = &H400 + 10
'    Public Const TB_ISBUTTONPRESSED As Integer = &H400 + 11
'    Public Const TB_ISBUTTONHIDDEN As Integer = &H400 + 12
'    Public Const TB_ISBUTTONINDETERMINATE As Integer = &H400 + 13
'    Public Const TB_ISBUTTONHIGHLIGHTED As Integer = &H400 + 14
'    Public Const TB_SETSTATE As Integer = &H400 + 17
'    Public Const TB_GETSTATE As Integer = &H400 + 18
'    Public Const TB_ADDBITMAP As Integer = &H400 + 19
'    Public Const TB_ADDBUTTONSA As Integer = &H400 + 20
'    Public Const TB_ADDBUTTONSW As Integer = &H400 + 68
'    Public Const TB_INSERTBUTTONA As Integer = &H400 + 21
'    Public Const TB_INSERTBUTTONW As Integer = &H400 + 67
'    Public Const TB_DELETEBUTTON As Integer = &H400 + 22
'    Public Const TB_GETBUTTON As Integer = &H400 + 23
'    Public Const TB_BUTTONCOUNT As Integer = &H400 + 24
'    Public Const TB_COMMANDTOINDEX As Integer = &H400 + 25
'    Public Const TB_SAVERESTOREA As Integer = &H400 + 26
'    Public Const TB_SAVERESTOREW As Integer = &H400 + 76
'    Public Const TB_CUSTOMIZE As Integer = &H400 + 27
'    Public Const TB_ADDSTRINGA As Integer = &H400 + 28
'    Public Const TB_ADDSTRINGW As Integer = &H400 + 77
'    Public Const TB_GETITEMRECT As Integer = &H400 + 29
'    Public Const TB_BUTTONSTRUCTSIZE As Integer = &H400 + 30
'    Public Const TB_SETBUTTONSIZE As Integer = &H400 + 31
'    Public Const TB_SETBITMAPSIZE As Integer = &H400 + 32
'    Public Const TB_AUTOSIZE As Integer = &H400 + 33
'    Public Const TB_GETTOOLTIPS As Integer = &H400 + 35
'    Public Const TB_SETTOOLTIPS As Integer = &H400 + 36
'    Public Const TB_SETPARENT As Integer = &H400 + 37
'    Public Const TB_SETROWS As Integer = &H400 + 39
'    Public Const TB_GETROWS As Integer = &H400 + 40
'    Public Const TB_GETBITMAPFLAGS As Integer = &H400 + 41
'    Public Const TB_SETCMDID As Integer = &H400 + 42
'    Public Const TB_CHANGEBITMAP As Integer = &H400 + 43
'    Public Const TB_GETBITMAP As Integer = &H400 + 44
'    Public Const TB_GETBUTTONTEXTA As Integer = &H400 + 45
'    Public Const TB_GETBUTTONTEXTW As Integer = &H400 + 75
'    Public Const TB_REPLACEBITMAP As Integer = &H400 + 46
'    Public Const TB_SETINDENT As Integer = &H400 + 47
'    Public Const TB_SETIMAGELIST As Integer = &H400 + 48
'    Public Const TB_GETIMAGELIST As Integer = &H400 + 49
'    Public Const TB_LOADIMAGES As Integer = &H400 + 50
'    Public Const TB_GETRECT As Integer = &H400 + 51
'    Public Const TB_SETHOTIMAGELIST As Integer = &H400 + 52
'    Public Const TB_GETHOTIMAGELIST As Integer = &H400 + 53
'    Public Const TB_SETDISABLEDIMAGELIST As Integer = &H400 + 54
'    Public Const TB_GETDISABLEDIMAGELIST As Integer = &H400 + 55
'    Public Const TB_SETSTYLE As Integer = &H400 + 56
'    Public Const TB_GETSTYLE As Integer = &H400 + 57
'    Public Const TB_GETBUTTONSIZE As Integer = &H400 + 58
'    Public Const TB_SETBUTTONWIDTH As Integer = &H400 + 59
'    Public Const TB_SETMAXTEXTROWS As Integer = &H400 + 60
'    Public Const TB_GETTEXTROWS As Integer = &H400 + 61
'    Public Const TB_GETOBJECT As Integer = &H400 + 62
'    Public Const TB_GETBUTTONINFOW As Integer = &H400 + 63
'    Public Const TB_SETBUTTONINFOW As Integer = &H400 + 64
'    Public Const TB_GETBUTTONINFOA As Integer = &H400 + 65
'    Public Const TB_SETBUTTONINFOA As Integer = &H400 + 66
'    Public Const TB_HITTEST As Integer = &H400 + 69
'    Public Const TB_GETHOTITEM As Integer = &H400 + 71
'    Public Const TB_SETHOTITEM As Integer = &H400 + 72
'    Public Const TB_SETANCHORHIGHLIGHT As Integer = &H400 + 73
'    Public Const TB_GETANCHORHIGHLIGHT As Integer = &H400 + 74
'    Public Const TB_MAPACCELERATORA As Integer = &H400 + 78
'    Public Const TB_GETINSERTMARK As Integer = &H400 + 79
'    Public Const TB_SETINSERTMARK As Integer = &H400 + 80
'    Public Const TB_INSERTMARKHITTEST As Integer = &H400 + 81
'    Public Const TB_MOVEBUTTON As Integer = &H400 + 82
'    Public Const TB_GETMAXSIZE As Integer = &H400 + 83
'    Public Const TB_SETEXTENDEDSTYLE As Integer = &H400 + 84
'    Public Const TB_GETEXTENDEDSTYLE As Integer = &H400 + 85
'    Public Const TB_GETPADDING As Integer = &H400 + 86
'    Public Const TB_SETPADDING As Integer = &H400 + 87
'    Public Const TB_SETINSERTMARKCOLOR As Integer = &H400 + 88
'    Public Const TB_GETINSERTMARKCOLOR As Integer = &H400 + 89
'    Public Const TB_MAPACCELERATORW As Integer = &H400 + 90
'    Public Const TB_SETCOLORSCHEME As Integer = win.CCM_SETCOLORSCHEME
'    Public Const TB_GETCOLORSCHEME As Integer = win.CCM_GETCOLORSCHEME
'    Public Const TB_SETUNICODEFORMAT As Integer = win.CCM_SETUNICODEFORMAT
'    Public Const TB_GETUNICODEFORMAT As Integer = win.CCM_GETUNICODEFORMAT
'    Public Const TBIMHT_AFTER As Integer = &H1
'    Public Const TBIMHT_BACKGROUND As Integer = &H2
'    Public Const TBIF_IMAGE As Integer = &H1
'    Public Const TBIF_TEXT As Integer = &H2
'    Public Const TBIF_STATE As Integer = &H4
'    Public Const TBIF_STYLE As Integer = &H8
'    Public Const TBIF_LPARAM As Integer = &H10
'    Public Const TBIF_COMMAND As Integer = &H20
'    Public Const TBIF_SIZE As Integer = &H40
'    Public Const TBBF_LARGE As Integer = &H1
'    Public Const TBN_GETBUTTONINFOA As Integer = 0 - 700 - 0
'    Public Const TBN_GETBUTTONINFOW As Integer = 0 - 700 - 20
'    Public Const TBN_BEGINDRAG As Integer = 0 - 700 - 1
'    Public Const TBN_ENDDRAG As Integer = 0 - 700 - 2
'    Public Const TBN_BEGINADJUST As Integer = 0 - 700 - 3
'    Public Const TBN_ENDADJUST As Integer = 0 - 700 - 4
'    Public Const TBN_RESET As Integer = 0 - 700 - 5
'    Public Const TBN_QUERYINSERT As Integer = 0 - 700 - 6
'    Public Const TBN_QUERYDELETE As Integer = 0 - 700 - 7
'    Public Const TBN_TOOLBARCHANGE As Integer = 0 - 700 - 8
'    Public Const TBN_CUSTHELP As Integer = 0 - 700 - 9
'    Public Const TBN_DROPDOWN As Integer = 0 - 700 - 10
'    Public Const TBN_CLOSEUP As Integer = 0 - 700 - 11
'    Public Const TBN_GETOBJECT As Integer = 0 - 700 - 12
'    Public Const TBN_HOTITEMCHANGE As Integer = 0 - 700 - 13
'    Public Const TBN_DRAGOUT As Integer = 0 - 700 - 14
'    Public Const TBN_DELETINGBUTTON As Integer = 0 - 700 - 15
'    Public Const TBN_GETDISPINFOA As Integer = 0 - 700 - 16
'    Public Const TBN_GETDISPINFOW As Integer = 0 - 700 - 17
'    Public Const TBN_GETINFOTIPA As Integer = 0 - 700 - 18
'    Public Const TBN_GETINFOTIPW As Integer = 0 - 700 - 19
'    Public Const TTS_ALWAYSTIP As Integer = &H1
'    Public Const TTS_NOPREFIX As Integer = &H2
'    Public Const TTF_IDISHWND As Integer = &H1
'    Public Const TTF_CENTERTIP As Integer = &H2
'    Public Const TTF_RTLREADING As Integer = &H4
'    Public Const TTF_SUBClass As Integer = &H10
'    Public Const TTF_TRACK As Integer = &H20
'    Public Const TTF_ABSOLUTE As Integer = &H80
'    Public Const TTF_TRANSPARENT As Integer = &H100
'    Public Const TTF_DI_SETITEM As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TTF_TRANSPARENT = 0x0100,
'    '        TTF_DI_SETITEM = unchecked((int)0x8000),
'    '--------------------------^--- GenCode(token): unexpected token type
'    Public Const TTDT_AUTOMATIC As Integer = 0
'    Public Const TTDT_RESHOW As Integer = 1
'    Public Const TTDT_AUTOPOP As Integer = 2
'    Public Const TTDT_INITIAL As Integer = 3
'    Public Const TTM_ACTIVATE As Integer = &H400 + 1
'    Public Const TTM_ADJUSTRECT As Integer = &H400 + 31
'    Public Const TTM_SETDELAYTIME As Integer = &H400 + 3
'    Public Const TTM_ADDTOOLA As Integer = &H400 + 4
'    Public Const TTM_ADDTOOLW As Integer = &H400 + 50
'    Public Const TTM_DELTOOLA As Integer = &H400 + 5
'    Public Const TTM_DELTOOLW As Integer = &H400 + 51
'    Public Const TTM_NEWTOOLRECTA As Integer = &H400 + 6
'    Public Const TTM_NEWTOOLRECTW As Integer = &H400 + 52
'    Public Const TTM_RELAYEVENT As Integer = &H400 + 7
'    Public Const TTM_GETTOOLINFOA As Integer = &H400 + 8
'    Public Const TTM_GETTOOLINFOW As Integer = &H400 + 53
'    Public Const TTM_SETTOOLINFOA As Integer = &H400 + 9
'    Public Const TTM_SETTOOLINFOW As Integer = &H400 + 54
'    Public Const TTM_HITTESTA As Integer = &H400 + 10
'    Public Const TTM_HITTESTW As Integer = &H400 + 55
'    Public Const TTM_GETTEXTA As Integer = &H400 + 11
'    Public Const TTM_GETTEXTW As Integer = &H400 + 56
'    Public Const TTM_UPDATE As Integer = &H400 + 29
'    Public Const TTM_UPDATETIPTEXTA As Integer = &H400 + 12
'    Public Const TTM_UPDATETIPTEXTW As Integer = &H400 + 57
'    Public Const TTM_GETTOOLCOUNT As Integer = &H400 + 13
'    Public Const TTM_ENUMTOOLSA As Integer = &H400 + 14
'    Public Const TTM_ENUMTOOLSW As Integer = &H400 + 58
'    Public Const TTM_GETCURRENTTOOLA As Integer = &H400 + 15
'    Public Const TTM_GETCURRENTTOOLW As Integer = &H400 + 59
'    Public Const TTM_WINDOWFROMPOINT As Integer = &H400 + 16
'    Public Const TTM_TRACKACTIVATE As Integer = &H400 + 17
'    Public Const TTM_TRACKPOSITION As Integer = &H400 + 18
'    Public Const TTM_SETTIPBKCOLOR As Integer = &H400 + 19
'    Public Const TTM_SETTIPTEXTCOLOR As Integer = &H400 + 20
'    Public Const TTM_GETDELAYTIME As Integer = &H400 + 21
'    Public Const TTM_GETTIPBKCOLOR As Integer = &H400 + 22
'    Public Const TTM_GETTIPTEXTCOLOR As Integer = &H400 + 23
'    Public Const TTM_SETMAXTIPWIDTH As Integer = &H400 + 24
'    Public Const TTM_GETMAXTIPWIDTH As Integer = &H400 + 25
'    Public Const TTM_SETMARGIN As Integer = &H400 + 26
'    Public Const TTM_GETMARGIN As Integer = &H400 + 27
'    Public Const TTM_POP As Integer = &H400 + 28
'    Public Const TTN_GETDISPINFOA As Integer = 0 - 520 - 0
'    Public Const TTN_GETDISPINFOW As Integer = 0 - 520 - 10
'    Public Const TTN_SHOW As Integer = 0 - 520 - 1
'    Public Const TTN_POP As Integer = 0 - 520 - 2
'    Public Const TTN_NEEDTEXTA As Integer = 0 - 520 - 0
'    Public Const TTN_NEEDTEXTW As Integer = 0 - 520 - 10
'    Public Const TBS_AUTOTICKS As Integer = &H1
'    Public Const TBS_VERT As Integer = &H2
'    Public Const TBS_HORZ As Integer = &H0
'    Public Const TBS_TOP As Integer = &H4
'    Public Const TBS_BOTTOM As Integer = &H0
'    Public Const TBS_LEFT As Integer = &H4
'    Public Const TBS_RIGHT As Integer = &H0
'    Public Const TBS_BOTH As Integer = &H8
'    Public Const TBS_NOTICKS As Integer = &H10
'    Public Const TBS_ENABLESELRANGE As Integer = &H20
'    Public Const TBS_FIXEDLENGTH As Integer = &H40
'    Public Const TBS_NOTHUMB As Integer = &H80
'    Public Const TBS_TOOLTIPS As Integer = &H100
'    Public Const TBM_GETPOS As Integer = &H400
'    Public Const TBM_GETRANGEMIN As Integer = &H400 + 1
'    Public Const TBM_GETRANGEMAX As Integer = &H400 + 2
'    Public Const TBM_GETTIC As Integer = &H400 + 3
'    Public Const TBM_SETTIC As Integer = &H400 + 4
'    Public Const TBM_SETPOS As Integer = &H400 + 5
'    Public Const TBM_SETRANGE As Integer = &H400 + 6
'    Public Const TBM_SETRANGEMIN As Integer = &H400 + 7
'    Public Const TBM_SETRANGEMAX As Integer = &H400 + 8
'    Public Const TBM_CLEARTICS As Integer = &H400 + 9
'    Public Const TBM_SETSEL As Integer = &H400 + 10
'    Public Const TBM_SETSELSTART As Integer = &H400 + 11
'    Public Const TBM_SETSELEND As Integer = &H400 + 12
'    Public Const TBM_GETPTICS As Integer = &H400 + 14
'    Public Const TBM_GETTICPOS As Integer = &H400 + 15
'    Public Const TBM_GETNUMTICS As Integer = &H400 + 16
'    Public Const TBM_GETSELSTART As Integer = &H400 + 17
'    Public Const TBM_GETSELEND As Integer = &H400 + 18
'    Public Const TBM_CLEARSEL As Integer = &H400 + 19
'    Public Const TBM_SETTICFREQ As Integer = &H400 + 20
'    Public Const TBM_SETPAGESIZE As Integer = &H400 + 21
'    Public Const TBM_GETPAGESIZE As Integer = &H400 + 22
'    Public Const TBM_SETLINESIZE As Integer = &H400 + 23
'    Public Const TBM_GETLINESIZE As Integer = &H400 + 24
'    Public Const TBM_GETTHUMBRECT As Integer = &H400 + 25
'    Public Const TBM_GETCHANNELRECT As Integer = &H400 + 26
'    Public Const TBM_SETTHUMBLENGTH As Integer = &H400 + 27
'    Public Const TBM_GETTHUMBLENGTH As Integer = &H400 + 28
'    Public Const TBM_SETTOOLTIPS As Integer = &H400 + 29
'    Public Const TBM_GETTOOLTIPS As Integer = &H400 + 30
'    Public Const TBM_SETTIPSIDE As Integer = &H400 + 31
'    Public Const TBTS_TOP As Integer = 0
'    Public Const TBTS_LEFT As Integer = 1
'    Public Const TBTS_BOTTOM As Integer = 2
'    Public Const TBTS_RIGHT As Integer = 3
'    Public Const TBM_SETBUDDY As Integer = &H400 + 32
'    Public Const TBM_GETBUDDY As Integer = &H400 + 33
'    Public Const TB_LINEUP As Integer = 0
'    Public Const TB_LINEDOWN As Integer = 1
'    Public Const TB_PAGEUP As Integer = 2
'    Public Const TB_PAGEDOWN As Integer = 3
'    Public Const TB_THUMBPOSITION As Integer = 4
'    Public Const TB_THUMBTRACK As Integer = 5
'    Public Const TB_TOP As Integer = 6
'    Public Const TB_BOTTOM As Integer = 7
'    Public Const TB_ENDTRACK As Integer = 8
'    Public Const TBCD_TICS As Integer = &H1
'    Public Const TBCD_THUMB As Integer = &H2
'    Public Const TBCD_CHANNEL As Integer = &H3
'    Public Const TBCDRF_NOEDGES As Integer = &H10000
'    Public Const TBCDRF_HILITEHOTTRACK As Integer = &H20000
'    Public Const TBCDRF_NOOFFSET As Integer = &H40000
'    Public Const TBCDRF_NOMARK As Integer = &H80000
'    Public Const TBCDRF_NOETCHEDEFFECT As Integer = &H100000
'    Public Const TVS_HASBUTTONS As Integer = &H1
'    Public Const TVS_HASLINES As Integer = &H2
'    Public Const TVS_LINESATROOT As Integer = &H4
'    Public Const TVS_EDITLABELS As Integer = &H8
'    Public Const TVS_DISABLEDRAGDROP As Integer = &H10
'    Public Const TVS_SHOWSELALWAYS As Integer = &H20
'    Public Const TVS_RTLREADING As Integer = &H40
'    Public Const TVS_NOTOOLTIPS As Integer = &H80
'    Public Const TVS_CHECKBOXES As Integer = &H100
'    Public Const TVS_TRACKSELECT As Integer = &H200
'    Public Const TVS_SHAREDIMAGELISTS As Integer = &H0
'    Public Const TVS_PRIVATEIMAGELISTS As Integer = &H400
'    Public Const TVS_FULLROWSELECT As Integer = &H1000
'    Public Const TVIF_TEXT As Integer = &H1
'    Public Const TVIF_IMAGE As Integer = &H2
'    Public Const TVIF_PARAM As Integer = &H4
        Public Const TVIF_STATE As Integer = &H8
'    Public Const TVIF_HANDLE As Integer = &H10
'    Public Const TVIF_SELECTEDIMAGE As Integer = &H20
'    Public Const TVIF_CHILDREN As Integer = &H40
'    Public Const TVIS_SELECTED As Integer = &H2
'    Public Const TVIS_CUT As Integer = &H4
'    Public Const TVIS_DROPHILITED As Integer = &H8
'    Public Const TVIS_BOLD As Integer = &H10
'    Public Const TVIS_EXPANDED As Integer = &H20
'    Public Const TVIS_EXPANDEDONCE As Integer = &H40
'    Public Const TVIS_EXPANDPARTIAL As Integer = &H80
'    Public Const TVIS_OVERLAYMASK As Integer = &HF00
        Public Const TVIS_STATEIMAGEMASK As Integer = &HF000
'    Public Const TVIS_USERMASK As Integer = &HF000
'    Public Const TVI_ROOT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TVIS_USERMASK = 0xF000,
'    '        TVI_ROOT = (unchecked((int)0xFFFF0000)),
'    '---------------------^--- GenCode(token): unexpected token type
'    Public Const TVI_FIRST As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TVI_ROOT = (unchecked((int)0xFFFF0000)),
'    '                   TVI_FIRST = (unchecked((int)0xFFFF0001)),
'    '---------------------------------^--- GenCode(token): unexpected token type
'    Public Const TVI_LAST As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                   TVI_FIRST = (unchecked((int)0xFFFF0001)),
'    '                               TVI_LAST = (unchecked((int)0xFFFF0002)),
'    '--------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const TVI_SORT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                               TVI_LAST = (unchecked((int)0xFFFF0002)),
'    '                                          TVI_SORT = (unchecked((int)0xFFFF0003)),
'    '-------------------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const TVM_INSERTITEMA As Integer = &H1100 + 0
'    Public Const TVM_INSERTITEMW As Integer = &H1100 + 50
'    Public Const TVM_DELETEITEM As Integer = &H1100 + 1
'    Public Const TVM_EXPAND As Integer = &H1100 + 2
'    Public Const TVE_COLLAPSE As Integer = &H1
'    Public Const TVE_EXPAND As Integer = &H2
'    Public Const TVE_TOGGLE As Integer = &H3
'    Public Const TVE_EXPANDPARTIAL As Integer = &H4000
'    Public Const TVE_COLLAPSERESET As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TVE_EXPANDPARTIAL = 0x4000,
'    '        TVE_COLLAPSERESET = unchecked((int)0x8000),
'    '-----------------------------^--- GenCode(token): unexpected token type
'    Public Const TVM_GETITEMRECT As Integer = &H1100 + 4
'    Public Const TVM_GETCOUNT As Integer = &H1100 + 5
'    Public Const TVM_GETINDENT As Integer = &H1100 + 6
'    Public Const TVM_SETINDENT As Integer = &H1100 + 7
'    Public Const TVM_GETIMAGELIST As Integer = &H1100 + 8
'    Public Const TVSIL_NORMAL As Integer = 0
'    Public Const TVSIL_STATE As Integer = 2
'    Public Const TVM_SETIMAGELIST As Integer = &H1100 + 9
'    Public Const TVM_GETNEXTITEM As Integer = &H1100 + 10
'    Public Const TVGN_ROOT As Integer = &H0
'    Public Const TVGN_NEXT As Integer = &H1
'    Public Const TVGN_PREVIOUS As Integer = &H2
'    Public Const TVGN_PARENT As Integer = &H3
'    Public Const TVGN_CHILD As Integer = &H4
'    Public Const TVGN_FIRSTVISIBLE As Integer = &H5
'    Public Const TVGN_NEXTVISIBLE As Integer = &H6
'    Public Const TVGN_PREVIOUSVISIBLE As Integer = &H7
'    Public Const TVGN_DROPHILITE As Integer = &H8
'    Public Const TVGN_CARET As Integer = &H9
'    Public Const TVM_SELECTITEM As Integer = &H1100 + 11
'    Public Const TVM_GETITEMA As Integer = &H1100 + 12
'    Public Const TVM_GETITEMW As Integer = &H1100 + 62
        Public Const TVM_SETITEMA As Integer = &H1100 + 13
'    Public Const TVM_SETITEMW As Integer = &H1100 + 63
'    Public Const TVM_EDITLABELA As Integer = &H1100 + 14
'    Public Const TVM_EDITLABELW As Integer = &H1100 + 65
'    Public Const TVM_GETEDITCONTROL As Integer = &H1100 + 15
'    Public Const TVM_GETVISIBLECOUNT As Integer = &H1100 + 16
'    Public Const TVM_HITTEST As Integer = &H1100 + 17
'    Public Const TVHT_NOWHERE As Integer = &H1
'    Public Const TVHT_ONITEMICON As Integer = &H2
'    Public Const TVHT_ONITEMLABEL As Integer = &H4
'    Public Const TVHT_ONITEMINDENT As Integer = &H8
'    Public Const TVHT_ONITEMBUTTON As Integer = &H10
'    Public Const TVHT_ONITEMRIGHT As Integer = &H20
'    Public Const TVHT_ONITEMSTATEICON As Integer = &H40
'    Public Const TVHT_ABOVE As Integer = &H100
'    Public Const TVHT_BELOW As Integer = &H200
'    Public Const TVHT_TORIGHT As Integer = &H400
'    Public Const TVHT_TOLEFT As Integer = &H800
'    Public Const TVM_CREATEDRAGIMAGE As Integer = &H1100 + 18
'    Public Const TVM_SORTCHILDREN As Integer = &H1100 + 19
'    Public Const TVM_ENSUREVISIBLE As Integer = &H1100 + 20
'    Public Const TVM_SORTCHILDRENCB As Integer = &H1100 + 21
'    Public Const TVM_ENDEDITLABELNOW As Integer = &H1100 + 22
'    Public Const TVM_GETISEARCHSTRINGA As Integer = &H1100 + 23
'    Public Const TVM_GETISEARCHSTRINGW As Integer = &H1100 + 64
'    Public Const TVM_SETTOOLTIPS As Integer = &H1100 + 24
'    Public Const TVM_GETTOOLTIPS As Integer = &H1100 + 25
'    Public Const TVM_SETITEMHEIGHT As Integer = &H1100 + 27
'    Public Const TVM_GETITEMHEIGHT As Integer = &H1100 + 28
'    Public Const TVN_SELCHANGINGA As Integer = 0 - 400 - 1
'    Public Const TVN_SELCHANGINGW As Integer = 0 - 400 - 50
'    Public Const TVN_SELCHANGEDA As Integer = 0 - 400 - 2
'    Public Const TVN_SELCHANGEDW As Integer = 0 - 400 - 51
'    Public Const TVC_UNKNOWN As Integer = &H0
'    Public Const TVC_BYMOUSE As Integer = &H1
'    Public Const TVC_BYKEYBOARD As Integer = &H2
'    Public Const TVN_GETDISPINFOA As Integer = 0 - 400 - 3
'    Public Const TVN_GETDISPINFOW As Integer = 0 - 400 - 52
'    Public Const TVN_SETDISPINFOA As Integer = 0 - 400 - 4
'    Public Const TVN_SETDISPINFOW As Integer = 0 - 400 - 53
'    Public Const TVIF_DI_SETITEM As Integer = &H1000
'    Public Const TVN_ITEMEXPANDINGA As Integer = 0 - 400 - 5
'    Public Const TVN_ITEMEXPANDINGW As Integer = 0 - 400 - 54
'    Public Const TVN_ITEMEXPANDEDA As Integer = 0 - 400 - 6
'    Public Const TVN_ITEMEXPANDEDW As Integer = 0 - 400 - 55
'    Public Const TVN_BEGINDRAGA As Integer = 0 - 400 - 7
'    Public Const TVN_BEGINDRAGW As Integer = 0 - 400 - 56
'    Public Const TVN_BEGINRDRAGA As Integer = 0 - 400 - 8
'    Public Const TVN_BEGINRDRAGW As Integer = 0 - 400 - 57
'    Public Const TVN_DELETEITEMA As Integer = 0 - 400 - 9
'    Public Const TVN_DELETEITEMW As Integer = 0 - 400 - 58
'    Public Const TVN_BEGINLABELEDITA As Integer = 0 - 400 - 10
'    Public Const TVN_BEGINLABELEDITW As Integer = 0 - 400 - 59
'    Public Const TVN_ENDLABELEDITA As Integer = 0 - 400 - 11
'    Public Const TVN_ENDLABELEDITW As Integer = 0 - 400 - 60
'    Public Const TVN_KEYDOWN As Integer = 0 - 400 - 12
'    Public Const TCS_SCROLLOPPOSITE As Integer = &H1
'    Public Const TCS_BOTTOM As Integer = &H2
'    Public Const TCS_RIGHT As Integer = &H2
'    Public Const TCS_MULTISELECT As Integer = &H4
'    Public Const TCS_FLATBUTTONS As Integer = &H8
'    Public Const TCS_FORCEICONLEFT As Integer = &H10
'    Public Const TCS_FORCELABELLEFT As Integer = &H20
'    Public Const TCS_HOTTRACK As Integer = &H40
'    Public Const TCS_VERTICAL As Integer = &H80
'    Public Const TCS_TABS As Integer = &H0
'    Public Const TCS_BUTTONS As Integer = &H100
'    Public Const TCS_SINGLELINE As Integer = &H0
'    Public Const TCS_MULTILINE As Integer = &H200
'    Public Const TCS_RIGHTJUSTIFY As Integer = &H0
'    Public Const TCS_FIXEDWIDTH As Integer = &H400
'    Public Const TCS_RAGGEDRIGHT As Integer = &H800
'    Public Const TCS_FOCUSONBUTTONDOWN As Integer = &H1000
'    Public Const TCS_OWNERDRAWFIXED As Integer = &H2000
'    Public Const TCS_TOOLTIPS As Integer = &H4000
'    Public Const TCS_FOCUSNEVER As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TCS_TOOLTIPS = 0x4000,
'    '        TCS_FOCUSNEVER = unchecked((int)0x8000),
'    '--------------------------^--- GenCode(token): unexpected token type
'    Public Const TCM_FIRST As Integer = &H1300
'    Public Const TCM_GETIMAGELIST As Integer = &H1300 + 2
'    Public Const TCM_SETIMAGELIST As Integer = &H1300 + 3
'    Public Const TCM_GETITEMCOUNT As Integer = &H1300 + 4
'    Public Const TCIF_TEXT As Integer = &H1
'    Public Const TCIF_IMAGE As Integer = &H2
'    Public Const TCIF_RTLREADING As Integer = &H4
'    Public Const TCIF_PARAM As Integer = &H8
'    Public Const TCIF_STATE As Integer = &H10
'    Public Const TCIS_BUTTONPRESSED As Integer = &H1
'    Public Const TCM_GETITEMA As Integer = &H1300 + 5
'    Public Const TCM_GETITEMW As Integer = &H1300 + 60
'    Public Const TCM_SETITEMA As Integer = &H1300 + 6
'    Public Const TCM_SETITEMW As Integer = &H1300 + 61
'    Public Const TCM_INSERTITEMA As Integer = &H1300 + 7
'    Public Const TCM_INSERTITEMW As Integer = &H1300 + 62
'    Public Const TCM_DELETEITEM As Integer = &H1300 + 8
'    Public Const TCM_DELETEALLITEMS As Integer = &H1300 + 9
'    Public Const TCM_GETITEMRECT As Integer = &H1300 + 10
'    Public Const TCM_GETCURSEL As Integer = &H1300 + 11
'    Public Const TCM_SETCURSEL As Integer = &H1300 + 12
'    Public Const TCHT_NOWHERE As Integer = &H1
'    Public Const TCHT_ONITEMICON As Integer = &H2
'    Public Const TCHT_ONITEMLABEL As Integer = &H4
'    Public Const TCHT_ONITEM As Integer = &H2 Or &H4
'    Public Const TCM_HITTEST As Integer = &H1300 + 13
'    Public Const TCM_SETITEMEXTRA As Integer = &H1300 + 14
'    Public Const TCM_ADJUSTRECT As Integer = &H1300 + 40
'    Public Const TCM_SETITEMSIZE As Integer = &H1300 + 41
'    Public Const TCM_REMOVEIMAGE As Integer = &H1300 + 42
'    Public Const TCM_SETPADDING As Integer = &H1300 + 43
'    Public Const TCM_GETROWCOUNT As Integer = &H1300 + 44
'    Public Const TCM_GETTOOLTIPS As Integer = &H1300 + 45
'    Public Const TCM_SETTOOLTIPS As Integer = &H1300 + 46
'    Public Const TCM_GETCURFOCUS As Integer = &H1300 + 47
'    Public Const TCM_SETCURFOCUS As Integer = &H1300 + 48
'    Public Const TCM_SETMINTABWIDTH As Integer = &H1300 + 49
'    Public Const TCM_DESELECTALL As Integer = &H1300 + 50
'    Public Const TCN_KEYDOWN As Integer = 0 - 550 - 0
'    Public Const TCN_SELCHANGE As Integer = 0 - 550 - 1
'    Public Const TCN_SELCHANGING As Integer = 0 - 550 - 2
'    Public Const THREAD_PRIORITY_LOWEST As Integer = - 2
'    Public Const THREAD_PRIORITY_BELOW_NORMAL As Integer = - 2 + 1
'    Public Const THREAD_PRIORITY_HIGHEST As Integer = 2
'    Public Const THREAD_PRIORITY_ABOVE_NORMAL As Integer = 2 - 1
'    Public Const THREAD_PRIORITY_ERROR_RETURN As Integer = &H7FFFFFFF
'    Public Const THREAD_PRIORITY_TIME_CRITICAL As Integer = 15
'    Public Const THREAD_PRIORITY_IDLE As Integer = - 15
'    Public Const TVHT_ONITEM As Integer = &H2 Or &H4 Or &H40
'    Public Const TBDDRET_DEFAULT As Integer = 0
'    Public Const TBDDRET_NODEFAULT As Integer = 1
'    Public Const TBDDRET_TREATPRESSED As Integer = 2
'    Public Const TBNF_IMAGE As Integer = &H1
'    Public Const TBNF_TEXT As Integer = &H2
'    Public Const TBNF_DI_SETITEM As Integer = &H10000000
    
'    Public Const TYMED_HGLOBAL As Integer = 1
'    Public Const TYMED_FILE As Integer = 2
'    Public Const TYMED_ISTREAM As Integer = 4
'    Public Const TYMED_ISTORAGE As Integer = 8
'    Public Const TYMED_GDI As Integer = 16
'    Public Const TYMED_MFPICT As Integer = 32
'    Public Const TYMED_ENHMF As Integer = 64
'    Public Const TYMED_NULL As Integer = 0
    
'    Public Const UI_CAP_2700 As Integer = &H1
'    Public Const UI_CAP_ROT90 As Integer = &H2
'    Public Const UI_CAP_ROTANY As Integer = &H4
'    Public Const UIS_SET As Integer = 1
'    Public Const UIS_CLEAR As Integer = 2
'    Public Const UIS_INITIALIZE As Integer = 3
'    Public Const UISF_HIDEFOCUS As Integer = &H1
'    Public Const UISF_HIDEACCEL As Integer = &H2
'    Public Const UNIQUE_NAME As Integer = &H0
'    Public Const UPDFCACHE_NODATACACHE As Integer = &H1
'    Public Const UPDFCACHE_ONSAVECACHE As Integer = &H2
'    Public Const UPDFCACHE_ONSTOPCACHE As Integer = &H4
'    Public Const UPDFCACHE_NORMALCACHE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                        UPDFCACHE_ONSTOPCACHE = (0x4),
'    '                                                                                UPDFCACHE_NORMALCACHE = (unchecked((int)0x8)),
'    '----------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const UPDFCACHE_IFBLANK As Integer = &H10
'    Public Const UPDFCACHE_ONLYIFBLANK As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                        UPDFCACHE_IFBLANK = (0x10),
'    '                                                                                                                            UPDFCACHE_ONLYIFBLANK = (unchecked((int)0x80000000)),
'    '------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const UPDFCACHE_IFBLANKORONSAVECACHE As Integer = &H10 Or &H2
'    Public Const UPDFCACHE_ALL As Integer = Not
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                    UPDFCACHE_IFBLANKORONSAVECACHE = ((0x10)|(0x2)),
'    '                                                                                                                                                                                     UPDFCACHE_ALL = (~(unchecked((int)0x80000000))),
'    '---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const UPDFCACHE_ALLBUTNODATACACHE As Integer = Not And Not &H1
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                                                     UPDFCACHE_ALL = (~(unchecked((int)0x80000000))),
'    '                                                                                                                                                                                                     UPDFCACHE_ALLBUTNODATACACHE = ((~(unchecked((int)0x80000000)))&~(0x1)),
'    '----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const USERCLASSTYPE_FULL As Integer = 1
'    Public Const USERCLASSTYPE_SHORT As Integer = 2
'    Public Const USERCLASSTYPE_APPNAME As Integer = 3
'    Public Const USER_MARSHAL_FC_BYTE As Integer = 1
'    Public Const USER_MARSHAL_FC_CHAR As Integer = 2
'    Public Const USER_MARSHAL_FC_SMALL As Integer = 3
'    Public Const USER_MARSHAL_FC_USMALL As Integer = 4
'    Public Const USER_MARSHAL_FC_WCHAR As Integer = 5
'    Public Const USER_MARSHAL_FC_SHORT As Integer = 6
'    Public Const USER_MARSHAL_FC_USHORT As Integer = 7
'    Public Const USER_MARSHAL_FC_LONG As Integer = 8
'    Public Const USER_MARSHAL_FC_ULONG As Integer = 9
'    Public Const USER_MARSHAL_FC_FLOAT As Integer = 10
'    Public Const USER_MARSHAL_FC_HYPER As Integer = 11
'    Public Const USER_MARSHAL_FC_DOUBLE As Integer = 12
'    Public Const UNLOAD_DLL_DEBUG_EVENT As Integer = 7
'    Public Const UNIVERSAL_NAME_INFO_LEVEL As Integer = &H1
'    Public Const UOI_FLAGS As Integer = 1
'    Public Const UOI_NAME As Integer = 2
'    Public Const UOI_TYPE As Integer = 3
'    Public Const UOI_USER_SID As Integer = 4
'    Public Const UDN_FIRST As Integer = 0 - 721
'    Public Const UDN_LAST As Integer = 0 - 740
'    Public Const UD_MAXVAL As Integer = &H7FFF
'    Public Const UD_MINVAL As Integer = - &H7FFF
'    Public Const UDS_WRAP As Integer = &H1
'    Public Const UDS_SETBUDDYINT As Integer = &H2
'    Public Const UDS_ALIGNRIGHT As Integer = &H4
'    Public Const UDS_ALIGNLEFT As Integer = &H8
'    Public Const UDS_AUTOBUDDY As Integer = &H10
'    Public Const UDS_ARROWKEYS As Integer = &H20
'    Public Const UDS_HORZ As Integer = &H40
'    Public Const UDS_NOTHOUSANDS As Integer = &H80
'    Public Const UDS_HOTTRACK As Integer = &H100
'    Public Const UDM_SETRANGE As Integer = &H400 + 101
'    Public Const UDM_GETRANGE As Integer = &H400 + 102
'    Public Const UDM_SETPOS As Integer = &H400 + 103
'    Public Const UDM_GETPOS As Integer = &H400 + 104
'    Public Const UDM_SETBUDDY As Integer = &H400 + 105
'    Public Const UDM_GETBUDDY As Integer = &H400 + 106
'    Public Const UDM_SETACCEL As Integer = &H400 + 107
'    Public Const UDM_GETACCEL As Integer = &H400 + 108
'    Public Const UDM_SETBASE As Integer = &H400 + 109
'    Public Const UDM_GETBASE As Integer = &H400 + 110
'    Public Const UDM_SETRANGE32 As Integer = &H400 + 111
'    Public Const UDM_GETRANGE32 As Integer = &H400 + 112
'    Public Const ULW_COLORKEY As Integer = &H1
'    Public Const ULW_ALPHA As Integer = &H2
'    Public Const ULW_OPAQUE As Integer = &H4
'    Public Const UDN_DELTAPOS As Integer = 0 - 721 - 1
'    ' NT5 begin 
'    ' NT5 end 
    
    
    
    
    
    
    
    
    
    
'    Public Const VARIANT_NOVALUEPROP As Integer = &H1
'    Public Const VARIANT_ALPHABOOL As Integer = &H2
'    Public Const VARIANT_NOUSEROVERRIDE As Integer = &H4
'    Public Const VAR_TIMEVALUEONLY As Integer = &H1
'    Public Const VAR_DATEVALUEONLY As Integer = &H2
'    Public Const VAR_VALIDDATE As Integer = &H4
'    Public Const VAR_CALENDAR_HIJRI As Integer = &H8
'    Public Const VARIANT_CALENDAR_HIJRI As Integer = &H8
'    Public Const VER_PLATFORM_WIN32s As Integer = 0
'    Public Const VER_PLATFORM_WIN32_WINDOWS As Integer = 1
'    Public Const VER_PLATFORM_WIN32_NT As Integer = 2
'    Public Const VIEW_E_FIRST As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        VER_PLATFORM_WIN32_NT = 2,
'    '        VIEW_E_FIRST = unchecked((int)0x80040140),
'    '------------------------^--- GenCode(token): unexpected token type
'    Public Const VIEW_E_LAST As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        VIEW_E_FIRST = unchecked((int)0x80040140),
'    '        VIEW_E_LAST = unchecked((int)0x8004014F),
'    '-----------------------^--- GenCode(token): unexpected token type
'    Public Const VIEW_S_FIRST As Integer = &H40140
'    Public Const VIEW_S_LAST As Integer = &H4014F
'    Public Const VIEW_E_DRAW As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        VIEW_S_LAST = 0x0004014F,
'    '        VIEW_E_DRAW = unchecked((int)0x80040140),
'    '-----------------------^--- GenCode(token): unexpected token type
'    Public Const VIEW_S_ALREADY_FROZEN As Integer = &H40140
'    Public Const VTA_BASELINE As Integer = 24
'    Public Const VTA_LEFT As Integer = 8
'    Public Const VTA_RIGHT As Integer = 0
'    Public Const VTA_CENTER As Integer = 6
'    Public Const VTA_BOTTOM As Integer = 2
'    Public Const VTA_TOP As Integer = 0
'    Public Const VARIABLE_PITCH As Integer = 2
'    Public Const VIETNAMESE_CHARSET As Integer = 163
'    Public Const VERTSIZE As Integer = 6
'    Public Const VERTRES As Integer = 10
'    Public Const VREFRESH As Integer = 116
'    Public Const VALID_INHERIT_FLAGS As Integer = &HF
'    Public Const VK_LBUTTON As Integer = &H1
'    Public Const VK_RBUTTON As Integer = &H2
'    Public Const VK_CANCEL As Integer = &H3
'    Public Const VK_MBUTTON As Integer = &H4
'    Public Const VK_BACK As Integer = &H8
'    Public Const VK_TAB As Integer = &H9
'    Public Const VK_CLEAR As Integer = &HC
'    Public Const VK_RETURN As Integer = &HD
'    Public Const VK_SHIFT As Integer = &H10
'    Public Const VK_CONTROL As Integer = &H11
'    Public Const VK_MENU As Integer = &H12
'    Public Const VK_PAUSE As Integer = &H13
'    Public Const VK_CAPITAL As Integer = &H14
'    Public Const VK_ESCAPE As Integer = &H1B
'    Public Const VK_SPACE As Integer = &H20
'    Public Const VK_PRIOR As Integer = &H21
'    Public Const VK_NEXT As Integer = &H22
'    Public Const VK_END As Integer = &H23
'    Public Const VK_HOME As Integer = &H24
'    Public Const VK_LEFT As Integer = &H25
'    Public Const VK_UP As Integer = &H26
'    Public Const VK_RIGHT As Integer = &H27
'    Public Const VK_DOWN As Integer = &H28
'    Public Const VK_SELECT As Integer = &H29
'    Public Const VK_PRINT As Integer = &H2A
'    Public Const VK_EXECUTE As Integer = &H2B
'    Public Const VK_SNAPSHOT As Integer = &H2C
'    Public Const VK_INSERT As Integer = &H2D
'    Public Const VK_DELETE As Integer = &H2E
'    Public Const VK_HELP As Integer = &H2F
'    Public Const VK_LWIN As Integer = &H5B
'    Public Const VK_RWIN As Integer = &H5C
'    Public Const VK_APPS As Integer = &H5D
'    Public Const VK_NUMPAD0 As Integer = &H60
'    Public Const VK_NUMPAD1 As Integer = &H61
'    Public Const VK_NUMPAD2 As Integer = &H62
'    Public Const VK_NUMPAD3 As Integer = &H63
'    Public Const VK_NUMPAD4 As Integer = &H64
'    Public Const VK_NUMPAD5 As Integer = &H65
'    Public Const VK_NUMPAD6 As Integer = &H66
'    Public Const VK_NUMPAD7 As Integer = &H67
'    Public Const VK_NUMPAD8 As Integer = &H68
'    Public Const VK_NUMPAD9 As Integer = &H69
'    Public Const VK_MULTIPLY As Integer = &H6A
'    Public Const VK_ADD As Integer = &H6B
'    Public Const VK_SEPARATOR As Integer = &H6C
'    Public Const VK_SUBTRACT As Integer = &H6D
'    Public Const VK_DECIMAL As Integer = &H6E
'    Public Const VK_DIVIDE As Integer = &H6F
'    Public Const VK_F1 As Integer = &H70
'    Public Const VK_F2 As Integer = &H71
'    Public Const VK_F3 As Integer = &H72
'    Public Const VK_F4 As Integer = &H73
'    Public Const VK_F5 As Integer = &H74
'    Public Const VK_F6 As Integer = &H75
'    Public Const VK_F7 As Integer = &H76
'    Public Const VK_F8 As Integer = &H77
'    Public Const VK_F9 As Integer = &H78
'    Public Const VK_F10 As Integer = &H79
'    Public Const VK_F11 As Integer = &H7A
'    Public Const VK_F12 As Integer = &H7B
'    Public Const VK_F13 As Integer = &H7C
'    Public Const VK_F14 As Integer = &H7D
'    Public Const VK_F15 As Integer = &H7E
'    Public Const VK_F16 As Integer = &H7F
'    Public Const VK_F17 As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        VK_F16 = 0x7F,
'    '        VK_F17 = unchecked((int)0x80),
'    '------------------^--- GenCode(token): unexpected token type
'    Public Const VK_F18 As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        VK_F17 = unchecked((int)0x80),
'    '        VK_F18 = unchecked((int)0x81),
'    '------------------^--- GenCode(token): unexpected token type
'    Public Const VK_F19 As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        VK_F18 = unchecked((int)0x81),
'    '        VK_F19 = unchecked((int)0x82),
'    '------------------^--- GenCode(token): unexpected token type
'    Public Const VK_F20 As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        VK_F19 = unchecked((int)0x82),
'    '        VK_F20 = unchecked((int)0x83),
'    '------------------^--- GenCode(token): unexpected token type
'    Public Const VK_F21 As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        VK_F20 = unchecked((int)0x83),
'    '        VK_F21 = unchecked((int)0x84),
'    '------------------^--- GenCode(token): unexpected token type
'    Public Const VK_F22 As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        VK_F21 = unchecked((int)0x84),
'    '        VK_F22 = unchecked((int)0x85),
'    '------------------^--- GenCode(token): unexpected token type
'    Public Const VK_F23 As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        VK_F22 = unchecked((int)0x85),
'    '        VK_F23 = unchecked((int)0x86),
'    '------------------^--- GenCode(token): unexpected token type
'    Public Const VK_F24 As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        VK_F23 = unchecked((int)0x86),
'    '        VK_F24 = unchecked((int)0x87),
'    '------------------^--- GenCode(token): unexpected token type
'    Public Const VK_NUMLOCK As Integer = &H90
'    Public Const VK_SCROLL As Integer = &H91
'    Public Const VK_LSHIFT As Integer = &HA0
'    Public Const VK_RSHIFT As Integer = &HA1
'    Public Const VK_LCONTROL As Integer = &HA2
'    Public Const VK_RCONTROL As Integer = &HA3
'    Public Const VK_LMENU As Integer = &HA4
'    Public Const VK_RMENU As Integer = &HA5
'    Public Const VK_PROCESSKEY As Integer = &HE5
'    Public Const VK_ATTN As Integer = &HF6
'    Public Const VK_CRSEL As Integer = &HF7
'    Public Const VK_EXSEL As Integer = &HF8
'    Public Const VK_EREOF As Integer = &HF9
'    Public Const VK_PLAY As Integer = &HFA
'    Public Const VK_ZOOM As Integer = &HFB
'    Public Const VK_NONAME As Integer = &HFC
'    Public Const VK_PA1 As Integer = &HFD
'    Public Const VK_OEM_CLEAR As Integer = &HFE
'    Public Const VS_FILE_INFO As Integer = 16
'    Public Const VS_VERSION_INFO As Integer = 1
'    Public Const VS_USER_DEFINED As Integer = 100
'    Public Const VS_FFI_SIGNATURE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        VS_USER_DEFINED = 100,
'    '        VS_FFI_SIGNATURE = unchecked((int)0xFEEF04BD),
'    '----------------------------^--- GenCode(token): unexpected token type
'    Public Const VS_FFI_STRUCVERSION As Integer = &H10000
'    Public Const VS_FFI_FILEFLAGSMASK As Integer = &H3F
'    Public Const VS_FF_DEBUG As Integer = &H1
'    Public Const VS_FF_PRERELEASE As Integer = &H2
'    Public Const VS_FF_PATCHED As Integer = &H4
'    Public Const VS_FF_PRIVATEBUILD As Integer = &H8
'    Public Const VS_FF_INFOINFERRED As Integer = &H10
'    Public Const VS_FF_SPECIALBUILD As Integer = &H20
'    Public Const VOS_UNKNOWN As Integer = &H0
'    Public Const VOS_DOS As Integer = &H10000
'    Public Const VOS_OS216 As Integer = &H20000
'    Public Const VOS_OS232 As Integer = &H30000
'    Public Const VOS_NT As Integer = &H40000
'    Public Const VOS__BASE As Integer = &H0
'    Public Const VOS__WINDOWS16 As Integer = &H1
'    Public Const VOS__PM16 As Integer = &H2
'    Public Const VOS__PM32 As Integer = &H3
'    Public Const VOS__WINDOWS32 As Integer = &H4
'    Public Const VOS_DOS_WINDOWS16 As Integer = &H10001
'    Public Const VOS_DOS_WINDOWS32 As Integer = &H10004
'    Public Const VOS_OS216_PM16 As Integer = &H20002
'    Public Const VOS_OS232_PM32 As Integer = &H30003
'    Public Const VOS_NT_WINDOWS32 As Integer = &H40004
'    Public Const VFT_UNKNOWN As Integer = &H0
'    Public Const VFT_APP As Integer = &H1
'    Public Const VFT_DLL As Integer = &H2
'    Public Const VFT_DRV As Integer = &H3
'    Public Const VFT_FONT As Integer = &H4
'    Public Const VFT_VXD As Integer = &H5
'    Public Const VFT_STATIC_LIB As Integer = &H7
'    Public Const VFT2_UNKNOWN As Integer = &H0
'    Public Const VFT2_DRV_PRINTER As Integer = &H1
'    Public Const VFT2_DRV_KEYBOARD As Integer = &H2
'    Public Const VFT2_DRV_LANGUAGE As Integer = &H3
'    Public Const VFT2_DRV_DISPLAY As Integer = &H4
'    Public Const VFT2_DRV_MOUSE As Integer = &H5
'    Public Const VFT2_DRV_NETWORK As Integer = &H6
'    Public Const VFT2_DRV_SYSTEM As Integer = &H7
'    Public Const VFT2_DRV_INSTALLABLE As Integer = &H8
'    Public Const VFT2_DRV_SOUND As Integer = &H9
'    Public Const VFT2_DRV_COMM As Integer = &HA
'    Public Const VFT2_DRV_INPUTMETHOD As Integer = &HB
'    Public Const VFT2_FONT_RASTER As Integer = &H1
'    Public Const VFT2_FONT_VECTOR As Integer = &H2
'    Public Const VFT2_FONT_TRUETYPE As Integer = &H3
'    Public Const VFFF_ISSHAREDFILE As Integer = &H1
'    Public Const VFF_CURNEDEST As Integer = &H1
'    Public Const VFF_FILEINUSE As Integer = &H2
'    Public Const VFF_BUFFTOOSMALL As Integer = &H4
'    Public Const VIFF_FORCEINSTALL As Integer = &H1
'    Public Const VIFF_DONTDELETEOLD As Integer = &H2
'    Public Const VIF_TEMPFILE As Integer = &H1
'    Public Const VIF_MISMATCH As Integer = &H2
'    Public Const VIF_SRCOLD As Integer = &H4
'    Public Const VIF_DIFFLANG As Integer = &H8
'    Public Const VIF_DIFFCODEPG As Integer = &H10
'    Public Const VIF_DIFFTYPE As Integer = &H20
'    Public Const VIF_WRITEPROT As Integer = &H40
'    Public Const VIF_FILEINUSE As Integer = &H80
'    Public Const VIF_OUTOFSPACE As Integer = &H100
'    Public Const VIF_ACCESSVIOLATION As Integer = &H200
'    Public Const VIF_SHARINGVIOLATION As Integer = &H400
'    Public Const VIF_CANNOTCREATE As Integer = &H800
'    Public Const VIF_CANNOTDELETE As Integer = &H1000
'    Public Const VIF_CANNOTRENAME As Integer = &H2000
'    Public Const VIF_CANNOTDELETECUR As Integer = &H4000
'    Public Const VIF_OUTOFMEMORY As Integer = &H8000
'    Public Const VIF_CANNOTREADSRC As Integer = &H10000
'    Public Const VIF_CANNOTREADDST As Integer = &H20000
'    Public Const VIF_BUFFTOOSMALL As Integer = &H40000
'    Public Const VIEW_LARGEICONS As Integer = 0
'    Public Const VIEW_SMALLICONS As Integer = 1
'    Public Const VIEW_LIST As Integer = 2
'    Public Const VIEW_DETAILS As Integer = 3
'    Public Const VIEW_SORTNAME As Integer = 4
'    Public Const VIEW_SORTSIZE As Integer = 5
'    Public Const VIEW_SORTDATE As Integer = 6
'    Public Const VIEW_SORTTYPE As Integer = 7
'    Public Const VIEW_PARENTFOLDER As Integer = 8
'    Public Const VIEW_NETCONNECT As Integer = 9
'    Public Const VIEW_NETDISCONNECT As Integer = 10
'    Public Const VIEW_NEWFOLDER As Integer = 11
'    Public Const VIEW_VIEWMENU As Integer = 12
    
'    Public Const WM_DDE_FIRST As Integer = &H3E0
'    Public Const WM_DDE_INITIATE As Integer = &H3E0
'    Public Const WM_DDE_TERMINATE As Integer = &H3E0 + 1
'    Public Const WM_DDE_ADVISE As Integer = &H3E0 + 2
'    Public Const WM_DDE_UNADVISE As Integer = &H3E0 + 3
'    Public Const WM_DDE_ACK As Integer = &H3E0 + 4
'    Public Const WM_DDE_DATA As Integer = &H3E0 + 5
'    Public Const WM_DDE_REQUEST As Integer = &H3E0 + 6
'    Public Const WM_DDE_POKE As Integer = &H3E0 + 7
'    Public Const WM_DDE_EXECUTE As Integer = &H3E0 + 8
'    Public Const WM_DDE_LAST As Integer = &H3E0 + 8
'    Public Const WAVERR_BASE As Integer = 32
'    Public Const WAVERR_BADFORMAT As Integer = 32 + 0
'    Public Const WAVERR_STILLPLAYING As Integer = 32 + 1
'    Public Const WAVERR_UNPREPARED As Integer = 32 + 2
'    Public Const WAVERR_SYNC As Integer = 32 + 3
'    Public Const WAVERR_LASTERROR As Integer = 32 + 3
'    Public Const WOM_OPEN As Integer = &H3BB
'    Public Const WOM_CLOSE As Integer = &H3BC
'    Public Const WOM_DONE As Integer = &H3BD
'    Public Const WIM_OPEN As Integer = &H3BE
'    Public Const WIM_CLOSE As Integer = &H3BF
'    Public Const WIM_DATA As Integer = &H3C0
'    Public Const WAVE_FORMAT_QUERY As Integer = &H1
'    Public Const WAVE_ALLOWSYNC As Integer = &H2
'    Public Const WAVE_MAPPED As Integer = &H4
'    Public Const WAVE_FORMAT_DIRECT As Integer = &H8
'    Public Const WAVE_FORMAT_DIRECT_QUERY As Integer = &H1 Or &H8
'    Public Const WHDR_DONE As Integer = &H1
'    Public Const WHDR_PREPARED As Integer = &H2
'    Public Const WHDR_BEGINLOOP As Integer = &H4
'    Public Const WHDR_ENDLOOP As Integer = &H8
'    Public Const WHDR_INQUEUE As Integer = &H10
'    Public Const WAVECAPS_PITCH As Integer = &H1
'    Public Const WAVECAPS_PLAYBACKRATE As Integer = &H2
'    Public Const WAVECAPS_VOLUME As Integer = &H4
'    Public Const WAVECAPS_LRVOLUME As Integer = &H8
'    Public Const WAVECAPS_SYNC As Integer = &H10
'    Public Const WAVECAPS_SAMPLEACCURATE As Integer = &H20
'    Public Const WAVECAPS_DIRECTSOUND As Integer = &H40
'    Public Const WAVE_INVALIDFORMAT As Integer = &H0
'    Public Const WAVE_FORMAT_1M08 As Integer = &H1
'    Public Const WAVE_FORMAT_1S08 As Integer = &H2
'    Public Const WAVE_FORMAT_1M16 As Integer = &H4
'    Public Const WAVE_FORMAT_1S16 As Integer = &H8
'    Public Const WAVE_FORMAT_2M08 As Integer = &H10
'    Public Const WAVE_FORMAT_2S08 As Integer = &H20
'    Public Const WAVE_FORMAT_2M16 As Integer = &H40
'    Public Const WAVE_FORMAT_2S16 As Integer = &H80
'    Public Const WAVE_FORMAT_4M08 As Integer = &H100
'    Public Const WAVE_FORMAT_4S08 As Integer = &H200
'    Public Const WAVE_FORMAT_4M16 As Integer = &H400
'    Public Const WAVE_FORMAT_4S16 As Integer = &H800
        Public Const WAVE_FORMAT_PCM As Integer = &H1
        Public Const WAVE_FORMAT_ADPCM As Integer = &H2
        Public Const WAVE_FORMAT_IEEE_FLOAT As Integer = &H3

'    Public Const WIN32 As Integer = 100
'    Public Const WIZ_CXDLG As Integer = 276
'    Public Const WIZ_CYDLG As Integer = 140
'    Public Const WIZ_CXBMP As Integer = 80
'    Public Const WIZ_BODYX As Integer = 92
'    Public Const WIZ_BODYCX As Integer = 184
'    Public Const WIN_CERT_REVISION_1_0 As Integer = &H100
'    Public Const WIN_CERT_TYPE_X509 As Integer = &H1
'    Public Const WIN_CERT_TYPE_PKCS_SIGNED_DATA As Integer = &H2
'    Public Const WIN_CERT_TYPE_RESERVED_1 As Integer = &H3
'    Public Const WINDOW_BUFFER_SIZE_EVENT As Integer = &H4
'    Public Const WINVER As Integer = &H400
'    Public Const WHITEONBLACK As Integer = 2
'    Public Const WINDING As Integer = 2
'    Public Const WHITE_BRUSH As Integer = 0
'    Public Const WHITE_PEN As Integer = 6
'    Public Const WGL_FONT_LINES As Integer = 0
'    Public Const WGL_FONT_POLYGONS As Integer = 1
'    Public Const WGL_SWAP_MAIN_PLANE As Integer = &H1
'    Public Const WGL_SWAP_OVERLAY1 As Integer = &H2
'    Public Const WGL_SWAP_OVERLAY2 As Integer = &H4
'    Public Const WGL_SWAP_OVERLAY3 As Integer = &H8
'    Public Const WGL_SWAP_OVERLAY4 As Integer = &H10
'    Public Const WGL_SWAP_OVERLAY5 As Integer = &H20
'    Public Const WGL_SWAP_OVERLAY6 As Integer = &H40
'    Public Const WGL_SWAP_OVERLAY7 As Integer = &H80
'    Public Const WGL_SWAP_OVERLAY8 As Integer = &H100
'    Public Const WGL_SWAP_OVERLAY9 As Integer = &H200
'    Public Const WGL_SWAP_OVERLAY10 As Integer = &H400
'    Public Const WGL_SWAP_OVERLAY11 As Integer = &H800
'    Public Const WGL_SWAP_OVERLAY12 As Integer = &H1000
'    Public Const WGL_SWAP_OVERLAY13 As Integer = &H2000
'    Public Const WGL_SWAP_OVERLAY14 As Integer = &H4000
'    Public Const WGL_SWAP_OVERLAY15 As Integer = &H8000
'    Public Const WGL_SWAP_UNDERLAY1 As Integer = &H10000
'    Public Const WGL_SWAP_UNDERLAY2 As Integer = &H20000
'    Public Const WGL_SWAP_UNDERLAY3 As Integer = &H40000
'    Public Const WGL_SWAP_UNDERLAY4 As Integer = &H80000
'    Public Const WGL_SWAP_UNDERLAY5 As Integer = &H100000
'    Public Const WGL_SWAP_UNDERLAY6 As Integer = &H200000
'    Public Const WGL_SWAP_UNDERLAY7 As Integer = &H400000
'    Public Const WGL_SWAP_UNDERLAY8 As Integer = &H800000
'    Public Const WGL_SWAP_UNDERLAY9 As Integer = &H1000000
'    Public Const WGL_SWAP_UNDERLAY10 As Integer = &H2000000
'    Public Const WGL_SWAP_UNDERLAY11 As Integer = &H4000000
'    Public Const WGL_SWAP_UNDERLAY12 As Integer = &H8000000
'    Public Const WGL_SWAP_UNDERLAY13 As Integer = &H10000000
'    Public Const WGL_SWAP_UNDERLAY14 As Integer = &H20000000
'    Public Const WGL_SWAP_UNDERLAY15 As Integer = &H40000000
'    Public Const WNNC_NET_MSNET As Integer = &H10000
'    Public Const WNNC_NET_LANMAN As Integer = &H20000
'    Public Const WNNC_NET_NETWARE As Integer = &H30000
'    Public Const WNNC_NET_VINES As Integer = &H40000
'    Public Const WNNC_NET_10NET As Integer = &H50000
'    Public Const WNNC_NET_LOCUS As Integer = &H60000
'    Public Const WNNC_NET_SUN_PC_NFS As Integer = &H70000
'    Public Const WNNC_NET_LANSTEP As Integer = &H80000
'    Public Const WNNC_NET_9TILES As Integer = &H90000
'    Public Const WNNC_NET_LANTASTIC As Integer = &HA0000
'    Public Const WNNC_NET_AS400 As Integer = &HB0000
'    Public Const WNNC_NET_FTP_NFS As Integer = &HC0000
'    Public Const WNNC_NET_PATHWORKS As Integer = &HD0000
'    Public Const WNNC_NET_LIFENET As Integer = &HE0000
'    Public Const WNNC_NET_POWERLAN As Integer = &HF0000
'    Public Const WNNC_NET_BWNFS As Integer = &H100000
'    Public Const WNNC_NET_COGENT As Integer = &H110000
'    Public Const WNNC_NET_FARALLON As Integer = &H120000
'    Public Const WNNC_NET_APPLETALK As Integer = &H130000
'    Public Const WNNC_NET_INTERGRAPH As Integer = &H140000
'    Public Const WNNC_NET_SYMFONET As Integer = &H150000
'    Public Const WNNC_NET_CLEARCASE As Integer = &H160000
'    Public Const WNFMT_MULTILINE As Integer = &H1
'    Public Const WNFMT_ABBREVIATED As Integer = &H2
'    Public Const WNFMT_INENUM As Integer = &H10
'    Public Const WNFMT_CONNECTION As Integer = &H20
'    Public Const WN_SUCCESS As Integer = 0
'    Public Const WN_NO_ERROR As Integer = 0
'    Public Const WN_NOT_SUPPORTED As Integer = 50
'    Public Const WN_CANCEL As Integer = 1223
'    Public Const WN_RETRY As Integer = 1237
'    Public Const WN_NET_ERROR As Integer = 59
'    Public Const WN_MORE_DATA As Integer = 234
'    Public Const WN_BAD_POINTER As Integer = 487
'    Public Const WN_BAD_VALUE As Integer = 87
'    Public Const WN_BAD_USER As Integer = 2202
'    Public Const WN_BAD_PASSWORD As Integer = 86
'    Public Const WN_ACCESS_DENIED As Integer = 5
'    Public Const WN_FUNCTION_BUSY As Integer = 170
'    Public Const WN_WINDOWS_ERROR As Integer = 59
'    Public Const WN_OUT_OF_MEMORY As Integer = 8
'    Public Const WN_NO_NETWORK As Integer = 1222
'    Public Const WN_EXTENDED_ERROR As Integer = 1208
'    Public Const WN_BAD_LEVEL As Integer = 124
'    Public Const WN_BAD_HANDLE As Integer = 6
'    Public Const WN_NOT_INITIALIZING As Integer = 1247
'    Public Const WN_NO_MORE_DEVICES As Integer = 1248
'    Public Const WN_NOT_CONNECTED As Integer = 2250
'    Public Const WN_OPEN_FILES As Integer = 2401
'    Public Const WN_DEVICE_IN_USE As Integer = 2404
'    Public Const WN_BAD_NETNAME As Integer = 67
'    Public Const WN_BAD_LOCALNAME As Integer = 1200
'    Public Const WN_ALREADY_CONNECTED As Integer = 85
'    Public Const WN_DEVICE_ERROR As Integer = 31
'    Public Const WN_CONNECTION_CLOSED As Integer = 1201
'    Public Const WN_NO_NET_OR_BAD_PATH As Integer = 1203
'    Public Const WN_BAD_PROVIDER As Integer = 1204
'    Public Const WN_CANNOT_OPEN_PROFILE As Integer = 1205
'    Public Const WN_BAD_PROFILE As Integer = 1206
'    Public Const WN_BAD_DEV_TYPE As Integer = 66
'    Public Const WN_DEVICE_ALREADY_REMEMBERED As Integer = 1202
'    Public Const WN_NO_MORE_ENTRIES As Integer = 259
'    Public Const WN_NOT_CONTAINER As Integer = 1207
'    Public Const WN_NOT_AUTHENTICATED As Integer = 1244
'    Public Const WN_NOT_LOGGED_ON As Integer = 1245
'    Public Const WN_NOT_VALIDATED As Integer = 1311
'    Public Const WNCON_FORNETCARD As Integer = &H1
'    Public Const WNCON_NOTROUTED As Integer = &H2
'    Public Const WNCON_SLOWLINK As Integer = &H4
'    Public Const WNCON_DYNAMIC As Integer = &H8
'    Public Const WC_DEFAULTCHECK As Integer = &H100
'    Public Const WC_COMPOSITECHECK As Integer = &H200
'    Public Const WC_DISCARDNS As Integer = &H10
'    Public Const WC_SEPCHARS As Integer = &H20
'    Public Const WC_DEFAULTCHAR As Integer = &H40
'    Public Const WRITE_DAC As Integer = &H40000
'    Public Const WRITE_OWNER As Integer = &H80000
'    Public Const WIN31_Class As Integer = 0
'    Public Const WH_MIN As Integer = - 1
'    Public Const WH_MSGFILTER As Integer = - 1
'    Public Const WH_JOURNALRECORD As Integer = 0
'    Public Const WH_JOURNALPLAYBACK As Integer = 1
'    Public Const WH_KEYBOARD As Integer = 2
'    Public Const WH_GETMESSAGE As Integer = 3
'    Public Const WH_CALLWNDPROC As Integer = 4
'    Public Const WH_CBT As Integer = 5
'    Public Const WH_SYSMSGFILTER As Integer = 6
'    Public Const WH_MOUSE As Integer = 7
'    Public Const WH_HARDWARE As Integer = 8
'    Public Const WH_DEBUG As Integer = 9
'    Public Const WH_SHELL As Integer = 10
'    Public Const WH_FOREGROUNDIDLE As Integer = 11
'    Public Const WH_CALLWNDPROCRET As Integer = 12
'    Public Const WH_MAX As Integer = 12
'    ' WH_MAX = 11;
'    Public Const WH_MINHOOK As Integer = - 1
'    Public Const WH_MAXHOOK As Integer = 12
'    Public Const WINSTA_ENUMDESKTOPS As Integer = &H1
'    Public Const WINSTA_READATTRIBUTES As Integer = &H2
'    Public Const WINSTA_ACCESSCLIPBOARD As Integer = &H4
'    Public Const WINSTA_CREATEDESKTOP As Integer = &H8
'    Public Const WINSTA_WRITEATTRIBUTES As Integer = &H10
'    Public Const WINSTA_ACCESSGLOBALATOMS As Integer = &H20
'    Public Const WINSTA_EXITWINDOWS As Integer = &H40
'    Public Const WINSTA_ENUMERATE As Integer = &H100
'    Public Const WINSTA_READSCREEN As Integer = &H200
'    Public Const WSF_VISIBLE As Integer = &H1
'    Public Const WM_NULL As Integer = &H0
'    Public Const WM_CREATE As Integer = &H1
'    Public Const WM_DESTROY As Integer = &H2
'    Public Const WM_MOVE As Integer = &H3
'    Public Const WM_SIZE As Integer = &H5
'    Public Const WM_ACTIVATE As Integer = &H6
'    Public Const WA_INACTIVE As Integer = 0
'    Public Const WA_ACTIVE As Integer = 1
'    Public Const WA_CLICKACTIVE As Integer = 2
        Public Const WM_SETFOCUS As Integer = &H7
'    Public Const WM_KILLFOCUS As Integer = &H8
'    Public Const WM_ENABLE As Integer = &HA
'    Public Const WM_SETREDRAW As Integer = &HB
'    Public Const WM_SETTEXT As Integer = &HC
'    Public Const WM_GETTEXT As Integer = &HD
'    Public Const WM_GETTEXTLENGTH As Integer = &HE
'    Public Const WM_PAINT As Integer = &HF
'    Public Const WM_CLOSE As Integer = &H10
'    Public Const WM_QUERYENDSESSION As Integer = &H11
'    Public Const WM_QUIT As Integer = &H12
'    Public Const WM_QUERYOPEN As Integer = &H13
'    Public Const WM_ERASEBKGND As Integer = &H14
        Public Const WM_SYSCOLORCHANGE As Integer = &H15
'    Public Const WM_ENDSESSION As Integer = &H16
'    Public Const WM_SHOWWINDOW As Integer = &H18
'    Public Const WM_WININICHANGE As Integer = &H1A
        Public Const WM_SETTINGCHANGE As Integer = &H1A
'    Public Const WM_DEVMODECHANGE As Integer = &H1B
'    Public Const WM_ACTIVATEAPP As Integer = &H1C
'    Public Const WM_FONTCHANGE As Integer = &H1D
'    Public Const WM_TIMECHANGE As Integer = &H1E
'    Public Const WM_CANCELMODE As Integer = &H1F
'    Public Const WM_SETCURSOR As Integer = &H20
'    Public Const WM_MOUSEACTIVATE As Integer = &H21
'    Public Const WM_CHILDACTIVATE As Integer = &H22
'    Public Const WM_QUEUESYNC As Integer = &H23
'    Public Const WM_GETMINMAXINFO As Integer = &H24
'    Public Const WM_PAINTICON As Integer = &H26
'    Public Const WM_ICONERASEBKGND As Integer = &H27
'    Public Const WM_NEXTDLGCTL As Integer = &H28
'    Public Const WM_SPOOLERSTATUS As Integer = &H2A
'    Public Const WM_DRAWITEM As Integer = &H2B
'    Public Const WM_MEASUREITEM As Integer = &H2C
'    Public Const WM_DELETEITEM As Integer = &H2D
'    Public Const WM_VKEYTOITEM As Integer = &H2E
'    Public Const WM_CHARTOITEM As Integer = &H2F
'    Public Const WM_SETFONT As Integer = &H30
'    Public Const WM_GETFONT As Integer = &H31
'    Public Const WM_SETHOTKEY As Integer = &H32
'    Public Const WM_GETHOTKEY As Integer = &H33
'    Public Const WM_QUERYDRAGICON As Integer = &H37
'    Public Const WM_COMPAREITEM As Integer = &H39
'    Public Const WM_GETOBJECT As Integer = &H3D
'    Public Const WM_COMPACTING As Integer = &H41
'    Public Const WM_COMMNOTIFY As Integer = &H44
'    Public Const WM_WINDOWPOSCHANGING As Integer = &H46
'    Public Const WM_WINDOWPOSCHANGED As Integer = &H47
'    Public Const WM_POWER As Integer = &H48
'    Public Const WM_COPYDATA As Integer = &H4A
'    Public Const WM_CANCELJOURNAL As Integer = &H4B
'    Public Const WM_NOTIFY As Integer = &H4E
'    Public Const WM_INPUTLANGCHANGEREQUEST As Integer = &H50
'    Public Const WM_INPUTLANGCHANGE As Integer = &H51
'    Public Const WM_TCARD As Integer = &H52
        Public Const WM_HELP As Integer = &H53
'    Public Const WM_USERCHANGED As Integer = &H54
'    Public Const WM_NOTIFYFORMAT As Integer = &H55
        Public Const WM_CONTEXTMENU As Integer = &H7B
'    Public Const WM_STYLECHANGING As Integer = &H7C
'    Public Const WM_STYLECHANGED As Integer = &H7D
'    Public Const WM_DISPLAYCHANGE As Integer = &H7E
'    Public Const WM_GETICON As Integer = &H7F
'    Public Const WM_SETICON As Integer = &H80
'    Public Const WM_NCCREATE As Integer = &H81
'    Public Const WM_NCDESTROY As Integer = &H82
'    Public Const WM_NCCALCSIZE As Integer = &H83
'    Public Const WM_NCHITTEST As Integer = &H84
'    Public Const WM_NCPAINT As Integer = &H85
'    Public Const WM_NCACTIVATE As Integer = &H86
        Public Const WM_GETDLGCODE As Integer = &H87
'    Public Const WM_NCMOUSEMOVE As Integer = &HA0
'    Public Const WM_NCLBUTTONDOWN As Integer = &HA1
'    Public Const WM_NCLBUTTONUP As Integer = &HA2
'    Public Const WM_NCLBUTTONDBLCLK As Integer = &HA3
'    Public Const WM_NCRBUTTONDOWN As Integer = &HA4
'    Public Const WM_NCRBUTTONUP As Integer = &HA5
'    Public Const WM_NCRBUTTONDBLCLK As Integer = &HA6
'    Public Const WM_NCMBUTTONDOWN As Integer = &HA7
'    Public Const WM_NCMBUTTONUP As Integer = &HA8
'    Public Const WM_NCMBUTTONDBLCLK As Integer = &HA9
'    Public Const WM_KEYFIRST As Integer = &H100
        Public Const WM_KEYDOWN As Integer = &H100
        Public Const WM_KEYUP As Integer = &H101
        Public Const WM_CHAR As Integer = &H102
'    Public Const WM_DEADCHAR As Integer = &H103
        Public Const WM_SYSKEYDOWN As Integer = &H104
        Public Const WM_SYSKEYUP As Integer = &H105
        Public Const WM_SYSCHAR As Integer = &H106
'    Public Const WM_SYSDEADCHAR As Integer = &H107
'    Public Const WM_KEYLAST As Integer = &H108
'    Public Const WM_IME_STARTCOMPOSITION As Integer = &H10D
'    Public Const WM_IME_ENDCOMPOSITION As Integer = &H10E
'    Public Const WM_IME_COMPOSITION As Integer = &H10F
'    Public Const WM_IME_KEYLAST As Integer = &H10F
'    Public Const WM_INITDIALOG As Integer = &H110
'    Public Const WM_COMMAND As Integer = &H111
        Public Const WM_SYSCOMMAND As Integer = &H112
'    Public Const WM_TIMER As Integer = &H113
'    Public Const WM_HSCROLL As Integer = &H114
'    Public Const WM_VSCROLL As Integer = &H115
'    Public Const WM_INITMENU As Integer = &H116
'    Public Const WM_INITMENUPOPUP As Integer = &H117
'    Public Const WM_MENUSELECT As Integer = &H11F
'    Public Const WM_MENUCHAR As Integer = &H120
'    Public Const WM_ENTERIDLE As Integer = &H121
'    Public Const WM_CHANGEUISTATE As Integer = &H127
'    Public Const WM_UPDATEUISTATE As Integer = &H128
'    Public Const WM_QUERYUISTATE As Integer = &H129
'    Public Const WM_CTLCOLORMSGBOX As Integer = &H132
'    Public Const WM_CTLCOLOREDIT As Integer = &H133
'    Public Const WM_CTLCOLORLISTBOX As Integer = &H134
'    Public Const WM_CTLCOLORBTN As Integer = &H135
'    Public Const WM_CTLCOLORDLG As Integer = &H136
'    Public Const WM_CTLCOLORSCROLLBAR As Integer = &H137
'    Public Const WM_CTLCOLORSTATIC As Integer = &H138
'    Public Const WM_MOUSEFIRST As Integer = &H200
'    Public Const WM_MOUSEMOVE As Integer = &H200
'    Public Const WM_LBUTTONDOWN As Integer = &H201
'    Public Const WM_LBUTTONUP As Integer = &H202
'    Public Const WM_LBUTTONDBLCLK As Integer = &H203
        Public Const WM_RBUTTONDOWN As Integer = &H204
        Public Const WM_RBUTTONUP As Integer = &H205
'    Public Const WM_RBUTTONDBLCLK As Integer = &H206
'    Public Const WM_MBUTTONDOWN As Integer = &H207
'    Public Const WM_MBUTTONUP As Integer = &H208
'    Public Const WM_MBUTTONDBLCLK As Integer = &H209
'    Public Const WM_NCMOUSEHOVER As Integer = &H2A0
'    Public Const WM_NCMOUSELEAVE As Integer = &H2A2
'    Public Const WM_MOUSEWHEEL As Integer = &H20A
'    Public Const WM_MOUSELAST As Integer = &H20A
'    Public Const WHEEL_DELTA As Integer = 120
'    Public Const WM_PARENTNOTIFY As Integer = &H210
'    Public Const WM_ENTERMENULOOP As Integer = &H211
'    Public Const WM_EXITMENULOOP As Integer = &H212
'    Public Const WM_NEXTMENU As Integer = &H213
'    Public Const WM_SIZING As Integer = &H214
'    Public Const WM_CAPTURECHANGED As Integer = &H215
'    Public Const WM_MOVING As Integer = &H216
'    Public Const WM_POWERBROADCAST As Integer = &H218
'    Public Const WM_DEVICECHANGE As Integer = &H219
'    Public Const WM_IME_SETCONTEXT As Integer = &H281
'    Public Const WM_IME_NOTIFY As Integer = &H282
'    Public Const WM_IME_CONTROL As Integer = &H283
'    Public Const WM_IME_COMPOSITIONFULL As Integer = &H284
'    Public Const WM_IME_SELECT As Integer = &H285
'    Public Const WM_IME_CHAR As Integer = &H286
'    Public Const WM_IME_KEYDOWN As Integer = &H290
'    Public Const WM_IME_KEYUP As Integer = &H291
'    Public Const WM_MDICREATE As Integer = &H220
'    Public Const WM_MDIDESTROY As Integer = &H221
'    Public Const WM_MDIACTIVATE As Integer = &H222
'    Public Const WM_MDIRESTORE As Integer = &H223
'    Public Const WM_MDINEXT As Integer = &H224
'    Public Const WM_MDIMAXIMIZE As Integer = &H225
'    Public Const WM_MDITILE As Integer = &H226
'    Public Const WM_MDICASCADE As Integer = &H227
'    Public Const WM_MDIICONARRANGE As Integer = &H228
'    Public Const WM_MDIGETACTIVE As Integer = &H229
'    Public Const WM_MDISETMENU As Integer = &H230
'    Public Const WM_ENTERSIZEMOVE As Integer = &H231
'    Public Const WM_EXITSIZEMOVE As Integer = &H232
'    Public Const WM_DROPFILES As Integer = &H233
'    Public Const WM_MDIREFRESHMENU As Integer = &H234
'    Public Const WM_MOUSEHOVER As Integer = &H2A1
'    Public Const WM_MOUSELEAVE As Integer = &H2A3
'    Public Const WM_CUT As Integer = &H300
'    Public Const WM_COPY As Integer = &H301
     Public Const WM_PASTE As Integer = &H302
'    Public Const WM_CLEAR As Integer = &H303
'    Public Const WM_UNDO As Integer = &H304
'    Public Const WM_RENDERFORMAT As Integer = &H305
'    Public Const WM_RENDERALLFORMATS As Integer = &H306
'    Public Const WM_DESTROYCLIPBOARD As Integer = &H307
'    Public Const WM_DRAWCLIPBOARD As Integer = &H308
'    Public Const WM_PAINTCLIPBOARD As Integer = &H309
'    Public Const WM_VSCROLLCLIPBOARD As Integer = &H30A
'    Public Const WM_SIZECLIPBOARD As Integer = &H30B
'    Public Const WM_ASKCBFORMATNAME As Integer = &H30C
'    Public Const WM_CHANGECBCHAIN As Integer = &H30D
'    Public Const WM_HSCROLLCLIPBOARD As Integer = &H30E
'    Public Const WM_QUERYNEWPALETTE As Integer = &H30F
'    Public Const WM_PALETTEISCHANGING As Integer = &H310
        Public Const WM_PALETTECHANGED As Integer = &H311
'    Public Const WM_HOTKEY As Integer = &H312
'    Public Const WM_PRINT As Integer = &H317
'    Public Const WM_PRINTCLIENT As Integer = &H318
        Public Const WM_THEMECHANGED = &H31A
'    Public Const WM_HANDHELDFIRST As Integer = &H358
'    Public Const WM_HANDHELDLAST As Integer = &H35F
'    Public Const WM_AFXFIRST As Integer = &H360
'    Public Const WM_AFXLAST As Integer = &H37F
'    Public Const WM_PENWINFIRST As Integer = &H380
'    Public Const WM_PENWINLAST As Integer = &H38F
'    Public Const WM_APP As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        WM_PENWINLAST = 0x038F,
'    '        WM_APP = unchecked((int)0x8000),
'    '------------------^--- GenCode(token): unexpected token type
     Public Const WM_USER As Integer = &H400
'    Public Const WM_REFLECT As Integer = WM_USER + &H1C00
'    Public Const WMSZ_LEFT As Integer = 1
'    Public Const WMSZ_RIGHT As Integer = 2
'    Public Const WMSZ_TOP As Integer = 3
'    Public Const WMSZ_TOPLEFT As Integer = 4
'    Public Const WMSZ_TOPRIGHT As Integer = 5
'    Public Const WMSZ_BOTTOM As Integer = 6
'    Public Const WMSZ_BOTTOMLEFT As Integer = 7
'    Public Const WMSZ_BOTTOMRIGHT As Integer = 8
'    Public Const WVR_ALIGNTOP As Integer = &H10
'    Public Const WVR_ALIGNLEFT As Integer = &H20
'    Public Const WVR_ALIGNBOTTOM As Integer = &H40
'    Public Const WVR_ALIGNRIGHT As Integer = &H80
'    Public Const WVR_HREDRAW As Integer = &H100
'    Public Const WVR_VREDRAW As Integer = &H200
'    Public Const WVR_VALIDRECTS As Integer = &H400
'    Public Const WS_OVERLAPPED As Integer = &H0
'    Public Const WS_POPUP As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        WS_OVERLAPPED = 0x00000000,
'    '        WS_POPUP = unchecked((int)0x80000000),
'    '--------------------^--- GenCode(token): unexpected token type
'    Public Const WS_CHILD As Integer = &H40000000
'    Public Const WS_MINIMIZE As Integer = &H20000000
'    Public Const WS_VISIBLE As Integer = &H10000000
'    Public Const WS_DISABLED As Integer = &H8000000
'    Public Const WS_CLIPSIBLINGS As Integer = &H4000000
'    Public Const WS_CLIPCHILDREN As Integer = &H2000000
'    Public Const WS_MAXIMIZE As Integer = &H1000000
'    Public Const WS_CAPTION As Integer = &HC00000
'    Public Const WS_BORDER As Integer = &H800000
'    Public Const WS_DLGFRAME As Integer = &H400000
'    Public Const WS_VSCROLL As Integer = &H200000
'    Public Const WS_HSCROLL As Integer = &H100000
'    Public Const WS_SYSMENU As Integer = &H80000
'    Public Const WS_THICKFRAME As Integer = &H40000
'    Public Const WS_GROUP As Integer = &H20000
'    Public Const WS_TABSTOP As Integer = &H10000
'    Public Const WS_MINIMIZEBOX As Integer = &H20000
'    Public Const WS_MAXIMIZEBOX As Integer = &H10000
'    Public Const WS_TILED As Integer = &H0
'    Public Const WS_ICONIC As Integer = &H20000000
'    Public Const WS_SIZEBOX As Integer = &H40000
'    Public Const WS_OVERLAPPEDWINDOW As Integer = &H0 Or &HC00000 Or &H80000 Or &H40000 Or &H20000 Or &H10000
'    Public Const WS_POPUPWINDOW As Integer = Or &H800000 Or &H80000
'    '
'    'Note:  Error processing original source shown below
'    '        WS_OVERLAPPEDWINDOW = (0x00000000|0x00C00000|0x00080000|0x00040000|0x00020000|0x00010000),
'    '                              WS_POPUPWINDOW = (unchecked((int)0x80000000)|0x00800000|0x00080000),
'    '-------------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const WS_CHILDWINDOW As Integer = &H40000000
'    Public Const WS_EX_DLGMODALFRAME As Integer = &H1
'    Public Const WS_EX_NOPARENTNOTIFY As Integer = &H4
'    Public Const WS_EX_TOPMOST As Integer = &H8
'    Public Const WS_EX_ACCEPTFILES As Integer = &H10
'    Public Const WS_EX_TRANSPARENT As Integer = &H20
'    Public Const WS_EX_MDICHILD As Integer = &H40
'    Public Const WS_EX_TOOLWINDOW As Integer = &H80
'    Public Const WS_EX_WINDOWEDGE As Integer = &H100
'    Public Const WS_EX_CLIENTEDGE As Integer = &H200
'    Public Const WS_EX_CONTEXTHELP As Integer = &H400
'    Public Const WS_EX_RIGHT As Integer = &H1000
'    Public Const WS_EX_LEFT As Integer = &H0
'    Public Const WS_EX_RTLREADING As Integer = &H2000
'    Public Const WS_EX_LTRREADING As Integer = &H0
'    Public Const WS_EX_LEFTSCROLLBAR As Integer = &H4000
'    Public Const WS_EX_RIGHTSCROLLBAR As Integer = &H0
'    Public Const WS_EX_CONTROLPARENT As Integer = &H10000
'    Public Const WS_EX_STATICEDGE As Integer = &H20000
'    Public Const WS_EX_APPWINDOW As Integer = &H40000
'    Public Const WS_EX_OVERLAPPEDWINDOW As Integer = &H100 Or &H200
'    Public Const WS_EX_PALETTEWINDOW As Integer = &H100 Or &H80 Or &H8
'    Public Const WS_EX_LAYERED As Integer = &H80000
'    Public Const WS_EX_NOINHERITLAYOUT As Integer = &H100000
'    Public Const WS_EX_LAYOUTRTL As Integer = &H400000
'    Public Const WS_EX_NOACTIVATE As Integer = &H8000000
'    Public Const WPF_SETMINPOSITION As Integer = &H1
'    Public Const WPF_RESTORETOMAXIMIZED As Integer = &H2
'    Public Const WB_LEFT As Integer = 0
'    Public Const WB_RIGHT As Integer = 1
'    Public Const WB_ISDELIMITER As Integer = 2
'    Public Const WDT_INPROC_CALL As Integer = &H48746457
'    Public Const WDT_REMOTE_CALL As Integer = &H52746457
'    Public Const WM_CHOOSEFONT_GETLOGFONT As Integer = &H400 + 1
'    Public Const WM_PSD_PAGESETUPDLG As Integer = &H400
'    Public Const WM_PSD_FULLPAGERECT As Integer = &H400 + 1
'    Public Const WM_PSD_MINMARGINRECT As Integer = &H400 + 2
'    Public Const WM_PSD_MARGINRECT As Integer = &H400 + 3
'    Public Const WM_PSD_GREEKTEXTRECT As Integer = &H400 + 4
'    Public Const WM_PSD_ENVSTAMPRECT As Integer = &H400 + 5
'    Public Const WM_PSD_YAFULLPAGERECT As Integer = &H400 + 6
'    Public Const WAIT_IO_COMPLETION As Integer = &HC0
'    Public Const WS_TILEDWINDOW As Integer = &H0 Or &HC00000 Or &H80000 Or &H40000 Or &H20000 Or &H10000
    
'    ' NT5 Begin 
'    ' Disable inheritence of mirroring by children
'    ' Right to left mirroring
'    ' NT5 End 
    
    
'    Public Const WAIT_OBJECT_0 As Integer = &H0
'    Public Const WAIT_FAILED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        public const int WAIT_OBJECT_0 = 0x00000000,
'    '        WAIT_FAILED = unchecked((int)0xFFFFFFFF),
'    '-----------------------^--- GenCode(token): unexpected token type
'    Public Const WAIT_TIMEOUT As Integer = &H102
'    Public Const WAIT_ABANDONED As Integer = &H80
'    Public Const WAIT_ABANDONED_0 As Integer = WAIT_ABANDONED
'    Public Const WHITENESS As Integer = &HFF0062
    
    
'    Public Const XST_NULL As Integer = 0
'    Public Const XST_INCOMPLETE As Integer = 1
'    Public Const XST_CONNECTED As Integer = 2
'    Public Const XST_INIT1 As Integer = 3
'    Public Const XST_INIT2 As Integer = 4
'    Public Const XST_REQSENT As Integer = 5
'    Public Const XST_DATARCVD As Integer = 6
'    Public Const XST_POKESENT As Integer = 7
'    Public Const XST_POKEACKRCVD As Integer = 8
'    Public Const XST_EXECSENT As Integer = 9
'    Public Const XST_EXECACKRCVD As Integer = 10
'    Public Const XST_ADVSENT As Integer = 11
'    Public Const XST_UNADVSENT As Integer = 12
'    Public Const XST_ADVACKRCVD As Integer = 13
'    Public Const XST_UNADVACKRCVD As Integer = 14
'    Public Const XST_ADVDATASENT As Integer = 15
'    Public Const XST_ADVDATAACKRCVD As Integer = 16
'    Public Const XTYPF_NOBLOCK As Integer = &H2
'    Public Const XTYPF_NODATA As Integer = &H4
'    Public Const XTYPF_ACKREQ As Integer = &H8
'    Public Const XCLASS_MASK As Integer = &HFC00
'    Public Const XCLASS_BOOL As Integer = &H1000
'    Public Const XCLASS_DATA As Integer = &H2000
'    Public Const XCLASS_FLAGS As Integer = &H4000
'    Public Const XCLASS_NOTIFICATION As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        XCLASS_FLAGS = 0x4000,
'    '        XCLASS_NOTIFICATION = unchecked((int)0x8000),
'    '-------------------------------^--- GenCode(token): unexpected token type
'    Public Const XTYP_ERROR As Integer = &H0 Or Or &H2
'    '
'    'Note:  Error processing original source shown below
'    '        XCLASS_NOTIFICATION = unchecked((int)0x8000),
'    '        XTYP_ERROR = (0x0000|unchecked((int)0x8000)|0x0002),
'    '------------------------------^--- GenCode(token): unexpected token type
'    Public Const XTYP_ADVDATA As Integer = &H10 Or &H4000
'    Public Const XTYP_ADVREQ As Integer = &H20 Or &H2000 Or &H2
'    Public Const XTYP_ADVSTART As Integer = &H30 Or &H1000
'    Public Const XTYP_ADVSTOP As Integer = &H40 Or
'    '
'    'Note:  Error processing original source shown below
'    '                                                  XTYP_ADVSTART = (0x0030|0x1000),
'    '                                                                  XTYP_ADVSTOP = (0x0040|unchecked((int)0x8000)),
'    '------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const XTYP_EXECUTE As Integer = &H50 Or &H4000
'    Public Const XTYP_CONNECT As Integer = &H60 Or &H1000 Or &H2
'    Public Const XTYP_CONNECT_CONFIRM As Integer = &H70 Or Or &H2
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                XTYP_CONNECT = (0x0060|0x1000|0x0002),
'    '                                                                                                               XTYP_CONNECT_CONFIRM = (0x0070|unchecked((int)0x8000)|0x0002),
'    '-----------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const XTYP_XACT_COMPLETE As Integer = &H80 Or
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                               XTYP_CONNECT_CONFIRM = (0x0070|unchecked((int)0x8000)|0x0002),
'    '                                                                                                                                      XTYP_XACT_COMPLETE = (0x0080|unchecked((int)0x8000)),
'    '--------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const XTYP_POKE As Integer = &H90 Or &H4000
'    Public Const XTYP_REGISTER As Integer = &HA0 Or Or &H2
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                           XTYP_POKE = (0x0090|0x4000),
'    '                                                                                                                                                                       XTYP_REGISTER = (0x00A0|unchecked((int)0x8000)|0x0002),
'    '------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const XTYP_REQUEST As Integer = &HB0 Or &H2000
'    Public Const XTYP_DISCONNECT As Integer = &HC0 Or Or &H2
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                                                       XTYP_REQUEST = (0x00B0|0x2000),
'    '                                                                                                                                                                                                      XTYP_DISCONNECT = (0x00C0|unchecked((int)0x8000)|0x0002),
'    '---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const XTYP_UNREGISTER As Integer = &HD0 Or Or &H2
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                                                                      XTYP_DISCONNECT = (0x00C0|unchecked((int)0x8000)|0x0002),
'    '                                                                                                                                                                                                                        XTYP_UNREGISTER = (0x00D0|unchecked((int)0x8000)|0x0002),
'    '---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Public Const XTYP_WILDCONNECT As Integer = &HE0 Or &H2000 Or &H2
'    Public Const XTYP_MASK As Integer = &HF0
'    Public Const XTYP_SHIFT As Integer = 4
'    Public Const XTYP_MONITOR As Integer = &HF0 Or Or &H2
'    '
'    'Note:  Error processing original source shown below
'    '        XTYP_SHIFT = 4,
'    '        XTYP_MONITOR = (0x00F0|unchecked((int)0x8000)|0x0002);
'    '--------------------------------^--- GenCode(token): unexpected token type
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
'    Public Shared CBEM_GETITEM As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.CBEM_GETITEMA, win.CBEM_GETITEMW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared CBEM_SETITEM As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.CBEM_SETITEMA, win.CBEM_SETITEMW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared CBEN_ENDEDIT As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.CBEN_ENDEDITA, win.CBEN_ENDEDITW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared CBEM_INSERTITEM As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.CBEM_INSERTITEMA, win.CBEM_INSERTITEMW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared LVM_GETITEMTEXT As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.LVM_GETITEMTEXTA, win.LVM_GETITEMTEXTW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared LVM_SETITEMTEXT As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.LVM_SETITEMTEXTA, win.LVM_SETITEMTEXTW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared ACM_OPEN As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.ACM_OPENA, win.ACM_OPENW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared DTM_SETFORMAT As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.DTM_SETFORMATA, win.DTM_SETFORMATW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared DTN_USERSTRING As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.DTN_USERSTRINGA, win.DTN_USERSTRINGW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared DTN_WMKEYDOWN As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.DTN_WMKEYDOWNA, win.DTN_WMKEYDOWNW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared DTN_FORMAT As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.DTN_FORMATA, win.DTN_FORMATW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared DTN_FORMATQUERY As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.DTN_FORMATQUERYA, win.DTN_FORMATQUERYW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared EMR_EXTTEXTOUT As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.EMR_EXTTEXTOUTA, win.EMR_EXTTEXTOUTW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared EMR_POLYTEXTOUT As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.EMR_POLYTEXTOUTA, win.EMR_POLYTEXTOUTW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared HDM_INSERTITEM As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.HDM_INSERTITEMA, win.HDM_INSERTITEMW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared HDM_GETITEM As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.HDM_GETITEMA, win.HDM_GETITEMW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared HDM_SETITEM As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.HDM_SETITEMA, win.HDM_SETITEMW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared HDN_ITEMCHANGING As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.HDN_ITEMCHANGINGA, win.HDN_ITEMCHANGINGW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared HDN_ITEMCHANGED As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.HDN_ITEMCHANGEDA, win.HDN_ITEMCHANGEDW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared HDN_ITEMCLICK As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.HDN_ITEMCLICKA, win.HDN_ITEMCLICKW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared HDN_ITEMDBLCLICK As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.HDN_ITEMDBLCLICKA, win.HDN_ITEMDBLCLICKW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared HDN_DIVIDERDBLCLICK As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.HDN_DIVIDERDBLCLICKA, win.HDN_DIVIDERDBLCLICKW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared HDN_BEGINTRACK As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.HDN_BEGINTRACKA, win.HDN_BEGINTRACKW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared HDN_ENDTRACK As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.HDN_ENDTRACKA, win.HDN_ENDTRACKW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared HDN_TRACK As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.HDN_TRACKA, win.HDN_TRACKW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared HDN_GETDISPINFO As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.HDN_GETDISPINFOA, win.HDN_GETDISPINFOW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared LVM_GETITEM As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.LVM_GETITEMA, win.LVM_GETITEMW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared LVM_SETITEM As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.LVM_SETITEMA, win.LVM_SETITEMW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared LVM_INSERTITEM As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.LVM_INSERTITEMA, win.LVM_INSERTITEMW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared LVM_FINDITEM As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.LVM_FINDITEMA, win.LVM_FINDITEMW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared LVM_GETSTRINGWIDTH As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.LVM_GETSTRINGWIDTHA, win.LVM_GETSTRINGWIDTHW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared LVM_EDITLABEL As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.LVM_EDITLABELA, win.LVM_EDITLABELW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared LVM_GETCOLUMN As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.LVM_GETCOLUMNA, win.LVM_GETCOLUMNW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared LVM_SETCOLUMN As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.LVM_SETCOLUMNA, win.LVM_SETCOLUMNW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared LVM_GETISEARCHSTRING As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.LVM_GETISEARCHSTRINGA, win.LVM_GETISEARCHSTRINGW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared LVM_INSERTCOLUMN As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.LVM_INSERTCOLUMNA, win.LVM_INSERTCOLUMNW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared LVN_BEGINLABELEDIT As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.LVN_BEGINLABELEDITA, win.LVN_BEGINLABELEDITW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared LVN_ENDLABELEDIT As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.LVN_ENDLABELEDITA, win.LVN_ENDLABELEDITW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared LVN_ODFINDITEM As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.LVN_ODFINDITEMA, win.LVN_ODFINDITEMW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared LVN_GETDISPINFO As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.LVN_GETDISPINFOA, win.LVN_GETDISPINFOW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared LVN_SETDISPINFO As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.LVN_SETDISPINFOA, win.LVN_SETDISPINFOW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared PSM_SETTITLE As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.PSM_SETTITLEA, win.PSM_SETTITLEW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared PSM_SETFINISHTEXT As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.PSM_SETFINISHTEXTA, win.PSM_SETFINISHTEXTW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared RB_INSERTBAND As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.RB_INSERTBANDA, win.RB_INSERTBANDW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared RB_SETBANDINFO As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.RB_SETBANDINFOA, win.RB_SETBANDINFOW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared SB_SETTEXT As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.SB_SETTEXTA, win.SB_SETTEXTW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared SB_GETTEXT As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.SB_GETTEXTA, win.SB_GETTEXTW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared SB_GETTEXTLENGTH As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.SB_GETTEXTLENGTHA, win.SB_GETTEXTLENGTHW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared SB_SETTIPTEXT As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.SB_SETTIPTEXTA, win.SB_SETTIPTEXTW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared SB_GETTIPTEXT As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.SB_GETTIPTEXTA, win.SB_GETTIPTEXTW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared TB_SAVERESTORE As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TB_SAVERESTOREA, win.TB_SAVERESTOREW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared TB_ADDSTRING As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TB_ADDSTRINGA, win.TB_ADDSTRINGW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared TB_GETBUTTONTEXT As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TB_GETBUTTONTEXTA, win.TB_GETBUTTONTEXTW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared TB_MAPACCELERATOR As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TB_MAPACCELERATORA, win.TB_MAPACCELERATORW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared TB_GETBUTTONINFO As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TB_GETBUTTONINFOA, win.TB_GETBUTTONINFOW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared TB_SETBUTTONINFO As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TB_SETBUTTONINFOA, win.TB_SETBUTTONINFOW) 'Note:  For performance reasons this should be changed to nested IF statements
'    '
'    'Note:  Error processing original source shown below
'    '        public static readonly int TB_SETBUTTONINFO = Marshal.SystemDefaultCharSize == 1 ? win.TB_SETBUTTONINFOA : win.TB_SETBUTTONINFOW;
'    '#if cpr
'    '---^--- Pre-processor directives not translated
'    Public Const RB_GETBANDINFO As Integer = IIf(Win32Lib.systemCommCtrlVersion >= Win32Lib.WIN32_IE400, IIf(Marshal.SystemDefaultCharSize = 1, win.RB_GETBANDINFOA, win.RB_GETBANDINFOW), win.RB_GETBANDINFO_OLD)
'    'Note:  For performance reasons this should be changed to nested IF statements
'    'Note:  For performance reasons this should be changed to nested IF statements
'    Public Const TB_INSERTBUTTON As Integer = IIf(Marshal.SystemDefaultCharSize = 1 OrElse Win32Lib.systemCommCtrlVersion < Win32Lib.WIN32_IE400, win.TB_INSERTBUTTONA, win.TB_INSERTBUTTONW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Const TB_ADDBUTTONS As Integer = IIf(Marshal.SystemDefaultCharSize = 1 OrElse Win32Lib.systemCommCtrlVersion < Win32Lib.WIN32_IE400, win.TB_ADDBUTTONSA, win.TB_ADDBUTTONSW) 'Note:  For performance reasons this should be changed to nested IF statements
'    '
'    'Note:  Error processing original source shown below
'    '                                         win.TB_ADDBUTTONSA : win.TB_ADDBUTTONSW;
'    '#else
'    '---^--- Pre-processor directives not translated
'    Public Shared RB_GETBANDINFO As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.RB_GETBANDINFOA, win.RB_GETBANDINFOW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared TB_INSERTBUTTON As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TB_INSERTBUTTONA, win.TB_INSERTBUTTONW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared TB_ADDBUTTONS As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TB_ADDBUTTONSA, win.TB_ADDBUTTONSW) 'Note:  For performance reasons this should be changed to nested IF statements
'    '
'    'Note:  Error processing original source shown below
'    '        public static readonly int TB_ADDBUTTONS = (Marshal.SystemDefaultCharSize == 1) ? win.TB_ADDBUTTONSA : win.TB_ADDBUTTONSW;
'    '#endif
'    '---^--- Pre-processor directives not translated
'    Public Shared TBN_GETBUTTONINFO As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TBN_GETBUTTONINFOA, win.TBN_GETBUTTONINFOW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared TBN_GETINFOTIP As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TBN_GETINFOTIPA, win.TBN_GETINFOTIPW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared TBN_GETDISPINFO As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TBN_GETDISPINFOA, win.TBN_GETDISPINFOW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared TTM_ADDTOOL As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TTM_ADDTOOLA, win.TTM_ADDTOOLW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared TTM_DELTOOL As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TTM_DELTOOLA, win.TTM_DELTOOLW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared TTM_NEWTOOLRECT As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TTM_NEWTOOLRECTA, win.TTM_NEWTOOLRECTW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared TTM_GETTOOLINFO As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TTM_GETTOOLINFOA, win.TTM_GETTOOLINFOW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared TTM_SETTOOLINFO As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TTM_SETTOOLINFOA, win.TTM_SETTOOLINFOW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared TTM_HITTEST As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TTM_HITTESTA, win.TTM_HITTESTW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared TTM_GETTEXT As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TTM_GETTEXTA, win.TTM_GETTEXTW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared TTM_UPDATETIPTEXT As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TTM_UPDATETIPTEXTA, win.TTM_UPDATETIPTEXTW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared TTM_ENUMTOOLS As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TTM_ENUMTOOLSA, win.TTM_ENUMTOOLSW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared TTM_GETCURRENTTOOL As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TTM_GETCURRENTTOOLA, win.TTM_GETCURRENTTOOLW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared TTN_GETDISPINFO As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TTN_GETDISPINFOA, win.TTN_GETDISPINFOW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared TTN_NEEDTEXT As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TTN_NEEDTEXTA, win.TTN_NEEDTEXTW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared TVM_INSERTITEM As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TVM_INSERTITEMA, win.TVM_INSERTITEMW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared TVM_GETITEM As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TVM_GETITEMA, win.TVM_GETITEMW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared TVM_SETITEM As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TVM_SETITEMA, win.TVM_SETITEMW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared TVM_EDITLABEL As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TVM_EDITLABELA, win.TVM_EDITLABELW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared TVM_GETISEARCHSTRING As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TVM_GETISEARCHSTRINGA, win.TVM_GETISEARCHSTRINGW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared TVN_SELCHANGING As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TVN_SELCHANGINGA, win.TVN_SELCHANGINGW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared TVN_SELCHANGED As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TVN_SELCHANGEDA, win.TVN_SELCHANGEDW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared TVN_GETDISPINFO As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TVN_GETDISPINFOA, win.TVN_GETDISPINFOW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared TVN_SETDISPINFO As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TVN_SETDISPINFOA, win.TVN_SETDISPINFOW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared TVN_ITEMEXPANDING As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TVN_ITEMEXPANDINGA, win.TVN_ITEMEXPANDINGW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared TVN_ITEMEXPANDED As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TVN_ITEMEXPANDEDA, win.TVN_ITEMEXPANDEDW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared TVN_BEGINDRAG As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TVN_BEGINDRAGA, win.TVN_BEGINDRAGW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared TVN_BEGINRDRAG As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TVN_BEGINRDRAGA, win.TVN_BEGINRDRAGW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared TVN_DELETEITEM As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TVN_DELETEITEMA, win.TVN_DELETEITEMW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared TVN_BEGINLABELEDIT As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TVN_BEGINLABELEDITA, win.TVN_BEGINLABELEDITW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared TVN_ENDLABELEDIT As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TVN_ENDLABELEDITA, win.TVN_ENDLABELEDITW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared TCM_GETITEM As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TCM_GETITEMA, win.TCM_GETITEMW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared TCM_SETITEM As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TCM_SETITEMA, win.TCM_SETITEMW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared TCM_INSERTITEM As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TCM_INSERTITEMA, win.TCM_INSERTITEMW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Public Shared CP_WINNEUTRAL As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.CP_WINANSI, win.CP_WINUNICODE) 'Note:  For performance reasons this should be changed to nested IF statements
    
    
'    Public Shared LBSELCHSTRINGA As String = "commdlg_LBSelChangedNotify"
'    Public Shared SHAREVISTRINGA As String = "commdlg_ShareViolation"
'    Public Shared FILEOKSTRINGA As String = "commdlg_FileNameOK"
'    Public Shared COLOROKSTRINGA As String = "commdlg_ColorOK"
'    Public Shared SETRGBSTRINGA As String = "commdlg_SetRGBColor"
'    Public Shared HELPMSGSTRINGA As String = "commdlg_help"
'    Public Shared FINDMSGSTRINGA As String = "commdlg_FindReplace"
'    Public Shared LBSELCHSTRINGW As String = "commdlg_LBSelChangedNotify"
'    Public Shared SHAREVISTRINGW As String = "commdlg_ShareViolation"
'    Public Shared FILEOKSTRINGW As String = "commdlg_FileNameOK"
'    Public Shared COLOROKSTRINGW As String = "commdlg_ColorOK"
'    Public Shared SETRGBSTRINGW As String = "commdlg_SetRGBColor"
'    Public Shared HELPMSGSTRINGW As String = "commdlg_help"
'    Public Shared FINDMSGSTRINGW As String = "commdlg_FindReplace"
'    Public Shared SZDDESYS_TOPIC As String = "System"
'    Public Shared SZDDESYS_ITEM_TOPICS As String = "Topics"
'    Public Shared SZDDESYS_ITEM_SYSITEMS As String = "SysItems"
'    Public Shared SZDDESYS_ITEM_RTNMSG As String = "ReturnMessage"
'    Public Shared SZDDESYS_ITEM_STATUS As String = "Status"
'    Public Shared SZDDESYS_ITEM_FORMATS As String = "Formats"
'    Public Shared SZDDESYS_ITEM_HELP As String = "Help"
'    Public Shared SZDDE_ITEM_ITEMLIST As String = "TopicItemList"
'    Public Shared ALL_TRANSPORTS As String = "M???"
'    Public Shared MS_NBF As String = "MNBF"
'    Public Shared MS_DEF_PROV_A As String = "Microsoft Base Cryptographic Provider v1.0"
'    Public Shared MS_DEF_PROV_W As String = "Microsoft Base Cryptographic Provider v1.0"
'    Public Shared SE_CREATE_TOKEN_NAME As String = "SeCreateTokenPrivilege"
'    Public Shared SE_ASSIGNPRIMARYTOKEN_NAME As String = "SeAssignPrimaryTokenPrivilege"
'    Public Shared SE_LOCK_MEMORY_NAME As String = "SeLockMemoryPrivilege"
'    Public Shared SE_INCREASE_QUOTA_NAME As String = "SeIncreaseQuotaPrivilege"
'    Public Shared SE_UNSOLICITED_INPUT_NAME As String = "SeUnsolicitedInputPrivilege"
'    Public Shared SE_MACHINE_ACCOUNT_NAME As String = "SeMachineAccountPrivilege"
'    Public Shared SE_TCB_NAME As String = "SeTcbPrivilege"
'    Public Shared SE_SECURITY_NAME As String = "SeSecurityPrivilege"
'    Public Shared SE_TAKE_OWNERSHIP_NAME As String = "SeTakeOwnershipPrivilege"
'    Public Shared SE_LOAD_DRIVER_NAME As String = "SeLoadDriverPrivilege"
'    Public Shared SE_SYSTEM_PROFILE_NAME As String = "SeSystemProfilePrivilege"
'    Public Shared SE_SYSTEMTIME_NAME As String = "SeSystemtimePrivilege"
'    Public Shared SE_PROF_SINGLE_PROCESS_NAME As String = "SeProfileSingleProcessPrivilege"
'    Public Shared SE_INC_BASE_PRIORITY_NAME As String = "SeIncreaseBasePriorityPrivilege"
'    Public Shared SE_CREATE_PAGEFILE_NAME As String = "SeCreatePagefilePrivilege"
'    Public Shared SE_CREATE_PERMANENT_NAME As String = "SeCreatePermanentPrivilege"
'    Public Shared SE_BACKUP_NAME As String = "SeBackupPrivilege"
'    Public Shared SE_RESTORE_NAME As String = "SeRestorePrivilege"
'    Public Shared SE_SHUTDOWN_NAME As String = "SeShutdownPrivilege"
'    Public Shared SE_DEBUG_NAME As String = "SeDebugPrivilege"
'    Public Shared SE_AUDIT_NAME As String = "SeAuditPrivilege"
'    Public Shared SE_SYSTEM_ENVIRONMENT_NAME As String = "SeSystemEnvironmentPrivilege"
'    Public Shared SE_CHANGE_NOTIFY_NAME As String = "SeChangeNotifyPrivilege"
'    Public Shared SE_REMOTE_SHUTDOWN_NAME As String = "SeRemoteShutdownPrivilege"
'    Public Shared SPLREG_DEFAULT_SPOOL_DIRECTORY As String = "DefaultSpoolDirectory"
'    Public Shared SPLREG_PORT_THREAD_PRIORITY_DEFAULT As String = "PortThreadPriorityDefault"
'    Public Shared SPLREG_PORT_THREAD_PRIORITY As String = "PortThreadPriority"
'    Public Shared SPLREG_SCHEDULER_THREAD_PRIORITY_DEFAULT As String = "SchedulerThreadPriorityDefault"
'    Public Shared SPLREG_SCHEDULER_THREAD_PRIORITY As String = "SchedulerThreadPriority"
'    Public Shared SPLREG_BEEP_ENABLED As String = "BeepEnabled"
'    Public Shared SPLREG_NET_POPUP As String = "NetPopup"
'    Public Shared SPLREG_EVENT_LOG As String = "EventLog"
'    Public Shared SPLREG_MAJOR_VERSION As String = "MajorVersion"
'    Public Shared SPLREG_MINOR_VERSION As String = "MinorVersion"
'    Public Shared SPLREG_ARCHITECTURE As String = "Architecture"
'    Public Shared SERVICES_ACTIVE_DATABASEW As String = "ServicesActive"
'    Public Shared SERVICES_FAILED_DATABASEW As String = "ServicesFailed"
'    Public Shared SERVICES_ACTIVE_DATABASEA As String = "ServicesActive"
'    Public Shared SERVICES_FAILED_DATABASEA As String = "ServicesFailed"
'    Public Shared WC_HEADERA As String = "SysHeader32"
'    Public Shared WC_HEADERW As String = "SysHeader32"
'    Public Shared WC_HEADER As String = "SysHeader"
'    Public Shared TOOLBARCLASSNAMEW As String = "WFCToolbarWindow32"
'    Public Shared TOOLBARCLASSNAMEA As String = "WFCToolbarWindow32"
'    Public Shared TOOLBARCLASSNAME As String = "WFCToolbarWindow32"
'    Public Shared TOOLBARCLASSNAMEW As String = "ToolbarWindow32"
'    Public Shared TOOLBARCLASSNAMEA As String = "ToolbarWindow32"
'    Public Shared TOOLBARCLASSNAME As String = "ToolbarWindow32"
'    Public Shared REBARCLASSNAMEW As String = "ReBarWindow32"
'    Public Shared REBARCLASSNAMEA As String = "ReBarWindow32"
'    Public Shared REBARCLASSNAME As String = "ReBarWindow32"
'    Public Shared TOOLTIPS_CLASSW As String = "WFCTooltips32"
'    Public Shared TOOLTIPS_CLASSA As String = "WFCTooltips32"
'    Public Shared TOOLTIPS_Class As String = "WFCTooltips32"
'    Public Shared TOOLTIPS_CLASSW As String = "tooltips_class32"
'    Public Shared TOOLTIPS_CLASSA As String = "tooltips_class32"
'    Public Shared TOOLTIPS_Class As String = "tooltips_class32"
'    Public Shared DRAGLISTMSGSTRING As String = "commctrl_DragListMsg"
'    Public Shared HOTKEY_CLASSA As String = "msctls_hotkey32"
'    Public Shared HOTKEY_CLASSW As String = "msctls_hotkey32"
'    Public Shared HOTKEY_Class As String = "msctls_hotkey32"
'    Public Shared WC_BUTTON As String = "BUTTON"
'    Public Shared WC_COMBOBOX As String = "COMBOBOX"
'    Public Shared WC_EDIT As String = "EDIT"
'    Public Shared WC_LISTBOX As String = "LISTBOX"
'    Public Shared WC_MDICLIENT As String = "MDICLIENT"
'    Public Shared WC_SCROLLBAR As String = "SCROLLBAR"
'    Public Shared WC_RICHEDITA As String = "RichEdit32"
'    Public Shared WC_RICHEDITW As String = "RichEdit32"
'    Public Shared WC_RICHEDIT As String = "RichEdit32"
'    Public Shared WC_DATETIMEPICKA As String = "WFCDateTimePick32"
'    Public Shared WC_DATETIMEPICKW As String = "WFCDateTimePick32"
'    Public Shared WC_DATETIMEPICK As String = "WFCDateTimePick32"
'    Public Shared WC_LISTVIEWA As String = "WFCListView32"
'    Public Shared WC_LISTVIEWW As String = "WFCListView32"
'    Public Shared WC_LISTVIEW As String = "WFCListView32"
'    Public Shared WC_MONTHCALA As String = "WFCMonthCal32"
'    Public Shared WC_MONTHCALW As String = "WFCMonthCal32"
'    Public Shared WC_MONTHCAL As String = "WFCMonthCal32"
'    Public Shared WC_PROGRESSA As String = "WFCProgress32"
'    Public Shared WC_PROGRESSW As String = "WFCProgress32"
'    Public Shared WC_PROGRESS As String = "WFCProgress32"
'    Public Shared WC_STATUSBARA As String = "WFCStatusBar32"
'    Public Shared WC_STATUSBARW As String = "WFCStatusBar32"
'    Public Shared WC_STATUSBAR As String = "WFCStatusBar32"
'    Public Shared WC_TOOLBAR As String = "WFCToolbarWindow32"
'    Public Shared WC_TRACKBARA As String = "WFCTrackbar32"
'    Public Shared WC_TRACKBARW As String = "WFCTrackbar32"
'    Public Shared WC_TRACKBAR As String = "WFCTrackbar32"
'    Public Shared WC_TREEVIEWA As String = "WFCTreeView32"
'    Public Shared WC_TREEVIEWW As String = "WFCTreeView32"
'    Public Shared WC_TREEVIEW As String = "WFCTreeView32"
'    Public Shared WC_DATETIMEPICKA As String = "SysDateTimePick32"
'    Public Shared WC_DATETIMEPICKW As String = "SysDateTimePick32"
'    Public Shared WC_DATETIMEPICK As String = "SysDateTimePick32"
'    Public Shared WC_LISTVIEWA As String = "SysListView32"
'    Public Shared WC_LISTVIEWW As String = "SysListView32"
'    Public Shared WC_LISTVIEW As String = "SysListView32"
'    Public Shared WC_MONTHCALA As String = "SysMonthCal32"
'    Public Shared WC_MONTHCALW As String = "SysMonthCal32"
'    Public Shared WC_MONTHCAL As String = "SysMonthCal32"
'    Public Shared WC_PROGRESSA As String = "msctls_progress32"
'    Public Shared WC_PROGRESSW As String = "msctls_progress32"
'    Public Shared WC_PROGRESS As String = "msctls_progress32"
'    Public Shared WC_STATUSBARA As String = "msctls_statusbar32"
'    Public Shared WC_STATUSBARW As String = "msctls_statusbar32"
'    Public Shared WC_STATUSBAR As String = "msctls_statusbar32"
'    Public Shared WC_TOOLBAR As String = "ToolbarWindow32"
'    Public Shared WC_TRACKBARA As String = "msctls_trackbar32"
'    Public Shared WC_TRACKBARW As String = "msctls_trackbar32"
'    Public Shared WC_TRACKBAR As String = "msctls_trackbar32"
'    Public Shared WC_TREEVIEWA As String = "SysTreeView32"
'    Public Shared WC_TREEVIEWW As String = "SysTreeView32"
'    Public Shared WC_TREEVIEW As String = "SysTreeView32"
'    Public Shared WC_COMBOBOXEXW As String = "ComboBoxEx32"
'    Public Shared WC_COMBOBOXEXA As String = "ComboBoxEx32"
'    Public Shared WC_COMBOBOXEX As String = "ComboBoxEx32"
'    Public Shared WC_STATIC As String = "STATIC"
'    Public Shared WC_TABCONTROLA As String = "WFCTabControl32"
'    Public Shared WC_TABCONTROLW As String = "WFCTabControl32"
'    Public Shared WC_TABCONTROL As String = "WFCTabControl32"
'    Public Shared WC_TABCONTROLA As String = "SysTabControl32"
'    Public Shared WC_TABCONTROLW As String = "SysTabControl32"
'    Public Shared WC_TABCONTROL As String = "SysTabControl32"
'    Public Shared LBSELCHSTRING As String = "commdlg_LBSelChangedNotify"
'    Public Shared FINDMSGSTRING As String = "commdlg_FindReplace"
'    Public Shared SHAREVISTRING As String = "commdlg_ShareViolation"
'    Public Shared SERVICES_FAILED_DATABASE As String = "ServicesFailed"
'    Public Shared MS_DEF_PROV_ As String = "Microsoft Base Cryptographic Provider v1.0"
'    Public Shared HELPMSGSTRING As String = "commdlg_help"
'    Public Shared FILEOKSTRING As String = "commdlg_FileNameOK"
'    Public Shared SERVICES_ACTIVE_DATABASE As String = "ServicesActive"
'    Public Shared COLOROKSTRING As String = "commdlg_ColorOK"
'    Public Shared SETRGBSTRING As String = "commdlg_SetRGBColor"
'    Public Shared MSH_MOUSEWHEEL As String = "MSWHEEL_ROLLMSG"
'    Public Shared MSH_SCROLL_LINES As String = "MSH_SCROLL_LINES_MSG"
'    Public Shared MSH_WHEELSUPPORT As String = "MSH_WHEELSUPPORT_MSG"
'    Public Shared MOUSEZ_CLASSNAME As String = "MouseZ"
'    Public Shared MOUSEZ_TITLE As String = "Magellan MSWHEEL"
    
'    '
'    'Note:  Error processing original source shown below
'    '        
'    '#if WINCTL                                        
'    '---^--- Pre-processor directives not translated
'    '
'    'Note:  Error processing original source shown below
'    '        TOOLBARCLASSNAME = "WFCToolbarWindow32",
'    '#else
'    '---^--- Pre-processor directives not translated
'    '
'    'Note:  Error processing original source shown below
'    '        TOOLBARCLASSNAME = "ToolbarWindow32",
'    '#endif 
'    '---^--- Pre-processor directives not translated
    
    
'    '
'    'Note:  Error processing original source shown below
'    '        
'    '#if WINCTL        
'    '---^--- Pre-processor directives not translated
'    '
'    'Note:  Error processing original source shown below
'    '        TOOLTIPS_CLASS = "WFCTooltips32",
'    '#else
'    '---^--- Pre-processor directives not translated
'    '
'    'Note:  Error processing original source shown below
'    '        TOOLTIPS_CLASS = "tooltips_class32",
'    '#endif
'    '---^--- Pre-processor directives not translated
'    '
''        STATUSCLASSNAMEW = "msctls_statusbar32",
''        STATUSCLASSNAMEA = "msctls_statusbar32",
''        STATUSCLASSNAME = "msctls_statusbar32",
''        TRACKBAR_CLASSA = "msctls_trackbar32",
''        TRACKBAR_CLASSW = "msctls_trackbar32",
''        TRACKBAR_CLASS = "msctls_trackbar32",
''        
'    '
''        UPDOWN_CLASSA = "msctls_updown32",
''        UPDOWN_CLASSW = "msctls_updown32",
''        UPDOWN_CLASS = "msctls_updown32",
''        PROGRESS_CLASSA = "msctls_progress32",
''        PROGRESS_CLASSW = "msctls_progress32",
''        PROGRESS_CLASS = "msctls_progress32",
''        
    
'    '
'    'Note:  Error processing original source shown below
'    '                 
'    '#if WINCTL
'    '---^--- Pre-processor directives not translated
'    '
'    'Note:  Error processing original source shown below
'    '        WC_TREEVIEW = "WFCTreeView32",
'    '#else
'    '---^--- Pre-processor directives not translated
'    '
'    'Note:  Error processing original source shown below
'    '        WC_TREEVIEW = "SysTreeView32",
'    '#endif
'    '---^--- Pre-processor directives not translated
    
'    '
'    'Note:  Error processing original source shown below
'    '        
'    '#if WINCTL                         
'    '---^--- Pre-processor directives not translated
'    '
'    'Note:  Error processing original source shown below
'    '        WC_TABCONTROL = "WFCTabControl32",
'    '#else        
'    '---^--- Pre-processor directives not translated
'    '
'    'Note:  Error processing original source shown below
'    '        WC_TABCONTROL = "SysTabControl32",
'    '#endif        
'    '---^--- Pre-processor directives not translated
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                                                                                                                                                                                                                                                  DOMAIN_ALIAS_RID_REPLICATOR = (0x00000228),
'    '                                                                                                                                                                                                                                                                                                                                                                                                                DACL_SECURITY_INFORMATION = (0X00000004),
'    '-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- Syntax error: ')' expected
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                                                                                                                                                                                                                                                  DOMAIN_ALIAS_RID_REPLICATOR = (0x00000228),
'    '                                                                                                                                                                                                                                                                                                                                                                                                                DACL_SECURITY_INFORMATION = (0X00000004),
'    '--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- Syntax error: 'identifier' expected
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                                                                                                                                                                                                                                                  DOMAIN_ALIAS_RID_REPLICATOR = (0x00000228),
'    '                                                                                                                                                                                                                                                                                                                                                                                                                DACL_SECURITY_INFORMATION = (0X00000004),
'    '---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- Syntax error: 'identifier' expected
'    '
'    'Note:  Error processing original source shown below
'    '        EVENT_OBJECT_SELECTION = 0x8006,
'    '        EVENTLOG_SEQUENTIAL_READ = 0X0001,
'    '-------------------------------------^--- Syntax error: ';' expected
'    '
'    'Note:  Error processing original source shown below
'    '        EVENT_OBJECT_SELECTION = 0x8006,
'    '        EVENTLOG_SEQUENTIAL_READ = 0X0001,
'    '------------------------------------------^--- Syntax error: 'identifier' expected
'    '
'    'Note:  Error processing original source shown below
'    '        EVENTLOG_SEQUENTIAL_READ = 0X0001,
'    '        EVENTLOG_SEEK_READ = 0X0002,
'    '-------------------------------^--- Syntax error: ';' expected
'    '
'    'Note:  Error processing original source shown below
'    '        EVENTLOG_SEQUENTIAL_READ = 0X0001,
'    '        EVENTLOG_SEEK_READ = 0X0002,
'    '------------------------------------^--- Syntax error: 'identifier' expected
'    '
'    'Note:  Error processing original source shown below
'    '        EVENTLOG_SEEK_READ = 0X0002,
'    '        EVENTLOG_FORWARDS_READ = 0X0004,
'    '-----------------------------------^--- Syntax error: ';' expected
'    '
'    'Note:  Error processing original source shown below
'    '        EVENTLOG_SEEK_READ = 0X0002,
'    '        EVENTLOG_FORWARDS_READ = 0X0004,
'    '----------------------------------------^--- Syntax error: 'identifier' expected
'    '
'    'Note:  Error processing original source shown below
'    '        EVENTLOG_FORWARDS_READ = 0X0004,
'    '        EVENTLOG_BACKWARDS_READ = 0X0008,
'    '------------------------------------^--- Syntax error: ';' expected
'    '
'    'Note:  Error processing original source shown below
'    '        EVENTLOG_FORWARDS_READ = 0X0004,
'    '        EVENTLOG_BACKWARDS_READ = 0X0008,
'    '-----------------------------------------^--- Syntax error: 'identifier' expected
'    '
'    'Note:  Error processing original source shown below
'    '        EVENTLOG_BACKWARDS_READ = 0X0008,
'    '        EVENTLOG_SUCCESS = 0X0000,
'    '-----------------------------^--- Syntax error: ';' expected
'    '
'    'Note:  Error processing original source shown below
'    '        EVENTLOG_BACKWARDS_READ = 0X0008,
'    '        EVENTLOG_SUCCESS = 0X0000,
'    '----------------------------------^--- Syntax error: 'identifier' expected
'    '
'    'Note:  Error processing original source shown below
'    '        GENERIC_ALL = (0x10000000),
'    '        GROUP_SECURITY_INFORMATION = (0X00000002),
'    '----------------------------------------^--- Syntax error: ')' expected
'    '
'    'Note:  Error processing original source shown below
'    '        GENERIC_ALL = (0x10000000),
'    '        GROUP_SECURITY_INFORMATION = (0X00000002),
'    '-------------------------------------------------^--- Syntax error: 'identifier' expected
'    '
'    'Note:  Error processing original source shown below
'    '        GENERIC_ALL = (0x10000000),
'    '        GROUP_SECURITY_INFORMATION = (0X00000002),
'    '--------------------------------------------------^--- Syntax error: 'identifier' expected
'    '
'    'Note:  Error processing original source shown below
'    '        MAP_FOLDDIGITS = 0x00000080;
'    '        public const long MAXLONGLONG = (0x7fffffffffffffffL);
'    '-------------------------------------------------------------^--- expression expected
'    '
'    'Note:  Error processing original source shown below
'    '        OBJECT_INHERIT_ACE = (0x1),
'    '                             OWNER_SECURITY_INFORMATION = (0X00000001),
'    '-------------------------------------------------------------^--- Syntax error: ')' expected
'    '
'    'Note:  Error processing original source shown below
'    '        OBJECT_INHERIT_ACE = (0x1),
'    '                             OWNER_SECURITY_INFORMATION = (0X00000001),
'    '----------------------------------------------------------------------^--- Syntax error: 'identifier' expected
'    '
'    'Note:  Error processing original source shown below
'    '        OBJECT_INHERIT_ACE = (0x1),
'    '                             OWNER_SECURITY_INFORMATION = (0X00000001),
'    '-----------------------------------------------------------------------^--- Syntax error: 'identifier' expected
'    '
'    'Note:  Error processing original source shown below
'    '        SECURITY_WORLD_RID = (0x00000000),
'    '        SECURITY_LOCAL_RID = (0X00000000),
'    '--------------------------------^--- Syntax error: ')' expected
'    '
'    'Note:  Error processing original source shown below
'    '        SECURITY_WORLD_RID = (0x00000000),
'    '        SECURITY_LOCAL_RID = (0X00000000),
'    '-----------------------------------------^--- Syntax error: 'identifier' expected
'    '
'    'Note:  Error processing original source shown below
'    '        SECURITY_WORLD_RID = (0x00000000),
'    '        SECURITY_LOCAL_RID = (0X00000000),
'    '------------------------------------------^--- Syntax error: 'identifier' expected
'    '
'    'Note:  Error processing original source shown below
'    '                                                      SECURITY_STATIC_TRACKING = (false);
'    '        public const int SACL_SECURITY_INFORMATION = (0X00000008),
'    '--------------------------------------------------------^--- Syntax error: ')' expected
'    '
'    'Note:  Error processing original source shown below
'    '                                                      SECURITY_STATIC_TRACKING = (false);
'    '        public const int SACL_SECURITY_INFORMATION = (0X00000008),
'    '-----------------------------------------------------------------^--- Syntax error: 'identifier' expected
'    '
'    'Note:  Error processing original source shown below
'    '                                                      SECURITY_STATIC_TRACKING = (false);
'    '        public const int SACL_SECURITY_INFORMATION = (0X00000008),
'    '------------------------------------------------------------------^--- Syntax error: 'identifier' expected
'    '
''        ANIMATE_CLASSW = "SysAnimate32",
''        ANIMATE_CLASSA = "SysAnimate32",
''        ANIMATE_CLASS = "SysAnimate32",
''        MONTHCAL_CLASSW = "SysMonthCal32",
''        MONTHCAL_CLASSA = "SysMonthCal32",
''        MONTHCAL_CLASS = "SysMonthCal32",
''        DATETIMEPICK_CLASSW = "SysDateTimePick32",
''        DATETIMEPICK_CLASSA = "SysDateTimePick32",
''        DATETIMEPICK_CLASS = "SysDateTimePick32",
''        
    
    
'    Public Shared HKEY_CLASSES_ROOT As RegistryHive = RegistryHive.ClassesRoot
'    Public Shared HKEY_CURRENT_USER As RegistryHive = RegistryHive.CurrentUser
'    Public Shared HKEY_LOCAL_MACHINE As RegistryHive = RegistryHive.LocalMachine
'    Public Shared HKEY_USERS As RegistryHive = RegistryHive.Users
'    Public Shared HKEY_PERFORMANCE_DATA As RegistryHive = RegistryHive.PerformanceData
'    Public Shared HKEY_CURRENT_CONFIG As RegistryHive = RegistryHive.CurrentConfig
'    Public Shared HKEY_DYN_DATA As RegistryHive = RegistryHive.DynData
End Class 'win

End Namespace
