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

Namespace Microsoft.VisualStudio.Editors.Interop


' Users can make their classes implement this interface to get access to all
' the constants in the Win32 API easily.
    <System.Runtime.InteropServices.ComVisible(False)> _
Friend Class win
'    Friend Const APPCMD_CLIENTONLY As Integer = &H10
'    Friend Const APPCMD_FILTERINITS As Integer = &H20
'    Friend Const APPCMD_MASK As Integer = &HFF0
'    Friend Const APPCLASS_STANDARD As Integer = &H0
'    Friend Const APPCLASS_MASK As Integer = &HF
'    Friend Const APPCLASS_MONITOR As Integer = &H1
'    Friend Const ATTR_INPUT As Integer = &H0
'    Friend Const ATTR_TARGET_CONVERTED As Integer = &H1
'    Friend Const ATTR_CONVERTED As Integer = &H2
'    Friend Const ATTR_TARGET_NOTCONVERTED As Integer = &H3
'    Friend Const ATTR_INPUT_ERROR As Integer = &H4
'    Friend Const AUXCAPS_CDAUDIO As Integer = 1
'    Friend Const AUXCAPS_AUXIN As Integer = 2
'    Friend Const AUXCAPS_VOLUME As Integer = &H1
'    Friend Const AUXCAPS_LRVOLUME As Integer = &H2
    '    Friend Const ASYNCH As Integer = &h80
    '    Friend Const ASYNC_MODE_COMPATIBILITY As Integer = &H1
'    Friend Const ASYNC_MODE_DEFAULT As Integer = &H0
'    Friend Const ACTIVEOBJECT_STRONG As Integer = &H0
'    Friend Const ACTIVEOBJECT_WEAK As Integer = &H1
'    Friend Const ABM_NEW As Integer = &H0
'    Friend Const ABM_REMOVE As Integer = &H1
'    Friend Const ABM_QUERYPOS As Integer = &H2
'    Friend Const ABM_SETPOS As Integer = &H3
'    Friend Const ABM_GETSTATE As Integer = &H4
'    Friend Const ABM_GETTASKBARPOS As Integer = &H5
'    Friend Const ABM_ACTIVATE As Integer = &H6
'    Friend Const ABM_GETAUTOHIDEBAR As Integer = &H7
'    Friend Const ABM_SETAUTOHIDEBAR As Integer = &H8
'    Friend Const ABM_WINDOWPOSCHANGED As Integer = &H9
'    Friend Const ABN_STATECHANGE As Integer = &H0
'    Friend Const ABN_POSCHANGED As Integer = &H1
'    Friend Const ABN_FULLSCREENAPP As Integer = &H2
'    Friend Const ABN_WINDOWARRANGE As Integer = &H3
'    Friend Const ABS_AUTOHIDE As Integer = &H1
'    Friend Const ABS_ALWAYSONTOP As Integer = &H2
'    Friend Const ABE_LEFT As Integer = 0
'    Friend Const ABE_TOP As Integer = 1
'    Friend Const ABE_RIGHT As Integer = 2
'    Friend Const ABE_BOTTOM As Integer = 3
'    Friend Const AC_LINE_OFFLINE As Integer = &H0
'    Friend Const AC_LINE_ONLINE As Integer = &H1
'    Friend Const AC_LINE_BACKUP_POWER As Integer = &H2
'    Friend Const AC_LINE_UNKNOWN As Integer = &HFF
'    Friend Const ALG_CLASS_ANY As Integer = 0
'    Friend Const AC_SRC_OVER As Integer = &H0
'    Friend Const AC_SRC_ALPHA As Integer = &H1
'    Friend Const ALG_CLASS_SIGNATURE As Integer = Machine.Shift.Left(1, 13)
'    Friend Const ALG_CLASS_MSG_ENCRYPT As Integer = Machine.Shift.Left(2, 13)
'    Friend Const ALG_CLASS_DATA_ENCRYPT As Integer = Machine.Shift.Left(3, 13)
'    Friend Const ALG_CLASS_HASH As Integer = Machine.Shift.Left(4, 13)
'    Friend Const ALG_CLASS_KEY_EXCHANGE As Integer = Machine.Shift.Left(5, 13)
'    Friend Const ALG_TYPE_ANY As Integer = 0
'    Friend Const ALG_TYPE_DSS As Integer = Machine.Shift.Left(1, 9)
'    Friend Const ALG_TYPE_RSA As Integer = Machine.Shift.Left(2, 9)
'    Friend Const ALG_TYPE_BLOCK As Integer = Machine.Shift.Left(3, 9)
'    Friend Const ALG_TYPE_STREAM As Integer = Machine.Shift.Left(4, 9)
'    Friend Const ALG_SID_ANY As Integer = 0
'    Friend Const ALG_SID_RSA_ANY As Integer = 0
'    Friend Const ALG_SID_RSA_PKCS As Integer = 1
'    Friend Const ALG_SID_RSA_MSATWORK As Integer = 2
'    Friend Const ALG_SID_RSA_ENTRUST As Integer = 3
'    Friend Const ALG_SID_RSA_PGP As Integer = 4
'    Friend Const ALG_SID_DSS_ANY As Integer = 0
'    Friend Const ALG_SID_DSS_PKCS As Integer = 1
'    Friend Const ALG_SID_DSS_DMS As Integer = 2
'    Friend Const ALG_SID_DES As Integer = 1
'    Friend Const ALG_SID_3DES As Integer = 3
'    Friend Const ALG_SID_DESX As Integer = 4
'    Friend Const ALG_SID_IDEA As Integer = 5
'    Friend Const ALG_SID_CAST As Integer = 6
'    Friend Const ALG_SID_SAFERSK64 As Integer = 7
'    Friend Const ALD_SID_SAFERSK128 As Integer = 8
'    Friend Const ALG_SID_RC2 As Integer = 2
'    Friend Const ALG_SID_RC4 As Integer = 1
'    Friend Const ALG_SID_SEAL As Integer = 2
'    Friend Const ALG_SID_MD2 As Integer = 1
'    Friend Const ALG_SID_MD4 As Integer = 2
'    Friend Const ALG_SID_MD5 As Integer = 3
'    Friend Const ALG_SID_SHA As Integer = 4
'    Friend Const ALG_SID_MAC As Integer = 5
'    Friend Const ALG_SID_RIPEMD As Integer = 6
'    Friend Const ALG_SID_RIPEMD160 As Integer = 7
'    Friend Const ALG_SID_SSL3SHAMD5 As Integer = 8
'    Friend Const ALG_SID_EXAMPLE As Integer = 80
'    Friend Const AT_KEYEXCHANGE As Integer = 1
'    Friend Const AT_SIGNATURE As Integer = 2
'    Friend Const ALTERNATE As Integer = 1
'    Friend Const ASPECT_FILTERING As Integer = &H1
'    Friend Const ABORTDOC As Integer = 2
'    Friend Const ANTIALIASED_QUALITY As Integer = 4
'    Friend Const ANSI_CHARSET As Integer = 0
'    Friend Const ARABIC_CHARSET As Integer = 178
'    Friend Const ABSOLUTE As Integer = 1
'    Friend Const ANSI_FIXED_FONT As Integer = 11
'    Friend Const ANSI_VAR_FONT As Integer = 12
'    Friend Const AD_COUNTERCLOCKWISE As Integer = 1
'    Friend Const AD_CLOCKWISE As Integer = 2
'    Friend Const ASPECTX As Integer = 40
'    Friend Const ASPECTY As Integer = 42
'    Friend Const ASPECTXY As Integer = 44
'    Friend Const ANYSIZE_ARRAY As Integer = 1
'    Friend Const APPLICATION_ERROR_MASK As Integer = &H20000000
'    Friend Const ACCESS_SYSTEM_SECURITY As Integer = &H1000000
'    Friend Const ACL_REVISION As Integer = 2
'    Friend Const ACL_REVISION1 As Integer = 1
'    Friend Const ACL_REVISION2 As Integer = 2
'    Friend Const ACL_REVISION3 As Integer = 3
'    Friend Const ACCESS_ALLOWED_ACE_TYPE As Integer = &H0
'    Friend Const ACCESS_DENIED_ACE_TYPE As Integer = &H1
'    Friend Const ARW_BOTTOMLEFT As Integer = &H0
'    Friend Const ARW_BOTTOMRIGHT As Integer = &H1
'    Friend Const ARW_TOPLEFT As Integer = &H2
'    Friend Const ARW_TOPRIGHT As Integer = &H3
'    Friend Const ARW_STARTMASK As Integer = &H3
'    Friend Const ARW_STARTRIGHT As Integer = &H1
'    Friend Const ARW_STARTTOP As Integer = &H2
'    Friend Const ARW_LEFT As Integer = &H0
'    Friend Const ARW_RIGHT As Integer = &H0
'    Friend Const ARW_UP As Integer = &H4
'    Friend Const ARW_DOWN As Integer = &H4
'    Friend Const ARW_HIDE As Integer = &H8
'    Friend Const ARW_VALID As Integer = &HF
'    Friend Const ATF_TIMEOUTON As Integer = &H1
'    Friend Const ATF_ONOFFFEEDBACK As Integer = &H2
'    Friend Const ACS_CENTER As Integer = &H1
'    Friend Const ACS_TRANSPARENT As Integer = &H2
'    Friend Const ACS_AUTOPLAY As Integer = &H4
'    Friend Const ACS_TIMER As Integer = &H8
'    Friend Const ACM_OPENA As Integer = &H400 + 100
'    Friend Const ACM_OPENW As Integer = &H400 + 103
'    Friend Const ACM_PLAY As Integer = &H400 + 101
'    Friend Const ACM_STOP As Integer = &H400 + 102
'    Friend Const ACN_START As Integer = 1
'    Friend Const ACN_STOP As Integer = 2
'    Friend Const ADVF_NODATA As Integer = 1
'    Friend Const ADVF_ONLYONCE As Integer = 2
'    Friend Const ADVF_PRIMEFIRST As Integer = 4
'    Friend Const ADVFCACHE_NOHANDLER As Integer = 8
'    Friend Const ADVFCACHE_FORCEBUILTIN As Integer = 16
'    Friend Const ADVFCACHE_ONSAVE As Integer = 32
'    Friend Const ADVFCACHE_DATAONSTOP As Integer = 64
'    Friend Const AW_HOR_POSITIVE As Integer = &H1
'    Friend Const AW_HOR_NEGATIVE As Integer = &H2
'    Friend Const AW_VER_POSITIVE As Integer = &H4
'    Friend Const AW_VER_NEGATIVE As Integer = &H8
'    Friend Const AW_CENTER As Integer = &H10
'    Friend Const AW_HIDE As Integer = &H10000
'    Friend Const AW_ACTIVATE As Integer = &H20000
'    Friend Const AW_SLIDE As Integer = &H40000
'    Friend Const AW_BLEND As Integer = &H80000
'    ' NT5 begin 
'    ' NT5 end 
    
    
'    Friend Const BOLD_FONTTYPE As Integer = &H100
'    Friend Const BAUD_075 As Integer = &H1
'    Friend Const BAUD_110 As Integer = &H2
'    Friend Const BAUD_134_5 As Integer = &H4
'    Friend Const BAUD_150 As Integer = &H8
'    Friend Const BAUD_300 As Integer = &H10
'    Friend Const BAUD_600 As Integer = &H20
'    Friend Const BAUD_1200 As Integer = &H40
'    Friend Const BAUD_1800 As Integer = &H80
'    Friend Const BAUD_2400 As Integer = &H100
'    Friend Const BAUD_4800 As Integer = &H200
'    Friend Const BAUD_7200 As Integer = &H400
'    Friend Const BAUD_9600 As Integer = &H800
'    Friend Const BAUD_14400 As Integer = &H1000
'    Friend Const BAUD_19200 As Integer = &H2000
'    Friend Const BAUD_38400 As Integer = &H4000
'    Friend Const BAUD_56K As Integer = &H8000
'    Friend Const BAUD_128K As Integer = &H10000
'    Friend Const BAUD_115200 As Integer = &H20000
'    Friend Const BAUD_57600 As Integer = &H40000
'    Friend Const BAUD_USER As Integer = &H10000000
'    Friend Const BACKUP_INVALID As Integer = &H0
'    Friend Const BACKUP_DATA As Integer = &H1
'    Friend Const BACKUP_EA_DATA As Integer = &H2
'    Friend Const BACKUP_SECURITY_DATA As Integer = &H3
'    Friend Const BACKUP_ALTERNATE_DATA As Integer = &H4
'    Friend Const BACKUP_LINK As Integer = &H5
'    Friend Const BACKUP_PROPERTY_DATA As Integer = &H6
'    Friend Const BATTERY_FLAG_HIGH As Integer = &H1
'    Friend Const BATTERY_FLAG_LOW As Integer = &H2
'    Friend Const BATTERY_FLAG_CRITICAL As Integer = &H4
'    Friend Const BATTERY_FLAG_CHARGING As Integer = &H8
'    Friend Const BATTERY_FLAG_NO_BATTERY As Integer = &h80
    '    Friend Const BATTERY_FLAG_UNKNOWN As Integer = &HFF
'    Friend Const BATTERY_PERCENTAGE_UNKNOWN As Integer = &HFF
    '    Friend Const BATTERY_LIFE_UNKNOWN As Integer = &hFFFFFFFF
    '    Friend Const BACKGROUND_BLUE As Integer = &H10
'    Friend Const BACKGROUND_GREEN As Integer = &H20
'    Friend Const BACKGROUND_RED As Integer = &H40
'    Friend Const BACKGROUND_INTENSITY As Integer = &H80
'    Friend Const BLACKONWHITE As Integer = 1
'    Friend Const BANDINFO As Integer = 24
'    Friend Const BEGIN_PATH As Integer = 4096
'    Friend Const BI_RGB As Integer = 0
'    Friend Const BI_RLE8 As Integer = 1
'    Friend Const BI_RLE4 As Integer = 2
'    Friend Const BI_BITFIELDS As Integer = 3
'    Friend Const BALTIC_CHARSET As Integer = 186
'    Friend Const BKMODE_LAST As Integer = 2
'    Friend Const BLACK_BRUSH As Integer = 4
'    Friend Const BLACK_PEN As Integer = 7
'    Friend Const BS_SOLID As Integer = 0
'    Friend Const BS_NULL As Integer = 1
'    Friend Const BS_HOLLOW As Integer = 1
'    Friend Const BS_HATCHED As Integer = 2
'    Friend Const BS_PATTERN As Integer = 3
'    Friend Const BS_INDEXED As Integer = 4
'    Friend Const BS_DIBPATTERN As Integer = 5
'    Friend Const BS_DIBPATTERNPT As Integer = 6
'    Friend Const BS_PATTERN8X8 As Integer = 7
'    Friend Const BS_DIBPATTERN8X8 As Integer = 8
'    Friend Const BS_MONOPATTERN As Integer = 9
'    Friend Const BITSPIXEL As Integer = 12
'    Friend Const BLTALIGNMENT As Integer = 119
'    Friend Const BDR_RAISEDOUTER As Integer = &H1
'    Friend Const BDR_SUNKENOUTER As Integer = &H2
'    Friend Const BDR_RAISEDINNER As Integer = &H4
'    Friend Const BDR_SUNKENINNER As Integer = &H8
'    Friend Const BDR_OUTER As Integer = &H3
'    Friend Const BDR_INNER As Integer = &HC
'    Friend Const BDR_RAISED As Integer = &H5
'    Friend Const BDR_SUNKEN As Integer = &HA
'    Friend Const BF_LEFT As Integer = &H1
'    Friend Const BF_TOP As Integer = &H2
'    Friend Const BF_RIGHT As Integer = &H4
'    Friend Const BF_BOTTOM As Integer = &H8
'    Friend Const BF_TOPLEFT As Integer = &H2 Or &H1
'    Friend Const BF_TOPRIGHT As Integer = &H2 Or &H4
'    Friend Const BF_BOTTOMLEFT As Integer = &H8 Or &H1
'    Friend Const BF_BOTTOMRIGHT As Integer = &H8 Or &H4
'    Friend Const BF_RECT As Integer = &H1 Or &H2 Or &H4 Or &H8
'    Friend Const BF_DIAGONAL As Integer = &H10
'    Friend Const BF_DIAGONAL_ENDTOPRIGHT As Integer = &H10 Or &H2 Or &H4
'    Friend Const BF_DIAGONAL_ENDTOPLEFT As Integer = &H10 Or &H2 Or &H1
'    Friend Const BF_DIAGONAL_ENDBOTTOMLEFT As Integer = &H10 Or &H8 Or &H1
'    Friend Const BF_DIAGONAL_ENDBOTTOMRIGHT As Integer = &H10 Or &H8 Or &H4
'    Friend Const BF_MIDDLE As Integer = &H800
'    Friend Const BF_SOFT As Integer = &H1000
'    Friend Const BF_ADJUST As Integer = &H2000
'    Friend Const BF_FLAT As Integer = &H4000
    '    Friend Const BF_MONO As Integer = &h8000
    '    Friend Const BSM_ALLCOMPONENTS As Integer = &H0
'    Friend Const BSM_VXDS As Integer = &H1
'    Friend Const BSM_NETDRIVER As Integer = &H2
'    Friend Const BSM_INSTALLABLEDRIVERS As Integer = &H4
'    Friend Const BSM_APPLICATIONS As Integer = &H8
'    Friend Const BSM_ALLDESKTOPS As Integer = &H10
'    Friend Const BSF_QUERY As Integer = &H1
'    Friend Const BSF_IGNORECURRENTTASK As Integer = &H2
'    Friend Const BSF_FLUSHDISK As Integer = &H4
'    Friend Const BSF_NOHANG As Integer = &H8
'    Friend Const BSF_POSTMESSAGE As Integer = &H10
'    Friend Const BSF_FORCEIFHUNG As Integer = &H20
'    Friend Const BSF_NOTIMEOUTIFNOTHUNG As Integer = &H40
'    Friend Const BROADCAST_QUERY_DENY As Integer = &H424D5144
'    Friend Const BS_PUSHBUTTON As Integer = &H0
'    Friend Const BS_DEFPUSHBUTTON As Integer = &H1
'    Friend Const BS_CHECKBOX As Integer = &H2
'    Friend Const BS_AUTOCHECKBOX As Integer = &H3
'    Friend Const BS_RADIOBUTTON As Integer = &H4
'    Friend Const BS_3STATE As Integer = &H5
'    Friend Const BS_AUTO3STATE As Integer = &H6
'    Friend Const BS_GROUPBOX As Integer = &H7
'    Friend Const BS_USERBUTTON As Integer = &H8
'    Friend Const BS_AUTORADIOBUTTON As Integer = &H9
'    Friend Const BS_OWNERDRAW As Integer = &HB
'    Friend Const BS_LEFTTEXT As Integer = &H20
'    Friend Const BS_TEXT As Integer = &H0
'    Friend Const BS_ICON As Integer = &H40
'    Friend Const BS_BITMAP As Integer = &H80
'    Friend Const BS_LEFT As Integer = &H100
'    Friend Const BS_RIGHT As Integer = &H200
'    Friend Const BS_CENTER As Integer = &H300
'    Friend Const BS_TOP As Integer = &H400
'    Friend Const BS_BOTTOM As Integer = &H800
'    Friend Const BS_VCENTER As Integer = &HC00
'    Friend Const BS_PUSHLIKE As Integer = &H1000
'    Friend Const BS_MULTILINE As Integer = &H2000
'    Friend Const BS_NOTIFY As Integer = &H4000
'    Friend Const BS_FLAT As Integer = &H8000
'    Friend Const BS_RIGHTBUTTON As Integer = &H20
'    Friend Const BN_CLICKED As Integer = 0
'    Friend Const BN_PAINT As Integer = 1
'    Friend Const BN_HILITE As Integer = 2
'    Friend Const BN_UNHILITE As Integer = 3
'    Friend Const BN_DISABLE As Integer = 4
'    Friend Const BN_DOUBLECLICKED As Integer = 5
'    Friend Const BN_PUSHED As Integer = 2
'    Friend Const BN_UNPUSHED As Integer = 3
'    Friend Const BN_DBLCLK As Integer = 5
'    Friend Const BN_SETFOCUS As Integer = 6
'    Friend Const BN_KILLFOCUS As Integer = 7
'    Friend Const BM_GETCHECK As Integer = &HF0
'    Friend Const BM_SETCHECK As Integer = &HF1
'    Friend Const BM_GETSTATE As Integer = &HF2
'    Friend Const BM_SETSTATE As Integer = &HF3
'    Friend Const BM_SETSTYLE As Integer = &HF4
'    Friend Const BM_CLICK As Integer = &HF5
'    Friend Const BM_GETIMAGE As Integer = &HF6
'    Friend Const BM_SETIMAGE As Integer = &HF7
'    Friend Const BST_UNCHECKED As Integer = &H0
'    Friend Const BST_CHECKED As Integer = &H1
'    Friend Const BST_INDETERMINATE As Integer = &H2
'    Friend Const BST_PUSHED As Integer = &H4
'    Friend Const BST_FOCUS As Integer = &H8
'    Friend Const BLACKNESS As Integer = &H42
    
    
'    Friend Const CDERR_DIALOGFAILURE As Integer = &HFFFF
'    Friend Const CDERR_GENERALCODES As Integer = &H0
'    Friend Const CDERR_STRUCTSIZE As Integer = &H1
'    Friend Const CDERR_INITIALIZATION As Integer = &H2
'    Friend Const CDERR_NOTEMPLATE As Integer = &H3
'    Friend Const CDERR_NOHINSTANCE As Integer = &H4
'    Friend Const CDERR_LOADSTRFAILURE As Integer = &H5
'    Friend Const CDERR_FINDRESFAILURE As Integer = &H6
'    Friend Const CDERR_LOADRESFAILURE As Integer = &H7
'    Friend Const CDERR_LOCKRESFAILURE As Integer = &H8
'    Friend Const CDERR_MEMALLOCFAILURE As Integer = &H9
'    Friend Const CDERR_MEMLOCKFAILURE As Integer = &HA
'    Friend Const CDERR_NOHOOK As Integer = &HB
'    Friend Const CDERR_REGISTERMSGFAIL As Integer = &HC
'    Friend Const CFERR_CHOOSEFONTCODES As Integer = &H2000
'    Friend Const CFERR_NOFONTS As Integer = &H2001
'    Friend Const CFERR_MAXLESSTHANMIN As Integer = &H2002
'    Friend Const CCERR_CHOOSECOLORCODES As Integer = &H5000
'    Friend Const CDN_FIRST As Integer = 0 - 601
'    Friend Const CDN_LAST As Integer = 0 - 699
'    Friend Const CDN_INITDONE As Integer = 0 - 601 - &H0
'    Friend Const CDN_SELCHANGE As Integer = 0 - 601 - &H1
'    Friend Const CDN_FOLDERCHANGE As Integer = 0 - 601 - &H2
'    Friend Const CDN_SHAREVIOLATION As Integer = 0 - 601 - &H3
'    Friend Const CDN_HELP As Integer = 0 - 601 - &H4
'    Friend Const CDN_FILEOK As Integer = 0 - 601 - &H5
'    Friend Const CDN_TYPECHANGE As Integer = 0 - 601 - &H6
'    Friend Const CC_RGBINIT As Integer = &H1
'    Friend Const CC_FULLOPEN As Integer = &H2
'    Friend Const CC_PREVENTFULLOPEN As Integer = &H4
'    Friend Const CC_SHOWHELP As Integer = &H8
'    Friend Const CC_ENABLEHOOK As Integer = &H10
'    Friend Const CC_ENABLETEMPLATE As Integer = &H20
'    Friend Const CC_ENABLETEMPLATEHANDLE As Integer = &H40
'    Friend Const CC_SOLIDCOLOR As Integer = &H80
'    Friend Const CC_ANYCOLOR As Integer = &H100
'    Friend Const CF_SCREENFONTS As Integer = &H1
'    Friend Const CF_PRINTERFONTS As Integer = &H2
'    Friend Const CF_BOTH As Integer = &H1 Or &H2
'    Friend Const CF_SHOWHELP As Integer = &H4
'    Friend Const CF_ENABLEHOOK As Integer = &H8
'    Friend Const CF_ENABLETEMPLATE As Integer = &H10
'    Friend Const CF_ENABLETEMPLATEHANDLE As Integer = &H20
'    Friend Const CF_INITTOLOGFONTSTRUCT As Integer = &H40
'    Friend Const CF_USESTYLE As Integer = &H80
'    Friend Const CF_EFFECTS As Integer = &H100
'    Friend Const CF_APPLY As Integer = &H200
'    Friend Const CF_ANSIONLY As Integer = &H400
'    Friend Const CF_SCRIPTSONLY As Integer = &H400
'    Friend Const CF_NOVECTORFONTS As Integer = &H800
'    Friend Const CF_NOOEMFONTS As Integer = &H800
'    Friend Const CF_NOSIMULATIONS As Integer = &H1000
'    Friend Const CF_LIMITSIZE As Integer = &H2000
'    Friend Const CF_FIXEDPITCHONLY As Integer = &H4000
'    Friend Const CF_WYSIWYG As Integer = &H8000
'    Friend Const CF_FORCEFONTEXIST As Integer = &H10000
'    Friend Const CF_SCALABLEONLY As Integer = &H20000
'    Friend Const CF_TTONLY As Integer = &H40000
'    Friend Const CF_NOFACESEL As Integer = &H80000
'    Friend Const CF_NOSTYLESEL As Integer = &H100000
'    Friend Const CF_NOSIZESEL As Integer = &H200000
'    Friend Const CF_SELECTSCRIPT As Integer = &H400000
'    Friend Const CF_NOSCRIPTSEL As Integer = &H800000
'    Friend Const CF_NOVERTFONTS As Integer = &H1000000
'    Friend Const CD_LBSELNOITEMS As Integer = - 1
'    Friend Const CD_LBSELCHANGE As Integer = 0
'    Friend Const CD_LBSELSUB As Integer = 1
'    Friend Const CD_LBSELADD As Integer = 2
'    Friend Const CADV_LATEACK As Integer = &HFFFF
'    Friend Const CP_WINANSI As Integer = 1004
'    Friend Const CP_WINUNICODE As Integer = 1200
'    ' CP_WINNEUTRAL = 1004;
'    Friend Const CBF_FAIL_SELFCONNECTIONS As Integer = &H1000
'    Friend Const CBF_FAIL_CONNECTIONS As Integer = &H2000
'    Friend Const CBF_FAIL_ADVISES As Integer = &H4000
'    Friend Const CBF_FAIL_EXECUTES As Integer = &H8000
'    Friend Const CBF_FAIL_POKES As Integer = &H10000
'    Friend Const CBF_FAIL_REQUESTS As Integer = &H20000
'    Friend Const CBF_FAIL_ALLSVRXACTIONS As Integer = &H3F000
'    Friend Const CBF_SKIP_CONNECT_CONFIRMS As Integer = &H40000
'    Friend Const CBF_SKIP_REGISTRATIONS As Integer = &H80000
'    Friend Const CBF_SKIP_UNREGISTRATIONS As Integer = &H100000
'    Friend Const CBF_SKIP_DISCONNECTS As Integer = &H200000
'    Friend Const CBF_SKIP_ALLNOTIFICATIONS As Integer = &H3C0000
'    Friend Const ctlFirst As Integer = &H400
'    Friend Const ctlLast As Integer = &H4FF
'    Friend Const chx1 As Integer = &H410
'    Friend Const chx2 As Integer = &H411
'    Friend Const chx3 As Integer = &H412
'    Friend Const chx4 As Integer = &H413
'    Friend Const chx5 As Integer = &H414
'    Friend Const chx6 As Integer = &H415
'    Friend Const chx7 As Integer = &H416
'    Friend Const chx8 As Integer = &H417
'    Friend Const chx9 As Integer = &H418
'    Friend Const chx10 As Integer = &H419
'    Friend Const chx11 As Integer = &H41A
'    Friend Const chx12 As Integer = &H41B
'    Friend Const chx13 As Integer = &H41C
'    Friend Const chx14 As Integer = &H41D
'    Friend Const chx15 As Integer = &H41E
'    Friend Const chx16 As Integer = &H41F
'    Friend Const cmb1 As Integer = &H470
'    Friend Const cmb2 As Integer = &H471
'    Friend Const cmb3 As Integer = &H472
'    Friend Const cmb4 As Integer = &H473
'    Friend Const cmb5 As Integer = &H474
'    Friend Const cmb6 As Integer = &H475
'    Friend Const cmb7 As Integer = &H476
'    Friend Const cmb8 As Integer = &H477
'    Friend Const cmb9 As Integer = &H478
'    Friend Const cmb10 As Integer = &H479
'    Friend Const cmb11 As Integer = &H47A
'    Friend Const cmb12 As Integer = &H47B
'    Friend Const cmb13 As Integer = &H47C
'    Friend Const cmb14 As Integer = &H47D
'    Friend Const cmb15 As Integer = &H47E
'    Friend Const cmb16 As Integer = &H47F
'    Friend Const CPS_COMPLETE As Integer = &H1
'    Friend Const CPS_CONVERT As Integer = &H2
'    Friend Const CPS_REVERT As Integer = &H3
'    Friend Const CPS_CANCEL As Integer = &H4
'    Friend Const CS_INSERTCHAR As Integer = &H2000
'    Friend Const CS_NOMOVECARET As Integer = &H4000
'    Friend Const CFS_DEFAULT As Integer = &H0
'    Friend Const CFS_RECT As Integer = &H1
'    Friend Const CFS_POINT As Integer = &H2
'    Friend Const CFS_FORCE_POSITION As Integer = &H20
'    Friend Const CFS_CANDIDATEPOS As Integer = &H40
'    Friend Const CFS_EXCLUDE As Integer = &H80
'    Friend Const CALLBACK_TYPEMASK As Integer = &H70000
'    Friend Const CALLBACK_NULL As Integer = &H0
'    Friend Const CALLBACK_WINDOW As Integer = &H10000
'    Friend Const CALLBACK_TASK As Integer = &H20000
'    Friend Const CALLBACK_FUNCTION As Integer = &H30000
'    Friend Const CALLBACK_THREAD As Integer = &H20000
'    Friend Const CALLBACK_EVENT As Integer = &H50000
'    Friend Const CFSEPCHAR As Char = "+"c
'    Friend Const CALL_PENDING As Integer = &H2
'    Friend Const CWCSTORAGENAME As Integer = 32
'    Friend Const COM_RIGHTS_EXECUTE As Integer = 1
'    Friend Const cbNDRContext As Integer = 20
'    Friend Const CREATE_NEW As Integer = 1
'    Friend Const CREATE_ALWAYS As Integer = 2
'    Friend Const CALLBACK_CHUNK_FINISHED As Integer = &H0
'    Friend Const CALLBACK_STREAM_SWITCH As Integer = &H1
'    Friend Const COPY_FILE_FAIL_IF_EXISTS As Integer = &H1
'    Friend Const COPY_FILE_RESTARTABLE As Integer = &H2
    '    Friend Const COMMPROP_INITIALIZED As Integer = &hE73CF52E
    '    Friend Const CREATE_SUSPENDED As Integer = &H4
'    Friend Const CREATE_NEW_CONSOLE As Integer = &H10
'    Friend Const CREATE_NEW_PROCESS_GROUP As Integer = &H200
'    Friend Const CREATE_UNICODE_ENVIRONMENT As Integer = &H400
'    Friend Const CREATE_SEPARATE_WOW_VDM As Integer = &H800
'    Friend Const CREATE_SHARED_WOW_VDM As Integer = &H1000
'    Friend Const CREATE_FORCEDOS As Integer = &H2000
'    Friend Const CREATE_DEFAULT_ERROR_MODE As Integer = &H4000000
'    Friend Const CREATE_NO_WINDOW As Integer = &H8000000
'    Friend Const CREATE_THREAD_DEBUG_EVENT As Integer = 2
'    Friend Const CREATE_PROCESS_DEBUG_EVENT As Integer = 3
'    Friend Const CBR_110 As Integer = 110
'    Friend Const CBR_300 As Integer = 300
'    Friend Const CBR_600 As Integer = 600
'    Friend Const CBR_1200 As Integer = 1200
'    Friend Const CBR_2400 As Integer = 2400
'    Friend Const CBR_4800 As Integer = 4800
'    Friend Const CBR_9600 As Integer = 9600
'    Friend Const CBR_14400 As Integer = 14400
'    Friend Const CBR_19200 As Integer = 19200
'    Friend Const CBR_38400 As Integer = 38400
'    Friend Const CBR_56000 As Integer = 56000
'    Friend Const CBR_57600 As Integer = 57600
'    Friend Const CBR_115200 As Integer = 115200
'    Friend Const CBR_128000 As Integer = 128000
'    Friend Const CBR_256000 As Integer = 256000
'    Friend Const CE_RXOVER As Integer = &H1
'    Friend Const CE_OVERRUN As Integer = &H2
'    Friend Const CE_RXPARITY As Integer = &H4
'    Friend Const CE_FRAME As Integer = &H8
'    Friend Const CE_BREAK As Integer = &H10
'    Friend Const CE_TXFULL As Integer = &H100
'    Friend Const CE_PTO As Integer = &H200
'    Friend Const CE_IOE As Integer = &H400
'    Friend Const CE_DNS As Integer = &H800
'    Friend Const CE_OOP As Integer = &H1000
    '    Friend Const CE_MODE As Integer = &h8000
    '    Friend Const CLRRTS As Integer = 4
'    Friend Const CLRDTR As Integer = 6
'    Friend Const CLRBREAK As Integer = 9
'    Friend Const CAPSLOCK_ON As Integer = &H80
'    Friend Const CTRL_C_EVENT As Integer = 0
'    Friend Const CTRL_BREAK_EVENT As Integer = 1
'    Friend Const CTRL_CLOSE_EVENT As Integer = 2
'    Friend Const CTRL_LOGOFF_EVENT As Integer = 5
'    Friend Const CTRL_SHUTDOWN_EVENT As Integer = 6
'    Friend Const CONSOLE_TEXTMODE_BUFFER As Integer = 1
'    Friend Const CRYPT_MODE_CBCI As Integer = 6
'    Friend Const CRYPT_MODE_CFBP As Integer = 7
'    Friend Const CRYPT_MODE_OFBP As Integer = 8
'    Friend Const CRYPT_MODE_CBCOFM As Integer = 9
'    Friend Const CRYPT_MODE_CBCOFMI As Integer = 10
'    Friend Const CALG_MD2 As Integer = Machine.Shift.Left(4, 13) Or 0 Or 1
'    Friend Const CALG_MD4 As Integer = Machine.Shift.Left(4, 13) Or 0 Or 2
'    Friend Const CALG_MD5 As Integer = Machine.Shift.Left(4, 13) Or 0 Or 3
'    Friend Const CALG_SHA As Integer = Machine.Shift.Left(4, 13) Or 0 Or 4
'    Friend Const CALG_MAC As Integer = Machine.Shift.Left(4, 13) Or 0 Or 5
'    Friend Const CALG_RSA_SIGN As Integer = Machine.Shift.Left(1, 13) Or Machine.Shift.Left(2, 9) Or 0
'    Friend Const CALG_DSS_SIGN As Integer = Machine.Shift.Left(1, 13) Or Machine.Shift.Left(1, 9) Or 0
'    Friend Const CALG_RSA_KEYX As Integer = Machine.Shift.Left(5, 13) Or Machine.Shift.Left(2, 9) Or 0
'    Friend Const CALG_DES As Integer = Machine.Shift.Left(3, 13) Or Machine.Shift.Left(3, 9) Or 1
'    Friend Const CALG_RC2 As Integer = Machine.Shift.Left(3, 13) Or Machine.Shift.Left(3, 9) Or 2
'    Friend Const CALG_RC4 As Integer = Machine.Shift.Left(3, 13) Or Machine.Shift.Left(4, 9) Or 1
'    Friend Const CALG_SEAL As Integer = Machine.Shift.Left(3, 13) Or Machine.Shift.Left(4, 9) Or 2
'    Friend Const CRYPT_VERIFYCONTEXT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                CALG_SEAL = ((3<<13)|(4<<9)|2),
'    '                                                                                                                                                            CRYPT_VERIFYCONTEXT = unchecked((int)0xF0000000),
'    '-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const CRYPT_NEWKEYSET As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                            CRYPT_VERIFYCONTEXT = unchecked((int)0xF0000000),
'    '        CRYPT_NEWKEYSET = unchecked((int)0x8),
'    '---------------------------^--- GenCode(token): unexpected token type
'    Friend Const CRYPT_DELETEKEYSET As Integer = &H10
'    Friend Const CRYPT_EXPORTABLE As Integer = &H1
'    Friend Const CRYPT_USER_PROTECTED As Integer = &H2
'    Friend Const CRYPT_CREATE_SALT As Integer = &H4
'    Friend Const CRYPT_UPDATE_KEY As Integer = &H8
'    Friend Const CRYPT_USERDATA As Integer = 1
'    Friend Const CRYPT_MODE_CBC As Integer = 1
'    Friend Const CRYPT_MODE_ECB As Integer = 2
'    Friend Const CRYPT_MODE_OFB As Integer = 3
'    Friend Const CRYPT_MODE_CFB As Integer = 4
'    Friend Const CRYPT_MODE_CTS As Integer = 5
'    Friend Const CRYPT_ENCRYPT As Integer = &H1
'    Friend Const CRYPT_DECRYPT As Integer = &H2
'    Friend Const CRYPT_READ As Integer = &H8
'    Friend Const CRYPT_WRITE As Integer = &H10
'    Friend Const CRYPT_MAC As Integer = &H20
'    Friend Const CRYPT_FAILED As Boolean = False
'    Friend Const CRYPT_SUCCEED As Boolean = True
'    Friend Const CRYPT_FIRST As Integer = 1
'    Friend Const CRYPT_NEXT As Integer = 2
'    Friend Const CRYPT_IMPL_HARDWARE As Integer = 1
'    Friend Const CRYPT_IMPL_SOFTWARE As Integer = 2
'    Friend Const CRYPT_IMPL_MIXED As Integer = 3
'    Friend Const CRYPT_IMPL_UNKNOWN As Integer = 4
'    Friend Const CUR_BLOB_VERSION As Integer = 2
'    Friend Const CO_E_INIT_TLS As Integer = Fix()
'    '
'    'Note:  Error processing original source shown below
'    '        CUR_BLOB_VERSION = 2,
'    '        CO_E_INIT_TLS = (int)unchecked((int)0x80004006),
'    '------------------------------^--- GenCode(token): unexpected token type
'    Friend Const CO_E_INIT_SHARED_ALLOCATOR As Integer = Fix()
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_INIT_TLS = (int)unchecked((int)0x80004006),
'    '        CO_E_INIT_SHARED_ALLOCATOR = (int)unchecked((int)0x80004007),
'    '-------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const CO_E_INIT_MEMORY_ALLOCATOR As Integer = Fix()
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_INIT_SHARED_ALLOCATOR = (int)unchecked((int)0x80004007),
'    '        CO_E_INIT_MEMORY_ALLOCATOR = (int)unchecked((int)0x80004008),
'    '-------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const CO_E_INIT_CLASS_CACHE As Integer = Fix()
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_INIT_MEMORY_ALLOCATOR = (int)unchecked((int)0x80004008),
'    '        CO_E_INIT_CLASS_CACHE = (int)unchecked((int)0x80004009),
'    '--------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const CO_E_INIT_RPC_CHANNEL As Integer = Fix()
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_INIT_CLASS_CACHE = (int)unchecked((int)0x80004009),
'    '        CO_E_INIT_RPC_CHANNEL = (int)unchecked((int)0x8000400A),
'    '--------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const CO_E_INIT_TLS_SET_CHANNEL_CONTROL As Integer = Fix()
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_INIT_RPC_CHANNEL = (int)unchecked((int)0x8000400A),
'    '        CO_E_INIT_TLS_SET_CHANNEL_CONTROL = (int)unchecked((int)0x8000400B),
'    '--------------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const CO_E_INIT_TLS_CHANNEL_CONTROL As Integer = Fix()
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_INIT_TLS_SET_CHANNEL_CONTROL = (int)unchecked((int)0x8000400B),
'    '        CO_E_INIT_TLS_CHANNEL_CONTROL = (int)unchecked((int)0x8000400C),
'    '----------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const CO_E_INIT_UNACCEPTED_USER_ALLOCATOR As Integer = Fix()
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_INIT_TLS_CHANNEL_CONTROL = (int)unchecked((int)0x8000400C),
'    '        CO_E_INIT_UNACCEPTED_USER_ALLOCATOR = (int)unchecked((int)0x8000400D),
'    '----------------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const CO_E_INIT_SCM_MUTEX_EXISTS As Integer = Fix()
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_INIT_UNACCEPTED_USER_ALLOCATOR = (int)unchecked((int)0x8000400D),
'    '        CO_E_INIT_SCM_MUTEX_EXISTS = (int)unchecked((int)0x8000400E),
'    '-------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const CO_E_INIT_SCM_FILE_MAPPING_EXISTS As Integer = Fix()
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_INIT_SCM_MUTEX_EXISTS = (int)unchecked((int)0x8000400E),
'    '        CO_E_INIT_SCM_FILE_MAPPING_EXISTS = (int)unchecked((int)0x8000400F),
'    '--------------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const CO_E_INIT_SCM_MAP_VIEW_OF_FILE As Integer = Fix()
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_INIT_SCM_FILE_MAPPING_EXISTS = (int)unchecked((int)0x8000400F),
'    '        CO_E_INIT_SCM_MAP_VIEW_OF_FILE = (int)unchecked((int)0x80004010),
'    '-----------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const CO_E_INIT_SCM_EXEC_FAILURE As Integer = Fix()
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_INIT_SCM_MAP_VIEW_OF_FILE = (int)unchecked((int)0x80004010),
'    '        CO_E_INIT_SCM_EXEC_FAILURE = (int)unchecked((int)0x80004011),
'    '-------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const CO_E_INIT_ONLY_SINGLE_THREADED As Integer = Fix()
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_INIT_SCM_EXEC_FAILURE = (int)unchecked((int)0x80004011),
'    '        CO_E_INIT_ONLY_SINGLE_THREADED = (int)unchecked((int)0x80004012),
'    '-----------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const CO_E_CANT_REMOTE As Integer = Fix()
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_INIT_ONLY_SINGLE_THREADED = (int)unchecked((int)0x80004012),
'    '        CO_E_CANT_REMOTE = (int)unchecked((int)0x80004013),
'    '---------------------------------^--- GenCode(token): unexpected token type
'    Friend Const CO_E_BAD_SERVER_NAME As Integer = Fix()
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_CANT_REMOTE = (int)unchecked((int)0x80004013),
'    '        CO_E_BAD_SERVER_NAME = (int)unchecked((int)0x80004014),
'    '-------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const CO_E_WRONG_SERVER_IDENTITY As Integer = Fix()
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_BAD_SERVER_NAME = (int)unchecked((int)0x80004014),
'    '        CO_E_WRONG_SERVER_IDENTITY = (int)unchecked((int)0x80004015),
'    '-------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const CO_E_OLE1DDE_DISABLED As Integer = Fix()
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_WRONG_SERVER_IDENTITY = (int)unchecked((int)0x80004015),
'    '        CO_E_OLE1DDE_DISABLED = (int)unchecked((int)0x80004016),
'    '--------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const CO_E_RUNAS_SYNTAX As Integer = Fix()
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_OLE1DDE_DISABLED = (int)unchecked((int)0x80004016),
'    '        CO_E_RUNAS_SYNTAX = (int)unchecked((int)0x80004017),
'    '----------------------------------^--- GenCode(token): unexpected token type
'    Friend Const CO_E_CREATEPROCESS_FAILURE As Integer = Fix()
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_RUNAS_SYNTAX = (int)unchecked((int)0x80004017),
'    '        CO_E_CREATEPROCESS_FAILURE = (int)unchecked((int)0x80004018),
'    '-------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const CO_E_RUNAS_CREATEPROCESS_FAILURE As Integer = Fix()
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_CREATEPROCESS_FAILURE = (int)unchecked((int)0x80004018),
'    '        CO_E_RUNAS_CREATEPROCESS_FAILURE = (int)unchecked((int)0x80004019),
'    '-------------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const CO_E_RUNAS_LOGON_FAILURE As Integer = Fix()
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_RUNAS_CREATEPROCESS_FAILURE = (int)unchecked((int)0x80004019),
'    '        CO_E_RUNAS_LOGON_FAILURE = (int)unchecked((int)0x8000401A),
'    '-----------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const CO_E_LAUNCH_PERMSSION_DENIED As Integer = Fix()
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_RUNAS_LOGON_FAILURE = (int)unchecked((int)0x8000401A),
'    '        CO_E_LAUNCH_PERMSSION_DENIED = (int)unchecked((int)0x8000401B),
'    '---------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const CO_E_START_SERVICE_FAILURE As Integer = Fix()
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_LAUNCH_PERMSSION_DENIED = (int)unchecked((int)0x8000401B),
'    '        CO_E_START_SERVICE_FAILURE = (int)unchecked((int)0x8000401C),
'    '-------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const CO_E_REMOTE_COMMUNICATION_FAILURE As Integer = Fix()
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_START_SERVICE_FAILURE = (int)unchecked((int)0x8000401C),
'    '        CO_E_REMOTE_COMMUNICATION_FAILURE = (int)unchecked((int)0x8000401D),
'    '--------------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const CO_E_SERVER_START_TIMEOUT As Integer = Fix()
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_REMOTE_COMMUNICATION_FAILURE = (int)unchecked((int)0x8000401D),
'    '        CO_E_SERVER_START_TIMEOUT = (int)unchecked((int)0x8000401E),
'    '------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const CO_E_CLSREG_INCONSISTENT As Integer = Fix()
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_SERVER_START_TIMEOUT = (int)unchecked((int)0x8000401E),
'    '        CO_E_CLSREG_INCONSISTENT = (int)unchecked((int)0x8000401F),
'    '-----------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const CO_E_IIDREG_INCONSISTENT As Integer = Fix()
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_CLSREG_INCONSISTENT = (int)unchecked((int)0x8000401F),
'    '        CO_E_IIDREG_INCONSISTENT = (int)unchecked((int)0x80004020),
'    '-----------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const CO_E_NOT_SUPPORTED As Integer = Fix()
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_IIDREG_INCONSISTENT = (int)unchecked((int)0x80004020),
'    '        CO_E_NOT_SUPPORTED = (int)unchecked((int)0x80004021),
'    '-----------------------------------^--- GenCode(token): unexpected token type
'    Friend Const CLASSFACTORY_E_FIRST As Integer = Fix()
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_NOT_SUPPORTED = (int)unchecked((int)0x80004021),
'    '        CLASSFACTORY_E_FIRST = (int)unchecked((int)0x80040110),
'    '-------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const CLASSFACTORY_E_LAST As Integer = Fix()
'    '
'    'Note:  Error processing original source shown below
'    '        CLASSFACTORY_E_FIRST = (int)unchecked((int)0x80040110),
'    '        CLASSFACTORY_E_LAST = (int)unchecked((int)0x8004011F),
'    '------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const CLASSFACTORY_S_FIRST As Integer = &H40110
'    Friend Const CLASSFACTORY_S_LAST As Integer = &H4011F
'    Friend Const CLASS_E_NOAGGREGATION As Integer = Fix()
'    '
'    'Note:  Error processing original source shown below
'    '        CLASSFACTORY_S_LAST = 0x0004011F,
'    '        CLASS_E_NOAGGREGATION = (int)unchecked((int)0x80040110),
'    '--------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const CLASS_E_CLASSNOTAVAILABLE As Integer = Fix()
'    '
'    'Note:  Error processing original source shown below
'    '        CLASS_E_NOAGGREGATION = (int)unchecked((int)0x80040110),
'    '        CLASS_E_CLASSNOTAVAILABLE = (int)unchecked((int)0x80040111),
'    '------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const CACHE_E_FIRST As Integer = Fix()
'    '
'    'Note:  Error processing original source shown below
'    '        CLASS_E_CLASSNOTAVAILABLE = (int)unchecked((int)0x80040111),
'    '        CACHE_E_FIRST = (int)unchecked((int)0x80040170),
'    '------------------------------^--- GenCode(token): unexpected token type
'    Friend Const CACHE_E_LAST As Integer = Fix()
'    '
'    'Note:  Error processing original source shown below
'    '        CACHE_E_FIRST = (int)unchecked((int)0x80040170),
'    '        CACHE_E_LAST = (int)unchecked((int)0x8004017F),
'    '-----------------------------^--- GenCode(token): unexpected token type
'    Friend Const CACHE_S_FIRST As Integer = &H40170
'    Friend Const CACHE_S_LAST As Integer = &H4017F
'    Friend Const CACHE_E_NOCACHE_UPDATED As Integer = Fix()
'    '
'    'Note:  Error processing original source shown below
'    '        CACHE_S_LAST = 0x0004017F,
'    '        CACHE_E_NOCACHE_UPDATED = (int)unchecked((int)0x80040170),
'    '----------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const CLIENTSITE_E_FIRST As Integer = Fix()
'    '
'    'Note:  Error processing original source shown below
'    '        CACHE_E_NOCACHE_UPDATED = (int)unchecked((int)0x80040170),
'    '        CLIENTSITE_E_FIRST = (int)unchecked((int)0x80040190),
'    '-----------------------------------^--- GenCode(token): unexpected token type
'    Friend Const CLIENTSITE_E_LAST As Integer = Fix()
'    '
'    'Note:  Error processing original source shown below
'    '        CLIENTSITE_E_FIRST = (int)unchecked((int)0x80040190),
'    '        CLIENTSITE_E_LAST = (int)unchecked((int)0x8004019F),
'    '----------------------------------^--- GenCode(token): unexpected token type
'    Friend Const CLIENTSITE_S_FIRST As Integer = &H40190
'    Friend Const CLIENTSITE_S_LAST As Integer = &H4019F
'    Friend Const CONVERT10_E_FIRST As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CLIENTSITE_S_LAST = 0x0004019F,
'    '        CONVERT10_E_FIRST = unchecked((int)0x800401C0),
'    '-----------------------------^--- GenCode(token): unexpected token type
'    Friend Const CONVERT10_E_LAST As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CONVERT10_E_FIRST = unchecked((int)0x800401C0),
'    '        CONVERT10_E_LAST = unchecked((int)0x800401CF),
'    '----------------------------^--- GenCode(token): unexpected token type
'    Friend Const CONVERT10_S_FIRST As Integer = &H401C0
'    Friend Const CONVERT10_S_LAST As Integer = &H401CF
'    Friend Const CONVERT10_E_OLESTREAM_GET As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CONVERT10_S_LAST = 0x000401CF,
'    '        CONVERT10_E_OLESTREAM_GET = unchecked((int)0x800401C0),
'    '-------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const CONVERT10_E_OLESTREAM_PUT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CONVERT10_E_OLESTREAM_GET = unchecked((int)0x800401C0),
'    '        CONVERT10_E_OLESTREAM_PUT = unchecked((int)0x800401C1),
'    '-------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const CONVERT10_E_OLESTREAM_FMT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CONVERT10_E_OLESTREAM_PUT = unchecked((int)0x800401C1),
'    '        CONVERT10_E_OLESTREAM_FMT = unchecked((int)0x800401C2),
'    '-------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const CONVERT10_E_OLESTREAM_BITMAP_TO_DIB As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CONVERT10_E_OLESTREAM_FMT = unchecked((int)0x800401C2),
'    '        CONVERT10_E_OLESTREAM_BITMAP_TO_DIB = unchecked((int)0x800401C3),
'    '-----------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const CONVERT10_E_STG_FMT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CONVERT10_E_OLESTREAM_BITMAP_TO_DIB = unchecked((int)0x800401C3),
'    '        CONVERT10_E_STG_FMT = unchecked((int)0x800401C4),
'    '-------------------------------^--- GenCode(token): unexpected token type
'    Friend Const CONVERT10_E_STG_NO_STD_STREAM As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CONVERT10_E_STG_FMT = unchecked((int)0x800401C4),
'    '        CONVERT10_E_STG_NO_STD_STREAM = unchecked((int)0x800401C5),
'    '-----------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const CONVERT10_E_STG_DIB_TO_BITMAP As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CONVERT10_E_STG_NO_STD_STREAM = unchecked((int)0x800401C5),
'    '        CONVERT10_E_STG_DIB_TO_BITMAP = unchecked((int)0x800401C6),
'    '-----------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const CLIPBRD_E_FIRST As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CONVERT10_E_STG_DIB_TO_BITMAP = unchecked((int)0x800401C6),
'    '        CLIPBRD_E_FIRST = unchecked((int)0x800401D0),
'    '---------------------------^--- GenCode(token): unexpected token type
'    Friend Const CLIPBRD_E_LAST As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CLIPBRD_E_FIRST = unchecked((int)0x800401D0),
'    '        CLIPBRD_E_LAST = unchecked((int)0x800401DF),
'    '--------------------------^--- GenCode(token): unexpected token type
'    Friend Const CLIPBRD_S_FIRST As Integer = &H401D0
'    Friend Const CLIPBRD_S_LAST As Integer = &H401DF
'    Friend Const CLIPBRD_E_CANT_OPEN As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CLIPBRD_S_LAST = 0x000401DF,
'    '        CLIPBRD_E_CANT_OPEN = unchecked((int)0x800401D0),
'    '-------------------------------^--- GenCode(token): unexpected token type
'    Friend Const CLIPBRD_E_CANT_EMPTY As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CLIPBRD_E_CANT_OPEN = unchecked((int)0x800401D0),
'    '        CLIPBRD_E_CANT_EMPTY = unchecked((int)0x800401D1),
'    '--------------------------------^--- GenCode(token): unexpected token type
'    Friend Const CLIPBRD_E_CANT_SET As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CLIPBRD_E_CANT_EMPTY = unchecked((int)0x800401D1),
'    '        CLIPBRD_E_CANT_SET = unchecked((int)0x800401D2),
'    '------------------------------^--- GenCode(token): unexpected token type
'    Friend Const CLIPBRD_E_BAD_DATA As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CLIPBRD_E_CANT_SET = unchecked((int)0x800401D2),
'    '        CLIPBRD_E_BAD_DATA = unchecked((int)0x800401D3),
'    '------------------------------^--- GenCode(token): unexpected token type
'    Friend Const CLIPBRD_E_CANT_CLOSE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CLIPBRD_E_BAD_DATA = unchecked((int)0x800401D3),
'    '        CLIPBRD_E_CANT_CLOSE = unchecked((int)0x800401D4),
'    '--------------------------------^--- GenCode(token): unexpected token type
'    Friend Const CO_E_FIRST As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CLIPBRD_E_CANT_CLOSE = unchecked((int)0x800401D4),
'    '        CO_E_FIRST = unchecked((int)0x800401F0),
'    '----------------------^--- GenCode(token): unexpected token type
'    Friend Const CO_E_LAST As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_FIRST = unchecked((int)0x800401F0),
'    '        CO_E_LAST = unchecked((int)0x800401FF),
'    '---------------------^--- GenCode(token): unexpected token type
'    Friend Const CO_S_FIRST As Integer = &H401F0
'    Friend Const CO_S_LAST As Integer = &H401FF
'    Friend Const CO_E_NOTINITIALIZED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CO_S_LAST = 0x000401FF,
'    '        CO_E_NOTINITIALIZED = unchecked((int)0x800401F0),
'    '-------------------------------^--- GenCode(token): unexpected token type
'    Friend Const CO_E_ALREADYINITIALIZED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_NOTINITIALIZED = unchecked((int)0x800401F0),
'    '        CO_E_ALREADYINITIALIZED = unchecked((int)0x800401F1),
'    '-----------------------------------^--- GenCode(token): unexpected token type
'    Friend Const CO_E_CANTDETERMINECLASS As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_ALREADYINITIALIZED = unchecked((int)0x800401F1),
'    '        CO_E_CANTDETERMINECLASS = unchecked((int)0x800401F2),
'    '-----------------------------------^--- GenCode(token): unexpected token type
'    Friend Const CO_E_CLASSSTRING As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_CANTDETERMINECLASS = unchecked((int)0x800401F2),
'    '        CO_E_CLASSSTRING = unchecked((int)0x800401F3),
'    '----------------------------^--- GenCode(token): unexpected token type
'    Friend Const CO_E_IIDSTRING As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_CLASSSTRING = unchecked((int)0x800401F3),
'    '        CO_E_IIDSTRING = unchecked((int)0x800401F4),
'    '--------------------------^--- GenCode(token): unexpected token type
'    Friend Const CO_E_APPNOTFOUND As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_IIDSTRING = unchecked((int)0x800401F4),
'    '        CO_E_APPNOTFOUND = unchecked((int)0x800401F5),
'    '----------------------------^--- GenCode(token): unexpected token type
'    Friend Const CO_E_APPSINGLEUSE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_APPNOTFOUND = unchecked((int)0x800401F5),
'    '        CO_E_APPSINGLEUSE = unchecked((int)0x800401F6),
'    '-----------------------------^--- GenCode(token): unexpected token type
'    Friend Const CO_E_ERRORINAPP As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_APPSINGLEUSE = unchecked((int)0x800401F6),
'    '        CO_E_ERRORINAPP = unchecked((int)0x800401F7),
'    '---------------------------^--- GenCode(token): unexpected token type
'    Friend Const CO_E_DLLNOTFOUND As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_ERRORINAPP = unchecked((int)0x800401F7),
'    '        CO_E_DLLNOTFOUND = unchecked((int)0x800401F8),
'    '----------------------------^--- GenCode(token): unexpected token type
'    Friend Const CO_E_ERRORINDLL As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_DLLNOTFOUND = unchecked((int)0x800401F8),
'    '        CO_E_ERRORINDLL = unchecked((int)0x800401F9),
'    '---------------------------^--- GenCode(token): unexpected token type
'    Friend Const CO_E_WRONGOSFORAPP As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_ERRORINDLL = unchecked((int)0x800401F9),
'    '        CO_E_WRONGOSFORAPP = unchecked((int)0x800401FA),
'    '------------------------------^--- GenCode(token): unexpected token type
'    Friend Const CO_E_OBJNOTREG As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_WRONGOSFORAPP = unchecked((int)0x800401FA),
'    '        CO_E_OBJNOTREG = unchecked((int)0x800401FB),
'    '--------------------------^--- GenCode(token): unexpected token type
'    Friend Const CO_E_OBJISREG As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_OBJNOTREG = unchecked((int)0x800401FB),
'    '        CO_E_OBJISREG = unchecked((int)0x800401FC),
'    '-------------------------^--- GenCode(token): unexpected token type
'    Friend Const CO_E_OBJNOTCONNECTED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_OBJISREG = unchecked((int)0x800401FC),
'    '        CO_E_OBJNOTCONNECTED = unchecked((int)0x800401FD),
'    '--------------------------------^--- GenCode(token): unexpected token type
'    Friend Const CO_E_APPDIDNTREG As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_OBJNOTCONNECTED = unchecked((int)0x800401FD),
'    '        CO_E_APPDIDNTREG = unchecked((int)0x800401FE),
'    '----------------------------^--- GenCode(token): unexpected token type
'    Friend Const CO_E_RELEASED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_APPDIDNTREG = unchecked((int)0x800401FE),
'    '        CO_E_RELEASED = unchecked((int)0x800401FF),
'    '-------------------------^--- GenCode(token): unexpected token type
'    Friend Const CACHE_S_FORMATETC_NOTSUPPORTED As Integer = &H40170
'    Friend Const CACHE_S_SAMECACHE As Integer = &H40171
'    Friend Const CACHE_S_SOMECACHES_NOTUPDATED As Integer = &H40172
'    Friend Const CONVERT10_S_NO_PRESENTATION As Integer = &H401C0
'    Friend Const CO_E_CLASS_CREATE_FAILED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CONVERT10_S_NO_PRESENTATION = 0x000401C0,
'    '        CO_E_CLASS_CREATE_FAILED = unchecked((int)0x80080001),
'    '------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const CO_E_SCM_ERROR As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_CLASS_CREATE_FAILED = unchecked((int)0x80080001),
'    '        CO_E_SCM_ERROR = unchecked((int)0x80080002),
'    '--------------------------^--- GenCode(token): unexpected token type
'    Friend Const CO_E_SCM_RPC_FAILURE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_SCM_ERROR = unchecked((int)0x80080002),
'    '        CO_E_SCM_RPC_FAILURE = unchecked((int)0x80080003),
'    '--------------------------------^--- GenCode(token): unexpected token type
'    Friend Const CO_E_BAD_PATH As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_SCM_RPC_FAILURE = unchecked((int)0x80080003),
'    '        CO_E_BAD_PATH = unchecked((int)0x80080004),
'    '-------------------------^--- GenCode(token): unexpected token type
'    Friend Const CO_E_SERVER_EXEC_FAILURE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_BAD_PATH = unchecked((int)0x80080004),
'    '        CO_E_SERVER_EXEC_FAILURE = unchecked((int)0x80080005),
'    '------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const CO_E_OBJSRV_RPC_FAILURE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_SERVER_EXEC_FAILURE = unchecked((int)0x80080005),
'    '        CO_E_OBJSRV_RPC_FAILURE = unchecked((int)0x80080006),
'    '-----------------------------------^--- GenCode(token): unexpected token type
'    Friend Const CO_E_SERVER_STOPPING As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CO_E_OBJSRV_RPC_FAILURE = unchecked((int)0x80080006),
'    '        CO_E_SERVER_STOPPING = unchecked((int)0x80080008),
'    '--------------------------------^--- GenCode(token): unexpected token type
'    Friend Const CO_S_NOTALLINTERFACES As Integer = &H80012
'    Friend Const CERT_E_EXPIRED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CO_S_NOTALLINTERFACES = 0x00080012,
'    '        CERT_E_EXPIRED = unchecked((int)0x800B0101),
'    '--------------------------^--- GenCode(token): unexpected token type
'    Friend Const CERT_E_VALIDIYPERIODNESTING As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CERT_E_EXPIRED = unchecked((int)0x800B0101),
'    '        CERT_E_VALIDIYPERIODNESTING = unchecked((int)0x800B0102),
'    '---------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const CERT_E_ROLE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CERT_E_VALIDIYPERIODNESTING = unchecked((int)0x800B0102),
'    '        CERT_E_ROLE = unchecked((int)0x800B0103),
'    '-----------------------^--- GenCode(token): unexpected token type
'    Friend Const CERT_E_CRITICAL As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CERT_E_ROLE = unchecked((int)0x800B0103),
'    '        CERT_E_CRITICAL = unchecked((int)0x800B0105),
'    '---------------------------^--- GenCode(token): unexpected token type
'    Friend Const CERT_E_PURPOSE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CERT_E_CRITICAL = unchecked((int)0x800B0105),
'    '        CERT_E_PURPOSE = unchecked((int)0x800B0106),
'    '--------------------------^--- GenCode(token): unexpected token type
'    Friend Const CERT_E_ISSUERCHAINING As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CERT_E_PURPOSE = unchecked((int)0x800B0106),
'    '        CERT_E_ISSUERCHAINING = unchecked((int)0x800B0107),
'    '---------------------------------^--- GenCode(token): unexpected token type
'    Friend Const CERT_E_MALFORMED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CERT_E_ISSUERCHAINING = unchecked((int)0x800B0107),
'    '        CERT_E_MALFORMED = unchecked((int)0x800B0108),
'    '----------------------------^--- GenCode(token): unexpected token type
'    Friend Const CERT_E_UNTRUSTEDROOT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CERT_E_MALFORMED = unchecked((int)0x800B0108),
'    '        CERT_E_UNTRUSTEDROOT = unchecked((int)0x800B0109),
'    '--------------------------------^--- GenCode(token): unexpected token type
'    Friend Const CERT_E_CHAINING As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CERT_E_UNTRUSTEDROOT = unchecked((int)0x800B0109),
'    '        CERT_E_CHAINING = unchecked((int)0x800B010A),
'    '---------------------------^--- GenCode(token): unexpected token type
'    Friend Const COMPLEXREGION As Integer = 3
'    Friend Const COLORONCOLOR As Integer = 3
'    Friend Const CLIP_TO_PATH As Integer = 4097
'    Friend Const CLOSECHANNEL As Integer = 4112
'    Friend Const CM_OUT_OF_GAMUT As Integer = 255
'    Friend Const CM_IN_GAMUT As Integer = 0
'    Friend Const CLIP_DEFAULT_PRECIS As Integer = 0
'    Friend Const CLIP_CHARACTER_PRECIS As Integer = 1
'    Friend Const CLIP_STROKE_PRECIS As Integer = 2
'    Friend Const CLIP_MASK As Integer = &HF
'    Friend Const CLIP_LH_ANGLES As Integer = Machine.Shift.Left(1, 4)
'    Friend Const CLIP_TT_ALWAYS As Integer = Machine.Shift.Left(2, 4)
'    Friend Const CLIP_EMBEDDED As Integer = Machine.Shift.Left(8, 4)
'    Friend Const CHINESEBIG5_CHARSET As Integer = 136
'    Friend Const CLR_INVALID As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                          CHINESEBIG5_CHARSET = 136,
'    '        CLR_INVALID = unchecked((int)0xFFFFFFFF),
'    '-----------------------^--- GenCode(token): unexpected token type
'    Friend Const CURVECAPS As Integer = 28
'    Friend Const CLIPCAPS As Integer = 36
'    Friend Const COLORRES As Integer = 108
'    Friend Const CC_NONE As Integer = 0
'    Friend Const CC_CIRCLES As Integer = 1
'    Friend Const CC_PIE As Integer = 2
'    Friend Const CC_CHORD As Integer = 4
'    Friend Const CC_ELLIPSES As Integer = 8
'    Friend Const CC_WIDE As Integer = 16
'    Friend Const CC_STYLED As Integer = 32
'    Friend Const CC_WIDESTYLED As Integer = 64
'    Friend Const CC_INTERIORS As Integer = 128
'    Friend Const CC_ROUNDRECT As Integer = 256
'    Friend Const CP_NONE As Integer = 0
'    Friend Const CP_RECTANGLE As Integer = 1
'    Friend Const CP_REGION As Integer = 2
'    Friend Const CBM_INIT As Integer = &H4
'    Friend Const CCHDEVICENAME As Integer = 32
'    Friend Const CCHFORMNAME As Integer = 32
'    Friend Const CA_NEGATIVE As Integer = &H1
'    Friend Const CA_LOG_FILTER As Integer = &H2
'    Friend Const CONNECT_UPDATE_PROFILE As Integer = &H1
'    Friend Const CONNECT_UPDATE_RECENT As Integer = &H2
'    Friend Const CONNECT_TEMPORARY As Integer = &H4
'    Friend Const CONNECT_INTERACTIVE As Integer = &H8
'    Friend Const CONNECT_PROMPT As Integer = &H10
'    Friend Const CONNECT_NEED_DRIVE As Integer = &H20
'    Friend Const CONNECT_REFCOUNT As Integer = &H40
'    Friend Const CONNECT_REDIRECT As Integer = &H80
'    Friend Const CONNECT_LOCALDRIVE As Integer = &H100
'    Friend Const CONNECT_CURRENT_MEDIA As Integer = &H200
'    Friend Const CONNECT_DEFERRED As Integer = &H400
'    Friend Const CONNECT_RESERVED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CONNECT_DEFERRED = 0x00000400,
'    '        CONNECT_RESERVED = unchecked((int)0xFF000000),
'    '----------------------------^--- GenCode(token): unexpected token type
'    Friend Const CONNDLG_RO_PATH As Integer = &H1
'    Friend Const CONNDLG_CONN_POINT As Integer = &H2
'    Friend Const CONNDLG_USE_MRU As Integer = &H4
'    Friend Const CONNDLG_HIDE_BOX As Integer = &H8
'    Friend Const CONNDLG_PERSIST As Integer = &H10
'    Friend Const CONNDLG_NOT_PERSIST As Integer = &H20
'    Friend Const CT_CTYPE1 As Integer = &H1
'    Friend Const CT_CTYPE2 As Integer = &H2
'    Friend Const CT_CTYPE3 As Integer = &H4
'    Friend Const C1_UPPER As Integer = &H1
'    Friend Const C1_LOWER As Integer = &H2
'    Friend Const C1_DIGIT As Integer = &H4
'    Friend Const C1_SPACE As Integer = &H8
'    Friend Const C1_PUNCT As Integer = &H10
'    Friend Const C1_CNTRL As Integer = &H20
'    Friend Const C1_BLANK As Integer = &H40
'    Friend Const C1_XDIGIT As Integer = &H80
'    Friend Const C1_ALPHA As Integer = &H100
'    Friend Const C2_LEFTTORIGHT As Integer = &H1
'    Friend Const C2_RIGHTTOLEFT As Integer = &H2
'    Friend Const C2_EUROPENUMBER As Integer = &H3
'    Friend Const C2_EUROPESEPARATOR As Integer = &H4
'    Friend Const C2_EUROPETERMINATOR As Integer = &H5
'    Friend Const C2_ARABICNUMBER As Integer = &H6
'    Friend Const C2_COMMONSEPARATOR As Integer = &H7
'    Friend Const C2_BLOCKSEPARATOR As Integer = &H8
'    Friend Const C2_SEGMENTSEPARATOR As Integer = &H9
'    Friend Const C2_WHITESPACE As Integer = &HA
'    Friend Const C2_OTHERNEUTRAL As Integer = &HB
'    Friend Const C2_NOTAPPLICABLE As Integer = &H0
'    Friend Const C3_NONSPACING As Integer = &H1
'    Friend Const C3_DIACRITIC As Integer = &H2
'    Friend Const C3_VOWELMARK As Integer = &H4
'    Friend Const C3_SYMBOL As Integer = &H8
'    Friend Const C3_KATAKANA As Integer = &H10
'    Friend Const C3_HIRAGANA As Integer = &H20
'    Friend Const C3_HALFWIDTH As Integer = &H40
'    Friend Const C3_FULLWIDTH As Integer = &H80
'    Friend Const C3_IDEOGRAPH As Integer = &H100
'    Friend Const C3_KASHIDA As Integer = &H200
'    Friend Const C3_LEXICAL As Integer = &H400
'    Friend Const C3_ALPHA As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        C3_LEXICAL = 0x0400,
'    '        C3_ALPHA = unchecked((int)0x8000),
'    '--------------------^--- GenCode(token): unexpected token type
'    Friend Const C3_NOTAPPLICABLE As Integer = &H0
'    Friend Const CP_INSTALLED As Integer = &H1
'    Friend Const CP_SUPPORTED As Integer = &H2
'    Friend Const CP_ACP As Integer = 0
'    Friend Const CP_OEMCP As Integer = 1
'    Friend Const CP_MACCP As Integer = 2
'    Friend Const CP_UTF7 As Integer = 65000
'    Friend Const CP_UTF8 As Integer = 65001
'    Friend Const CTRY_DEFAULT As Integer = 0
'    Friend Const CTRY_AUSTRALIA As Integer = 61
'    Friend Const CTRY_AUSTRIA As Integer = 43
'    Friend Const CTRY_BELGIUM As Integer = 32
'    Friend Const CTRY_BRAZIL As Integer = 55
'    Friend Const CTRY_BULGARIA As Integer = 359
'    Friend Const CTRY_CANADA As Integer = 2
'    Friend Const CTRY_CROATIA As Integer = 385
'    Friend Const CTRY_CZECH As Integer = 42
'    Friend Const CTRY_DENMARK As Integer = 45
'    Friend Const CTRY_FINLAND As Integer = 358
'    Friend Const CTRY_FRANCE As Integer = 33
'    Friend Const CTRY_GERMANY As Integer = 49
'    Friend Const CTRY_GREECE As Integer = 30
'    Friend Const CTRY_HONG_KONG As Integer = 852
'    Friend Const CTRY_HUNGARY As Integer = 36
'    Friend Const CTRY_ICELAND As Integer = 354
'    Friend Const CTRY_IRELAND As Integer = 353
'    Friend Const CTRY_ITALY As Integer = 39
'    Friend Const CTRY_JAPAN As Integer = 81
'    Friend Const CTRY_MEXICO As Integer = 52
'    Friend Const CTRY_NETHERLANDS As Integer = 31
'    Friend Const CTRY_NEW_ZEALAND As Integer = 64
'    Friend Const CTRY_NORWAY As Integer = 47
'    Friend Const CTRY_POLAND As Integer = 48
'    Friend Const CTRY_PORTUGAL As Integer = 351
'    Friend Const CTRY_PRCHINA As Integer = 86
'    Friend Const CTRY_ROMANIA As Integer = 40
'    Friend Const CTRY_RUSSIA As Integer = 7
'    Friend Const CTRY_SINGAPORE As Integer = 65
'    Friend Const CTRY_SLOVAK As Integer = 42
'    Friend Const CTRY_SLOVENIA As Integer = 386
'    Friend Const CTRY_SOUTH_KOREA As Integer = 82
'    Friend Const CTRY_SPAIN As Integer = 34
'    Friend Const CTRY_SWEDEN As Integer = 46
'    Friend Const CTRY_SWITZERLAND As Integer = 41
'    Friend Const CTRY_TAIWAN As Integer = 886
'    Friend Const CTRY_TURKEY As Integer = 90
'    Friend Const CTRY_UNITED_KINGDOM As Integer = 44
'    Friend Const CTRY_UNITED_STATES As Integer = 1
'    Friend Const CAL_ICALINTVALUE As Integer = &H1
'    Friend Const CAL_SCALNAME As Integer = &H2
'    Friend Const CAL_IYEAROFFSETRANGE As Integer = &H3
'    Friend Const CAL_SERASTRING As Integer = &H4
'    Friend Const CAL_SSHORTDATE As Integer = &H5
'    Friend Const CAL_SLONGDATE As Integer = &H6
'    Friend Const CAL_SDAYNAME1 As Integer = &H7
'    Friend Const CAL_SDAYNAME2 As Integer = &H8
'    Friend Const CAL_SDAYNAME3 As Integer = &H9
'    Friend Const CAL_SDAYNAME4 As Integer = &HA
'    Friend Const CAL_SDAYNAME5 As Integer = &HB
'    Friend Const CAL_SDAYNAME6 As Integer = &HC
'    Friend Const CAL_SDAYNAME7 As Integer = &HD
'    Friend Const CAL_SABBREVDAYNAME1 As Integer = &HE
'    Friend Const CAL_SABBREVDAYNAME2 As Integer = &HF
'    Friend Const CAL_SABBREVDAYNAME3 As Integer = &H10
'    Friend Const CAL_SABBREVDAYNAME4 As Integer = &H11
'    Friend Const CAL_SABBREVDAYNAME5 As Integer = &H12
'    Friend Const CAL_SABBREVDAYNAME6 As Integer = &H13
'    Friend Const CAL_SABBREVDAYNAME7 As Integer = &H14
'    Friend Const CAL_SMONTHNAME1 As Integer = &H15
'    Friend Const CAL_SMONTHNAME2 As Integer = &H16
'    Friend Const CAL_SMONTHNAME3 As Integer = &H17
'    Friend Const CAL_SMONTHNAME4 As Integer = &H18
'    Friend Const CAL_SMONTHNAME5 As Integer = &H19
'    Friend Const CAL_SMONTHNAME6 As Integer = &H1A
'    Friend Const CAL_SMONTHNAME7 As Integer = &H1B
'    Friend Const CAL_SMONTHNAME8 As Integer = &H1C
'    Friend Const CAL_SMONTHNAME9 As Integer = &H1D
'    Friend Const CAL_SMONTHNAME10 As Integer = &H1E
'    Friend Const CAL_SMONTHNAME11 As Integer = &H1F
'    Friend Const CAL_SMONTHNAME12 As Integer = &H20
'    Friend Const CAL_SMONTHNAME13 As Integer = &H21
'    Friend Const CAL_SABBREVMONTHNAME1 As Integer = &H22
'    Friend Const CAL_SABBREVMONTHNAME2 As Integer = &H23
'    Friend Const CAL_SABBREVMONTHNAME3 As Integer = &H24
'    Friend Const CAL_SABBREVMONTHNAME4 As Integer = &H25
'    Friend Const CAL_SABBREVMONTHNAME5 As Integer = &H26
'    Friend Const CAL_SABBREVMONTHNAME6 As Integer = &H27
'    Friend Const CAL_SABBREVMONTHNAME7 As Integer = &H28
'    Friend Const CAL_SABBREVMONTHNAME8 As Integer = &H29
'    Friend Const CAL_SABBREVMONTHNAME9 As Integer = &H2A
'    Friend Const CAL_SABBREVMONTHNAME10 As Integer = &H2B
'    Friend Const CAL_SABBREVMONTHNAME11 As Integer = &H2C
'    Friend Const CAL_SABBREVMONTHNAME12 As Integer = &H2D
'    Friend Const CAL_SABBREVMONTHNAME13 As Integer = &H2E
'    Friend Const CAL_GREGORIAN As Integer = 1
'    Friend Const CAL_GREGORIAN_US As Integer = 2
'    Friend Const CAL_JAPAN As Integer = 3
'    Friend Const CAL_TAIWAN As Integer = 4
'    Friend Const CAL_KOREA As Integer = 5
'    Friend Const CAL_HIJRI As Integer = 6
'    Friend Const CAL_THAI As Integer = 7
'    Friend Const CAL_HEBREW As Integer = 8
'    Friend Const CONTAINER_INHERIT_ACE As Integer = &H2
'    Friend Const COMPRESSION_FORMAT_NONE As Integer = &H0
'    Friend Const COMPRESSION_FORMAT_DEFAULT As Integer = &H1
'    Friend Const COMPRESSION_FORMAT_LZNT1 As Integer = &H2
'    Friend Const COMPRESSION_ENGINE_STANDARD As Integer = &H0
'    Friend Const COMPRESSION_ENGINE_MAXIMUM As Integer = &H100
'    Friend Const CS_VREDRAW As Integer = &H1
'    Friend Const CS_HREDRAW As Integer = &H2
'    Friend Const CS_KEYCVTWINDOW As Integer = &H4
'    Friend Const CS_DBLCLKS As Integer = &H8
'    Friend Const CS_OWNDC As Integer = &H20
'    Friend Const CS_CLASSDC As Integer = &H40
'    Friend Const CS_PARENTDC As Integer = &H80
'    Friend Const CS_NOKEYCVT As Integer = &H100
'    Friend Const CS_NOCLOSE As Integer = &H200
'    Friend Const CS_SAVEBITS As Integer = &H800
'    Friend Const CS_BYTEALIGNCLIENT As Integer = &H1000
'    Friend Const CS_BYTEALIGNWINDOW As Integer = &H2000
'    Friend Const CS_GLOBALCLASS As Integer = &H4000
'    Friend Const CS_IME As Integer = &H10000
'    Friend Const CF_TEXT As Integer = 1
'    Friend Const CF_BITMAP As Integer = 2
'    Friend Const CF_METAFILEPICT As Integer = 3
'    Friend Const CF_SYLK As Integer = 4
'    Friend Const CF_DIF As Integer = 5
'    Friend Const CF_TIFF As Integer = 6
'    Friend Const CF_OEMTEXT As Integer = 7
'    Friend Const CF_DIB As Integer = 8
'    Friend Const CF_PALETTE As Integer = 9
'    Friend Const CF_PENDATA As Integer = 10
'    Friend Const CF_RIFF As Integer = 11
'    Friend Const CF_WAVE As Integer = 12
'    Friend Const CF_UNICODETEXT As Integer = 13
'    Friend Const CF_ENHMETAFILE As Integer = 14
'    Friend Const CF_HDROP As Integer = 15
'    Friend Const CF_LOCALE As Integer = 16
'    Friend Const CF_MAX As Integer = 17
'    Friend Const CF_OWNERDISPLAY As Integer = &H80
'    Friend Const CF_DSPTEXT As Integer = &H81
'    Friend Const CF_DSPBITMAP As Integer = &H82
'    Friend Const CF_DSPMETAFILEPICT As Integer = &H83
'    Friend Const CF_DSPENHMETAFILE As Integer = &H8E
'    Friend Const CF_PRIVATEFIRST As Integer = &H200
'    Friend Const CF_PRIVATELAST As Integer = &H2FF
'    Friend Const CF_GDIOBJFIRST As Integer = &H300
'    Friend Const CF_GDIOBJLAST As Integer = &H3FF
'    Friend Const CW_USEDEFAULT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CF_GDIOBJLAST = 0x03FF,
'    '        CW_USEDEFAULT = (unchecked((int)0x80000000)),
'    '--------------------------^--- GenCode(token): unexpected token type
'    Friend Const CWP_ALL As Integer = &H0
'    Friend Const CWP_SKIPINVISIBLE As Integer = &H1
'    Friend Const CWP_SKIPDISABLED As Integer = &H2
'    Friend Const CWP_SKIPTRANSPARENT As Integer = &H4
'    Friend Const CTLCOLOR_MSGBOX As Integer = 0
'    Friend Const CTLCOLOR_EDIT As Integer = 1
'    Friend Const CTLCOLOR_LISTBOX As Integer = 2
'    Friend Const CTLCOLOR_BTN As Integer = 3
'    Friend Const CTLCOLOR_DLG As Integer = 4
'    Friend Const CTLCOLOR_SCROLLBAR As Integer = 5
'    Friend Const CTLCOLOR_STATIC As Integer = 6
'    Friend Const CTLCOLOR_MAX As Integer = 7
'    Friend Const COLOR_SCROLLBAR As Integer = 0
'    Friend Const COLOR_BACKGROUND As Integer = 1
'    Friend Const COLOR_ACTIVECAPTION As Integer = 2
'    Friend Const COLOR_INACTIVECAPTION As Integer = 3
'    Friend Const COLOR_MENU As Integer = 4
'    Friend Const COLOR_WINDOW As Integer = 5
'    Friend Const COLOR_WINDOWFRAME As Integer = 6
'    Friend Const COLOR_MENUTEXT As Integer = 7
'    Friend Const COLOR_WINDOWTEXT As Integer = 8
'    Friend Const COLOR_CAPTIONTEXT As Integer = 9
'    Friend Const COLOR_ACTIVEBORDER As Integer = 10
'    Friend Const COLOR_INACTIVEBORDER As Integer = 11
'    Friend Const COLOR_APPWORKSPACE As Integer = 12
'    Friend Const COLOR_HIGHLIGHT As Integer = 13
'    Friend Const COLOR_HIGHLIGHTTEXT As Integer = 14
'    Friend Const COLOR_BTNFACE As Integer = 15
'    Friend Const COLOR_BTNSHADOW As Integer = 16
'    Friend Const COLOR_GRAYTEXT As Integer = 17
'    Friend Const COLOR_BTNTEXT As Integer = 18
'    Friend Const COLOR_INACTIVECAPTIONTEXT As Integer = 19
'    Friend Const COLOR_BTNHIGHLIGHT As Integer = 20
'    Friend Const COLOR_3DDKSHADOW As Integer = 21
'    Friend Const COLOR_3DLIGHT As Integer = 22
'    Friend Const COLOR_INFOTEXT As Integer = 23
'    Friend Const COLOR_INFOBK As Integer = 24
'    Friend Const COLOR_DESKTOP As Integer = 1
'    Friend Const COLOR_3DFACE As Integer = 15
'    Friend Const COLOR_3DSHADOW As Integer = 16
'    Friend Const COLOR_3DHIGHLIGHT As Integer = 20
'    Friend Const COLOR_3DHILIGHT As Integer = 20
'    Friend Const COLOR_BTNHILIGHT As Integer = 20
'    Friend Const CB_OKAY As Integer = 0
'    Friend Const CB_ERR As Integer = - 1
'    Friend Const CB_ERRSPACE As Integer = - 2
'    Friend Const CBN_ERRSPACE As Integer = - 1
'    Friend Const CBN_SELCHANGE As Integer = 1
'    Friend Const CBN_DBLCLK As Integer = 2
'    Friend Const CBN_SETFOCUS As Integer = 3
'    Friend Const CBN_KILLFOCUS As Integer = 4
'    Friend Const CBN_EDITCHANGE As Integer = 5
'    Friend Const CBN_EDITUPDATE As Integer = 6
'    Friend Const CBN_DROPDOWN As Integer = 7
'    Friend Const CBN_CLOSEUP As Integer = 8
'    Friend Const CBN_SELENDOK As Integer = 9
'    Friend Const CBN_SELENDCANCEL As Integer = 10
'    Friend Const CBS_SIMPLE As Integer = &H1
'    Friend Const CBS_DROPDOWN As Integer = &H2
'    Friend Const CBS_DROPDOWNLIST As Integer = &H3
'    Friend Const CBS_OWNERDRAWFIXED As Integer = &H10
'    Friend Const CBS_OWNERDRAWVARIABLE As Integer = &H20
'    Friend Const CBS_AUTOHSCROLL As Integer = &H40
'    Friend Const CBS_OEMCONVERT As Integer = &H80
'    Friend Const CBS_SORT As Integer = &H100
'    Friend Const CBS_HASSTRINGS As Integer = &H200
'    Friend Const CBS_NOINTEGRALHEIGHT As Integer = &H400
'    Friend Const CBS_DISABLENOSCROLL As Integer = &H800
'    Friend Const CBS_UPPERCASE As Integer = &H2000
'    Friend Const CBS_LOWERCASE As Integer = &H4000
'    Friend Const CB_GETEDITSEL As Integer = &H140
'    Friend Const CB_LIMITTEXT As Integer = &H141
'    Friend Const CB_SETEDITSEL As Integer = &H142
'    Friend Const CB_ADDSTRING As Integer = &H143
'    Friend Const CB_DELETESTRING As Integer = &H144
'    Friend Const CB_DIR As Integer = &H145
'    Friend Const CB_GETCOUNT As Integer = &H146
'    Friend Const CB_GETCURSEL As Integer = &H147
'    Friend Const CB_GETLBTEXT As Integer = &H148
'    Friend Const CB_GETLBTEXTLEN As Integer = &H149
'    Friend Const CB_INSERTSTRING As Integer = &H14A
'    Friend Const CB_RESETCONTENT As Integer = &H14B
'    Friend Const CB_FINDSTRING As Integer = &H14C
'    Friend Const CB_SELECTSTRING As Integer = &H14D
'    Friend Const CB_SETCURSEL As Integer = &H14E
'    Friend Const CB_SHOWDROPDOWN As Integer = &H14F
'    Friend Const CB_GETITEMDATA As Integer = &H150
'    Friend Const CB_SETITEMDATA As Integer = &H151
'    Friend Const CB_GETDROPPEDCONTROLRECT As Integer = &H152
'    Friend Const CB_SETITEMHEIGHT As Integer = &H153
'    Friend Const CB_GETITEMHEIGHT As Integer = &H154
'    Friend Const CB_SETEXTENDEDUI As Integer = &H155
'    Friend Const CB_GETEXTENDEDUI As Integer = &H156
'    Friend Const CB_GETDROPPEDSTATE As Integer = &H157
'    Friend Const CB_FINDSTRINGEXACT As Integer = &H158
'    Friend Const CB_SETLOCALE As Integer = &H159
'    Friend Const CB_GETLOCALE As Integer = &H15A
'    Friend Const CB_GETTOPINDEX As Integer = &H15B
'    Friend Const CB_SETTOPINDEX As Integer = &H15C
'    Friend Const CB_GETHORIZONTALEXTENT As Integer = &H15D
'    Friend Const CB_SETHORIZONTALEXTENT As Integer = &H15E
'    Friend Const CB_GETDROPPEDWIDTH As Integer = &H15F
'    Friend Const CB_SETDROPPEDWIDTH As Integer = &H160
'    Friend Const CB_INITSTORAGE As Integer = &H161
'    Friend Const CB_MSGMAX As Integer = &H162
'    ' CB_MSGMAX = 0x015B;
'    Friend Const CDS_UPDATEREGISTRY As Integer = &H1
'    Friend Const CDS_TEST As Integer = &H2
'    Friend Const CDS_FULLSCREEN As Integer = &H4
'    Friend Const CDS_GLOBAL As Integer = &H8
'    Friend Const CDS_SET_PRIMARY As Integer = &H10
'    Friend Const CDS_RESET As Integer = &H40000000
'    Friend Const CDS_SETRECT As Integer = &H20000000
'    Friend Const CDS_NORESET As Integer = &H10000000
'    Friend Const CBEN_FIRST As Integer = 0 - 800
'    Friend Const CBEN_LAST As Integer = 0 - 830
'    Friend Const CDRF_DODEFAULT As Integer = &H0
'    Friend Const CDRF_NEWFONT As Integer = &H2
'    Friend Const CDRF_SKIPDEFAULT As Integer = &H4
'    Friend Const CDRF_NOTIFYPOSTPAINT As Integer = &H10
'    Friend Const CDRF_NOTIFYITEMDRAW As Integer = &H20
'    Friend Const CDRF_NOTIFYSUBITEMDRAW As Integer = CDRF_NOTIFYITEMDRAW
'    Friend Const CDRF_NOTIFYPOSTERASE As Integer = &H40
'    Friend Const CDRF_NOTIFYITEMERASE As Integer = &H80
'    Friend Const CDDS_PREPAINT As Integer = &H1
'    Friend Const CDDS_POSTPAINT As Integer = &H2
'    Friend Const CDDS_PREERASE As Integer = &H3
'    Friend Const CDDS_POSTERASE As Integer = &H4
'    Friend Const CDDS_ITEM As Integer = &H10000
'    Friend Const CDDS_SUBITEM As Integer = &H20000
'    Friend Const CDDS_ITEMPREPAINT As Integer = &H10000 Or &H1
'    Friend Const CDDS_ITEMPOSTPAINT As Integer = &H10000 Or &H2
'    Friend Const CDDS_ITEMPREERASE As Integer = &H10000 Or &H3
'    Friend Const CDDS_ITEMPOSTERASE As Integer = &H10000 Or &H4
'    Friend Const CDIS_SELECTED As Integer = &H1
'    Friend Const CDIS_GRAYED As Integer = &H2
'    Friend Const CDIS_DISABLED As Integer = &H4
'    Friend Const CDIS_CHECKED As Integer = &H8
'    Friend Const CDIS_FOCUS As Integer = &H10
'    Friend Const CDIS_DEFAULT As Integer = &H20
'    Friend Const CDIS_HOT As Integer = &H40
'    Friend Const CDIS_MARKED As Integer = &H80
'    Friend Const CDIS_INDETERMINATE As Integer = &H100
'    Friend Const CLR_NONE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CDIS_INDETERMINATE = 0x0100,
'    '        CLR_NONE = unchecked((int)0xFFFFFFFF),
'    '--------------------^--- GenCode(token): unexpected token type
'    Friend Const CLR_DEFAULT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CLR_NONE = unchecked((int)0xFFFFFFFF),
'    '        CLR_DEFAULT = unchecked((int)0xFF000000),
'    '-----------------------^--- GenCode(token): unexpected token type
'    Friend Const CLR_HILIGHT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        CLR_DEFAULT = unchecked((int)0xFF000000),
'    '        CLR_HILIGHT = unchecked((int)0xFF000000),
'    '-----------------------^--- GenCode(token): unexpected token type
'    Friend Const CMB_MASKED As Integer = &H2
'    Friend Const CCS_TOP As Integer = &H1
'    Friend Const CCS_NOMOVEY As Integer = &H2
'    Friend Const CCS_BOTTOM As Integer = &H3
'    Friend Const CCS_NORESIZE As Integer = &H4
'    Friend Const CCS_NOPARENTALIGN As Integer = &H8
'    Friend Const CCS_ADJUSTABLE As Integer = &H20
'    Friend Const CCS_NODIVIDER As Integer = &H40
'    Friend Const CCS_VERT As Integer = &H80
'    Friend Const CCS_LEFT As Integer = &H80 Or &H1
'    Friend Const CCS_RIGHT As Integer = &H80 Or &H3
'    Friend Const CCS_NOMOVEX As Integer = &H80 Or &H2
'    Friend Const CBEIF_TEXT As Integer = &H1
'    Friend Const CBEIF_IMAGE As Integer = &H2
'    Friend Const CBEIF_SELECTEDIMAGE As Integer = &H4
'    Friend Const CBEIF_OVERLAY As Integer = &H8
'    Friend Const CBEIF_INDENT As Integer = &H10
'    Friend Const CBEIF_LPARAM As Integer = &H20
'    Friend Const CBEIF_DI_SETITEM As Integer = &H10000000
'    Friend Const CBEM_INSERTITEMA As Integer = &H400 + 1
'    Friend Const CBEM_SETIMAGELIST As Integer = &H400 + 2
'    Friend Const CBEM_GETIMAGELIST As Integer = &H400 + 3
'    Friend Const CBEM_GETITEMA As Integer = &H400 + 4
'    Friend Const CBEM_SETITEMA As Integer = &H400 + 5
'    Friend Const CBEM_DELETEITEM As Integer = &H144
'    Friend Const CBEM_GETCOMBOCONTROL As Integer = &H400 + 6
'    Friend Const CBEM_GETEDITCONTROL As Integer = &H400 + 7
'    Friend Const CBEM_SETEXSTYLE As Integer = &H400 + 8
'    Friend Const CBEM_GETEXSTYLE As Integer = &H400 + 9
'    Friend Const CBEM_HASEDITCHANGED As Integer = &H400 + 10
'    Friend Const CBEM_INSERTITEMW As Integer = &H400 + 11
'    Friend Const CBEM_SETITEMW As Integer = &H400 + 12
'    Friend Const CBEM_GETITEMW As Integer = &H400 + 13
'    Friend Const CBES_EX_NOEDITIMAGE As Integer = &H1
'    Friend Const CBES_EX_NOEDITIMAGEINDENT As Integer = &H2
'    Friend Const CBES_EX_PATHWORDBREAKPROC As Integer = &H4
'    Friend Const CBEN_GETDISPINFO As Integer = 0 - 800 - 0
'    Friend Const CBEN_INSERTITEM As Integer = 0 - 800 - 1
'    Friend Const CBEN_DELETEITEM As Integer = 0 - 800 - 2
'    Friend Const CBEN_BEGINEDIT As Integer = 0 - 800 - 4
'    Friend Const CBEN_ENDEDITA As Integer = 0 - 800 - 5
'    Friend Const CBEN_ENDEDITW As Integer = 0 - 800 - 6
'    Friend Const CBENF_KILLFOCUS As Integer = 1
'    Friend Const CBENF_RETURN As Integer = 2
'    Friend Const CBENF_ESCAPE As Integer = 3
'    Friend Const CBENF_DROPDOWN As Integer = 4
'    Friend Const CBEMAXSTRLEN As Integer = 260
'    Friend Const CDM_FIRST As Integer = &H400 + 100
'    Friend Const CDM_LAST As Integer = &H400 + 200
'    Friend Const CDM_GETSPEC As Integer = &H400 + 100 + &H0
'    Friend Const CDM_GETFILEPATH As Integer = &H400 + 100 + &H1
'    Friend Const CDM_GETFOLDERPATH As Integer = &H400 + 100 + &H2
'    Friend Const CDM_GETFOLDERIDLIST As Integer = &H400 + 100 + &H3
'    Friend Const CDM_SETCONTROLTEXT As Integer = &H400 + 100 + &H4
'    Friend Const CDM_HIDECONTROL As Integer = &H400 + 100 + &H5
'    Friend Const CDM_SETDEFEXT As Integer = &H400 + 100 + &H6
'    Friend Const CONTROL_C_EXIT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                CDM_SETDEFEXT = ((0x0400+100)+0x0006),
'    '                                                                                                                                                                CONTROL_C_EXIT = (unchecked((int)0xC000013A)),
'    '-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const CCM_FIRST As Integer = &H2000
'    Friend Const CCM_SETBKCOLOR As Integer = CCM_FIRST + 1
'    Friend Const CCM_SETCOLORSCHEME As Integer = CCM_FIRST + 2
'    Friend Const CCM_GETCOLORSCHEME As Integer = CCM_FIRST + 3
'    Friend Const CCM_GETDROPTARGET As Integer = CCM_FIRST + 4
'    Friend Const CCM_SETUNICODEFORMAT As Integer = CCM_FIRST + 5
'    Friend Const CCM_GETUNICODEFORMAT As Integer = CCM_FIRST + 6
    
        Friend Const CLSCTX_INPROC_SERVER As Integer = &H1
        Friend Const CLSCTX_INPROC_HANDLER As Integer = &H2
        Friend Const CLSCTX_LOCAL_SERVER As Integer = &H4
        Friend Const CLSCTX_INPROC_SERVER16 As Integer = &H8
        Friend Const CLSCTX_REMOTE_SERVER As Integer = &H10
        Friend Const CLSCTX_INPROC_HANDLER16 As Integer = &H20
        Friend Const CLSCTX_INPROC_SERVERX86 As Integer = &H40
        Friend Const CLSCTX_INPROC_HANDLERX86 As Integer = &H80
        Friend Const CLSCTX_ESERVER_HANDLER As Integer = &H100
        Friend Const CLSCTX_RESERVED As Integer = &H200
        Friend Const CLSCTX_NO_CODE_DOWNLOAD As Integer = &H400
    
'    Friend Const CTRLINFO_EATS_RETURN As Integer = 1
'    Friend Const CTRLINFO_EATS_ESCAPE As Integer = 2
    
'    Friend Const DN_DEFAULTPRN As Integer = &H1
        '    Friend Const DDE_FACK As Integer = &h8000
        '    Friend Const DDE_FBUSY As Integer = &H4000
'    Friend Const DDE_FDEFERUPD As Integer = &H4000
        '    Friend Const DDE_FACKREQ As Integer = &h8000
        '    Friend Const DDE_FRELEASE As Integer = &H2000
'    Friend Const DDE_FREQUESTED As Integer = &H1000
'    Friend Const DDE_FAPPSTATUS As Integer = &HFF
'    Friend Const DDE_FNOTPROCESSED As Integer = &H0
'    Friend Const DDE_FACKRESERVED As Integer = Not(Or &H4000 Or &HFF)
'    '
'    'Note:  Error processing original source shown below
'    '        DDE_FNOTPROCESSED = 0x0000,
'    '        DDE_FACKRESERVED = (~(unchecked((int)0x8000)|0x4000|0x00ff)),
'    '-------------------------------^--- GenCode(token): unexpected token type
'    Friend Const DDE_FADVRESERVED As Integer = Not(Or &H4000)
'    '
'    'Note:  Error processing original source shown below
'    '        DDE_FACKRESERVED = (~(unchecked((int)0x8000)|0x4000|0x00ff)),
'    '                           DDE_FADVRESERVED = (~(unchecked((int)0x8000)|0x4000)),
'    '--------------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const DDE_FDATRESERVED As Integer = Not(Or &H2000 Or &H1000)
'    '
'    'Note:  Error processing original source shown below
'    '                           DDE_FADVRESERVED = (~(unchecked((int)0x8000)|0x4000)),
'    '                                              DDE_FDATRESERVED = (~(unchecked((int)0x8000)|0x2000|0x1000)),
'    '---------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const DDE_FPOKRESERVED As Integer = Not &H2000
'    Friend Const DNS_REGISTER As Integer = &H1
'    Friend Const DNS_UNREGISTER As Integer = &H2
'    Friend Const DNS_FILTERON As Integer = &H4
'    Friend Const DNS_FILTEROFF As Integer = &H8
'    Friend Const DMLERR_NO_ERROR As Integer = 0
'    Friend Const DMLERR_FIRST As Integer = &H4000
'    Friend Const DMLERR_ADVACKTIMEOUT As Integer = &H4000
'    Friend Const DMLERR_BUSY As Integer = &H4001
'    Friend Const DMLERR_DATAACKTIMEOUT As Integer = &H4002
'    Friend Const DMLERR_DLL_NOT_INITIALIZED As Integer = &H4003
'    Friend Const DMLERR_DLL_USAGE As Integer = &H4004
'    Friend Const DMLERR_EXECACKTIMEOUT As Integer = &H4005
'    Friend Const DMLERR_INVALIDPARAMETER As Integer = &H4006
'    Friend Const DMLERR_LOW_MEMORY As Integer = &H4007
'    Friend Const DMLERR_MEMORY_ERROR As Integer = &H4008
'    Friend Const DMLERR_NOTPROCESSED As Integer = &H4009
'    Friend Const DMLERR_NO_CONV_ESTABLISHED As Integer = &H400A
'    Friend Const DMLERR_POKEACKTIMEOUT As Integer = &H400B
'    Friend Const DMLERR_POSTMSG_FAILED As Integer = &H400C
'    Friend Const DMLERR_REENTRANCY As Integer = &H400D
'    Friend Const DMLERR_SERVER_DIED As Integer = &H400E
'    Friend Const DMLERR_SYS_ERROR As Integer = &H400F
'    Friend Const DMLERR_UNADVACKTIMEOUT As Integer = &H4010
'    Friend Const DMLERR_UNFOUND_QUEUE_ID As Integer = &H4011
'    Friend Const DMLERR_LAST As Integer = &H4011
'    Friend Const DIALOPTION_BILLING As Integer = &H40
'    Friend Const DIALOPTION_QUIET As Integer = &H80
'    Friend Const DIALOPTION_DIALTONE As Integer = &H100
'    Friend Const DRV_LOAD As Integer = &H1
'    Friend Const DRV_ENABLE As Integer = &H2
'    Friend Const DRV_OPEN As Integer = &H3
'    Friend Const DRV_CLOSE As Integer = &H4
'    Friend Const DRV_DISABLE As Integer = &H5
'    Friend Const DRV_FREE As Integer = &H6
'    Friend Const DRV_CONFIGURE As Integer = &H7
'    Friend Const DRV_QUERYCONFIGURE As Integer = &H8
'    Friend Const DRV_INSTALL As Integer = &H9
'    Friend Const DRV_REMOVE As Integer = &HA
'    Friend Const DRV_EXITSESSION As Integer = &HB
'    Friend Const DRV_POWER As Integer = &HF
'    Friend Const DRV_RESERVED As Integer = &H800
'    Friend Const DRV_USER As Integer = &H4000
'    Friend Const DRVCNF_CANCEL As Integer = &H0
'    Friend Const DRVCNF_OK As Integer = &H1
'    Friend Const DRVCNF_RESTART As Integer = &H2
'    Friend Const DRV_CANCEL As Integer = &H0
'    Friend Const DRV_OK As Integer = &H1
'    Friend Const DRV_RESTART As Integer = &H2
'    Friend Const DRV_MCI_FIRST As Integer = &H800
'    Friend Const DRV_MCI_LAST As Integer = &H800 + &HFFF
'    Friend Const DEREGISTERED As Integer = &H5
'    Friend Const DUPLICATE As Integer = &H6
'    Friend Const DUPLICATE_DEREG As Integer = &H7
        Friend Const DISPID_UNKNOWN As Integer = -1
'    Friend Const DISPID_VALUE As Integer = 0
'    Friend Const DISPID_PROPERTYPUT As Integer = - 3
'    Friend Const DISPID_NEWENUM As Integer = - 4
'    Friend Const DISPID_EVALUATE As Integer = - 5
'    Friend Const DISPID_DESTRUCTOR As Integer = - 7
'    Friend Const DISPID_COLLECT As Integer = - 8
'    Friend Const DISPATCH_METHOD As Integer = &H1
'    Friend Const DISPATCH_PROPERTYGET As Integer = &H2
'    Friend Const DISPATCH_PROPERTYPUT As Integer = &H4
'    Friend Const DISPATCH_PROPERTYPUTREF As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        DISPATCH_PROPERTYPUT = 0x4,
'    '        DISPATCH_PROPERTYPUTREF = unchecked((int)0x8),
'    '-----------------------------------^--- GenCode(token): unexpected token type
'    Friend Const DROPEFFECT_NONE As Integer = 0
'    Friend Const DROPEFFECT_COPY As Integer = 1
'    Friend Const DROPEFFECT_MOVE As Integer = 2
'    Friend Const DROPEFFECT_LINK As Integer = 4
'    Friend Const DROPEFFECT_SCROLL As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                              DROPEFFECT_LINK = (4),
'    '                                                                                DROPEFFECT_SCROLL = (unchecked((int)0x80000000)),
'    '------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const DD_DEFSCROLLINSET As Integer = 11
'    Friend Const DD_DEFSCROLLDELAY As Integer = 50
'    Friend Const DD_DEFSCROLLINTERVAL As Integer = 50
'    Friend Const DD_DEFDRAGDELAY As Integer = 200
'    Friend Const DD_DEFDRAGMINDIST As Integer = 2
'    Friend Const DCE_C_ERROR_STRING_LEN As Integer = 256
'    Friend Const DATABITS_5 As Integer = &H1
'    Friend Const DATABITS_6 As Integer = &H2
'    Friend Const DATABITS_7 As Integer = &H4
'    Friend Const DATABITS_8 As Integer = &H8
'    Friend Const DATABITS_16 As Integer = &H10
'    Friend Const DATABITS_16X As Integer = &H20
'    Friend Const DTR_CONTROL_DISABLE As Integer = &H0
'    Friend Const DTR_CONTROL_ENABLE As Integer = &H1
'    Friend Const DTR_CONTROL_HANDSHAKE As Integer = &H2
'    Friend Const DEBUG_PROCESS As Integer = &H1
'    Friend Const DEBUG_ONLY_THIS_PROCESS As Integer = &H2
'    Friend Const DETACHED_PROCESS As Integer = &H8
'    Friend Const DRIVE_UNKNOWN As Integer = 0
'    Friend Const DRIVE_NO_ROOT_DIR As Integer = 1
'    Friend Const DRIVE_REMOVABLE As Integer = 2
'    Friend Const DRIVE_FIXED As Integer = 3
'    Friend Const DRIVE_REMOTE As Integer = 4
'    Friend Const DRIVE_CDROM As Integer = 5
'    Friend Const DRIVE_RAMDISK As Integer = 6
'    Friend Const DONT_RESOLVE_DLL_REFERENCES As Integer = &H1
'    Friend Const DDD_RAW_TARGET_PATH As Integer = &H1
'    Friend Const DDD_REMOVE_DEFINITION As Integer = &H2
'    Friend Const DDD_EXACT_MATCH_ON_REMOVE As Integer = &H4
'    Friend Const DDD_NO_BROADCAST_SYSTEM As Integer = &H8
'    Friend Const DOCKINFO_UNDOCKED As Integer = &H1
'    Friend Const DOCKINFO_DOCKED As Integer = &H2
'    Friend Const DOCKINFO_USER_SUPPLIED As Integer = &H4
'    Friend Const DOCKINFO_USER_UNDOCKED As Integer = &H4 Or &H1
'    Friend Const DOCKINFO_USER_DOCKED As Integer = &H4 Or &H2
'    Friend Const DOUBLE_CLICK As Integer = &H2
'    Friend Const DM_UPDATE As Integer = 1
'    Friend Const DM_COPY As Integer = 2
'    Friend Const DM_PROMPT As Integer = 4
'    Friend Const DM_MODIFY As Integer = 8
'    Friend Const DM_IN_BUFFER As Integer = 8
'    Friend Const DM_IN_PROMPT As Integer = 4
'    Friend Const DM_OUT_BUFFER As Integer = 2
'    Friend Const DM_OUT_DEFAULT As Integer = 1
'    Friend Const DC_FIELDS As Integer = 1
'    Friend Const DC_PAPERS As Integer = 2
'    Friend Const DC_PAPERSIZE As Integer = 3
'    Friend Const DC_MINEXTENT As Integer = 4
'    Friend Const DC_MAXEXTENT As Integer = 5
'    Friend Const DC_BINS As Integer = 6
'    Friend Const DC_DUPLEX As Integer = 7
'    Friend Const DC_SIZE As Integer = 8
'    Friend Const DC_EXTRA As Integer = 9
'    Friend Const DC_VERSION As Integer = 10
'    Friend Const DC_DRIVER As Integer = 11
'    Friend Const DC_BINNAMES As Integer = 12
'    Friend Const DC_ENUMRESOLUTIONS As Integer = 13
'    Friend Const DC_FILEDEPENDENCIES As Integer = 14
'    Friend Const DC_TRUETYPE As Integer = 15
'    Friend Const DC_PAPERNAMES As Integer = 16
'    Friend Const DC_ORIENTATION As Integer = 17
'    Friend Const DC_COPIES As Integer = 18
'    Friend Const DV_E_FORMATETC As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        DC_COPIES = 18,
'    '        DV_E_FORMATETC = unchecked((int)0x80040064),
'    '--------------------------^--- GenCode(token): unexpected token type
'    Friend Const DV_E_DVTARGETDEVICE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        DV_E_FORMATETC = unchecked((int)0x80040064),
'    '        DV_E_DVTARGETDEVICE = unchecked((int)0x80040065),
'    '-------------------------------^--- GenCode(token): unexpected token type
'    Friend Const DV_E_STGMEDIUM As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        DV_E_DVTARGETDEVICE = unchecked((int)0x80040065),
'    '        DV_E_STGMEDIUM = unchecked((int)0x80040066),
'    '--------------------------^--- GenCode(token): unexpected token type
'    Friend Const DV_E_STATDATA As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        DV_E_STGMEDIUM = unchecked((int)0x80040066),
'    '        DV_E_STATDATA = unchecked((int)0x80040067),
'    '-------------------------^--- GenCode(token): unexpected token type
'    Friend Const DV_E_LINDEX As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        DV_E_STATDATA = unchecked((int)0x80040067),
'    '        DV_E_LINDEX = unchecked((int)0x80040068),
'    '-----------------------^--- GenCode(token): unexpected token type
'    Friend Const DV_E_TYMED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        DV_E_LINDEX = unchecked((int)0x80040068),
'    '        DV_E_TYMED = unchecked((int)0x80040069),
'    '----------------------^--- GenCode(token): unexpected token type
'    Friend Const DV_E_CLIPFORMAT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        DV_E_TYMED = unchecked((int)0x80040069),
'    '        DV_E_CLIPFORMAT = unchecked((int)0x8004006A),
'    '---------------------------^--- GenCode(token): unexpected token type
'    Friend Const DV_E_DVASPECT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        DV_E_CLIPFORMAT = unchecked((int)0x8004006A),
'    '        DV_E_DVASPECT = unchecked((int)0x8004006B),
'    '-------------------------^--- GenCode(token): unexpected token type
'    Friend Const DV_E_DVTARGETDEVICE_SIZE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        DV_E_DVASPECT = unchecked((int)0x8004006B),
'    '        DV_E_DVTARGETDEVICE_SIZE = unchecked((int)0x8004006C),
'    '------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const DV_E_NOIVIEWOBJECT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        DV_E_DVTARGETDEVICE_SIZE = unchecked((int)0x8004006C),
'    '        DV_E_NOIVIEWOBJECT = unchecked((int)0x8004006D),
'    '------------------------------^--- GenCode(token): unexpected token type
'    Friend Const DRAGDROP_E_FIRST As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        DV_E_NOIVIEWOBJECT = unchecked((int)0x8004006D),
'    '        DRAGDROP_E_FIRST = unchecked((int)0x80040100),
'    '----------------------------^--- GenCode(token): unexpected token type
'    Friend Const DRAGDROP_E_LAST As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        DRAGDROP_E_FIRST = unchecked((int)0x80040100),
'    '        DRAGDROP_E_LAST = unchecked((int)0x8004010F),
'    '---------------------------^--- GenCode(token): unexpected token type
'    Friend Const DRAGDROP_S_FIRST As Integer = &H40100
'    Friend Const DRAGDROP_S_LAST As Integer = &H4010F
'    Friend Const DRAGDROP_E_NOTREGISTERED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        DRAGDROP_S_LAST = 0x0004010F,
'    '        DRAGDROP_E_NOTREGISTERED = unchecked((int)0x80040100),
'    '------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const DRAGDROP_E_ALREADYREGISTERED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        DRAGDROP_E_NOTREGISTERED = unchecked((int)0x80040100),
'    '        DRAGDROP_E_ALREADYREGISTERED = unchecked((int)0x80040101),
'    '----------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const DRAGDROP_E_INVALIDHWND As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        DRAGDROP_E_ALREADYREGISTERED = unchecked((int)0x80040101),
'    '        DRAGDROP_E_INVALIDHWND = unchecked((int)0x80040102),
'    '----------------------------------^--- GenCode(token): unexpected token type
'    Friend Const DATA_E_FIRST As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        DRAGDROP_E_INVALIDHWND = unchecked((int)0x80040102),
'    '        DATA_E_FIRST = unchecked((int)0x80040130),
'    '------------------------^--- GenCode(token): unexpected token type
'    Friend Const DATA_E_LAST As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        DATA_E_FIRST = unchecked((int)0x80040130),
'    '        DATA_E_LAST = unchecked((int)0x8004013F),
'    '-----------------------^--- GenCode(token): unexpected token type
'    Friend Const DATA_S_FIRST As Integer = &H40130
'    Friend Const DATA_S_LAST As Integer = &H4013F
'    Friend Const DRAGDROP_S_DROP As Integer = &H40100
'    Friend Const DRAGDROP_S_CANCEL As Integer = &H40101
'    Friend Const DRAGDROP_S_USEDEFAULTCURSORS As Integer = &H40102
'    Friend Const DATA_S_SAMEFORMATETC As Integer = &H40130
'    Friend Const DISP_E_UNKNOWNINTERFACE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        DATA_S_SAMEFORMATETC = 0x00040130,
'    '        DISP_E_UNKNOWNINTERFACE = unchecked((int)0x80020001),
'    '-----------------------------------^--- GenCode(token): unexpected token type
     Friend Const DISP_E_MEMBERNOTFOUND As Integer = &H80020003
'    '
'    'Note:  Error processing original source shown below
'    '        DISP_E_UNKNOWNINTERFACE = unchecked((int)0x80020001),
'    '        DISP_E_MEMBERNOTFOUND = unchecked((int)0x80020003),
'    '---------------------------------^--- GenCode(token): unexpected token type
'    Friend Const DISP_E_PARAMNOTFOUND As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        DISP_E_MEMBERNOTFOUND = unchecked((int)0x80020003),
'    '        DISP_E_PARAMNOTFOUND = unchecked((int)0x80020004),
'    '--------------------------------^--- GenCode(token): unexpected token type
'    Friend Const DISP_E_TYPEMISMATCH As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        DISP_E_PARAMNOTFOUND = unchecked((int)0x80020004),
'    '        DISP_E_TYPEMISMATCH = unchecked((int)0x80020005),
'    '-------------------------------^--- GenCode(token): unexpected token type
'    Friend Const DISP_E_UNKNOWNNAME As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        DISP_E_TYPEMISMATCH = unchecked((int)0x80020005),
'    '        DISP_E_UNKNOWNNAME = unchecked((int)0x80020006),
'    '------------------------------^--- GenCode(token): unexpected token type
'    Friend Const DISP_E_NONAMEDARGS As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        DISP_E_UNKNOWNNAME = unchecked((int)0x80020006),
'    '        DISP_E_NONAMEDARGS = unchecked((int)0x80020007),
'    '------------------------------^--- GenCode(token): unexpected token type
'    Friend Const DISP_E_BADVARTYPE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        DISP_E_NONAMEDARGS = unchecked((int)0x80020007),
'    '        DISP_E_BADVARTYPE = unchecked((int)0x80020008),
'    '-----------------------------^--- GenCode(token): unexpected token type
'    Friend Const DISP_E_EXCEPTION As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        DISP_E_BADVARTYPE = unchecked((int)0x80020008),
'    '        DISP_E_EXCEPTION = unchecked((int)0x80020009),
'    '----------------------------^--- GenCode(token): unexpected token type
'    Friend Const DISP_E_OVERFLOW As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        DISP_E_EXCEPTION = unchecked((int)0x80020009),
'    '        DISP_E_OVERFLOW = unchecked((int)0x8002000A),
'    '---------------------------^--- GenCode(token): unexpected token type
'    Friend Const DISP_E_BADINDEX As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        DISP_E_OVERFLOW = unchecked((int)0x8002000A),
'    '        DISP_E_BADINDEX = unchecked((int)0x8002000B),
'    '---------------------------^--- GenCode(token): unexpected token type
'    Friend Const DISP_E_UNKNOWNLCID As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        DISP_E_BADINDEX = unchecked((int)0x8002000B),
'    '        DISP_E_UNKNOWNLCID = unchecked((int)0x8002000C),
'    '------------------------------^--- GenCode(token): unexpected token type
'    Friend Const DISP_E_ARRAYISLOCKED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        DISP_E_UNKNOWNLCID = unchecked((int)0x8002000C),
'    '        DISP_E_ARRAYISLOCKED = unchecked((int)0x8002000D),
'    '--------------------------------^--- GenCode(token): unexpected token type
'    Friend Const DISP_E_BADPARAMCOUNT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        DISP_E_ARRAYISLOCKED = unchecked((int)0x8002000D),
'    '        DISP_E_BADPARAMCOUNT = unchecked((int)0x8002000E),
'    '--------------------------------^--- GenCode(token): unexpected token type
'    Friend Const DISP_E_PARAMNOTOPTIONAL As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        DISP_E_BADPARAMCOUNT = unchecked((int)0x8002000E),
'    '        DISP_E_PARAMNOTOPTIONAL = unchecked((int)0x8002000F),
'    '-----------------------------------^--- GenCode(token): unexpected token type
'    Friend Const DISP_E_BADCALLEE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        DISP_E_PARAMNOTOPTIONAL = unchecked((int)0x8002000F),
'    '        DISP_E_BADCALLEE = unchecked((int)0x80020010),
'    '----------------------------^--- GenCode(token): unexpected token type
'    Friend Const DISP_E_NOTACOLLECTION As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        DISP_E_BADCALLEE = unchecked((int)0x80020010),
'    '        DISP_E_NOTACOLLECTION = unchecked((int)0x80020011),
'    '---------------------------------^--- GenCode(token): unexpected token type
'    Friend Const DIGSIG_E_ENCODE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        DISP_E_NOTACOLLECTION = unchecked((int)0x80020011),
'    '        DIGSIG_E_ENCODE = unchecked((int)0x800B0005),
'    '---------------------------^--- GenCode(token): unexpected token type
'    Friend Const DIGSIG_E_DECODE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        DIGSIG_E_ENCODE = unchecked((int)0x800B0005),
'    '        DIGSIG_E_DECODE = unchecked((int)0x800B0006),
'    '---------------------------^--- GenCode(token): unexpected token type
'    Friend Const DIGSIG_E_EXTENSIBILITY As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        DIGSIG_E_DECODE = unchecked((int)0x800B0006),
'    '        DIGSIG_E_EXTENSIBILITY = unchecked((int)0x800B0007),
'    '----------------------------------^--- GenCode(token): unexpected token type
'    Friend Const DIGSIG_E_CRYPTO As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        DIGSIG_E_EXTENSIBILITY = unchecked((int)0x800B0007),
'    '        DIGSIG_E_CRYPTO = unchecked((int)0x800B0008),
'    '---------------------------^--- GenCode(token): unexpected token type
'    Friend Const DCB_RESET As Integer = &H1
'    Friend Const DCB_ACCUMULATE As Integer = &H2
'    Friend Const DCB_DIRTY As Integer = &H2
'    Friend Const DCB_SET As Integer = &H1 Or &H2
'    Friend Const DCB_ENABLE As Integer = &H4
'    Friend Const DCB_DISABLE As Integer = &H8
'    Friend Const DRAFTMODE As Integer = 7
'    Friend Const DEVICEDATA As Integer = 19
'    Friend Const DRAWPATTERNRECT As Integer = 25
'    Friend Const DOWNLOADFACE As Integer = 514
'    Friend Const DOWNLOADHEADER As Integer = 4111
'    Friend Const DEFAULT_QUALITY As Integer = 0
'    Friend Const DRAFT_QUALITY As Integer = 1
'    Friend Const DEFAULT_PITCH As Integer = 0
'    Friend Const DEFAULT_CHARSET As Integer = 1
'    Friend Const DEVICE_FONTTYPE As Integer = &H2
'    Friend Const DKGRAY_BRUSH As Integer = 3
'    Friend Const DEVICE_DEFAULT_FONT As Integer = 14
'    Friend Const DEFAULT_PALETTE As Integer = 15
'    Friend Const DEFAULT_GUI_FONT As Integer = 17
'    Friend Const DRIVERVERSION As Integer = 0
'    Friend Const DESKTOPVERTRES As Integer = 117
'    Friend Const DESKTOPHORZRES As Integer = 118
'    Friend Const DT_PLOTTER As Integer = 0
'    Friend Const DT_RASDISPLAY As Integer = 1
'    Friend Const DT_RASPRINTER As Integer = 2
'    Friend Const DT_RASCAMERA As Integer = 3
'    Friend Const DT_CHARSTREAM As Integer = 4
'    Friend Const DT_METAFILE As Integer = 5
'    Friend Const DT_DISPFILE As Integer = 6
'    Friend Const DIB_RGB_COLORS As Integer = 0
'    Friend Const DIB_PAL_COLORS As Integer = 1
'    Friend Const DM_SPECVERSION As Integer = &H401
'    Friend Const DM_ORIENTATION As Integer = &H1
'    Friend Const DM_PAPERSIZE As Integer = &H2
'    Friend Const DM_PAPERLENGTH As Integer = &H4
'    Friend Const DM_PAPERWIDTH As Integer = &H8
'    Friend Const DM_SCALE As Integer = &H10
'    Friend Const DM_COPIES As Integer = &H100
'    Friend Const DM_DEFAULTSOURCE As Integer = &H200
'    Friend Const DM_PRINTQUALITY As Integer = &H400
'    Friend Const DM_COLOR As Integer = &H800
'    Friend Const DM_DUPLEX As Integer = &H1000
'    Friend Const DM_YRESOLUTION As Integer = &H2000
'    Friend Const DM_TTOPTION As Integer = &H4000
'    Friend Const DM_COLLATE As Integer = &H8000
'    Friend Const DM_FORMNAME As Integer = &H10000
'    Friend Const DM_LOGPIXELS As Integer = &H20000
'    Friend Const DM_BITSPERPEL As Integer = &H40000
'    Friend Const DM_PELSWIDTH As Integer = &H80000
'    Friend Const DM_PELSHEIGHT As Integer = &H100000
'    Friend Const DM_DISPLAYFLAGS As Integer = &H200000
'    Friend Const DM_DISPLAYFREQUENCY As Integer = &H400000
'    Friend Const DM_PANNINGWIDTH As Integer = &H800000
'    Friend Const DM_PANNINGHEIGHT As Integer = &H1000000
'    Friend Const DM_ICMMETHOD As Integer = &H2000000
'    Friend Const DM_ICMINTENT As Integer = &H4000000
'    Friend Const DM_MEDIATYPE As Integer = &H8000000
'    Friend Const DM_DITHERTYPE As Integer = &H10000000
'    Friend Const DM_ICCMANUFACTURER As Integer = &H20000000
'    Friend Const DM_ICCMODEL As Integer = &H40000000
'    Friend Const DMORIENT_PORTRAIT As Integer = 1
'    Friend Const DMORIENT_LANDSCAPE As Integer = 2
'    Friend Const DMPAPER_LETTER As Integer = 1
'    Friend Const DMPAPER_LETTERSMALL As Integer = 2
'    Friend Const DMPAPER_TABLOID As Integer = 3
'    Friend Const DMPAPER_LEDGER As Integer = 4
'    Friend Const DMPAPER_LEGAL As Integer = 5
'    Friend Const DMPAPER_STATEMENT As Integer = 6
'    Friend Const DMPAPER_EXECUTIVE As Integer = 7
'    Friend Const DMPAPER_A3 As Integer = 8
'    Friend Const DMPAPER_A4 As Integer = 9
'    Friend Const DMPAPER_A4SMALL As Integer = 10
'    Friend Const DMPAPER_A5 As Integer = 11
'    Friend Const DMPAPER_B4 As Integer = 12
'    Friend Const DMPAPER_B5 As Integer = 13
'    Friend Const DMPAPER_FOLIO As Integer = 14
'    Friend Const DMPAPER_QUARTO As Integer = 15
'    Friend Const DMPAPER_10X14 As Integer = 16
'    Friend Const DMPAPER_11X17 As Integer = 17
'    Friend Const DMPAPER_NOTE As Integer = 18
'    Friend Const DMPAPER_ENV_9 As Integer = 19
'    Friend Const DMPAPER_ENV_10 As Integer = 20
'    Friend Const DMPAPER_ENV_11 As Integer = 21
'    Friend Const DMPAPER_ENV_12 As Integer = 22
'    Friend Const DMPAPER_ENV_14 As Integer = 23
'    Friend Const DMPAPER_CSHEET As Integer = 24
'    Friend Const DMPAPER_DSHEET As Integer = 25
'    Friend Const DMPAPER_ESHEET As Integer = 26
'    Friend Const DMPAPER_ENV_DL As Integer = 27
'    Friend Const DMPAPER_ENV_C5 As Integer = 28
'    Friend Const DMPAPER_ENV_C3 As Integer = 29
'    Friend Const DMPAPER_ENV_C4 As Integer = 30
'    Friend Const DMPAPER_ENV_C6 As Integer = 31
'    Friend Const DMPAPER_ENV_C65 As Integer = 32
'    Friend Const DMPAPER_ENV_B4 As Integer = 33
'    Friend Const DMPAPER_ENV_B5 As Integer = 34
'    Friend Const DMPAPER_ENV_B6 As Integer = 35
'    Friend Const DMPAPER_ENV_ITALY As Integer = 36
'    Friend Const DMPAPER_ENV_MONARCH As Integer = 37
'    Friend Const DMPAPER_ENV_PERSONAL As Integer = 38
'    Friend Const DMPAPER_FANFOLD_US As Integer = 39
'    Friend Const DMPAPER_FANFOLD_STD_GERMAN As Integer = 40
'    Friend Const DMPAPER_FANFOLD_LGL_GERMAN As Integer = 41
'    Friend Const DMPAPER_ISO_B4 As Integer = 42
'    Friend Const DMPAPER_JAPANESE_POSTCARD As Integer = 43
'    Friend Const DMPAPER_9X11 As Integer = 44
'    Friend Const DMPAPER_10X11 As Integer = 45
'    Friend Const DMPAPER_15X11 As Integer = 46
'    Friend Const DMPAPER_ENV_INVITE As Integer = 47
'    Friend Const DMPAPER_RESERVED_48 As Integer = 48
'    Friend Const DMPAPER_RESERVED_49 As Integer = 49
'    Friend Const DMPAPER_LETTER_EXTRA As Integer = 50
'    Friend Const DMPAPER_LEGAL_EXTRA As Integer = 51
'    Friend Const DMPAPER_TABLOID_EXTRA As Integer = 52
'    Friend Const DMPAPER_A4_EXTRA As Integer = 53
'    Friend Const DMPAPER_LETTER_TRANSVERSE As Integer = 54
'    Friend Const DMPAPER_A4_TRANSVERSE As Integer = 55
'    Friend Const DMPAPER_LETTER_EXTRA_TRANSVERSE As Integer = 56
'    Friend Const DMPAPER_A_PLUS As Integer = 57
'    Friend Const DMPAPER_B_PLUS As Integer = 58
'    Friend Const DMPAPER_LETTER_PLUS As Integer = 59
'    Friend Const DMPAPER_A4_PLUS As Integer = 60
'    Friend Const DMPAPER_A5_TRANSVERSE As Integer = 61
'    Friend Const DMPAPER_B5_TRANSVERSE As Integer = 62
'    Friend Const DMPAPER_A3_EXTRA As Integer = 63
'    Friend Const DMPAPER_A5_EXTRA As Integer = 64
'    Friend Const DMPAPER_B5_EXTRA As Integer = 65
'    Friend Const DMPAPER_A2 As Integer = 66
'    Friend Const DMPAPER_A3_TRANSVERSE As Integer = 67
'    Friend Const DMPAPER_A3_EXTRA_TRANSVERSE As Integer = 68
'    Friend Const DMPAPER_DBL_JAPANESE_POSTCARD As Integer = 69
'    Friend Const DMPAPER_A6 As Integer = 70
'    Friend Const DMPAPER_JENV_KAKU2 As Integer = 71
'    Friend Const DMPAPER_JENV_KAKU3 As Integer = 72
'    Friend Const DMPAPER_JENV_CHOU3 As Integer = 73
'    Friend Const DMPAPER_JENV_CHOU4 As Integer = 74
'    Friend Const DMPAPER_LETTER_ROTATED As Integer = 75
'    Friend Const DMPAPER_A3_ROTATED As Integer = 76
'    Friend Const DMPAPER_A4_ROTATED As Integer = 77
'    Friend Const DMPAPER_A5_ROTATED As Integer = 78
'    Friend Const DMPAPER_B4_JIS_ROTATED As Integer = 79
'    Friend Const DMPAPER_B5_JIS_ROTATED As Integer = 80
'    Friend Const DMPAPER_JAPANESE_POSTCARD_ROTATED As Integer = 81
'    Friend Const DMPAPER_DBL_JAPANESE_POSTCARD_ROTATED As Integer = 82
'    Friend Const DMPAPER_A6_ROTATED As Integer = 83
'    Friend Const DMPAPER_JENV_KAKU2_ROTATED As Integer = 84
'    Friend Const DMPAPER_JENV_KAKU3_ROTATED As Integer = 85
'    Friend Const DMPAPER_JENV_CHOU3_ROTATED As Integer = 86
'    Friend Const DMPAPER_JENV_CHOU4_ROTATED As Integer = 87
'    Friend Const DMPAPER_B6_JIS As Integer = 88
'    Friend Const DMPAPER_B6_JIS_ROTATED As Integer = 89
'    Friend Const DMPAPER_12X11 As Integer = 90
'    Friend Const DMPAPER_JENV_YOU4 As Integer = 91
'    Friend Const DMPAPER_JENV_YOU4_ROTATED As Integer = 92
'    Friend Const DMPAPER_P16K As Integer = 93
'    Friend Const DMPAPER_P32K As Integer = 94
'    Friend Const DMPAPER_P32KBIG As Integer = 95
'    Friend Const DMPAPER_PENV_1 As Integer = 96
'    Friend Const DMPAPER_PENV_2 As Integer = 97
'    Friend Const DMPAPER_PENV_3 As Integer = 98
'    Friend Const DMPAPER_PENV_4 As Integer = 99
'    Friend Const DMPAPER_PENV_5 As Integer = 100
'    Friend Const DMPAPER_PENV_6 As Integer = 101
'    Friend Const DMPAPER_PENV_7 As Integer = 102
'    Friend Const DMPAPER_PENV_8 As Integer = 103
'    Friend Const DMPAPER_PENV_9 As Integer = 104
'    Friend Const DMPAPER_PENV_10 As Integer = 105
'    Friend Const DMPAPER_P16K_ROTATED As Integer = 106
'    Friend Const DMPAPER_P32K_ROTATED As Integer = 107
'    Friend Const DMPAPER_P32KBIG_ROTATED As Integer = 108
'    Friend Const DMPAPER_PENV_1_ROTATED As Integer = 109
'    Friend Const DMPAPER_PENV_2_ROTATED As Integer = 110
'    Friend Const DMPAPER_PENV_3_ROTATED As Integer = 111
'    Friend Const DMPAPER_PENV_4_ROTATED As Integer = 112
'    Friend Const DMPAPER_PENV_5_ROTATED As Integer = 113
'    Friend Const DMPAPER_PENV_6_ROTATED As Integer = 114
'    Friend Const DMPAPER_PENV_7_ROTATED As Integer = 115
'    Friend Const DMPAPER_PENV_8_ROTATED As Integer = 116
'    Friend Const DMPAPER_PENV_9_ROTATED As Integer = 117
'    Friend Const DMPAPER_PENV_10_ROTATED As Integer = 118
'    Friend Const DMPAPER_LAST As Integer = DMPAPER_PENV_10_ROTATED
'    Friend Const DMPAPER_USER As Integer = 256
'    Friend Const DMBIN_UPPER As Integer = 1
'    Friend Const DMBIN_ONLYONE As Integer = 1
'    Friend Const DMBIN_LOWER As Integer = 2
'    Friend Const DMBIN_MIDDLE As Integer = 3
'    Friend Const DMBIN_MANUAL As Integer = 4
'    Friend Const DMBIN_ENVELOPE As Integer = 5
'    Friend Const DMBIN_ENVMANUAL As Integer = 6
'    Friend Const DMBIN_AUTO As Integer = 7
'    Friend Const DMBIN_TRACTOR As Integer = 8
'    Friend Const DMBIN_SMALLFMT As Integer = 9
'    Friend Const DMBIN_LARGEFMT As Integer = 10
'    Friend Const DMBIN_LARGECAPACITY As Integer = 11
'    Friend Const DMBIN_CASSETTE As Integer = 14
'    Friend Const DMBIN_FORMSOURCE As Integer = 15
'    Friend Const DMBIN_LAST As Integer = 15
'    Friend Const DMBIN_USER As Integer = 256
'    Friend Const DMRES_DRAFT As Integer = - 1
'    Friend Const DMRES_LOW As Integer = - 2
'    Friend Const DMRES_MEDIUM As Integer = - 3
'    Friend Const DMRES_HIGH As Integer = - 4
'    Friend Const DMCOLOR_MONOCHROME As Integer = 1
'    Friend Const DMCOLOR_COLOR As Integer = 2
'    Friend Const DMDUP_SIMPLEX As Integer = 1
'    Friend Const DMDUP_VERTICAL As Integer = 2
'    Friend Const DMDUP_HORIZONTAL As Integer = 3
'    Friend Const DMTT_BITMAP As Integer = 1
'    Friend Const DMTT_DOWNLOAD As Integer = 2
'    Friend Const DMTT_SUBDEV As Integer = 3
'    Friend Const DMTT_DOWNLOAD_OUTLINE As Integer = 4
'    Friend Const DMCOLLATE_FALSE As Integer = 0
'    Friend Const DMCOLLATE_TRUE As Integer = 1
'    Friend Const DMDISPLAYFLAGS_TEXTMODE As Integer = &H4
'    Friend Const DMICMMETHOD_NONE As Integer = 1
'    Friend Const DMICMMETHOD_SYSTEM As Integer = 2
'    Friend Const DMICMMETHOD_DRIVER As Integer = 3
'    Friend Const DMICMMETHOD_DEVICE As Integer = 4
'    Friend Const DMICMMETHOD_USER As Integer = 256
'    Friend Const DMICM_SATURATE As Integer = 1
'    Friend Const DMICM_CONTRAST As Integer = 2
'    Friend Const DMICM_COLORMETRIC As Integer = 3
'    Friend Const DMICM_USER As Integer = 256
'    Friend Const DMMEDIA_STANDARD As Integer = 1
'    Friend Const DMMEDIA_TRANSPARENCY As Integer = 2
'    Friend Const DMMEDIA_GLOSSY As Integer = 3
'    Friend Const DMMEDIA_USER As Integer = 256
'    Friend Const DMDITHER_NONE As Integer = 1
'    Friend Const DMDITHER_COARSE As Integer = 2
'    Friend Const DMDITHER_FINE As Integer = 3
'    Friend Const DMDITHER_LINEART As Integer = 4
'    Friend Const DMDITHER_GRAYSCALE As Integer = 5
'    Friend Const DMDITHER_USER As Integer = 256
'    Friend Const DC_BINADJUST As Integer = 19
'    Friend Const DC_EMF_COMPLIANT As Integer = 20
'    Friend Const DC_DATATYPE_PRODUCED As Integer = 21
'    Friend Const DC_COLLATE As Integer = 22
'    Friend Const DCTT_BITMAP As Integer = &H1
'    Friend Const DCTT_DOWNLOAD As Integer = &H2
'    Friend Const DCTT_SUBDEV As Integer = &H4
'    Friend Const DCTT_DOWNLOAD_OUTLINE As Integer = &H8
'    Friend Const DCBA_FACEUPNONE As Integer = &H0
'    Friend Const DCBA_FACEUPCENTER As Integer = &H1
'    Friend Const DCBA_FACEUPLEFT As Integer = &H2
'    Friend Const DCBA_FACEUPRIGHT As Integer = &H3
'    Friend Const DCBA_FACEDOWNNONE As Integer = &H100
'    Friend Const DCBA_FACEDOWNCENTER As Integer = &H101
'    Friend Const DCBA_FACEDOWNLEFT As Integer = &H102
'    Friend Const DCBA_FACEDOWNRIGHT As Integer = &H103
'    Friend Const DI_APPBANDING As Integer = &H1
'    Friend Const DISC_UPDATE_PROFILE As Integer = &H1
'    Friend Const DISC_NO_FORCE As Integer = &H40
'    Friend Const DATE_SHORTDATE As Integer = &H1
'    Friend Const DATE_LONGDATE As Integer = &H2
'    Friend Const DATE_USE_ALT_CALENDAR As Integer = &H4
'    Friend Const DUPLICATE_CLOSE_SOURCE As Integer = &H1
'    Friend Const DUPLICATE_SAME_ACCESS As Integer = &H2
'    Friend Const DELETE As Integer = &H10000
'    Friend Const DOMAIN_USER_RID_ADMIN As Integer = &H1F4
'    Friend Const DOMAIN_USER_RID_GUEST As Integer = &H1F5
'    Friend Const DOMAIN_GROUP_RID_ADMINS As Integer = &H200
'    Friend Const DOMAIN_GROUP_RID_USERS As Integer = &H201
'    Friend Const DOMAIN_GROUP_RID_GUESTS As Integer = &H202
'    Friend Const DOMAIN_ALIAS_RID_ADMINS As Integer = &H220
'    Friend Const DOMAIN_ALIAS_RID_USERS As Integer = &H221
'    Friend Const DOMAIN_ALIAS_RID_GUESTS As Integer = &H222
'    Friend Const DOMAIN_ALIAS_RID_POWER_USERS As Integer = &H223
'    Friend Const DOMAIN_ALIAS_RID_ACCOUNT_OPS As Integer = &H224
'    Friend Const DOMAIN_ALIAS_RID_SYSTEM_OPS As Integer = &H225
'    Friend Const DOMAIN_ALIAS_RID_PRINT_OPS As Integer = &H226
'    Friend Const DOMAIN_ALIAS_RID_BACKUP_OPS As Integer = &H227
'    Friend Const DOMAIN_ALIAS_RID_REPLICATOR As Integer = &H228
'    Friend Const DACL_SECURITY_INFORMATION As Integer = 0
    
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
        Friend Const DLGC_WANTTAB As Integer = &H2
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
    
'    Friend Const DVASPECT_CONTENT As Integer = 1
'    Friend Const DVASPECT_THUMBNAIL As Integer = 2
'    Friend Const DVASPECT_ICON As Integer = 4
'    Friend Const DVASPECT_DOCPRINT As Integer = 8
'    Friend Const DVASPECT_OPAQUE As Integer = 16
'    Friend Const DVASPECT_TRANSPARENT As Integer = 32
    
'    Friend Const DATADIR_GET As Integer = 1
'    Friend Const DATADIR_SET As Integer = 2
    
    
    
'    Friend Const EC_ENABLEALL As Integer = 0
'    Friend Const EC_ENABLEONE As Integer = &H80
'    Friend Const EC_DISABLE As Integer = &H8
'    Friend Const EC_QUERYWAITING As Integer = 2
'    Friend Const edt1 As Integer = &H480
'    Friend Const edt2 As Integer = &H481
'    Friend Const edt3 As Integer = &H482
'    Friend Const edt4 As Integer = &H483
'    Friend Const edt5 As Integer = &H484
'    Friend Const edt6 As Integer = &H485
'    Friend Const edt7 As Integer = &H486
'    Friend Const edt8 As Integer = &H487
'    Friend Const edt9 As Integer = &H488
'    Friend Const edt10 As Integer = &H489
'    Friend Const edt11 As Integer = &H48A
'    Friend Const edt12 As Integer = &H48B
'    Friend Const edt13 As Integer = &H48C
'    Friend Const edt14 As Integer = &H48D
'    Friend Const edt15 As Integer = &H48E
'    Friend Const edt16 As Integer = &H48F
'    Friend Const EXCEPTION_EXECUTE_HANDLER As Integer = 1
'    Friend Const EXCEPTION_CONTINUE_SEARCH As Integer = 0
'    Friend Const EXCEPTION_CONTINUE_EXECUTION As Integer = - 1
'    Friend Const EMBDHLP_INPROC_HANDLER As Integer = &H0
'    Friend Const EMBDHLP_INPROC_SERVER As Integer = &H1
'    Friend Const EMBDHLP_CREATENOW As Integer = &H0
'    Friend Const EMBDHLP_DELAYCREATE As Integer = &H10000
'    Friend Const EXCEPTION_DEBUG_EVENT As Integer = 1
'    Friend Const EXIT_THREAD_DEBUG_EVENT As Integer = 4
'    Friend Const EXIT_PROCESS_DEBUG_EVENT As Integer = 5
'    Friend Const EVENPARITY As Integer = 2
'    Friend Const EV_RXCHAR As Integer = &H1
'    Friend Const EV_RXFLAG As Integer = &H2
'    Friend Const EV_TXEMPTY As Integer = &H4
'    Friend Const EV_CTS As Integer = &H8
'    Friend Const EV_DSR As Integer = &H10
'    Friend Const EV_RLSD As Integer = &H20
'    Friend Const EV_BREAK As Integer = &H40
'    Friend Const EV_ERR As Integer = &H80
'    Friend Const EV_RING As Integer = &H100
'    Friend Const EV_PERR As Integer = &H200
'    Friend Const EV_RX80FULL As Integer = &H400
'    Friend Const EV_EVENT1 As Integer = &H800
'    Friend Const EV_EVENT2 As Integer = &H1000
'    Friend Const ENHANCED_KEY As Integer = &H100
'    Friend Const ENABLE_PROCESSED_INPUT As Integer = &H1
'    Friend Const ENABLE_LINE_INPUT As Integer = &H2
'    Friend Const ENABLE_ECHO_INPUT As Integer = &H4
'    Friend Const ENABLE_WINDOW_INPUT As Integer = &H8
'    Friend Const ENABLE_MOUSE_INPUT As Integer = &H10
'    Friend Const ENABLE_PROCESSED_OUTPUT As Integer = &H1
'    Friend Const ENABLE_WRAP_AT_EOL_OUTPUT As Integer = &H2
'    Friend Const ERROR_SUCCESS As Integer = 0
'    Friend Const ERROR_INVALID_FUNCTION As Integer = 1
'    Friend Const ERROR_FILE_NOT_FOUND As Integer = 2
'    Friend Const ERROR_PATH_NOT_FOUND As Integer = 3
'    Friend Const ERROR_TOO_MANY_OPEN_FILES As Integer = 4
'    Friend Const ERROR_ACCESS_DENIED As Integer = 5
'    Friend Const ERROR_INVALID_HANDLE As Integer = 6
'    Friend Const ERROR_ARENA_TRASHED As Integer = 7
'    Friend Const ERROR_NOT_ENOUGH_MEMORY As Integer = 8
'    Friend Const ERROR_INVALID_BLOCK As Integer = 9
'    Friend Const ERROR_BAD_ENVIRONMENT As Integer = 10
'    Friend Const ERROR_BAD_FORMAT As Integer = 11
'    Friend Const ERROR_INVALID_ACCESS As Integer = 12
'    Friend Const ERROR_INVALID_DATA As Integer = 13
'    Friend Const ERROR_OUTOFMEMORY As Integer = 14
'    Friend Const ERROR_INVALID_DRIVE As Integer = 15
'    Friend Const ERROR_CURRENT_DIRECTORY As Integer = 16
'    Friend Const ERROR_NOT_SAME_DEVICE As Integer = 17
'    Friend Const ERROR_NO_MORE_FILES As Integer = 18
'    Friend Const ERROR_WRITE_PROTECT As Integer = 19
'    Friend Const ERROR_BAD_UNIT As Integer = 20
'    Friend Const ERROR_NOT_READY As Integer = 21
'    Friend Const ERROR_BAD_COMMAND As Integer = 22
'    Friend Const ERROR_CRC As Integer = 23
'    Friend Const ERROR_BAD_LENGTH As Integer = 24
'    Friend Const ERROR_SEEK As Integer = 25
'    Friend Const ERROR_NOT_DOS_DISK As Integer = 26
'    Friend Const ERROR_SECTOR_NOT_FOUND As Integer = 27
'    Friend Const ERROR_OUT_OF_PAPER As Integer = 28
'    Friend Const ERROR_WRITE_FAULT As Integer = 29
'    Friend Const ERROR_READ_FAULT As Integer = 30
'    Friend Const ERROR_GEN_FAILURE As Integer = 31
'    Friend Const ERROR_SHARING_VIOLATION As Integer = 32
'    Friend Const ERROR_LOCK_VIOLATION As Integer = 33
'    Friend Const ERROR_WRONG_DISK As Integer = 34
'    Friend Const ERROR_SHARING_BUFFER_EXCEEDED As Integer = 36
'    Friend Const ERROR_HANDLE_EOF As Integer = 38
'    Friend Const ERROR_HANDLE_DISK_FULL As Integer = 39
'    Friend Const ERROR_NOT_SUPPORTED As Integer = 50
'    Friend Const ERROR_REM_NOT_LIST As Integer = 51
'    Friend Const ERROR_DUP_NAME As Integer = 52
'    Friend Const ERROR_BAD_NETPATH As Integer = 53
'    Friend Const ERROR_NETWORK_BUSY As Integer = 54
'    Friend Const ERROR_DEV_NOT_EXIST As Integer = 55
'    Friend Const ERROR_TOO_MANY_CMDS As Integer = 56
'    Friend Const ERROR_ADAP_HDW_ERR As Integer = 57
'    Friend Const ERROR_BAD_NET_RESP As Integer = 58
'    Friend Const ERROR_UNEXP_NET_ERR As Integer = 59
'    Friend Const ERROR_BAD_REM_ADAP As Integer = 60
'    Friend Const ERROR_PRINTQ_FULL As Integer = 61
'    Friend Const ERROR_NO_SPOOL_SPACE As Integer = 62
'    Friend Const ERROR_PRINT_CANCELLED As Integer = 63
'    Friend Const ERROR_NETNAME_DELETED As Integer = 64
'    Friend Const ERROR_NETWORK_ACCESS_DENIED As Integer = 65
'    Friend Const ERROR_BAD_DEV_TYPE As Integer = 66
'    Friend Const ERROR_BAD_NET_NAME As Integer = 67
'    Friend Const ERROR_TOO_MANY_NAMES As Integer = 68
'    Friend Const ERROR_TOO_MANY_SESS As Integer = 69
'    Friend Const ERROR_SHARING_PAUSED As Integer = 70
'    Friend Const ERROR_REQ_NOT_ACCEP As Integer = 71
'    Friend Const ERROR_REDIR_PAUSED As Integer = 72
'    Friend Const ERROR_FILE_EXISTS As Integer = 80
'    Friend Const ERROR_CANNOT_MAKE As Integer = 82
'    Friend Const ERROR_FAIL_I24 As Integer = 83
'    Friend Const ERROR_OUT_OF_STRUCTURES As Integer = 84
'    Friend Const ERROR_ALREADY_ASSIGNED As Integer = 85
'    Friend Const ERROR_INVALID_PASSWORD As Integer = 86
'    Friend Const ERROR_INVALID_PARAMETER As Integer = 87
'    Friend Const ERROR_NET_WRITE_FAULT As Integer = 88
'    Friend Const ERROR_NO_PROC_SLOTS As Integer = 89
'    Friend Const ERROR_TOO_MANY_SEMAPHORES As Integer = 100
'    Friend Const ERROR_EXCL_SEM_ALREADY_OWNED As Integer = 101
'    Friend Const ERROR_SEM_IS_SET As Integer = 102
'    Friend Const ERROR_TOO_MANY_SEM_REQUESTS As Integer = 103
'    Friend Const ERROR_INVALID_AT_INTERRUPT_TIME As Integer = 104
'    Friend Const ERROR_SEM_OWNER_DIED As Integer = 105
'    Friend Const ERROR_SEM_USER_LIMIT As Integer = 106
'    Friend Const ERROR_DISK_CHANGE As Integer = 107
'    Friend Const ERROR_DRIVE_LOCKED As Integer = 108
'    Friend Const ERROR_BROKEN_PIPE As Integer = 109
'    Friend Const ERROR_OPEN_FAILED As Integer = 110
'    Friend Const ERROR_BUFFER_OVERFLOW As Integer = 111
'    Friend Const ERROR_DISK_FULL As Integer = 112
'    Friend Const ERROR_NO_MORE_SEARCH_HANDLES As Integer = 113
'    Friend Const ERROR_INVALID_TARGET_HANDLE As Integer = 114
'    Friend Const ERROR_INVALID_CATEGORY As Integer = 117
'    Friend Const ERROR_INVALID_VERIFY_SWITCH As Integer = 118
'    Friend Const ERROR_BAD_DRIVER_LEVEL As Integer = 119
'    Friend Const ERROR_CALL_NOT_IMPLEMENTED As Integer = 120
'    Friend Const ERROR_SEM_TIMEOUT As Integer = 121
'    Friend Const ERROR_INSUFFICIENT_BUFFER As Integer = 122
'    Friend Const ERROR_INVALID_NAME As Integer = 123
'    Friend Const ERROR_INVALID_LEVEL As Integer = 124
'    Friend Const ERROR_NO_VOLUME_LABEL As Integer = 125
'    Friend Const ERROR_MOD_NOT_FOUND As Integer = 126
'    Friend Const ERROR_PROC_NOT_FOUND As Integer = 127
'    Friend Const ERROR_WAIT_NO_CHILDREN As Integer = 128
'    Friend Const ERROR_CHILD_NOT_COMPLETE As Integer = 129
'    Friend Const ERROR_DIRECT_ACCESS_HANDLE As Integer = 130
'    Friend Const ERROR_NEGATIVE_SEEK As Integer = 131
'    Friend Const ERROR_SEEK_ON_DEVICE As Integer = 132
'    Friend Const ERROR_IS_JOIN_TARGET As Integer = 133
'    Friend Const ERROR_IS_JOINED As Integer = 134
'    Friend Const ERROR_IS_SUBSTED As Integer = 135
'    Friend Const ERROR_NOT_JOINED As Integer = 136
'    Friend Const ERROR_NOT_SUBSTED As Integer = 137
'    Friend Const ERROR_JOIN_TO_JOIN As Integer = 138
'    Friend Const ERROR_SUBST_TO_SUBST As Integer = 139
'    Friend Const ERROR_JOIN_TO_SUBST As Integer = 140
'    Friend Const ERROR_SUBST_TO_JOIN As Integer = 141
'    Friend Const ERROR_BUSY_DRIVE As Integer = 142
'    Friend Const ERROR_SAME_DRIVE As Integer = 143
'    Friend Const ERROR_DIR_NOT_ROOT As Integer = 144
'    Friend Const ERROR_DIR_NOT_EMPTY As Integer = 145
'    Friend Const ERROR_IS_SUBST_PATH As Integer = 146
'    Friend Const ERROR_IS_JOIN_PATH As Integer = 147
'    Friend Const ERROR_PATH_BUSY As Integer = 148
'    Friend Const ERROR_IS_SUBST_TARGET As Integer = 149
'    Friend Const ERROR_SYSTEM_TRACE As Integer = 150
'    Friend Const ERROR_INVALID_EVENT_COUNT As Integer = 151
'    Friend Const ERROR_TOO_MANY_MUXWAITERS As Integer = 152
'    Friend Const ERROR_INVALID_LIST_FORMAT As Integer = 153
'    Friend Const ERROR_LABEL_TOO_LONG As Integer = 154
'    Friend Const ERROR_TOO_MANY_TCBS As Integer = 155
'    Friend Const ERROR_SIGNAL_REFUSED As Integer = 156
'    Friend Const ERROR_DISCARDED As Integer = 157
'    Friend Const ERROR_NOT_LOCKED As Integer = 158
'    Friend Const ERROR_BAD_THREADID_ADDR As Integer = 159
'    Friend Const ERROR_BAD_ARGUMENTS As Integer = 160
'    Friend Const ERROR_BAD_PATHNAME As Integer = 161
'    Friend Const ERROR_SIGNAL_PENDING As Integer = 162
'    Friend Const ERROR_MAX_THRDS_REACHED As Integer = 164
'    Friend Const ERROR_LOCK_FAILED As Integer = 167
'    Friend Const ERROR_BUSY As Integer = 170
'    Friend Const ERROR_CANCEL_VIOLATION As Integer = 173
'    Friend Const ERROR_ATOMIC_LOCKS_NOT_SUPPORTED As Integer = 174
'    Friend Const ERROR_INVALID_SEGMENT_NUMBER As Integer = 180
'    Friend Const ERROR_INVALID_ORDINAL As Integer = 182
'    Friend Const ERROR_ALREADY_EXISTS As Integer = 183
'    Friend Const ERROR_INVALID_FLAG_NUMBER As Integer = 186
'    Friend Const ERROR_SEM_NOT_FOUND As Integer = 187
'    Friend Const ERROR_INVALID_STARTING_CODESEG As Integer = 188
'    Friend Const ERROR_INVALID_STACKSEG As Integer = 189
'    Friend Const ERROR_INVALID_MODULETYPE As Integer = 190
'    Friend Const ERROR_INVALID_EXE_SIGNATURE As Integer = 191
'    Friend Const ERROR_EXE_MARKED_INVALID As Integer = 192
'    Friend Const ERROR_BAD_EXE_FORMAT As Integer = 193
'    Friend Const ERROR_ITERATED_DATA_EXCEEDS_64k As Integer = 194
'    Friend Const ERROR_INVALID_MINALLOCSIZE As Integer = 195
'    Friend Const ERROR_DYNLINK_FROM_INVALID_RING As Integer = 196
'    Friend Const ERROR_IOPL_NOT_ENABLED As Integer = 197
'    Friend Const ERROR_INVALID_SEGDPL As Integer = 198
'    Friend Const ERROR_AUTODATASEG_EXCEEDS_64k As Integer = 199
'    Friend Const ERROR_RING2SEG_MUST_BE_MOVABLE As Integer = 200
'    Friend Const ERROR_RELOC_CHAIN_XEEDS_SEGLIM As Integer = 201
'    Friend Const ERROR_INFLOOP_IN_RELOC_CHAIN As Integer = 202
'    Friend Const ERROR_ENVVAR_NOT_FOUND As Integer = 203
'    Friend Const ERROR_NO_SIGNAL_SENT As Integer = 205
'    Friend Const ERROR_FILENAME_EXCED_RANGE As Integer = 206
'    Friend Const ERROR_RING2_STACK_IN_USE As Integer = 207
'    Friend Const ERROR_META_EXPANSION_TOO_LONG As Integer = 208
'    Friend Const ERROR_INVALID_SIGNAL_NUMBER As Integer = 209
'    Friend Const ERROR_THREAD_1_INACTIVE As Integer = 210
'    Friend Const ERROR_LOCKED As Integer = 212
'    Friend Const ERROR_TOO_MANY_MODULES As Integer = 214
'    Friend Const ERROR_NESTING_NOT_ALLOWED As Integer = 215
'    Friend Const ERROR_EXE_MACHINE_TYPE_MISMATCH As Integer = 216
'    Friend Const ERROR_BAD_PIPE As Integer = 230
'    Friend Const ERROR_PIPE_BUSY As Integer = 231
'    Friend Const ERROR_NO_DATA As Integer = 232
'    Friend Const ERROR_PIPE_NOT_CONNECTED As Integer = 233
'    Friend Const ERROR_MORE_DATA As Integer = 234
'    Friend Const ERROR_VC_DISCONNECTED As Integer = 240
'    Friend Const ERROR_INVALID_EA_NAME As Integer = 254
'    Friend Const ERROR_EA_LIST_INCONSISTENT As Integer = 255
'    Friend Const ERROR_NO_MORE_ITEMS As Integer = 259
'    Friend Const ERROR_CANNOT_COPY As Integer = 266
'    Friend Const ERROR_DIRECTORY As Integer = 267
'    Friend Const ERROR_EAS_DIDNT_FIT As Integer = 275
'    Friend Const ERROR_EA_FILE_CORRUPT As Integer = 276
'    Friend Const ERROR_EA_TABLE_FULL As Integer = 277
'    Friend Const ERROR_INVALID_EA_HANDLE As Integer = 278
'    Friend Const ERROR_EAS_NOT_SUPPORTED As Integer = 282
'    Friend Const ERROR_NOT_OWNER As Integer = 288
'    Friend Const ERROR_TOO_MANY_POSTS As Integer = 298
'    Friend Const ERROR_PARTIAL_COPY As Integer = 299
'    Friend Const ERROR_MR_MID_NOT_FOUND As Integer = 317
'    Friend Const ERROR_INVALID_ADDRESS As Integer = 487
'    Friend Const ERROR_ARITHMETIC_OVERFLOW As Integer = 534
'    Friend Const ERROR_PIPE_CONNECTED As Integer = 535
'    Friend Const ERROR_PIPE_LISTENING As Integer = 536
'    Friend Const ERROR_EA_ACCESS_DENIED As Integer = 994
'    Friend Const ERROR_OPERATION_ABORTED As Integer = 995
'    Friend Const ERROR_IO_INCOMPLETE As Integer = 996
'    Friend Const ERROR_IO_PENDING As Integer = 997
'    Friend Const ERROR_NOACCESS As Integer = 998
'    Friend Const ERROR_SWAPERROR As Integer = 999
'    Friend Const ERROR_STACK_OVERFLOW As Integer = 1001
'    Friend Const ERROR_INVALID_MESSAGE As Integer = 1002
'    Friend Const ERROR_CAN_NOT_COMPLETE As Integer = 1003
'    Friend Const ERROR_INVALID_FLAGS As Integer = 1004
'    Friend Const ERROR_UNRECOGNIZED_VOLUME As Integer = 1005
'    Friend Const ERROR_FILE_INVALID As Integer = 1006
'    Friend Const ERROR_FULLSCREEN_MODE As Integer = 1007
'    Friend Const ERROR_NO_TOKEN As Integer = 1008
'    Friend Const ERROR_BADDB As Integer = 1009
'    Friend Const ERROR_BADKEY As Integer = 1010
'    Friend Const ERROR_CANTOPEN As Integer = 1011
'    Friend Const ERROR_CANTREAD As Integer = 1012
'    Friend Const ERROR_CANTWRITE As Integer = 1013
'    Friend Const ERROR_REGISTRY_RECOVERED As Integer = 1014
'    Friend Const ERROR_REGISTRY_CORRUPT As Integer = 1015
'    Friend Const ERROR_REGISTRY_IO_FAILED As Integer = 1016
'    Friend Const ERROR_NOT_REGISTRY_FILE As Integer = 1017
'    Friend Const ERROR_KEY_DELETED As Integer = 1018
'    Friend Const ERROR_NO_LOG_SPACE As Integer = 1019
'    Friend Const ERROR_KEY_HAS_CHILDREN As Integer = 1020
'    Friend Const ERROR_CHILD_MUST_BE_VOLATILE As Integer = 1021
'    Friend Const ERROR_NOTIFY_ENUM_DIR As Integer = 1022
'    Friend Const ERROR_DEPENDENT_SERVICES_RUNNING As Integer = 1051
'    Friend Const ERROR_INVALID_SERVICE_CONTROL As Integer = 1052
'    Friend Const ERROR_SERVICE_REQUEST_TIMEOUT As Integer = 1053
'    Friend Const ERROR_SERVICE_NO_THREAD As Integer = 1054
'    Friend Const ERROR_SERVICE_DATABASE_LOCKED As Integer = 1055
'    Friend Const ERROR_SERVICE_ALREADY_RUNNING As Integer = 1056
'    Friend Const ERROR_INVALID_SERVICE_ACCOUNT As Integer = 1057
'    Friend Const ERROR_SERVICE_DISABLED As Integer = 1058
'    Friend Const ERROR_CIRCULAR_DEPENDENCY As Integer = 1059
'    Friend Const ERROR_SERVICE_DOES_NOT_EXIST As Integer = 1060
'    Friend Const ERROR_SERVICE_CANNOT_ACCEPT_CTRL As Integer = 1061
'    Friend Const ERROR_SERVICE_NOT_ACTIVE As Integer = 1062
'    Friend Const ERROR_FAILED_SERVICE_CONTROLLER_CONNECT As Integer = 1063
'    Friend Const ERROR_EXCEPTION_IN_SERVICE As Integer = 1064
'    Friend Const ERROR_DATABASE_DOES_NOT_EXIST As Integer = 1065
'    Friend Const ERROR_SERVICE_SPECIFIC_ERROR As Integer = 1066
'    Friend Const ERROR_PROCESS_ABORTED As Integer = 1067
'    Friend Const ERROR_SERVICE_DEPENDENCY_FAIL As Integer = 1068
'    Friend Const ERROR_SERVICE_LOGON_FAILED As Integer = 1069
'    Friend Const ERROR_SERVICE_START_HANG As Integer = 1070
'    Friend Const ERROR_INVALID_SERVICE_LOCK As Integer = 1071
'    Friend Const ERROR_SERVICE_MARKED_FOR_DELETE As Integer = 1072
'    Friend Const ERROR_SERVICE_EXISTS As Integer = 1073
'    Friend Const ERROR_ALREADY_RUNNING_LKG As Integer = 1074
'    Friend Const ERROR_SERVICE_DEPENDENCY_DELETED As Integer = 1075
'    Friend Const ERROR_BOOT_ALREADY_ACCEPTED As Integer = 1076
'    Friend Const ERROR_SERVICE_NEVER_STARTED As Integer = 1077
'    Friend Const ERROR_DUPLICATE_SERVICE_NAME As Integer = 1078
'    Friend Const ERROR_DIFFERENT_SERVICE_ACCOUNT As Integer = 1079
'    Friend Const ERROR_END_OF_MEDIA As Integer = 1100
'    Friend Const ERROR_FILEMARK_DETECTED As Integer = 1101
'    Friend Const ERROR_BEGINNING_OF_MEDIA As Integer = 1102
'    Friend Const ERROR_SETMARK_DETECTED As Integer = 1103
'    Friend Const ERROR_NO_DATA_DETECTED As Integer = 1104
'    Friend Const ERROR_PARTITION_FAILURE As Integer = 1105
'    Friend Const ERROR_INVALID_BLOCK_LENGTH As Integer = 1106
'    Friend Const ERROR_DEVICE_NOT_PARTITIONED As Integer = 1107
'    Friend Const ERROR_UNABLE_TO_LOCK_MEDIA As Integer = 1108
'    Friend Const ERROR_UNABLE_TO_UNLOAD_MEDIA As Integer = 1109
'    Friend Const ERROR_MEDIA_CHANGED As Integer = 1110
'    Friend Const ERROR_BUS_RESET As Integer = 1111
'    Friend Const ERROR_NO_MEDIA_IN_DRIVE As Integer = 1112
'    Friend Const ERROR_NO_UNICODE_TRANSLATION As Integer = 1113
'    Friend Const ERROR_DLL_INIT_FAILED As Integer = 1114
'    Friend Const ERROR_SHUTDOWN_IN_PROGRESS As Integer = 1115
'    Friend Const ERROR_NO_SHUTDOWN_IN_PROGRESS As Integer = 1116
'    Friend Const ERROR_IO_DEVICE As Integer = 1117
'    Friend Const ERROR_SERIAL_NO_DEVICE As Integer = 1118
'    Friend Const ERROR_IRQ_BUSY As Integer = 1119
'    Friend Const ERROR_MORE_WRITES As Integer = 1120
'    Friend Const ERROR_COUNTER_TIMEOUT As Integer = 1121
'    Friend Const ERROR_FLOPPY_ID_MARK_NOT_FOUND As Integer = 1122
'    Friend Const ERROR_FLOPPY_WRONG_CYLINDER As Integer = 1123
'    Friend Const ERROR_FLOPPY_UNKNOWN_ERROR As Integer = 1124
'    Friend Const ERROR_FLOPPY_BAD_REGISTERS As Integer = 1125
'    Friend Const ERROR_DISK_RECALIBRATE_FAILED As Integer = 1126
'    Friend Const ERROR_DISK_OPERATION_FAILED As Integer = 1127
'    Friend Const ERROR_DISK_RESET_FAILED As Integer = 1128
'    Friend Const ERROR_EOM_OVERFLOW As Integer = 1129
'    Friend Const ERROR_NOT_ENOUGH_SERVER_MEMORY As Integer = 1130
'    Friend Const ERROR_POSSIBLE_DEADLOCK As Integer = 1131
'    Friend Const ERROR_MAPPED_ALIGNMENT As Integer = 1132
'    Friend Const ERROR_SET_POWER_STATE_VETOED As Integer = 1140
'    Friend Const ERROR_SET_POWER_STATE_FAILED As Integer = 1141
'    Friend Const ERROR_TOO_MANY_LINKS As Integer = 1142
'    Friend Const ERROR_OLD_WIN_VERSION As Integer = 1150
'    Friend Const ERROR_APP_WRONG_OS As Integer = 1151
'    Friend Const ERROR_SINGLE_INSTANCE_APP As Integer = 1152
'    Friend Const ERROR_RMODE_APP As Integer = 1153
'    Friend Const ERROR_INVALID_DLL As Integer = 1154
'    Friend Const ERROR_NO_ASSOCIATION As Integer = 1155
'    Friend Const ERROR_DDE_FAIL As Integer = 1156
'    Friend Const ERROR_DLL_NOT_FOUND As Integer = 1157
'    Friend Const ERROR_BAD_USERNAME As Integer = 2202
'    Friend Const ERROR_NOT_CONNECTED As Integer = 2250
'    Friend Const ERROR_OPEN_FILES As Integer = 2401
'    Friend Const ERROR_ACTIVE_CONNECTIONS As Integer = 2402
'    Friend Const ERROR_DEVICE_IN_USE As Integer = 2404
'    Friend Const ERROR_BAD_DEVICE As Integer = 1200
'    Friend Const ERROR_CONNECTION_UNAVAIL As Integer = 1201
'    Friend Const ERROR_DEVICE_ALREADY_REMEMBERED As Integer = 1202
'    Friend Const ERROR_NO_NET_OR_BAD_PATH As Integer = 1203
'    Friend Const ERROR_BAD_PROVIDER As Integer = 1204
'    Friend Const ERROR_CANNOT_OPEN_PROFILE As Integer = 1205
'    Friend Const ERROR_BAD_PROFILE As Integer = 1206
'    Friend Const ERROR_NOT_CONTAINER As Integer = 1207
'    Friend Const ERROR_EXTENDED_ERROR As Integer = 1208
'    Friend Const ERROR_INVALID_GROUPNAME As Integer = 1209
'    Friend Const ERROR_INVALID_COMPUTERNAME As Integer = 1210
'    Friend Const ERROR_INVALID_EVENTNAME As Integer = 1211
'    Friend Const ERROR_INVALID_DOMAINNAME As Integer = 1212
'    Friend Const ERROR_INVALID_SERVICENAME As Integer = 1213
'    Friend Const ERROR_INVALID_NETNAME As Integer = 1214
'    Friend Const ERROR_INVALID_SHARENAME As Integer = 1215
'    Friend Const ERROR_INVALID_PASSWORDNAME As Integer = 1216
'    Friend Const ERROR_INVALID_MESSAGENAME As Integer = 1217
'    Friend Const ERROR_INVALID_MESSAGEDEST As Integer = 1218
'    Friend Const ERROR_SESSION_CREDENTIAL_CONFLICT As Integer = 1219
'    Friend Const ERROR_REMOTE_SESSION_LIMIT_EXCEEDED As Integer = 1220
'    Friend Const ERROR_DUP_DOMAINNAME As Integer = 1221
'    Friend Const ERROR_NO_NETWORK As Integer = 1222
'    Friend Const ERROR_CANCELLED As Integer = 1223
'    Friend Const ERROR_USER_MAPPED_FILE As Integer = 1224
'    Friend Const ERROR_CONNECTION_REFUSED As Integer = 1225
'    Friend Const ERROR_GRACEFUL_DISCONNECT As Integer = 1226
'    Friend Const ERROR_ADDRESS_ALREADY_ASSOCIATED As Integer = 1227
'    Friend Const ERROR_ADDRESS_NOT_ASSOCIATED As Integer = 1228
'    Friend Const ERROR_CONNECTION_INVALID As Integer = 1229
'    Friend Const ERROR_CONNECTION_ACTIVE As Integer = 1230
'    Friend Const ERROR_NETWORK_UNREACHABLE As Integer = 1231
'    Friend Const ERROR_HOST_UNREACHABLE As Integer = 1232
'    Friend Const ERROR_PROTOCOL_UNREACHABLE As Integer = 1233
'    Friend Const ERROR_PORT_UNREACHABLE As Integer = 1234
'    Friend Const ERROR_REQUEST_ABORTED As Integer = 1235
'    Friend Const ERROR_CONNECTION_ABORTED As Integer = 1236
'    Friend Const ERROR_RETRY As Integer = 1237
'    Friend Const ERROR_CONNECTION_COUNT_LIMIT As Integer = 1238
'    Friend Const ERROR_LOGIN_TIME_RESTRICTION As Integer = 1239
'    Friend Const ERROR_LOGIN_WKSTA_RESTRICTION As Integer = 1240
'    Friend Const ERROR_INCORRECT_ADDRESS As Integer = 1241
'    Friend Const ERROR_ALREADY_REGISTERED As Integer = 1242
'    Friend Const ERROR_SERVICE_NOT_FOUND As Integer = 1243
'    Friend Const ERROR_NOT_AUTHENTICATED As Integer = 1244
'    Friend Const ERROR_NOT_LOGGED_ON As Integer = 1245
'    Friend Const ERROR_CONTINUE As Integer = 1246
'    Friend Const ERROR_ALREADY_INITIALIZED As Integer = 1247
'    Friend Const ERROR_NO_MORE_DEVICES As Integer = 1248
'    Friend Const ERROR_NOT_ALL_ASSIGNED As Integer = 1300
'    Friend Const ERROR_SOME_NOT_MAPPED As Integer = 1301
'    Friend Const ERROR_NO_QUOTAS_FOR_ACCOUNT As Integer = 1302
'    Friend Const ERROR_LOCAL_USER_SESSION_KEY As Integer = 1303
'    Friend Const ERROR_NULL_LM_PASSWORD As Integer = 1304
'    Friend Const ERROR_UNKNOWN_REVISION As Integer = 1305
'    Friend Const ERROR_REVISION_MISMATCH As Integer = 1306
'    Friend Const ERROR_INVALID_OWNER As Integer = 1307
'    Friend Const ERROR_INVALID_PRIMARY_GROUP As Integer = 1308
'    Friend Const ERROR_NO_IMPERSONATION_TOKEN As Integer = 1309
'    Friend Const ERROR_CANT_DISABLE_MANDATORY As Integer = 1310
'    Friend Const ERROR_NO_LOGON_SERVERS As Integer = 1311
'    Friend Const ERROR_NO_SUCH_LOGON_SESSION As Integer = 1312
'    Friend Const ERROR_NO_SUCH_PRIVILEGE As Integer = 1313
'    Friend Const ERROR_PRIVILEGE_NOT_HELD As Integer = 1314
'    Friend Const ERROR_INVALID_ACCOUNT_NAME As Integer = 1315
'    Friend Const ERROR_USER_EXISTS As Integer = 1316
'    Friend Const ERROR_NO_SUCH_USER As Integer = 1317
'    Friend Const ERROR_GROUP_EXISTS As Integer = 1318
'    Friend Const ERROR_NO_SUCH_GROUP As Integer = 1319
'    Friend Const ERROR_MEMBER_IN_GROUP As Integer = 1320
'    Friend Const ERROR_MEMBER_NOT_IN_GROUP As Integer = 1321
'    Friend Const ERROR_LAST_ADMIN As Integer = 1322
'    Friend Const ERROR_WRONG_PASSWORD As Integer = 1323
'    Friend Const ERROR_ILL_FORMED_PASSWORD As Integer = 1324
'    Friend Const ERROR_PASSWORD_RESTRICTION As Integer = 1325
'    Friend Const ERROR_LOGON_FAILURE As Integer = 1326
'    Friend Const ERROR_ACCOUNT_RESTRICTION As Integer = 1327
'    Friend Const ERROR_INVALID_LOGON_HOURS As Integer = 1328
'    Friend Const ERROR_INVALID_WORKSTATION As Integer = 1329
'    Friend Const ERROR_PASSWORD_EXPIRED As Integer = 1330
'    Friend Const ERROR_ACCOUNT_DISABLED As Integer = 1331
'    Friend Const ERROR_NONE_MAPPED As Integer = 1332
'    Friend Const ERROR_TOO_MANY_LUIDS_REQUESTED As Integer = 1333
'    Friend Const ERROR_LUIDS_EXHAUSTED As Integer = 1334
'    Friend Const ERROR_INVALID_SUB_AUTHORITY As Integer = 1335
'    Friend Const ERROR_INVALID_ACL As Integer = 1336
'    Friend Const ERROR_INVALID_SID As Integer = 1337
'    Friend Const ERROR_INVALID_SECURITY_DESCR As Integer = 1338
'    Friend Const ERROR_BAD_INHERITANCE_ACL As Integer = 1340
'    Friend Const ERROR_SERVER_DISABLED As Integer = 1341
'    Friend Const ERROR_SERVER_NOT_DISABLED As Integer = 1342
'    Friend Const ERROR_INVALID_ID_AUTHORITY As Integer = 1343
'    Friend Const ERROR_ALLOTTED_SPACE_EXCEEDED As Integer = 1344
'    Friend Const ERROR_INVALID_GROUP_ATTRIBUTES As Integer = 1345
'    Friend Const ERROR_BAD_IMPERSONATION_LEVEL As Integer = 1346
'    Friend Const ERROR_CANT_OPEN_ANONYMOUS As Integer = 1347
'    Friend Const ERROR_BAD_VALIDATION_CLASS As Integer = 1348
'    Friend Const ERROR_BAD_TOKEN_TYPE As Integer = 1349
'    Friend Const ERROR_NO_SECURITY_ON_OBJECT As Integer = 1350
'    Friend Const ERROR_CANT_ACCESS_DOMAIN_INFO As Integer = 1351
'    Friend Const ERROR_INVALID_SERVER_STATE As Integer = 1352
'    Friend Const ERROR_INVALID_DOMAIN_STATE As Integer = 1353
'    Friend Const ERROR_INVALID_DOMAIN_ROLE As Integer = 1354
'    Friend Const ERROR_NO_SUCH_DOMAIN As Integer = 1355
'    Friend Const ERROR_DOMAIN_EXISTS As Integer = 1356
'    Friend Const ERROR_DOMAIN_LIMIT_EXCEEDED As Integer = 1357
'    Friend Const ERROR_INTERNAL_DB_CORRUPTION As Integer = 1358
'    Friend Const ERROR_INTERNAL_ERROR As Integer = 1359
'    Friend Const ERROR_GENERIC_NOT_MAPPED As Integer = 1360
'    Friend Const ERROR_BAD_DESCRIPTOR_FORMAT As Integer = 1361
'    Friend Const ERROR_NOT_LOGON_PROCESS As Integer = 1362
'    Friend Const ERROR_LOGON_SESSION_EXISTS As Integer = 1363
'    Friend Const ERROR_NO_SUCH_PACKAGE As Integer = 1364
'    Friend Const ERROR_BAD_LOGON_SESSION_STATE As Integer = 1365
'    Friend Const ERROR_LOGON_SESSION_COLLISION As Integer = 1366
'    Friend Const ERROR_INVALID_LOGON_TYPE As Integer = 1367
'    Friend Const ERROR_CANNOT_IMPERSONATE As Integer = 1368
'    Friend Const ERROR_RXACT_INVALID_STATE As Integer = 1369
'    Friend Const ERROR_RXACT_COMMIT_FAILURE As Integer = 1370
'    Friend Const ERROR_SPECIAL_ACCOUNT As Integer = 1371
'    Friend Const ERROR_SPECIAL_GROUP As Integer = 1372
'    Friend Const ERROR_SPECIAL_USER As Integer = 1373
'    Friend Const ERROR_MEMBERS_PRIMARY_GROUP As Integer = 1374
'    Friend Const ERROR_TOKEN_ALREADY_IN_USE As Integer = 1375
'    Friend Const ERROR_NO_SUCH_ALIAS As Integer = 1376
'    Friend Const ERROR_MEMBER_NOT_IN_ALIAS As Integer = 1377
'    Friend Const ERROR_MEMBER_IN_ALIAS As Integer = 1378
'    Friend Const ERROR_ALIAS_EXISTS As Integer = 1379
'    Friend Const ERROR_LOGON_NOT_GRANTED As Integer = 1380
'    Friend Const ERROR_TOO_MANY_SECRETS As Integer = 1381
'    Friend Const ERROR_SECRET_TOO_LONG As Integer = 1382
'    Friend Const ERROR_INTERNAL_DB_ERROR As Integer = 1383
'    Friend Const ERROR_TOO_MANY_CONTEXT_IDS As Integer = 1384
'    Friend Const ERROR_LOGON_TYPE_NOT_GRANTED As Integer = 1385
'    Friend Const ERROR_NT_CROSS_ENCRYPTION_REQUIRED As Integer = 1386
'    Friend Const ERROR_NO_SUCH_MEMBER As Integer = 1387
'    Friend Const ERROR_INVALID_MEMBER As Integer = 1388
'    Friend Const ERROR_TOO_MANY_SIDS As Integer = 1389
'    Friend Const ERROR_LM_CROSS_ENCRYPTION_REQUIRED As Integer = 1390
'    Friend Const ERROR_NO_INHERITANCE As Integer = 1391
'    Friend Const ERROR_FILE_CORRUPT As Integer = 1392
'    Friend Const ERROR_DISK_CORRUPT As Integer = 1393
'    Friend Const ERROR_NO_USER_SESSION_KEY As Integer = 1394
'    Friend Const ERROR_LICENSE_QUOTA_EXCEEDED As Integer = 1395
'    Friend Const ERROR_WRONG_TARGET_NAME As Integer = 1396
'    Friend Const ERROR_MUTUAL_AUTH_FAILED As Integer = 1397
'    Friend Const ERROR_TIME_SKEW As Integer = 1398
'    Friend Const ERROR_CURRENT_DOMAIN_NOT_ALLOWED As Integer = 1399
'    Friend Const ERROR_INVALID_WINDOW_HANDLE As Integer = 1400
'    Friend Const ERROR_INVALID_MENU_HANDLE As Integer = 1401
'    Friend Const ERROR_INVALID_CURSOR_HANDLE As Integer = 1402
'    Friend Const ERROR_INVALID_ACCEL_HANDLE As Integer = 1403
'    Friend Const ERROR_INVALID_HOOK_HANDLE As Integer = 1404
'    Friend Const ERROR_INVALID_DWP_HANDLE As Integer = 1405
'    Friend Const ERROR_TLW_WITH_WSCHILD As Integer = 1406
'    Friend Const ERROR_CANNOT_FIND_WND_CLASS As Integer = 1407
'    Friend Const ERROR_WINDOW_OF_OTHER_THREAD As Integer = 1408
'    Friend Const ERROR_HOTKEY_ALREADY_REGISTERED As Integer = 1409
'    Friend Const ERROR_CLASS_ALREADY_EXISTS As Integer = 1410
'    Friend Const ERROR_CLASS_DOES_NOT_EXIST As Integer = 1411
'    Friend Const ERROR_CLASS_HAS_WINDOWS As Integer = 1412
'    Friend Const ERROR_INVALID_INDEX As Integer = 1413
'    Friend Const ERROR_INVALID_ICON_HANDLE As Integer = 1414
'    Friend Const ERROR_PRIVATE_DIALOG_INDEX As Integer = 1415
'    Friend Const ERROR_LISTBOX_ID_NOT_FOUND As Integer = 1416
'    Friend Const ERROR_NO_WILDCARD_CHARACTERS As Integer = 1417
'    Friend Const ERROR_CLIPBOARD_NOT_OPEN As Integer = 1418
'    Friend Const ERROR_HOTKEY_NOT_REGISTERED As Integer = 1419
'    Friend Const ERROR_WINDOW_NOT_DIALOG As Integer = 1420
'    Friend Const ERROR_CONTROL_ID_NOT_FOUND As Integer = 1421
'    Friend Const ERROR_INVALID_COMBOBOX_MESSAGE As Integer = 1422
'    Friend Const ERROR_WINDOW_NOT_COMBOBOX As Integer = 1423
'    Friend Const ERROR_INVALID_EDIT_HEIGHT As Integer = 1424
'    Friend Const ERROR_DC_NOT_FOUND As Integer = 1425
'    Friend Const ERROR_INVALID_HOOK_FILTER As Integer = 1426
'    Friend Const ERROR_INVALID_FILTER_PROC As Integer = 1427
'    Friend Const ERROR_HOOK_NEEDS_HMOD As Integer = 1428
'    Friend Const ERROR_GLOBAL_ONLY_HOOK As Integer = 1429
'    Friend Const ERROR_JOURNAL_HOOK_SET As Integer = 1430
'    Friend Const ERROR_HOOK_NOT_INSTALLED As Integer = 1431
'    Friend Const ERROR_INVALID_LB_MESSAGE As Integer = 1432
'    Friend Const ERROR_SETCOUNT_ON_BAD_LB As Integer = 1433
'    Friend Const ERROR_LB_WITHOUT_TABSTOPS As Integer = 1434
'    Friend Const ERROR_DESTROY_OBJECT_OF_OTHER_THREAD As Integer = 1435
'    Friend Const ERROR_CHILD_WINDOW_MENU As Integer = 1436
'    Friend Const ERROR_NO_SYSTEM_MENU As Integer = 1437
'    Friend Const ERROR_INVALID_MSGBOX_STYLE As Integer = 1438
'    Friend Const ERROR_INVALID_SPI_VALUE As Integer = 1439
'    Friend Const ERROR_SCREEN_ALREADY_LOCKED As Integer = 1440
'    Friend Const ERROR_HWNDS_HAVE_DIFF_PARENT As Integer = 1441
'    Friend Const ERROR_NOT_CHILD_WINDOW As Integer = 1442
'    Friend Const ERROR_INVALID_GW_COMMAND As Integer = 1443
'    Friend Const ERROR_INVALID_THREAD_ID As Integer = 1444
'    Friend Const ERROR_NON_MDICHILD_WINDOW As Integer = 1445
'    Friend Const ERROR_POPUP_ALREADY_ACTIVE As Integer = 1446
'    Friend Const ERROR_NO_SCROLLBARS As Integer = 1447
'    Friend Const ERROR_INVALID_SCROLLBAR_RANGE As Integer = 1448
'    Friend Const ERROR_INVALID_SHOWWIN_COMMAND As Integer = 1449
'    Friend Const ERROR_NO_SYSTEM_RESOURCES As Integer = 1450
'    Friend Const ERROR_NONPAGED_SYSTEM_RESOURCES As Integer = 1451
'    Friend Const ERROR_PAGED_SYSTEM_RESOURCES As Integer = 1452
'    Friend Const ERROR_WORKING_SET_QUOTA As Integer = 1453
'    Friend Const ERROR_PAGEFILE_QUOTA As Integer = 1454
'    Friend Const ERROR_COMMITMENT_LIMIT As Integer = 1455
'    Friend Const ERROR_MENU_ITEM_NOT_FOUND As Integer = 1456
'    Friend Const ERROR_INVALID_KEYBOARD_HANDLE As Integer = 1457
'    Friend Const ERROR_HOOK_TYPE_NOT_ALLOWED As Integer = 1458
'    Friend Const ERROR_REQUIRES_INTERACTIVE_WINDOWSTATION As Integer = 1459
'    Friend Const ERROR_TIMEOUT As Integer = 1460
'    Friend Const ERROR_EVENTLOG_FILE_CORRUPT As Integer = 1500
'    Friend Const ERROR_EVENTLOG_CANT_START As Integer = 1501
'    Friend Const ERROR_LOG_FILE_FULL As Integer = 1502
'    Friend Const ERROR_EVENTLOG_FILE_CHANGED As Integer = 1503
'    Friend Const EPT_S_INVALID_ENTRY As Integer = 1751
'    Friend Const EPT_S_CANT_PERFORM_OP As Integer = 1752
'    Friend Const EPT_S_NOT_REGISTERED As Integer = 1753
'    Friend Const ERROR_INVALID_USER_BUFFER As Integer = 1784
'    Friend Const ERROR_UNRECOGNIZED_MEDIA As Integer = 1785
'    Friend Const ERROR_NO_TRUST_LSA_SECRET As Integer = 1786
'    Friend Const ERROR_NO_TRUST_SAM_ACCOUNT As Integer = 1787
'    Friend Const ERROR_TRUSTED_DOMAIN_FAILURE As Integer = 1788
'    Friend Const ERROR_TRUSTED_RELATIONSHIP_FAILURE As Integer = 1789
'    Friend Const ERROR_TRUST_FAILURE As Integer = 1790
'    Friend Const ERROR_NETLOGON_NOT_STARTED As Integer = 1792
'    Friend Const ERROR_ACCOUNT_EXPIRED As Integer = 1793
'    Friend Const ERROR_REDIRECTOR_HAS_OPEN_HANDLES As Integer = 1794
'    Friend Const ERROR_PRINTER_DRIVER_ALREADY_INSTALLED As Integer = 1795
'    Friend Const ERROR_UNKNOWN_PORT As Integer = 1796
'    Friend Const ERROR_UNKNOWN_PRINTER_DRIVER As Integer = 1797
'    Friend Const ERROR_UNKNOWN_PRINTPROCESSOR As Integer = 1798
'    Friend Const ERROR_INVALID_SEPARATOR_FILE As Integer = 1799
'    Friend Const ERROR_INVALID_PRIORITY As Integer = 1800
'    Friend Const ERROR_INVALID_PRINTER_NAME As Integer = 1801
'    Friend Const ERROR_PRINTER_ALREADY_EXISTS As Integer = 1802
'    Friend Const ERROR_INVALID_PRINTER_COMMAND As Integer = 1803
'    Friend Const ERROR_INVALID_DATATYPE As Integer = 1804
'    Friend Const ERROR_INVALID_ENVIRONMENT As Integer = 1805
'    Friend Const ERROR_NOLOGON_INTERDOMAIN_TRUST_ACCOUNT As Integer = 1807
'    Friend Const ERROR_NOLOGON_WORKSTATION_TRUST_ACCOUNT As Integer = 1808
'    Friend Const ERROR_NOLOGON_SERVER_TRUST_ACCOUNT As Integer = 1809
'    Friend Const ERROR_DOMAIN_TRUST_INCONSISTENT As Integer = 1810
'    Friend Const ERROR_SERVER_HAS_OPEN_HANDLES As Integer = 1811
'    Friend Const ERROR_RESOURCE_DATA_NOT_FOUND As Integer = 1812
'    Friend Const ERROR_RESOURCE_TYPE_NOT_FOUND As Integer = 1813
'    Friend Const ERROR_RESOURCE_NAME_NOT_FOUND As Integer = 1814
'    Friend Const ERROR_RESOURCE_LANG_NOT_FOUND As Integer = 1815
'    Friend Const ERROR_NOT_ENOUGH_QUOTA As Integer = 1816
'    Friend Const EPT_S_CANT_CREATE As Integer = 1899
'    Friend Const ERROR_INVALID_TIME As Integer = 1901
'    Friend Const ERROR_INVALID_FORM_NAME As Integer = 1902
'    Friend Const ERROR_INVALID_FORM_SIZE As Integer = 1903
'    Friend Const ERROR_ALREADY_WAITING As Integer = 1904
'    Friend Const ERROR_PRINTER_DELETED As Integer = 1905
'    Friend Const ERROR_INVALID_PRINTER_STATE As Integer = 1906
'    Friend Const ERROR_PASSWORD_MUST_CHANGE As Integer = 1907
'    Friend Const ERROR_DOMAIN_CONTROLLER_NOT_FOUND As Integer = 1908
'    Friend Const ERROR_ACCOUNT_LOCKED_OUT As Integer = 1909
'    Friend Const ERROR_NO_BROWSER_SERVERS_FOUND As Integer = 6118
'    Friend Const ERROR_INVALID_PIXEL_FORMAT As Integer = 2000
'    Friend Const ERROR_BAD_DRIVER As Integer = 2001
'    Friend Const ERROR_INVALID_WINDOW_STYLE As Integer = 2002
'    Friend Const ERROR_METAFILE_NOT_SUPPORTED As Integer = 2003
'    Friend Const ERROR_TRANSFORM_NOT_SUPPORTED As Integer = 2004
'    Friend Const ERROR_CLIPPING_NOT_SUPPORTED As Integer = 2005
'    Friend Const ERROR_UNKNOWN_PRINT_MONITOR As Integer = 3000
'    Friend Const ERROR_PRINTER_DRIVER_IN_USE As Integer = 3001
'    Friend Const ERROR_SPOOL_FILE_NOT_FOUND As Integer = 3002
'    Friend Const ERROR_SPL_NO_STARTDOC As Integer = 3003
'    Friend Const ERROR_SPL_NO_ADDJOB As Integer = 3004
'    Friend Const ERROR_PRINT_PROCESSOR_ALREADY_INSTALLED As Integer = 3005
'    Friend Const ERROR_PRINT_MONITOR_ALREADY_INSTALLED As Integer = 3006
'    Friend Const ERROR_INVALID_PRINT_MONITOR As Integer = 3007
'    Friend Const ERROR_PRINT_MONITOR_IN_USE As Integer = 3008
'    Friend Const ERROR_PRINTER_HAS_JOBS_QUEUED As Integer = 3009
'    Friend Const ERROR_SUCCESS_REBOOT_REQUIRED As Integer = 3010
'    Friend Const ERROR_SUCCESS_RESTART_REQUIRED As Integer = 3011
'    Friend Const ERROR_WINS_INTERNAL As Integer = 4000
'    Friend Const ERROR_CAN_NOT_DEL_LOCAL_WINS As Integer = 4001
'    Friend Const ERROR_STATIC_INIT As Integer = 4002
'    Friend Const ERROR_INC_BACKUP As Integer = 4003
'    Friend Const ERROR_FULL_BACKUP As Integer = 4004
'    Friend Const ERROR_REC_NON_EXISTENT As Integer = 4005
'    Friend Const ERROR_RPL_NOT_ALLOWED As Integer = 4006
'    Friend Const E_UNEXPECTED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        ERROR_RPL_NOT_ALLOWED = 4006,
'    '        E_UNEXPECTED = unchecked((int)0x8000FFFF),
'    '------------------------^--- GenCode(token): unexpected token type
'    Friend Const E_NOTIMPL As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        E_UNEXPECTED = unchecked((int)0x8000FFFF),
'    '        E_NOTIMPL = unchecked((int)0x80004001),
'    '---------------------^--- GenCode(token): unexpected token type
'    Friend Const E_OUTOFMEMORY As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        E_NOTIMPL = unchecked((int)0x80004001),
'    '        E_OUTOFMEMORY = unchecked((int)0x8007000E),
'    '-------------------------^--- GenCode(token): unexpected token type
'    Friend Const E_INVALIDARG As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        E_OUTOFMEMORY = unchecked((int)0x8007000E),
'    '        E_INVALIDARG = unchecked((int)0x80070057),
'    '------------------------^--- GenCode(token): unexpected token type
'    Friend Const E_NOINTERFACE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        E_INVALIDARG = unchecked((int)0x80070057),
'    '        E_NOINTERFACE = unchecked((int)0x80004002),
'    '-------------------------^--- GenCode(token): unexpected token type
'    Friend Const E_POINTER As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        E_NOINTERFACE = unchecked((int)0x80004002),
'    '        E_POINTER = unchecked((int)0x80004003),
'    '---------------------^--- GenCode(token): unexpected token type
'    Friend Const E_HANDLE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        E_POINTER = unchecked((int)0x80004003),
'    '        E_HANDLE = unchecked((int)0x80070006),
'    '--------------------^--- GenCode(token): unexpected token type
'    Friend Const E_ABORT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        E_HANDLE = unchecked((int)0x80070006),
'    '        E_ABORT = unchecked((int)0x80004004),
'    '-------------------^--- GenCode(token): unexpected token type
'    Friend Const E_FAIL As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        E_ABORT = unchecked((int)0x80004004),
'    '        E_FAIL = unchecked((int)0x80004005),
'    '------------------^--- GenCode(token): unexpected token type
'    Friend Const E_ACCESSDENIED As Integer = 
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
'    Friend Const E_PENDING As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        // int E_ACCESSDENIED = unchecked((int)0x80000009);
'    '        public const int E_PENDING = unchecked((int)0x8000000A),
'    '--------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const ENUM_E_FIRST As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        public const int E_PENDING = unchecked((int)0x8000000A),
'    '        ENUM_E_FIRST = unchecked((int)0x800401B0),
'    '------------------------^--- GenCode(token): unexpected token type
'    Friend Const ENUM_E_LAST As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        ENUM_E_FIRST = unchecked((int)0x800401B0),
'    '        ENUM_E_LAST = unchecked((int)0x800401BF),
'    '-----------------------^--- GenCode(token): unexpected token type
'    Friend Const ENUM_S_FIRST As Integer = &H401B0
'    Friend Const ENUM_S_LAST As Integer = &H401BF
'    Friend Const [ERROR] As Integer = 0
'    Friend Const ETO_OPAQUE As Integer = &H2
'    Friend Const ETO_CLIPPED As Integer = &H4
'    Friend Const ETO_GLYPH_INDEX As Integer = &H10
'    Friend Const ETO_RTLREADING As Integer = &H80
'    Friend Const ETO_IGNORELANGUAGE As Integer = &H1000
'    Friend Const ENDDOC As Integer = 11
'    Friend Const ENABLEDUPLEX As Integer = 28
'    Friend Const ENUMPAPERBINS As Integer = 31
'    Friend Const EPSPRINTING As Integer = 33
'    Friend Const ENUMPAPERMETRICS As Integer = 34
'    Friend Const EXTTEXTOUT As Integer = 512
'    Friend Const ENABLERELATIVEWIDTHS As Integer = 768
'    Friend Const ENABLEPAIRKERNING As Integer = 769
'    Friend Const END_PATH As Integer = 4098
'    Friend Const EXT_DEVICE_CAPS As Integer = 4099
'    Friend Const ENCAPSULATED_POSTSCRIPT As Integer = 4116
'    Friend Const EASTEUROPE_CHARSET As Integer = 238
'    Friend Const ELF_VENDOR_SIZE As Integer = 4
'    Friend Const ELF_VERSION As Integer = 0
'    Friend Const ELF_CULTURE_LATIN As Integer = 0
'    Friend Const ENHMETA_SIGNATURE As Integer = &H464D4520
'    Friend Const ENHMETA_STOCK_OBJECT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        ENHMETA_SIGNATURE = 0x464D4520,
'    '        ENHMETA_STOCK_OBJECT = unchecked((int)0x80000000),
'    '--------------------------------^--- GenCode(token): unexpected token type
'    Friend Const EMR_HEADER As Integer = 1
'    Friend Const EMR_POLYBEZIER As Integer = 2
'    Friend Const EMR_POLYGON As Integer = 3
'    Friend Const EMR_POLYLINE As Integer = 4
'    Friend Const EMR_POLYBEZIERTO As Integer = 5
'    Friend Const EMR_POLYLINETO As Integer = 6
'    Friend Const EMR_POLYPOLYLINE As Integer = 7
'    Friend Const EMR_POLYPOLYGON As Integer = 8
'    Friend Const EMR_SETWINDOWEXTEX As Integer = 9
'    Friend Const EMR_SETWINDOWORGEX As Integer = 10
'    Friend Const EMR_SETVIEWPORTEXTEX As Integer = 11
'    Friend Const EMR_SETVIEWPORTORGEX As Integer = 12
'    Friend Const EMR_SETBRUSHORGEX As Integer = 13
'    Friend Const EMR_EOF As Integer = 14
'    Friend Const EMR_SETPIXELV As Integer = 15
'    Friend Const EMR_SETMAPPERFLAGS As Integer = 16
'    Friend Const EMR_SETMAPMODE As Integer = 17
'    Friend Const EMR_SETBKMODE As Integer = 18
'    Friend Const EMR_SETPOLYFILLMODE As Integer = 19
'    Friend Const EMR_SETROP2 As Integer = 20
'    Friend Const EMR_SETSTRETCHBLTMODE As Integer = 21
'    Friend Const EMR_SETTEXTALIGN As Integer = 22
'    Friend Const EMR_SETCOLORADJUSTMENT As Integer = 23
'    Friend Const EMR_SETTEXTCOLOR As Integer = 24
'    Friend Const EMR_SETBKCOLOR As Integer = 25
'    Friend Const EMR_OFFSETCLIPRGN As Integer = 26
'    Friend Const EMR_MOVETOEX As Integer = 27
'    Friend Const EMR_SETMETARGN As Integer = 28
'    Friend Const EMR_EXCLUDECLIPRECT As Integer = 29
'    Friend Const EMR_INTERSECTCLIPRECT As Integer = 30
'    Friend Const EMR_SCALEVIEWPORTEXTEX As Integer = 31
'    Friend Const EMR_SCALEWINDOWEXTEX As Integer = 32
'    Friend Const EMR_SAVEDC As Integer = 33
'    Friend Const EMR_RESTOREDC As Integer = 34
'    Friend Const EMR_SETWORLDTRANSFORM As Integer = 35
'    Friend Const EMR_MODIFYWORLDTRANSFORM As Integer = 36
'    Friend Const EMR_SELECTOBJECT As Integer = 37
'    Friend Const EMR_CREATEPEN As Integer = 38
'    Friend Const EMR_CREATEBRUSHINDIRECT As Integer = 39
'    Friend Const EMR_DELETEOBJECT As Integer = 40
'    Friend Const EMR_ANGLEARC As Integer = 41
'    Friend Const EMR_ELLIPSE As Integer = 42
'    Friend Const EMR_RECTANGLE As Integer = 43
'    Friend Const EMR_ROUNDRECT As Integer = 44
'    Friend Const EMR_ARC As Integer = 45
'    Friend Const EMR_CHORD As Integer = 46
'    Friend Const EMR_PIE As Integer = 47
'    Friend Const EMR_SELECTPALETTE As Integer = 48
'    Friend Const EMR_CREATEPALETTE As Integer = 49
'    Friend Const EMR_SETPALETTEENTRIES As Integer = 50
'    Friend Const EMR_RESIZEPALETTE As Integer = 51
'    Friend Const EMR_REALIZEPALETTE As Integer = 52
'    Friend Const EMR_EXTFLOODFILL As Integer = 53
'    Friend Const EMR_LINETO As Integer = 54
'    Friend Const EMR_ARCTO As Integer = 55
'    Friend Const EMR_POLYDRAW As Integer = 56
'    Friend Const EMR_SETARCDIRECTION As Integer = 57
'    Friend Const EMR_SETMITERLIMIT As Integer = 58
'    Friend Const EMR_BEGINPATH As Integer = 59
'    Friend Const EMR_ENDPATH As Integer = 60
'    Friend Const EMR_CLOSEFIGURE As Integer = 61
'    Friend Const EMR_FILLPATH As Integer = 62
'    Friend Const EMR_STROKEANDFILLPATH As Integer = 63
'    Friend Const EMR_STROKEPATH As Integer = 64
'    Friend Const EMR_FLATTENPATH As Integer = 65
'    Friend Const EMR_WIDENPATH As Integer = 66
'    Friend Const EMR_SELECTCLIPPATH As Integer = 67
'    Friend Const EMR_ABORTPATH As Integer = 68
'    Friend Const EMR_GDICOMMENT As Integer = 70
'    Friend Const EMR_FILLRGN As Integer = 71
'    Friend Const EMR_FRAMERGN As Integer = 72
'    Friend Const EMR_INVERTRGN As Integer = 73
'    Friend Const EMR_PAINTRGN As Integer = 74
'    Friend Const EMR_EXTSELECTCLIPRGN As Integer = 75
'    Friend Const EMR_BITBLT As Integer = 76
'    Friend Const EMR_STRETCHBLT As Integer = 77
'    Friend Const EMR_MASKBLT As Integer = 78
'    Friend Const EMR_PLGBLT As Integer = 79
'    Friend Const EMR_SETDIBITSTODEVICE As Integer = 80
'    Friend Const EMR_STRETCHDIBITS As Integer = 81
'    Friend Const EMR_EXTCREATEFONTINDIRECTW As Integer = 82
'    Friend Const EMR_EXTTEXTOUTA As Integer = 83
'    Friend Const EMR_EXTTEXTOUTW As Integer = 84
'    Friend Const EMR_POLYBEZIER16 As Integer = 85
'    Friend Const EMR_POLYGON16 As Integer = 86
'    Friend Const EMR_POLYLINE16 As Integer = 87
'    Friend Const EMR_POLYBEZIERTO16 As Integer = 88
'    Friend Const EMR_POLYLINETO16 As Integer = 89
'    Friend Const EMR_POLYPOLYLINE16 As Integer = 90
'    Friend Const EMR_POLYPOLYGON16 As Integer = 91
'    Friend Const EMR_POLYDRAW16 As Integer = 92
'    Friend Const EMR_CREATEMONOBRUSH As Integer = 93
'    Friend Const EMR_CREATEDIBPATTERNBRUSHPT As Integer = 94
'    Friend Const EMR_EXTCREATEPEN As Integer = 95
'    Friend Const EMR_POLYTEXTOUTA As Integer = 96
'    Friend Const EMR_POLYTEXTOUTW As Integer = 97
'    Friend Const EMR_SETICMMODE As Integer = 98
'    Friend Const EMR_CREATECOLORSPACE As Integer = 99
'    Friend Const EMR_SETCOLORSPACE As Integer = 100
'    Friend Const EMR_DELETECOLORSPACE As Integer = 101
'    Friend Const EMR_GLSRECORD As Integer = 102
'    Friend Const EMR_GLSBOUNDEDRECORD As Integer = 103
'    Friend Const EMR_PIXELFORMAT As Integer = 104
'    Friend Const EMR_MIN As Integer = 1
'    Friend Const EMR_MAX As Integer = 104
'    ' EMR_MAX = 97;
'    Friend Const EPS_SIGNATURE As Integer = &H46535045
'    Friend Const ENUM_ALL_CALENDARS As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        public const int EPS_SIGNATURE = 0x46535045,
'    '        ENUM_ALL_CALENDARS = unchecked((int)0xFfffffff),
'    '------------------------------^--- GenCode(token): unexpected token type
'    Friend Const ERROR_SEVERITY_SUCCESS As Integer = &H0
'    Friend Const ERROR_SEVERITY_INFORMATIONAL As Integer = &H40000000
'    Friend Const ERROR_SEVERITY_WARNING As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        ERROR_SEVERITY_INFORMATIONAL = 0x40000000,
'    '        ERROR_SEVERITY_WARNING = unchecked((int)0x80000000),
'    '----------------------------------^--- GenCode(token): unexpected token type
'    Friend Const ERROR_SEVERITY_ERROR As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        ERROR_SEVERITY_WARNING = unchecked((int)0x80000000),
'    '        ERROR_SEVERITY_ERROR = unchecked((int)0xC0000000),
'    '--------------------------------^--- GenCode(token): unexpected token type
'    Friend Const EXCEPTION_NONCONTINUABLE As Integer = &H1
'    Friend Const EXCEPTION_MAXIMUM_PARAMETERS As Integer = 15
'    Friend Const EVENT_MODIFY_STATE As Integer = &H2
'    Friend Const EVENT_OBJECT_SELECTION As Integer = &H8006
'    Friend Const EVENTLOG_SEQUENTIAL_READ As Integer = 0
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
        Friend Const EM_UNDO As Integer = &HC7
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
    
    
    
    
    
'    Friend Const FNERR_FILENAMECODES As Integer = &H3000
'    Friend Const FNERR_SUBCLASSFAILURE As Integer = &H3001
'    Friend Const FNERR_INVALIDFILENAME As Integer = &H3002
     Friend Const FNERR_BUFFERTOOSMALL As Integer = &H3003
'    Friend Const FRERR_FINDREPLACECODES As Integer = &H4000
'    Friend Const FRERR_BUFFERLENGTHZERO As Integer = &H4001
'    Friend Const FR_DOWN As Integer = &H1
'    Friend Const FR_WHOLEWORD As Integer = &H2
'    Friend Const FR_MATCHCASE As Integer = &H4
'    Friend Const FR_FINDNEXT As Integer = &H8
'    Friend Const FR_REPLACE As Integer = &H10
'    Friend Const FR_REPLACEALL As Integer = &H20
'    Friend Const FR_DIALOGTERM As Integer = &H40
'    Friend Const FR_SHOWHELP As Integer = &H80
'    Friend Const FR_ENABLEHOOK As Integer = &H100
'    Friend Const FR_ENABLETEMPLATE As Integer = &H200
'    Friend Const FR_NOUPDOWN As Integer = &H400
'    Friend Const FR_NOMATCHCASE As Integer = &H800
'    Friend Const FR_NOWHOLEWORD As Integer = &H1000
'    Friend Const FR_ENABLETEMPLATEHANDLE As Integer = &H2000
'    Friend Const FR_HIDEUPDOWN As Integer = &H4000
'    Friend Const FR_HIDEMATCHCASE As Integer = &H8000
'    Friend Const FR_HIDEWHOLEWORD As Integer = &H10000
'    Friend Const [FALSE] As Boolean = False
'    Friend Const frm1 As Integer = &H434
'    Friend Const frm2 As Integer = &H435
'    Friend Const frm3 As Integer = &H436
'    Friend Const frm4 As Integer = &H437
'    Friend Const FILEOPENORD As Integer = 1536
'    Friend Const FINDDLGORD As Integer = 1540
'    Friend Const FONTDLGORD As Integer = 1542
'    Friend Const FORMATDLGORD31 As Integer = 1543
'    Friend Const FORMATDLGORD30 As Integer = 1544
'    Friend Const FADF_AUTO As Integer = &H1
'    Friend Const FADF_STATIC As Integer = &H2
'    Friend Const FADF_EMBEDDED As Integer = &H4
'    Friend Const FADF_FIXEDSIZE As Integer = &H10
'    Friend Const FADF_BSTR As Integer = &H100
'    Friend Const FADF_UNKNOWN As Integer = &H200
'    Friend Const FADF_DISPATCH As Integer = &H400
'    Friend Const FADF_VARIANT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                              FADF_DISPATCH = (0x400),
'    '                                                                                                              FADF_VARIANT = (unchecked((int)0x800)),
'    '-------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const FADF_RESERVED As Integer = &HF0E8
'    Friend Const FO_MOVE As Integer = &H1
'    Friend Const FO_COPY As Integer = &H2
'    Friend Const FO_DELETE As Integer = &H3
'    Friend Const FO_RENAME As Integer = &H4
'    Friend Const FOF_MULTIDESTFILES As Integer = &H1
'    Friend Const FOF_CONFIRMMOUSE As Integer = &H2
'    Friend Const FOF_SILENT As Integer = &H4
'    Friend Const FOF_RENAMEONCOLLISION As Integer = &H8
'    Friend Const FOF_NOCONFIRMATION As Integer = &H10
'    Friend Const FOF_WANTMAPPINGHANDLE As Integer = &H20
'    Friend Const FOF_ALLOWUNDO As Integer = &H40
'    Friend Const FOF_FILESONLY As Integer = &H80
'    Friend Const FOF_SIMPLEPROGRESS As Integer = &H100
'    Friend Const FOF_NOCONFIRMMKDIR As Integer = &H200
'    Friend Const FOF_NOERRORUI As Integer = &H400
'    Friend Const FILE_BEGIN As Integer = 0
'    Friend Const FILE_CURRENT As Integer = 1
'    Friend Const FILE_END As Integer = 2
'    Friend Const FILE_FLAG_WRITE_THROUGH As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        FILE_END = 2,
'    '        FILE_FLAG_WRITE_THROUGH = unchecked((int)0x80000000),
'    '-----------------------------------^--- GenCode(token): unexpected token type
'    Friend Const FILE_FLAG_OVERLAPPED As Integer = &H40000000
'    Friend Const FILE_FLAG_NO_BUFFERING As Integer = &H20000000
'    Friend Const FILE_FLAG_RANDOM_ACCESS As Integer = &H10000000
'    Friend Const FILE_FLAG_SEQUENTIAL_SCAN As Integer = &H8000000
'    Friend Const FILE_FLAG_DELETE_ON_CLOSE As Integer = &H4000000
'    Friend Const FILE_FLAG_BACKUP_SEMANTICS As Integer = &H2000000
'    Friend Const FILE_FLAG_POSIX_SEMANTICS As Integer = &H1000000
'    Friend Const FILE_TYPE_UNKNOWN As Integer = &H0
'    Friend Const FILE_TYPE_DISK As Integer = &H1
'    Friend Const FILE_TYPE_CHAR As Integer = &H2
'    Friend Const FILE_TYPE_PIPE As Integer = &H3
'    Friend Const FILE_TYPE_REMOTE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        FILE_TYPE_PIPE = 0x0003,
'    '        FILE_TYPE_REMOTE = unchecked((int)0x8000),
'    '----------------------------^--- GenCode(token): unexpected token type
'    Friend Const FORMAT_MESSAGE_ALLOCATE_BUFFER As Integer = &H100
'    Friend Const FORMAT_MESSAGE_IGNORE_INSERTS As Integer = &H200
'    Friend Const FORMAT_MESSAGE_FROM_STRING As Integer = &H400
'    Friend Const FORMAT_MESSAGE_FROM_HMODULE As Integer = &H800
'    Friend Const FORMAT_MESSAGE_FROM_SYSTEM As Integer = &H1000
'    Friend Const FORMAT_MESSAGE_ARGUMENT_ARRAY As Integer = &H2000
'    Friend Const FORMAT_MESSAGE_MAX_WIDTH_MASK As Integer = &HFF
'    Friend Const FIND_FIRST_EX_CASE_SENSITIVE As Integer = &H1
'    Friend Const FROM_LEFT_1ST_BUTTON_PRESSED As Integer = &H1
'    Friend Const FROM_LEFT_2ND_BUTTON_PRESSED As Integer = &H4
'    Friend Const FROM_LEFT_3RD_BUTTON_PRESSED As Integer = &H8
'    Friend Const FROM_LEFT_4TH_BUTTON_PRESSED As Integer = &H10
'    Friend Const FOCUS_EVENT As Integer = &H10
'    Friend Const FOREGROUND_BLUE As Integer = &H1
'    Friend Const FOREGROUND_GREEN As Integer = &H2
'    Friend Const FOREGROUND_RED As Integer = &H4
'    Friend Const FOREGROUND_INTENSITY As Integer = &H8
     Friend Const [FALSE] As Integer = 0
'    Friend Const FACILITY_WINDOWS As Integer = 8
'    Friend Const FACILITY_STORAGE As Integer = 3
'    Friend Const FACILITY_RPC As Integer = 1
'    Friend Const FACILITY_SSPI As Integer = 9
     Friend Const FACILITY_WIN32 As Integer = 7
'    Friend Const FACILITY_CONTROL As Integer = 10
'    Friend Const FACILITY_NULL As Integer = 0
'    Friend Const FACILITY_INTERNET As Integer = 12
'    Friend Const FACILITY_ITF As Integer = 4
'    Friend Const FACILITY_DISPATCH As Integer = 2
'    Friend Const FACILITY_CERT As Integer = 11
'    Friend Const FACILITY_NT_BIT As Integer = &H10000000
'    Friend Const FLUSHOUTPUT As Integer = 6
'    Friend Const FIXED_PITCH As Integer = 1
'    Friend Const FS_LATIN1 As Integer = &H1
'    Friend Const FS_LATIN2 As Integer = &H2
'    Friend Const FS_CYRILLIC As Integer = &H4
'    Friend Const FS_GREEK As Integer = &H8
'    Friend Const FS_TURKISH As Integer = &H10
'    Friend Const FS_HEBREW As Integer = &H20
'    Friend Const FS_ARABIC As Integer = &H40
'    Friend Const FS_BALTIC As Integer = &H80
'    Friend Const FS_VIETNAMESE As Integer = &H100
'    Friend Const FS_THAI As Integer = &H10000
'    Friend Const FS_JISJAPAN As Integer = &H20000
'    Friend Const FS_CHINESESIMP As Integer = &H40000
'    Friend Const FS_WANSUNG As Integer = &H80000
'    Friend Const FS_CHINESETRAD As Integer = &H100000
'    Friend Const FS_JOHAB As Integer = &H200000
'    Friend Const FS_SYMBOL As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        FS_JOHAB = 0x00200000,
'    '        FS_SYMBOL = unchecked((int)0x80000000),
'    '---------------------^--- GenCode(token): unexpected token type
'    Friend Const FF_DONTCARE As Integer = Machine.Shift.Left(0, 4)
'    Friend Const FF_ROMAN As Integer = Machine.Shift.Left(1, 4)
'    Friend Const FF_SWISS As Integer = Machine.Shift.Left(2, 4)
'    Friend Const FF_MODERN As Integer = Machine.Shift.Left(3, 4)
'    Friend Const FF_SCRIPT As Integer = Machine.Shift.Left(4, 4)
'    Friend Const FF_DECORATIVE As Integer = Machine.Shift.Left(5, 4)
'    Friend Const FW_DONTCARE As Integer = 0
'    Friend Const FW_THIN As Integer = 100
'    Friend Const FW_EXTRALIGHT As Integer = 200
'    Friend Const FW_LIGHT As Integer = 300
'    Friend Const FW_NORMAL As Integer = 400
'    Friend Const FW_MEDIUM As Integer = 500
'    Friend Const FW_SEMIBOLD As Integer = 600
'    Friend Const FW_BOLD As Integer = 700
'    Friend Const FW_EXTRABOLD As Integer = 800
'    Friend Const FW_HEAVY As Integer = 900
'    Friend Const FW_ULTRALIGHT As Integer = 200
'    Friend Const FW_REGULAR As Integer = 400
'    Friend Const FW_DEMIBOLD As Integer = 600
'    Friend Const FW_ULTRABOLD As Integer = 800
'    Friend Const FW_BLACK As Integer = 900
'    Friend Const FLOODFILLBORDER As Integer = 0
'    Friend Const FLOODFILLSURFACE As Integer = 1
'    Friend Const FLI_MASK As Integer = &H103B
'    Friend Const FLI_GLYPHS As Integer = &H40000
'    Friend Const FONTMAPPER_MAX As Integer = 10
'    Friend Const FILE_READ_DATA As Integer = &H1
'    Friend Const FILE_LIST_DIRECTORY As Integer = &H1
'    Friend Const FILE_WRITE_DATA As Integer = &H2
'    Friend Const FILE_ADD_FILE As Integer = &H2
'    Friend Const FILE_APPEND_DATA As Integer = &H4
'    Friend Const FILE_ADD_SUBDIRECTORY As Integer = &H4
'    Friend Const FILE_CREATE_PIPE_INSTANCE As Integer = &H4
'    Friend Const FILE_READ_EA As Integer = &H8
'    Friend Const FILE_WRITE_EA As Integer = &H10
'    Friend Const FILE_EXECUTE As Integer = &H20
'    Friend Const FILE_TRAVERSE As Integer = &H20
'    Friend Const FILE_DELETE_CHILD As Integer = &H40
'    Friend Const FILE_READ_ATTRIBUTES As Integer = &H80
'    Friend Const FILE_WRITE_ATTRIBUTES As Integer = &H100
'    Friend Const FILE_SHARE_READ As Integer = &H1
'    Friend Const FILE_SHARE_WRITE As Integer = &H2
'    Friend Const FILE_SHARE_DELETE As Integer = &H4
'    Friend Const FILE_ATTRIBUTE_READONLY As Integer = &H1
'    Friend Const FILE_ATTRIBUTE_HIDDEN As Integer = &H2
'    Friend Const FILE_ATTRIBUTE_SYSTEM As Integer = &H4
'    Friend Const FILE_ATTRIBUTE_DIRECTORY As Integer = &H10
'    Friend Const FILE_ATTRIBUTE_ARCHIVE As Integer = &H20
'    Friend Const FILE_ATTRIBUTE_NORMAL As Integer = &H80
'    Friend Const FILE_ATTRIBUTE_TEMPORARY As Integer = &H100
'    Friend Const FILE_ATTRIBUTE_COMPRESSED As Integer = &H800
'    Friend Const FILE_ATTRIBUTE_OFFLINE As Integer = &H1000
'    Friend Const FILE_NOTIFY_CHANGE_FILE_NAME As Integer = &H1
'    Friend Const FILE_NOTIFY_CHANGE_DIR_NAME As Integer = &H2
'    Friend Const FILE_NOTIFY_CHANGE_ATTRIBUTES As Integer = &H4
'    Friend Const FILE_NOTIFY_CHANGE_SIZE As Integer = &H8
'    Friend Const FILE_NOTIFY_CHANGE_LAST_WRITE As Integer = &H10
'    Friend Const FILE_NOTIFY_CHANGE_LAST_ACCESS As Integer = &H20
'    Friend Const FILE_NOTIFY_CHANGE_CREATION As Integer = &H40
'    Friend Const FILE_NOTIFY_CHANGE_SECURITY As Integer = &H100
'    Friend Const FILE_ACTION_ADDED As Integer = &H1
'    Friend Const FILE_ACTION_REMOVED As Integer = &H2
'    Friend Const FILE_ACTION_MODIFIED As Integer = &H3
'    Friend Const FILE_ACTION_RENAMED_OLD_NAME As Integer = &H4
'    Friend Const FILE_ACTION_RENAMED_NEW_NAME As Integer = &H5
'    Friend Const FILE_CASE_SENSITIVE_SEARCH As Integer = &H1
'    Friend Const FILE_CASE_PRESERVED_NAMES As Integer = &H2
'    Friend Const FILE_UNICODE_ON_DISK As Integer = &H4
'    Friend Const FILE_PERSISTENT_ACLS As Integer = &H8
'    Friend Const FILE_FILE_COMPRESSION As Integer = &H10
'    Friend Const FILE_VOLUME_IS_COMPRESSED As Integer = &H8000
'    Friend Const FAILED_ACCESS_ACE_FLAG As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        FILE_VOLUME_IS_COMPRESSED = 0x00008000,
'    '        FAILED_ACCESS_ACE_FLAG = (unchecked((int)0x80)),
'    '-----------------------------------^--- GenCode(token): unexpected token type
'    Friend Const FRAME_FPO As Integer = 0
'    Friend Const FRAME_TRAP As Integer = 1
'    Friend Const FRAME_TSS As Integer = 2
'    Friend Const FRAME_NONFPO As Integer = 3
'    Friend Const FORM_USER As Integer = &H0
'    Friend Const FORM_BUILTIN As Integer = &H1
'    Friend Const FORM_PRINTER As Integer = &H2
'    Friend Const FVIRTKEY As Integer = &H1
'    Friend Const FNOINVERT As Integer = &H2
'    Friend Const FSHIFT As Integer = &H4
'    Friend Const FCONTROL As Integer = &H8
'    Friend Const FALT As Integer = &H10
'    Friend Const FKF_FILTERKEYSON As Integer = &H1
'    Friend Const FKF_AVAILABLE As Integer = &H2
'    Friend Const FKF_HOTKEYACTIVE As Integer = &H4
'    Friend Const FKF_CONFIRMHOTKEY As Integer = &H8
'    Friend Const FKF_HOTKEYSOUND As Integer = &H10
'    Friend Const FKF_INDICATOR As Integer = &H20
'    Friend Const FKF_CLICKON As Integer = &H40
'    Friend Const FS_CASE_IS_PRESERVED As Integer = &H2
'    Friend Const FS_CASE_SENSITIVE As Integer = &H1
'    Friend Const FS_UNICODE_STORED_ON_DISK As Integer = &H4
'    Friend Const FS_PERSISTENT_ACLS As Integer = &H8
'    Friend Const FS_VOL_IS_COMPRESSED As Integer = &H8000
'    Friend Const FS_FILE_COMPRESSION As Integer = &H10
'    Friend Const FILE_MAP_COPY As Integer = &H1
'    Friend Const FILE_MAP_WRITE As Integer = &H2
'    Friend Const FILE_MAP_READ As Integer = &H4
'    Friend Const FILE_ALL_ACCESS As Integer = &HF0000 Or &H100000 Or &H1FF
    
    
    
    
'    Friend Const grp1 As Integer = &H430
'    Friend Const grp2 As Integer = &H431
'    Friend Const grp3 As Integer = &H432
'    Friend Const grp4 As Integer = &H433
'    Friend Const GCS_COMPREADSTR As Integer = &H1
'    Friend Const GCS_COMPREADATTR As Integer = &H2
'    Friend Const GCS_COMPREADCLAUSE As Integer = &H4
'    Friend Const GCS_COMPSTR As Integer = &H8
'    Friend Const GCS_COMPATTR As Integer = &H10
'    Friend Const GCS_COMPCLAUSE As Integer = &H20
'    Friend Const GCS_CURSORPOS As Integer = &H80
'    Friend Const GCS_DELTASTART As Integer = &H100
'    Friend Const GCS_RESULTREADSTR As Integer = &H200
'    Friend Const GCS_RESULTREADCLAUSE As Integer = &H400
'    Friend Const GCS_RESULTSTR As Integer = &H800
'    Friend Const GCS_RESULTCLAUSE As Integer = &H1000
'    Friend Const GGL_LEVEL As Integer = &H1
'    Friend Const GGL_INDEX As Integer = &H2
'    Friend Const GGL_STRING As Integer = &H3
'    Friend Const GGL_PRIVATE As Integer = &H4
'    Friend Const GL_LEVEL_NOGUIDELINE As Integer = &H0
'    Friend Const GL_LEVEL_FATAL As Integer = &H1
'    Friend Const GL_LEVEL_ERROR As Integer = &H2
'    Friend Const GL_LEVEL_WARNING As Integer = &H3
'    Friend Const GL_LEVEL_INFORMATION As Integer = &H4
'    Friend Const GL_ID_UNKNOWN As Integer = &H0
'    Friend Const GL_ID_NOMODULE As Integer = &H1
'    Friend Const GL_ID_NODICTIONARY As Integer = &H10
'    Friend Const GL_ID_CANNOTSAVE As Integer = &H11
'    Friend Const GL_ID_NOCONVERT As Integer = &H20
'    Friend Const GL_ID_TYPINGERROR As Integer = &H21
'    Friend Const GL_ID_TOOMANYSTROKE As Integer = &H22
'    Friend Const GL_ID_READINGCONFLICT As Integer = &H23
'    Friend Const GL_ID_INPUTREADING As Integer = &H24
'    Friend Const GL_ID_INPUTRADICAL As Integer = &H25
'    Friend Const GL_ID_INPUTCODE As Integer = &H26
'    Friend Const GL_ID_INPUTSYMBOL As Integer = &H27
'    Friend Const GL_ID_CHOOSECANDIDATE As Integer = &H28
'    Friend Const GL_ID_REVERSECONVERSION As Integer = &H29
'    Friend Const GL_ID_PRIVATE_FIRST As Integer = &H8000
'    Friend Const GL_ID_PRIVATE_LAST As Integer = &HFFFF
'    Friend Const GCL_CONVERSION As Integer = &H1
'    Friend Const GCL_REVERSECONVERSION As Integer = &H2
'    Friend Const GCL_REVERSE_LENGTH As Integer = &H3
'    Friend Const GROUP_NAME As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        GCL_REVERSE_LENGTH = 0x0003,
'    '        GROUP_NAME = unchecked((int)0x80),
'    '----------------------^--- GenCode(token): unexpected token type
'    Friend Const GMEM_FIXED As Integer = &H0
'    Friend Const GMEM_MOVEABLE As Integer = &H2
'    Friend Const GMEM_NOCOMPACT As Integer = &H10
'    Friend Const GMEM_NODISCARD As Integer = &H20
'    Friend Const GMEM_ZEROINIT As Integer = &H40
'    Friend Const GMEM_MODIFY As Integer = &H80
'    Friend Const GMEM_DISCARDABLE As Integer = &H100
'    Friend Const GMEM_NOT_BANKED As Integer = &H1000
'    Friend Const GMEM_SHARE As Integer = &H2000
'    Friend Const GMEM_DDESHARE As Integer = &H2000
'    Friend Const GMEM_NOTIFY As Integer = &H4000
'    Friend Const GMEM_LOWER As Integer = &H1000
'    Friend Const GMEM_VALID_FLAGS As Integer = &H7F72
'    Friend Const GMEM_INVALID_HANDLE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        GMEM_VALID_FLAGS = 0x7F72,
'    '        GMEM_INVALID_HANDLE = unchecked((int)0x8000),
'    '-------------------------------^--- GenCode(token): unexpected token type
'    Friend Const GHND As Integer = &H2 Or &H40
'    Friend Const GPTR As Integer = &H0 Or &H40
'    Friend Const GMEM_DISCARDED As Integer = &H4000
'    Friend Const GMEM_LOCKCOUNT As Integer = &HFF
'    Friend Const GET_TAPE_MEDIA_INFORMATION As Integer = 0
'    Friend Const GET_TAPE_DRIVE_INFORMATION As Integer = 1
'    Friend Const GDI_ERROR As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        GET_TAPE_DRIVE_INFORMATION = 1,
'    '        GDI_ERROR = (unchecked((int)0xFFFFFFFF)),
'    '----------------------^--- GenCode(token): unexpected token type
'    Friend Const GETCOLORTABLE As Integer = 5
'    Friend Const GETPHYSPAGESIZE As Integer = 12
'    Friend Const GETPRINTINGOFFSET As Integer = 13
'    Friend Const GETSCALINGFACTOR As Integer = 14
'    Friend Const GETPENWIDTH As Integer = 16
'    Friend Const GETTECHNOLGY As Integer = 20
'    Friend Const GETTECHNOLOGY As Integer = 20
'    Friend Const GETVECTORPENSIZE As Integer = 26
'    Friend Const GETVECTORBRUSHSIZE As Integer = 27
'    Friend Const GETSETPAPERBINS As Integer = 29
'    Friend Const GETSETPRINTORIENT As Integer = 30
'    Friend Const GETSETPAPERMETRICS As Integer = 35
'    Friend Const GETDEVICEUNITS As Integer = 42
'    Friend Const GETEXTENDEDTEXTMETRICS As Integer = 256
'    Friend Const GETEXTENTTABLE As Integer = 257
'    Friend Const GETPAIRKERNTABLE As Integer = 258
'    Friend Const GETTRACKKERNTABLE As Integer = 259
'    Friend Const GETFACENAME As Integer = 513
'    Friend Const GETSETSCREENPARAMS As Integer = 3072
'    Friend Const GB2312_CHARSET As Integer = 134
'    Friend Const GREEK_CHARSET As Integer = 161
'    Friend Const GM_COMPATIBLE As Integer = 1
'    Friend Const GM_ADVANCED As Integer = 2
'    Friend Const GM_LAST As Integer = 2
'    Friend Const GRAY_BRUSH As Integer = 2
'    Friend Const GGO_METRICS As Integer = 0
'    Friend Const GGO_BITMAP As Integer = 1
'    Friend Const GGO_NATIVE As Integer = 2
'    Friend Const GGO_GRAY2_BITMAP As Integer = 4
'    Friend Const GGO_GRAY4_BITMAP As Integer = 5
'    Friend Const GGO_GRAY8_BITMAP As Integer = 6
'    Friend Const GGO_GLYPH_INDEX As Integer = &H80
'    Friend Const GCP_DBCS As Integer = &H1
'    Friend Const GCP_REORDER As Integer = &H2
'    Friend Const GCP_USEKERNING As Integer = &H8
'    Friend Const GCP_GLYPHSHAPE As Integer = &H10
'    Friend Const GCP_LIGATE As Integer = &H20
'    Friend Const GCP_DIACRITIC As Integer = &H100
'    Friend Const GCP_KASHIDA As Integer = &H400
'    Friend Const GCP_ERROR As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        GCP_KASHIDA = 0x0400,
'    '        GCP_ERROR = unchecked((int)0x8000),
'    '---------------------^--- GenCode(token): unexpected token type
'    Friend Const GCP_JUSTIFY As Integer = &H10000
'    Friend Const GCP_CLASSIN As Integer = &H80000
'    Friend Const GCP_MAXEXTENT As Integer = &H100000
'    Friend Const GCP_JUSTIFYIN As Integer = &H200000
'    Friend Const GCP_DISPLAYZWG As Integer = &H400000
'    Friend Const GCP_SYMSWAPOFF As Integer = &H800000
'    Friend Const GCP_NUMERICOVERRIDE As Integer = &H1000000
'    Friend Const GCP_NEUTRALOVERRIDE As Integer = &H2000000
'    Friend Const GCP_NUMERICSLATIN As Integer = &H4000000
'    Friend Const GCP_NUMERICSLOCAL As Integer = &H8000000
'    Friend Const GCPCLASS_LATIN As Integer = 1
'    Friend Const GCPCLASS_HEBREW As Integer = 2
'    Friend Const GCPCLASS_ARABIC As Integer = 2
'    Friend Const GCPCLASS_NEUTRAL As Integer = 3
'    Friend Const GCPCLASS_LOCALNUMBER As Integer = 4
'    Friend Const GCPCLASS_LATINNUMBER As Integer = 5
'    Friend Const GCPCLASS_LATINNUMERICTERMINATOR As Integer = 6
'    Friend Const GCPCLASS_LATINNUMERICSEPARATOR As Integer = 7
'    Friend Const GCPCLASS_NUMERICSEPARATOR As Integer = 8
'    Friend Const GCPCLASS_PREBOUNDLTR As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        GCPCLASS_NUMERICSEPARATOR = 8,
'    '        GCPCLASS_PREBOUNDLTR = unchecked((int)0x80),
'    '--------------------------------^--- GenCode(token): unexpected token type
'    Friend Const GCPCLASS_PREBOUNDRTL As Integer = &H40
'    Friend Const GCPCLASS_POSTBOUNDLTR As Integer = &H20
'    Friend Const GCPCLASS_POSTBOUNDRTL As Integer = &H10
'    Friend Const GCPGLYPH_LINKBEFORE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        GCPCLASS_POSTBOUNDRTL = 0x10,
'    '        GCPGLYPH_LINKBEFORE = unchecked((int)0x8000),
'    '-------------------------------^--- GenCode(token): unexpected token type
'    Friend Const GCPGLYPH_LINKAFTER As Integer = &H4000
'    Friend Const GDICOMMENT_IDENTIFIER As Integer = &H43494447
'    Friend Const GDICOMMENT_WINDOWS_METAFILE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        GDICOMMENT_IDENTIFIER = 0x43494447,
'    '        GDICOMMENT_WINDOWS_METAFILE = unchecked((int)0x80000001),
'    '---------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const GDICOMMENT_BEGINGROUP As Integer = &H2
'    Friend Const GDICOMMENT_ENDGROUP As Integer = &H3
'    Friend Const GDICOMMENT_MULTIFORMATS As Integer = &H40000004
'    Friend Const GENERIC_READ As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        GDICOMMENT_MULTIFORMATS = 0x40000004,
'    '        GENERIC_READ = (unchecked((int)0x80000000)),
'    '-------------------------^--- GenCode(token): unexpected token type
'    Friend Const GENERIC_WRITE As Integer = &H40000000
'    Friend Const GENERIC_EXECUTE As Integer = &H20000000
'    Friend Const GENERIC_ALL As Integer = &H10000000
'    Friend Const GROUP_SECURITY_INFORMATION As Integer = 0
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
        Friend Const GW_CHILD As UInteger = 5
'    Private GW_MAX As __unknown = 5
'    Private GMR_VISIBLE As __unknown = 0
'    Private GMR_DAYSTATE As __unknown = 1
'    Private GDTR_MIN As __unknown = &H1
'    Private GDTR_MAX As __unknown = &H2
'    Private GDT_ERROR As __unknown = - 1
'    Private GDT_VALID As __unknown = 0
'    Private GDT_NONE As __unknown = 1
'    Private __unknown As __unknown
    
    
    
'    Friend Const HDATA_APPOWNED As Integer = &H1
'    Friend Const HANGUP_PENDING As Integer = &H4
'    Friend Const HANGUP_COMPLETE As Integer = &H5
'    Friend Const HIGH_PRIORITY_CLASS As Integer = &H80
'    Friend Const HANDLE_FLAG_INHERIT As Integer = &H1
'    Friend Const HANDLE_FLAG_PROTECT_FROM_CLOSE As Integer = &H2
'    Friend Const HINSTANCE_ERROR As Integer = 32
'    Friend Const HW_PROFILE_GUIDLEN As Integer = 39
'    Friend Const HP_ALGID As Integer = &H1
'    Friend Const HP_HASHVAL As Integer = &H2
'    Friend Const HP_HASHSIZE As Integer = &H4
'    Friend Const HALFTONE As Integer = 4
'    Friend Const HANGEUL_CHARSET As Integer = 129
'    Friend Const HEBREW_CHARSET As Integer = 177
'    Friend Const HOLLOW_BRUSH As Integer = 5
'    Friend Const HS_HORIZONTAL As Integer = 0
'    Friend Const HS_VERTICAL As Integer = 1
'    Friend Const HS_FDIAGONAL As Integer = 2
'    Friend Const HS_BDIAGONAL As Integer = 3
'    Friend Const HS_CROSS As Integer = 4
'    Friend Const HS_DIAGCROSS As Integer = 5
'    Friend Const HORZSIZE As Integer = 4
'    Friend Const HORZRES As Integer = 8
'    Friend Const HEAP_NO_SERIALIZE As Integer = &H1
'    Friend Const HEAP_GROWABLE As Integer = &H2
'    Friend Const HEAP_GENERATE_EXCEPTIONS As Integer = &H4
'    Friend Const HEAP_ZERO_MEMORY As Integer = &H8
'    Friend Const HEAP_REALLOC_IN_PLACE_ONLY As Integer = &H10
'    Friend Const HEAP_TAIL_CHECKING_ENABLED As Integer = &H20
'    Friend Const HEAP_FREE_CHECKING_ENABLED As Integer = &H40
'    Friend Const HEAP_DISABLE_COALESCE_ON_FREE As Integer = &H80
'    Friend Const HEAP_CREATE_ALIGN_16 As Integer = &H10000
'    Friend Const HEAP_CREATE_ENABLE_TRACING As Integer = &H20000
'    Friend Const HEAP_MAXIMUM_TAG As Integer = &HFFF
'    Friend Const HEAP_PSEUDO_TAG_FLAG As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        HEAP_MAXIMUM_TAG = 0x0FFF,
'    '        HEAP_PSEUDO_TAG_FLAG = unchecked((int)0x8000),
'    '--------------------------------^--- GenCode(token): unexpected token type
'    Friend Const HEAP_TAG_SHIFT As Integer = 16
'    Friend Const HIDE_WINDOW As Integer = 0
'    Friend Const HC_ACTION As Integer = 0
'    Friend Const HC_GETNEXT As Integer = 1
'    Friend Const HC_SKIP As Integer = 2
'    Friend Const HC_NOREMOVE As Integer = 3
'    Friend Const HC_NOREM As Integer = 3
'    Friend Const HC_SYSMODALON As Integer = 4
'    Friend Const HC_SYSMODALOFF As Integer = 5
'    Friend Const HCBT_MOVESIZE As Integer = 0
'    Friend Const HCBT_MINMAX As Integer = 1
'    Friend Const HCBT_QS As Integer = 2
'    Friend Const HCBT_CREATEWND As Integer = 3
'    Friend Const HCBT_DESTROYWND As Integer = 4
'    Friend Const HCBT_ACTIVATE As Integer = 5
'    Friend Const HCBT_CLICKSKIPPED As Integer = 6
'    Friend Const HCBT_KEYSKIPPED As Integer = 7
'    Friend Const HCBT_SYSCOMMAND As Integer = 8
'    Friend Const HCBT_SETFOCUS As Integer = 9
'    Friend Const HSHELL_WINDOWCREATED As Integer = 1
'    Friend Const HSHELL_WINDOWDESTROYED As Integer = 2
'    Friend Const HSHELL_ACTIVATESHELLWINDOW As Integer = 3
'    Friend Const HSHELL_WINDOWACTIVATED As Integer = 4
'    Friend Const HSHELL_GETMINRECT As Integer = 5
'    Friend Const HSHELL_REDRAW As Integer = 6
'    Friend Const HSHELL_TASKMAN As Integer = 7
'    Friend Const HSHELL_LANGUAGE As Integer = 8
'    Friend Const HKL_PREV As Integer = 0
'    Friend Const HKL_NEXT As Integer = 1
'    Friend Const HTERROR As Integer = - 2
'    Friend Const HTTRANSPARENT As Integer = - 1
'    Friend Const HTNOWHERE As Integer = 0
'    Friend Const HTCLIENT As Integer = 1
'    Friend Const HTCAPTION As Integer = 2
'    Friend Const HTSYSMENU As Integer = 3
'    Friend Const HTGROWBOX As Integer = 4
'    Friend Const HTSIZE As Integer = 4
'    Friend Const HTMENU As Integer = 5
'    Friend Const HTHSCROLL As Integer = 6
'    Friend Const HTVSCROLL As Integer = 7
'    Friend Const HTMINBUTTON As Integer = 8
'    Friend Const HTMAXBUTTON As Integer = 9
'    Friend Const HTLEFT As Integer = 10
'    Friend Const HTRIGHT As Integer = 11
'    Friend Const HTTOP As Integer = 12
'    Friend Const HTTOPLEFT As Integer = 13
'    Friend Const HTTOPRIGHT As Integer = 14
'    Friend Const HTBOTTOM As Integer = 15
'    Friend Const HTBOTTOMLEFT As Integer = 16
'    Friend Const HTBOTTOMRIGHT As Integer = 17
'    Friend Const HTBORDER As Integer = 18
'    Friend Const HTREDUCE As Integer = 8
'    Friend Const HTZOOM As Integer = 9
'    Friend Const HTSIZEFIRST As Integer = 10
'    Friend Const HTSIZELAST As Integer = 17
'    Friend Const HTOBJECT As Integer = 19
'    Friend Const HTCLOSE As Integer = 20
'    Friend Const HTHELP As Integer = 21
'    Friend Const HOVER_DEFAULT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        HTHELP = 21,
'    '        HOVER_DEFAULT = unchecked((int)0xFFFFFFFF),
'    '-------------------------^--- GenCode(token): unexpected token type
'    Friend Const HELPINFO_WINDOW As Integer = &H1
'    Friend Const HELPINFO_MENUITEM As Integer = &H2
'    Friend Const HELP_CONTEXT As Integer = &H1
'    Friend Const HELP_QUIT As Integer = &H2
'    Friend Const HELP_INDEX As Integer = &H3
'    Friend Const HELP_CONTENTS As Integer = &H3
'    Friend Const HELP_HELPONHELP As Integer = &H4
'    Friend Const HELP_SETINDEX As Integer = &H5
'    Friend Const HELP_SETCONTENTS As Integer = &H5
'    Friend Const HELP_CONTEXTPOPUP As Integer = &H8
'    Friend Const HELP_FORCEFILE As Integer = &H9
'    Friend Const HELP_KEY As Integer = &H101
'    Friend Const HELP_COMMAND As Integer = &H102
'    Friend Const HELP_PARTIALKEY As Integer = &H105
'    Friend Const HELP_MULTIKEY As Integer = &H201
'    Friend Const HELP_SETWINPOS As Integer = &H203
'    Friend Const HELP_CONTEXTMENU As Integer = &HA
'    Friend Const HELP_FINDER As Integer = &HB
        '    Friend Const HELP_WM_HELP As Integer = &HC
'    Friend Const HELP_SETPOPUP_POS As Integer = &HD
'    Friend Const HELP_TCARD As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        HELP_SETPOPUP_POS = 0x000d,
'    '        HELP_TCARD = unchecked((int)0x8000),
'    '----------------------^--- GenCode(token): unexpected token type
'    Friend Const HELP_TCARD_DATA As Integer = &H10
'    Friend Const HELP_TCARD_OTHER_CALLER As Integer = &H11
'    Friend Const HCF_HIGHCONTRASTON As Integer = &H1
'    Friend Const HCF_AVAILABLE As Integer = &H2
'    Friend Const HCF_HOTKEYACTIVE As Integer = &H4
'    Friend Const HCF_CONFIRMHOTKEY As Integer = &H8
'    Friend Const HCF_HOTKEYSOUND As Integer = &H10
'    Friend Const HCF_INDICATOR As Integer = &H20
'    Friend Const HCF_HOTKEYAVAILABLE As Integer = &H40
'    Friend Const HDM_FIRST As Integer = &H1200
'    Friend Const HDN_FIRST As Integer = 0 - 300
'    Friend Const HDN_LAST As Integer = 0 - 399
'    Friend Const HDS_HORZ As Integer = &H0
'    Friend Const HDS_BUTTONS As Integer = &H2
'    Friend Const HDS_HOTTRACK As Integer = &H4
'    Friend Const HDS_HIDDEN As Integer = &H8
'    Friend Const HDS_DRAGDROP As Integer = &H40
'    Friend Const HDS_FULLDRAG As Integer = &H80
'    Friend Const HDI_WIDTH As Integer = &H1
'    Friend Const HDI_HEIGHT As Integer = &H1
     Friend Const HDI_TEXT As Integer = &H2
     Friend Const HDI_FORMAT As Integer = &H4
'    Friend Const HDI_LPARAM As Integer = &H8
'    Friend Const HDI_BITMAP As Integer = &H10
     Friend Const HDI_IMAGE As Integer = &H20
'    Friend Const HDI_DI_SETITEM As Integer = &H40
'    Friend Const HDI_ORDER As Integer = &H80
'    Friend Const HDF_LEFT As Integer = 0
'    Friend Const HDF_RIGHT As Integer = 1
'    Friend Const HDF_CENTER As Integer = 2
'    Friend Const HDF_JUSTIFYMASK As Integer = &H3
'    Friend Const HDF_RTLREADING As Integer = 4
'    Friend Const HDF_OWNERDRAW As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        HDF_RTLREADING = 4,
'    '        HDF_OWNERDRAW = unchecked((int)0x8000),
'    '-------------------------^--- GenCode(token): unexpected token type
     Friend Const HDF_STRING As Integer = &H4000
'    Friend Const HDF_BITMAP As Integer = &H2000
     Friend Const HDF_BITMAP_ON_RIGHT As Integer = &H1000
     Friend Const HDF_IMAGE As Integer = &H800
'    Friend Const HDM_GETITEMCOUNT As Integer = &H1200 + 0
'    Friend Const HDM_INSERTITEMA As Integer = &H1200 + 1
'    Friend Const HDM_INSERTITEMW As Integer = &H1200 + 10
'    Friend Const HDM_DELETEITEM As Integer = &H1200 + 2
'    Friend Const HDM_GETITEMA As Integer = &H1200 + 3
'    Friend Const HDM_GETITEMW As Integer = &H1200 + 11
'    Friend Const HDM_SETITEMA As Integer = &H1200 + 4
     Friend Const HDM_SETITEMW As Integer = &H1200 + 12
'    Friend Const HDM_LAYOUT As Integer = &H1200 + 5
'    Friend Const HHT_NOWHERE As Integer = &H1
'    Friend Const HHT_ONHEADER As Integer = &H2
'    Friend Const HHT_ONDIVIDER As Integer = &H4
'    Friend Const HHT_ONDIVOPEN As Integer = &H8
'    Friend Const HHT_ABOVE As Integer = &H100
'    Friend Const HHT_BELOW As Integer = &H200
'    Friend Const HHT_TORIGHT As Integer = &H400
'    Friend Const HHT_TOLEFT As Integer = &H800
'    Friend Const HDM_HITTEST As Integer = &H1200 + 6
'    Friend Const HDM_GETITEMRECT As Integer = &H1200 + 7
     Friend Const HDM_SETIMAGELIST As Integer = &H1200 + 8
'    Friend Const HDM_GETIMAGELIST As Integer = &H1200 + 9
'    Friend Const HDM_ORDERTOINDEX As Integer = &H1200 + 15
'    Friend Const HDM_CREATEDRAGIMAGE As Integer = &H1200 + 16
'    Friend Const HDM_GETORDERARRAY As Integer = &H1200 + 17
'    Friend Const HDM_SETORDERARRAY As Integer = &H1200 + 18
'    Friend Const HDM_SETHOTDIVIDER As Integer = &H1200 + 19
'    Friend Const HDN_ITEMCHANGINGA As Integer = 0 - 300 - 0
'    Friend Const HDN_ITEMCHANGINGW As Integer = 0 - 300 - 20
'    Friend Const HDN_ITEMCHANGEDA As Integer = 0 - 300 - 1
'    Friend Const HDN_ITEMCHANGEDW As Integer = 0 - 300 - 21
'    Friend Const HDN_ITEMCLICKA As Integer = 0 - 300 - 2
'    Friend Const HDN_ITEMCLICKW As Integer = 0 - 300 - 22
'    Friend Const HDN_ITEMDBLCLICKA As Integer = 0 - 300 - 3
'    Friend Const HDN_ITEMDBLCLICKW As Integer = 0 - 300 - 23
'    Friend Const HDN_DIVIDERDBLCLICKA As Integer = 0 - 300 - 5
'    Friend Const HDN_DIVIDERDBLCLICKW As Integer = 0 - 300 - 25
'    Friend Const HDN_BEGINTRACKA As Integer = 0 - 300 - 6
'    Friend Const HDN_BEGINTRACKW As Integer = 0 - 300 - 26
'    Friend Const HDN_ENDTRACKA As Integer = 0 - 300 - 7
'    Friend Const HDN_ENDTRACKW As Integer = 0 - 300 - 27
'    Friend Const HDN_TRACKA As Integer = 0 - 300 - 8
'    Friend Const HDN_TRACKW As Integer = 0 - 300 - 28
'    Friend Const HDN_GETDISPINFOA As Integer = 0 - 300 - 9
'    Friend Const HDN_GETDISPINFOW As Integer = 0 - 300 - 29
'    Friend Const HDN_BEGINDRAG As Integer = 0 - 300 - 10
'    Friend Const HDN_ENDDRAG As Integer = 0 - 300 - 11
'    Friend Const HIST_BACK As Integer = 0
'    Friend Const HIST_FORWARD As Integer = 1
'    Friend Const HIST_FAVORITES As Integer = 2
'    Friend Const HIST_ADDTOFAVORITES As Integer = 3
'    Friend Const HIST_VIEWTREE As Integer = 4
'    Friend Const HOTKEYF_SHIFT As Integer = &H1
'    Friend Const HOTKEYF_CONTROL As Integer = &H2
'    Friend Const HOTKEYF_ALT As Integer = &H4
'    Friend Const HOTKEYF_EXT As Integer = &H8
'    Friend Const HKCOMB_NONE As Integer = &H1
'    Friend Const HKCOMB_S As Integer = &H2
'    Friend Const HKCOMB_C As Integer = &H4
'    Friend Const HKCOMB_A As Integer = &H8
'    Friend Const HKCOMB_SC As Integer = &H10
'    Friend Const HKCOMB_SA As Integer = &H20
'    Friend Const HKCOMB_CA As Integer = &H40
'    Friend Const HKCOMB_SCA As Integer = &H80
'    Friend Const HKM_SETHOTKEY As Integer = &H400 + 1
'    Friend Const HKM_GETHOTKEY As Integer = &H400 + 2
'    Friend Const HKM_SETRULES As Integer = &H400 + 3
'    Friend Const HWND_TOP As Integer = 0
'    Friend Const HWND_BOTTOM As Integer = 1
'    Friend Const HWND_TOPMOST As Integer = - 1
'    Friend Const HWND_NOTOPMOST As Integer = - 2
'    Friend Const HICF_OTHER As Integer = &H0
'    Friend Const HICF_MOUSE As Integer = &H1
'    Friend Const HICF_ARROWKEYS As Integer = &H2
'    Friend Const HICF_ACCELERATOR As Integer = &H4
'    Friend Const HICF_DUPACCEL As Integer = &H8
'    Friend Const HICF_ENTERING As Integer = &H10
'    Friend Const HICF_LEAVING As Integer = &H20
'    Friend Const HICF_RESELECT As Integer = &H40
'    Friend Const HICF_TOGGLEDROPDOWN As Integer = &H100
'    Friend Const HINST_COMMCTRL As Integer = - 1
    
    
    
'    Friend Const ITALIC_FONTTYPE As Integer = &H200
'    Friend Const ico1 As Integer = &H43C
'    Friend Const ico2 As Integer = &H43D
'    Friend Const ico3 As Integer = &H43E
'    Friend Const ico4 As Integer = &H43F
'    Friend Const IMC_GETCANDIDATEPOS As Integer = &H7
'    Friend Const IMC_SETCANDIDATEPOS As Integer = &H8
'    Friend Const IMC_GETCOMPOSITIONFONT As Integer = &H9
'    Friend Const IMC_SETCOMPOSITIONFONT As Integer = &HA
'    Friend Const IMC_GETCOMPOSITIONWINDOW As Integer = &HB
'    Friend Const IMC_SETCOMPOSITIONWINDOW As Integer = &HC
'    Friend Const IMC_GETSTATUSWINDOWPOS As Integer = &HF
'    Friend Const IMC_SETSTATUSWINDOWPOS As Integer = &H10
'    Friend Const IMC_CLOSESTATUSWINDOW As Integer = &H21
'    Friend Const IMC_OPENSTATUSWINDOW As Integer = &H22
'    Friend Const ISC_SHOWUICANDIDATEWINDOW As Integer = &H1
'    Friend Const ISC_SHOWUICOMPOSITIONWINDOW As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        ISC_SHOWUICANDIDATEWINDOW = 0x00000001,
'    '        ISC_SHOWUICOMPOSITIONWINDOW = unchecked((int)0x80000000),
'    '---------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const ISC_SHOWUIGUIDELINE As Integer = &H40000000
'    Friend Const ISC_SHOWUIALLCANDIDATEWINDOW As Integer = &HF
'    Friend Const ISC_SHOWUIALL As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        ISC_SHOWUIALLCANDIDATEWINDOW = 0x0000000F,
'    '        ISC_SHOWUIALL = unchecked((int)0xC000000F),
'    '-------------------------^--- GenCode(token): unexpected token type
'    Friend Const IME_CHOTKEY_IME_NONIME_TOGGLE As Integer = &H10
'    Friend Const IME_CHOTKEY_SHAPE_TOGGLE As Integer = &H11
'    Friend Const IME_CHOTKEY_SYMBOL_TOGGLE As Integer = &H12
'    Friend Const IME_JHOTKEY_CLOSE_OPEN As Integer = &H30
'    Friend Const IME_KHOTKEY_SHAPE_TOGGLE As Integer = &H50
'    Friend Const IME_KHOTKEY_HANJACONVERT As Integer = &H51
'    Friend Const IME_KHOTKEY_ENGLISH As Integer = &H52
'    Friend Const IME_THOTKEY_IME_NONIME_TOGGLE As Integer = &H70
'    Friend Const IME_THOTKEY_SHAPE_TOGGLE As Integer = &H71
'    Friend Const IME_THOTKEY_SYMBOL_TOGGLE As Integer = &H72
'    Friend Const IME_HOTKEY_DSWITCH_FIRST As Integer = &H100
'    Friend Const IME_HOTKEY_DSWITCH_LAST As Integer = &H11F
'    Friend Const IME_HOTKEY_PRIVATE_FIRST As Integer = &H200
'    Friend Const IME_ITHOTKEY_RESEND_RESULTSTR As Integer = &H200
'    Friend Const IME_ITHOTKEY_PREVIOUS_COMPOSITION As Integer = &H201
'    Friend Const IME_ITHOTKEY_UISTYLE_TOGGLE As Integer = &H202
'    Friend Const IME_HOTKEY_PRIVATE_LAST As Integer = &H21F
'    Friend Const IMEVER_0310 As Integer = &H3000A
'    Friend Const IMEVER_0400 As Integer = &H40000
'    Friend Const IME_PROP_AT_CARET As Integer = &H10000
'    Friend Const IME_PROP_SPECIAL_UI As Integer = &H20000
'    Friend Const IME_PROP_CANDLIST_START_FROM_1 As Integer = &H40000
'    Friend Const IME_PROP_UNICODE As Integer = &H80000
'    Friend Const IGP_PROPERTY As Integer = &H4
'    Friend Const IGP_CONVERSION As Integer = &H8
'    Friend Const IGP_SENTENCE As Integer = &HC
'    Friend Const IGP_UI As Integer = &H10
'    Friend Const IGP_SETCOMPSTR As Integer = &H14
'    Friend Const IGP_SELECT As Integer = &H18
'    Friend Const IME_CMODE_ALPHANUMERIC As Integer = &H0
'    Friend Const IME_CMODE_NATIVE As Integer = &H1
'    Friend Const IME_CMODE_CHINESE As Integer = &H1
'    Friend Const IME_CMODE_HANGEUL As Integer = &H1
'    Friend Const IME_CMODE_HANGUL As Integer = &H1
'    Friend Const IME_CMODE_JAPANESE As Integer = &H1
'    Friend Const IME_CMODE_KATAKANA As Integer = &H2
'    Friend Const IME_CMODE_LANGUAGE As Integer = &H3
'    Friend Const IME_CMODE_FULLSHAPE As Integer = &H8
'    Friend Const IME_CMODE_ROMAN As Integer = &H10
'    Friend Const IME_CMODE_CHARCODE As Integer = &H20
'    Friend Const IME_CMODE_HANJACONVERT As Integer = &H40
'    Friend Const IME_CMODE_SOFTKBD As Integer = &H80
'    Friend Const IME_CMODE_NOCONVERSION As Integer = &H100
'    Friend Const IME_CMODE_EUDC As Integer = &H200
'    Friend Const IME_CMODE_SYMBOL As Integer = &H400
'    Friend Const IME_SMODE_NONE As Integer = &H0
'    Friend Const IME_SMODE_PLAURALCLAUSE As Integer = &H1
'    Friend Const IME_SMODE_SINGLECONVERT As Integer = &H2
'    Friend Const IME_SMODE_AUTOMATIC As Integer = &H4
'    Friend Const IME_SMODE_PHRASEPREDICT As Integer = &H8
'    Friend Const IME_CAND_UNKNOWN As Integer = &H0
'    Friend Const IME_CAND_READ As Integer = &H1
'    Friend Const IME_CAND_CODE As Integer = &H2
'    Friend Const IME_CAND_MEANING As Integer = &H3
'    Friend Const IME_CAND_RADICAL As Integer = &H4
'    Friend Const IME_CAND_STROKE As Integer = &H5
'    Friend Const IMN_CLOSESTATUSWINDOW As Integer = &H1
'    Friend Const IMN_OPENSTATUSWINDOW As Integer = &H2
'    Friend Const IMN_CHANGECANDIDATE As Integer = &H3
'    Friend Const IMN_CLOSECANDIDATE As Integer = &H4
'    Friend Const IMN_OPENCANDIDATE As Integer = &H5
'    Friend Const IMN_SETCONVERSIONMODE As Integer = &H6
'    Friend Const IMN_SETSENTENCEMODE As Integer = &H7
'    Friend Const IMN_SETOPENSTATUS As Integer = &H8
'    Friend Const IMN_SETCANDIDATEPOS As Integer = &H9
'    Friend Const IMN_SETCOMPOSITIONFONT As Integer = &HA
'    Friend Const IMN_SETCOMPOSITIONWINDOW As Integer = &HB
'    Friend Const IMN_SETSTATUSWINDOWPOS As Integer = &HC
'    Friend Const IMN_GUIDELINE As Integer = &HD
'    Friend Const IMN_PRIVATE As Integer = &HE
'    Friend Const IMM_ERROR_NODATA As Integer = - 1
'    Friend Const IMM_ERROR_GENERAL As Integer = - 2
'    Friend Const IME_CONFIG_GENERAL As Integer = 1
'    Friend Const IME_CONFIG_REGISTERWORD As Integer = 2
'    Friend Const IME_CONFIG_SELECTDICTIONARY As Integer = 3
'    Friend Const IME_ESC_QUERY_SUPPORT As Integer = &H3
'    Friend Const IME_ESC_RESERVED_FIRST As Integer = &H4
'    Friend Const IME_ESC_RESERVED_LAST As Integer = &H7FF
'    Friend Const IME_ESC_PRIVATE_FIRST As Integer = &H800
'    Friend Const IME_ESC_PRIVATE_LAST As Integer = &HFFF
'    Friend Const IME_ESC_SEQUENCE_TO_INTERNAL As Integer = &H1001
'    Friend Const IME_ESC_GET_EUDC_DICTIONARY As Integer = &H1003
'    Friend Const IME_ESC_SET_EUDC_DICTIONARY As Integer = &H1004
'    Friend Const IME_ESC_MAX_KEY As Integer = &H1005
'    Friend Const IME_ESC_IME_NAME As Integer = &H1006
'    Friend Const IME_ESC_SYNC_HOTKEY As Integer = &H1007
'    Friend Const IME_ESC_HANJA_MODE As Integer = &H1008
'    Friend Const IME_ESC_AUTOMATA As Integer = &H1009
'    Friend Const IME_ESC_PRIVATE_HOTKEY As Integer = &H100A
'    Friend Const IME_REGWORD_STYLE_EUDC As Integer = &H1
'    Friend Const IME_REGWORD_STYLE_USER_FIRST As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        IME_REGWORD_STYLE_EUDC = 0x00000001,
'    '        IME_REGWORD_STYLE_USER_FIRST = unchecked((int)0x80000000),
'    '----------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const IME_REGWORD_STYLE_USER_LAST As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        IME_REGWORD_STYLE_USER_FIRST = unchecked((int)0x80000000),
'    '        IME_REGWORD_STYLE_USER_LAST = unchecked((int)0xFFFFFFFF),
'    '---------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const IDLFLAG_NONE As Integer = 0
'    Friend Const IDLFLAG_FIN As Integer = &H1
'    Friend Const IDLFLAG_FOUT As Integer = &H2
'    Friend Const IDLFLAG_FLCID As Integer = &H4
'    Friend Const IDLFLAG_FRETVAL As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                    IDLFLAG_FLCID = ((0x4)),
'    '                                                                    IDLFLAG_FRETVAL = ((unchecked((int)0x8))),
'    '-----------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const IMPLTYPEFLAG_FDEFAULT As Integer = &H1
'    Friend Const IMPLTYPEFLAG_FSOURCE As Integer = &H2
'    Friend Const IMPLTYPEFLAG_FRESTRICTED As Integer = &H4
'    Friend Const IMPLTYPEFLAG_FDEFAULTVTABLE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                     IMPLTYPEFLAG_FRESTRICTED = (0x4),
'    '                                                                                                                                                                IMPLTYPEFLAG_FDEFAULTVTABLE = (unchecked((int)0x8)),
'    '------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const ID_DEFAULTINST As Integer = - 2
'    Friend Const ID_PSRESTARTWINDOWS As Integer = &H2
'    Friend Const ID_PSREBOOTSYSTEM As Integer = &H2 Or &H1
'    Friend Const IDLE_PRIORITY_CLASS As Integer = &H40
'    Friend Const IGNORE As Integer = 0
'    Friend Const INFINITE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        IGNORE = 0,
'    '        INFINITE = unchecked((int)0xFFFFFFFF),
'    '--------------------^--- GenCode(token): unexpected token type
'    Friend Const IE_BADID As Integer = - 1
'    Friend Const IE_OPEN As Integer = - 2
'    Friend Const IE_NOPEN As Integer = - 3
'    Friend Const IE_MEMORY As Integer = - 4
'    Friend Const IE_DEFAULT As Integer = - 5
'    Friend Const IE_HARDWARE As Integer = - 10
'    Friend Const IE_BYTESIZE As Integer = - 11
'    Friend Const IE_BAUDRATE As Integer = - 12
'    Friend Const INPLACE_E_NOTUNDOABLE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                             IE_BAUDRATE = (-12),
'    '                                                                                                           INPLACE_E_NOTUNDOABLE = unchecked((int)0x800401A0),
'    '------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const INPLACE_E_NOTOOLSPACE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                           INPLACE_E_NOTUNDOABLE = unchecked((int)0x800401A0),
'    '        INPLACE_E_NOTOOLSPACE = unchecked((int)0x800401A1),
'    '---------------------------------^--- GenCode(token): unexpected token type
'    Friend Const INPLACE_E_FIRST As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        INPLACE_E_NOTOOLSPACE = unchecked((int)0x800401A1),
'    '        INPLACE_E_FIRST = unchecked((int)0x800401A0),
'    '---------------------------^--- GenCode(token): unexpected token type
'    Friend Const INPLACE_E_LAST As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        INPLACE_E_FIRST = unchecked((int)0x800401A0),
'    '        INPLACE_E_LAST = unchecked((int)0x800401AF),
'    '--------------------------^--- GenCode(token): unexpected token type
'    Friend Const INPLACE_S_FIRST As Integer = &H401A0
'    Friend Const INPLACE_S_LAST As Integer = &H401AF
'    Friend Const INPLACE_S_TRUNCATED As Integer = &H401A0
'    Friend Const INPUTLANGCHANGE_SYSCHARSET As Integer = &H1
'    Friend Const INPUTLANGCHANGE_FORWARD As Integer = &H2
'    Friend Const INPUTLANGCHANGE_BACKWARD As Integer = &H4
'    Friend Const ILLUMINANT_DEVICE_DEFAULT As Integer = 0
'    Friend Const ILLUMINANT_A As Integer = 1
'    Friend Const ILLUMINANT_B As Integer = 2
'    Friend Const ILLUMINANT_C As Integer = 3
'    Friend Const ILLUMINANT_D50 As Integer = 4
'    Friend Const ILLUMINANT_D55 As Integer = 5
'    Friend Const ILLUMINANT_D65 As Integer = 6
'    Friend Const ILLUMINANT_D75 As Integer = 7
'    Friend Const ILLUMINANT_F2 As Integer = 8
'    Friend Const ILLUMINANT_MAX_INDEX As Integer = 8
'    Friend Const ILLUMINANT_TUNGSTEN As Integer = 1
'    Friend Const ILLUMINANT_DAYLIGHT As Integer = 3
'    Friend Const ILLUMINANT_FLUORESCENT As Integer = 8
'    Friend Const ILLUMINANT_NTSC As Integer = 3
'    Friend Const ICM_OFF As Integer = 1
'    Friend Const ICM_ON As Integer = 2
'    Friend Const ICM_QUERY As Integer = 3
'    Friend Const IO_COMPLETION_MODIFY_STATE As Integer = &H2
'    Friend Const INHERIT_ONLY_ACE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        IO_COMPLETION_MODIFY_STATE = 0x0002,
'    '        INHERIT_ONLY_ACE = (unchecked((int)0x8)),
'    '-----------------------------^--- GenCode(token): unexpected token type
'    Friend Const IMAGE_DOS_SIGNATURE As Integer = &H5A4D
'    Friend Const IMAGE_OS2_SIGNATURE As Integer = &H454E
'    Friend Const IMAGE_OS2_SIGNATURE_LE As Integer = &H454C
'    Friend Const IMAGE_VXD_SIGNATURE As Integer = &H454C
'    Friend Const IMAGE_NT_SIGNATURE As Integer = &H4550
'    Friend Const IMAGE_SIZEOF_FILE_HEADER As Integer = 20
'    Friend Const IMAGE_FILE_RELOCS_STRIPPED As Integer = &H1
'    Friend Const IMAGE_FILE_LINE_NUMS_STRIPPED As Integer = &H4
'    Friend Const IMAGE_FILE_LOCAL_SYMS_STRIPPED As Integer = &H8
'    Friend Const IMAGE_FILE_AGGRESIVE_WS_TRIM As Integer = &H10
'    Friend Const IMAGE_FILE_BYTES_REVERSED_LO As Integer = &H80
'    Friend Const IMAGE_FILE_32BIT_MACHINE As Integer = &H100
'    Friend Const IMAGE_FILE_DEBUG_STRIPPED As Integer = &H200
'    Friend Const IMAGE_FILE_REMOVABLE_RUN_FROM_SWAP As Integer = &H400
'    Friend Const IMAGE_FILE_NET_RUN_FROM_SWAP As Integer = &H800
'    Friend Const IMAGE_FILE_SYSTEM As Integer = &H1000
'    Friend Const IMAGE_FILE_DLL As Integer = &H2000
'    Friend Const IMAGE_FILE_UP_SYSTEM_ONLY As Integer = &H4000
'    Friend Const IMAGE_FILE_BYTES_REVERSED_HI As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        IMAGE_FILE_UP_SYSTEM_ONLY = 0x4000,
'    '        IMAGE_FILE_BYTES_REVERSED_HI = unchecked((int)0x8000),
'    '----------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const IMAGE_FILE_MACHINE_UNKNOWN As Integer = 0
'    Friend Const IMAGE_FILE_MACHINE_I386 As Integer = &H14C
'    Friend Const IMAGE_FILE_MACHINE_R3000 As Integer = &H162
'    Friend Const IMAGE_FILE_MACHINE_R4000 As Integer = &H166
'    Friend Const IMAGE_FILE_MACHINE_R10000 As Integer = &H168
'    Friend Const IMAGE_FILE_MACHINE_ALPHA As Integer = &H184
'    Friend Const IMAGE_FILE_MACHINE_POWERPC As Integer = &H1F0
'    Friend Const IMAGE_NUMBEROF_DIRECTORY_ENTRIES As Integer = 16
'    Friend Const IMAGE_SIZEOF_ROM_OPTIONAL_HEADER As Integer = 56
'    Friend Const IMAGE_SIZEOF_STD_OPTIONAL_HEADER As Integer = 28
'    Friend Const IMAGE_SIZEOF_NT_OPTIONAL_HEADER As Integer = 224
'    Friend Const IMAGE_NT_OPTIONAL_HDR_MAGIC As Integer = &H10B
'    Friend Const IMAGE_ROM_OPTIONAL_HDR_MAGIC As Integer = &H107
'    Friend Const IMAGE_SUBSYSTEM_UNKNOWN As Integer = 0
'    Friend Const IMAGE_SUBSYSTEM_NATIVE As Integer = 1
'    Friend Const IMAGE_SUBSYSTEM_WINDOWS_GUI As Integer = 2
'    Friend Const IMAGE_SUBSYSTEM_WINDOWS_CUI As Integer = 3
'    Friend Const IMAGE_SUBSYSTEM_OS2_CUI As Integer = 5
'    Friend Const IMAGE_SUBSYSTEM_POSIX_CUI As Integer = 7
'    Friend Const IMAGE_SUBSYSTEM_RESERVED8 As Integer = 8
'    Friend Const IMAGE_DIRECTORY_ENTRY_EXPORT As Integer = 0
'    Friend Const IMAGE_DIRECTORY_ENTRY_IMPORT As Integer = 1
'    Friend Const IMAGE_DIRECTORY_ENTRY_RESOURCE As Integer = 2
'    Friend Const IMAGE_DIRECTORY_ENTRY_EXCEPTION As Integer = 3
'    Friend Const IMAGE_DIRECTORY_ENTRY_SECURITY As Integer = 4
'    Friend Const IMAGE_DIRECTORY_ENTRY_BASERELOC As Integer = 5
'    Friend Const IMAGE_DIRECTORY_ENTRY_DEBUG As Integer = 6
'    Friend Const IMAGE_DIRECTORY_ENTRY_COPYRIGHT As Integer = 7
'    Friend Const IMAGE_DIRECTORY_ENTRY_GLOBALPTR As Integer = 8
'    Friend Const IMAGE_DIRECTORY_ENTRY_TLS As Integer = 9
'    Friend Const IMAGE_DIRECTORY_ENTRY_LOAD_CONFIG As Integer = 10
'    Friend Const IMAGE_DIRECTORY_ENTRY_BOUND_IMPORT As Integer = 11
'    Friend Const IMAGE_DIRECTORY_ENTRY_IAT As Integer = 12
'    Friend Const IMAGE_SIZEOF_SHORT_NAME As Integer = 8
'    Friend Const IMAGE_SIZEOF_SECTION_HEADER As Integer = 40
'    Friend Const IMAGE_SCN_TYPE_NO_PAD As Integer = &H8
'    Friend Const IMAGE_SCN_CNT_CODE As Integer = &H20
'    Friend Const IMAGE_SCN_CNT_INITIALIZED_DATA As Integer = &H40
'    Friend Const IMAGE_SCN_CNT_UNINITIALIZED_DATA As Integer = &H80
'    Friend Const IMAGE_SCN_LNK_OTHER As Integer = &H100
'    Friend Const IMAGE_SCN_LNK_INFO As Integer = &H200
'    Friend Const IMAGE_SCN_LNK_REMOVE As Integer = &H800
'    Friend Const IMAGE_SCN_LNK_COMDAT As Integer = &H1000
'    Friend Const IMAGE_SCN_MEM_FARDATA As Integer = &H8000
'    Friend Const IMAGE_SCN_MEM_PURGEABLE As Integer = &H20000
'    Friend Const IMAGE_SCN_MEM_16BIT As Integer = &H20000
'    Friend Const IMAGE_SCN_MEM_LOCKED As Integer = &H40000
'    Friend Const IMAGE_SCN_MEM_PRELOAD As Integer = &H80000
'    Friend Const IMAGE_SCN_ALIGN_1BYTES As Integer = &H100000
'    Friend Const IMAGE_SCN_ALIGN_2BYTES As Integer = &H200000
'    Friend Const IMAGE_SCN_ALIGN_4BYTES As Integer = &H300000
'    Friend Const IMAGE_SCN_ALIGN_8BYTES As Integer = &H400000
'    Friend Const IMAGE_SCN_ALIGN_16BYTES As Integer = &H500000
'    Friend Const IMAGE_SCN_ALIGN_32BYTES As Integer = &H600000
'    Friend Const IMAGE_SCN_ALIGN_64BYTES As Integer = &H700000
'    Friend Const IMAGE_SCN_LNK_NRELOC_OVFL As Integer = &H1000000
'    Friend Const IMAGE_SCN_MEM_DISCARDABLE As Integer = &H2000000
'    Friend Const IMAGE_SCN_MEM_NOT_CACHED As Integer = &H4000000
'    Friend Const IMAGE_SCN_MEM_NOT_PAGED As Integer = &H8000000
'    Friend Const IMAGE_SCN_MEM_SHARED As Integer = &H10000000
'    Friend Const IMAGE_SCN_MEM_EXECUTE As Integer = &H20000000
'    Friend Const IMAGE_SCN_MEM_READ As Integer = &H40000000
'    Friend Const IMAGE_SCN_MEM_WRITE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        IMAGE_SCN_MEM_READ = 0x40000000,
'    '        IMAGE_SCN_MEM_WRITE = unchecked((int)0x80000000),
'    '-------------------------------^--- GenCode(token): unexpected token type
'    Friend Const IMAGE_SCN_SCALE_INDEX As Integer = &H1
'    Friend Const IMAGE_SIZEOF_SYMBOL As Integer = 18
'    Friend Const IMAGE_SYM_TYPE_NULL As Integer = &H0
'    Friend Const IMAGE_SYM_TYPE_VOID As Integer = &H1
'    Friend Const IMAGE_SYM_TYPE_CHAR As Integer = &H2
'    Friend Const IMAGE_SYM_TYPE_SHORT As Integer = &H3
'    Friend Const IMAGE_SYM_TYPE_INT As Integer = &H4
'    Friend Const IMAGE_SYM_TYPE_LONG As Integer = &H5
'    Friend Const IMAGE_SYM_TYPE_FLOAT As Integer = &H6
'    Friend Const IMAGE_SYM_TYPE_DOUBLE As Integer = &H7
'    Friend Const IMAGE_SYM_TYPE_STRUCT As Integer = &H8
'    Friend Const IMAGE_SYM_TYPE_UNION As Integer = &H9
'    Friend Const IMAGE_SYM_TYPE_ENUM As Integer = &HA
'    Friend Const IMAGE_SYM_TYPE_MOE As Integer = &HB
'    Friend Const IMAGE_SYM_TYPE_BYTE As Integer = &HC
'    Friend Const IMAGE_SYM_TYPE_WORD As Integer = &HD
'    Friend Const IMAGE_SYM_TYPE_UINT As Integer = &HE
'    Friend Const IMAGE_SYM_TYPE_DWORD As Integer = &HF
'    Friend Const IMAGE_SYM_TYPE_PCODE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        IMAGE_SYM_TYPE_DWORD = 0x000F,
'    '        IMAGE_SYM_TYPE_PCODE = unchecked((int)0x8000),
'    '--------------------------------^--- GenCode(token): unexpected token type
'    Friend Const IMAGE_SYM_DTYPE_NULL As Integer = 0
'    Friend Const IMAGE_SYM_DTYPE_POINTER As Integer = 1
'    Friend Const IMAGE_SYM_DTYPE_FUNCTION As Integer = 2
'    Friend Const IMAGE_SYM_DTYPE_ARRAY As Integer = 3
'    Friend Const IMAGE_SYM_CLASS_NULL As Integer = &H0
'    Friend Const IMAGE_SYM_CLASS_AUTOMATIC As Integer = &H1
'    Friend Const IMAGE_SYM_CLASS_STATIC As Integer = &H3
'    Friend Const IMAGE_SYM_CLASS_REGISTER As Integer = &H4
'    Friend Const IMAGE_SYM_CLASS_LABEL As Integer = &H6
'    Friend Const IMAGE_SYM_CLASS_UNDEFINED_LABEL As Integer = &H7
'    Friend Const IMAGE_SYM_CLASS_MEMBER_OF_STRUCT As Integer = &H8
'    Friend Const IMAGE_SYM_CLASS_ARGUMENT As Integer = &H9
'    Friend Const IMAGE_SYM_CLASS_STRUCT_TAG As Integer = &HA
'    Friend Const IMAGE_SYM_CLASS_MEMBER_OF_UNION As Integer = &HB
'    Friend Const IMAGE_SYM_CLASS_UNION_TAG As Integer = &HC
'    Friend Const IMAGE_SYM_CLASS_TYPE_DEFINITION As Integer = &HD
'    Friend Const IMAGE_SYM_CLASS_UNDEFINED_STATIC As Integer = &HE
'    Friend Const IMAGE_SYM_CLASS_ENUM_TAG As Integer = &HF
'    Friend Const IMAGE_SYM_CLASS_MEMBER_OF_ENUM As Integer = &H10
'    Friend Const IMAGE_SYM_CLASS_REGISTER_PARAM As Integer = &H11
'    Friend Const IMAGE_SYM_CLASS_BIT_FIELD As Integer = &H12
'    Friend Const IMAGE_SYM_CLASS_BLOCK As Integer = &H64
'    Friend Const IMAGE_SYM_CLASS_FUNCTION As Integer = &H65
'    Friend Const IMAGE_SYM_CLASS_END_OF_STRUCT As Integer = &H66
'    Friend Const IMAGE_SYM_CLASS_FILE As Integer = &H67
'    Friend Const IMAGE_SYM_CLASS_SECTION As Integer = &H68
'    Friend Const IMAGE_SIZEOF_AUX_SYMBOL As Integer = 18
'    Friend Const IMAGE_COMDAT_SELECT_NODUPLICATES As Integer = 1
'    Friend Const IMAGE_COMDAT_SELECT_ANY As Integer = 2
'    Friend Const IMAGE_COMDAT_SELECT_SAME_SIZE As Integer = 3
'    Friend Const IMAGE_COMDAT_SELECT_EXACT_MATCH As Integer = 4
'    Friend Const IMAGE_COMDAT_SELECT_ASSOCIATIVE As Integer = 5
'    Friend Const IMAGE_COMDAT_SELECT_LARGEST As Integer = 6
'    Friend Const IMAGE_COMDAT_SELECT_NEWEST As Integer = 7
'    Friend Const IMAGE_SIZEOF_RELOCATION As Integer = 10
'    Friend Const IMAGE_REL_I386_ABSOLUTE As Integer = &H0
'    Friend Const IMAGE_REL_I386_DIR16 As Integer = &H1
'    Friend Const IMAGE_REL_I386_REL16 As Integer = &H2
'    Friend Const IMAGE_REL_I386_DIR32 As Integer = &H6
'    Friend Const IMAGE_REL_I386_DIR32NB As Integer = &H7
'    Friend Const IMAGE_REL_I386_SEG12 As Integer = &H9
'    Friend Const IMAGE_REL_I386_SECTION As Integer = &HA
'    Friend Const IMAGE_REL_I386_SECREL As Integer = &HB
'    Friend Const IMAGE_REL_I386_REL32 As Integer = &H14
'    Friend Const IMAGE_REL_MIPS_ABSOLUTE As Integer = &H0
'    Friend Const IMAGE_REL_MIPS_REFHALF As Integer = &H1
'    Friend Const IMAGE_REL_MIPS_REFWORD As Integer = &H2
'    Friend Const IMAGE_REL_MIPS_JMPADDR As Integer = &H3
'    Friend Const IMAGE_REL_MIPS_REFHI As Integer = &H4
'    Friend Const IMAGE_REL_MIPS_REFLO As Integer = &H5
'    Friend Const IMAGE_REL_MIPS_GPREL As Integer = &H6
'    Friend Const IMAGE_REL_MIPS_LITERAL As Integer = &H7
'    Friend Const IMAGE_REL_MIPS_SECTION As Integer = &HA
'    Friend Const IMAGE_REL_MIPS_SECREL As Integer = &HB
'    Friend Const IMAGE_REL_MIPS_SECRELLO As Integer = &HC
'    Friend Const IMAGE_REL_MIPS_SECRELHI As Integer = &HD
'    Friend Const IMAGE_REL_MIPS_REFWORDNB As Integer = &H22
'    Friend Const IMAGE_REL_MIPS_PAIR As Integer = &H25
'    Friend Const IMAGE_REL_ALPHA_ABSOLUTE As Integer = &H0
'    Friend Const IMAGE_REL_ALPHA_REFLONG As Integer = &H1
'    Friend Const IMAGE_REL_ALPHA_REFQUAD As Integer = &H2
'    Friend Const IMAGE_REL_ALPHA_GPREL32 As Integer = &H3
'    Friend Const IMAGE_REL_ALPHA_LITERAL As Integer = &H4
'    Friend Const IMAGE_REL_ALPHA_LITUSE As Integer = &H5
'    Friend Const IMAGE_REL_ALPHA_GPDISP As Integer = &H6
'    Friend Const IMAGE_REL_ALPHA_BRADDR As Integer = &H7
'    Friend Const IMAGE_REL_ALPHA_HINT As Integer = &H8
'    Friend Const IMAGE_REL_ALPHA_INLINE_REFLONG As Integer = &H9
'    Friend Const IMAGE_REL_ALPHA_REFHI As Integer = &HA
'    Friend Const IMAGE_REL_ALPHA_REFLO As Integer = &HB
'    Friend Const IMAGE_REL_ALPHA_PAIR As Integer = &HC
'    Friend Const IMAGE_REL_ALPHA_MATCH As Integer = &HD
'    Friend Const IMAGE_REL_ALPHA_SECTION As Integer = &HE
'    Friend Const IMAGE_REL_ALPHA_SECREL As Integer = &HF
'    Friend Const IMAGE_REL_ALPHA_REFLONGNB As Integer = &H10
'    Friend Const IMAGE_REL_ALPHA_SECRELLO As Integer = &H11
'    Friend Const IMAGE_REL_ALPHA_SECRELHI As Integer = &H12
'    Friend Const IMAGE_REL_PPC_ABSOLUTE As Integer = &H0
'    Friend Const IMAGE_REL_PPC_ADDR64 As Integer = &H1
'    Friend Const IMAGE_REL_PPC_ADDR32 As Integer = &H2
'    Friend Const IMAGE_REL_PPC_ADDR24 As Integer = &H3
'    Friend Const IMAGE_REL_PPC_ADDR16 As Integer = &H4
'    Friend Const IMAGE_REL_PPC_ADDR14 As Integer = &H5
'    Friend Const IMAGE_REL_PPC_REL24 As Integer = &H6
'    Friend Const IMAGE_REL_PPC_REL14 As Integer = &H7
'    Friend Const IMAGE_REL_PPC_TOCREL16 As Integer = &H8
'    Friend Const IMAGE_REL_PPC_TOCREL14 As Integer = &H9
'    Friend Const IMAGE_REL_PPC_ADDR32NB As Integer = &HA
'    Friend Const IMAGE_REL_PPC_SECREL As Integer = &HB
'    Friend Const IMAGE_REL_PPC_SECTION As Integer = &HC
'    Friend Const IMAGE_REL_PPC_IFGLUE As Integer = &HD
'    Friend Const IMAGE_REL_PPC_IMGLUE As Integer = &HE
'    Friend Const IMAGE_REL_PPC_SECREL16 As Integer = &HF
'    Friend Const IMAGE_REL_PPC_REFHI As Integer = &H10
'    Friend Const IMAGE_REL_PPC_REFLO As Integer = &H11
'    Friend Const IMAGE_REL_PPC_PAIR As Integer = &H12
'    Friend Const IMAGE_REL_PPC_SECRELLO As Integer = &H13
'    Friend Const IMAGE_REL_PPC_SECRELHI As Integer = &H14
'    Friend Const IMAGE_REL_PPC_TYPEMASK As Integer = &HFF
'    Friend Const IMAGE_REL_PPC_NEG As Integer = &H100
'    Friend Const IMAGE_REL_PPC_BRTAKEN As Integer = &H200
'    Friend Const IMAGE_REL_PPC_BRNTAKEN As Integer = &H400
'    Friend Const IMAGE_REL_PPC_TOCDEFN As Integer = &H800
'    Friend Const IMAGE_SIZEOF_LINENUMBER As Integer = 6
'    Friend Const IMAGE_SIZEOF_BASE_RELOCATION As Integer = 8
'    Friend Const IMAGE_REL_BASED_ABSOLUTE As Integer = 0
'    Friend Const IMAGE_REL_BASED_HIGH As Integer = 1
'    Friend Const IMAGE_REL_BASED_LOW As Integer = 2
'    Friend Const IMAGE_REL_BASED_HIGHLOW As Integer = 3
'    Friend Const IMAGE_REL_BASED_HIGHADJ As Integer = 4
'    Friend Const IMAGE_REL_BASED_MIPS_JMPADDR As Integer = 5
'    Friend Const IMAGE_REL_BASED_SECTION As Integer = 6
'    Friend Const IMAGE_REL_BASED_REL32 As Integer = 7
'    Friend Const IMAGE_ARCHIVE_START_SIZE As Integer = 8
'    Friend Const IMAGE_SIZEOF_ARCHIVE_MEMBER_HDR As Integer = 60
'    Friend Const IMAGE_ORDINAL_FLAG As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        IMAGE_SIZEOF_ARCHIVE_MEMBER_HDR = 60,
'    '        IMAGE_ORDINAL_FLAG = unchecked((int)0x80000000),
'    '------------------------------^--- GenCode(token): unexpected token type
'    Friend Const IMAGE_RESOURCE_NAME_IS_STRING As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        IMAGE_ORDINAL_FLAG = unchecked((int)0x80000000),
'    '        IMAGE_RESOURCE_NAME_IS_STRING = unchecked((int)0x80000000),
'    '-----------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const IMAGE_RESOURCE_DATA_IS_DIRECTORY As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        IMAGE_RESOURCE_NAME_IS_STRING = unchecked((int)0x80000000),
'    '        IMAGE_RESOURCE_DATA_IS_DIRECTORY = unchecked((int)0x80000000),
'    '--------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const IMAGE_DEBUG_TYPE_UNKNOWN As Integer = 0
'    Friend Const IMAGE_DEBUG_TYPE_COFF As Integer = 1
'    Friend Const IMAGE_DEBUG_TYPE_CODEVIEW As Integer = 2
'    Friend Const IMAGE_DEBUG_TYPE_FPO As Integer = 3
'    Friend Const IMAGE_DEBUG_TYPE_MISC As Integer = 4
'    Friend Const IMAGE_DEBUG_TYPE_EXCEPTION As Integer = 5
'    Friend Const IMAGE_DEBUG_TYPE_FIXUP As Integer = 6
'    Friend Const IMAGE_DEBUG_TYPE_OMAP_TO_SRC As Integer = 7
'    Friend Const IMAGE_DEBUG_TYPE_OMAP_FROM_SRC As Integer = 8
'    Friend Const IMAGE_DEBUG_MISC_EXENAME As Integer = 1
'    Friend Const IMAGE_SEPARATE_DEBUG_SIGNATURE As Integer = &H4944
'    Friend Const IMAGE_SEPARATE_DEBUG_FLAGS_MASK As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        IMAGE_SEPARATE_DEBUG_SIGNATURE = 0x4944,
'    '        IMAGE_SEPARATE_DEBUG_FLAGS_MASK = unchecked((int)0x8000),
'    '-------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const IMAGE_SEPARATE_DEBUG_MISMATCH As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        IMAGE_SEPARATE_DEBUG_FLAGS_MASK = unchecked((int)0x8000),
'    '        IMAGE_SEPARATE_DEBUG_MISMATCH = unchecked((int)0x8000),
'    '-----------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const IS_TEXT_UNICODE_ASCII16 As Integer = &H1
'    Friend Const IS_TEXT_UNICODE_REVERSE_ASCII16 As Integer = &H10
'    Friend Const IS_TEXT_UNICODE_STATISTICS As Integer = &H2
'    Friend Const IS_TEXT_UNICODE_REVERSE_STATISTICS As Integer = &H20
'    Friend Const IS_TEXT_UNICODE_CONTROLS As Integer = &H4
'    Friend Const IS_TEXT_UNICODE_REVERSE_CONTROLS As Integer = &H40
'    Friend Const IS_TEXT_UNICODE_SIGNATURE As Integer = &H8
'    Friend Const IS_TEXT_UNICODE_REVERSE_SIGNATURE As Integer = &H80
'    Friend Const IS_TEXT_UNICODE_ILLEGAL_CHARS As Integer = &H100
'    Friend Const IS_TEXT_UNICODE_ODD_LENGTH As Integer = &H200
'    Friend Const IS_TEXT_UNICODE_DBCS_LEADBYTE As Integer = &H400
'    Friend Const IS_TEXT_UNICODE_NULL_BYTES As Integer = &H1000
'    Friend Const IS_TEXT_UNICODE_UNICODE_MASK As Integer = &HF
'    Friend Const IS_TEXT_UNICODE_REVERSE_MASK As Integer = &HF0
'    Friend Const IS_TEXT_UNICODE_NOT_UNICODE_MASK As Integer = &HF00
'    Friend Const IS_TEXT_UNICODE_NOT_ASCII_MASK As Integer = &HF000
'    Friend Const ICON_SMALL As Integer = 0
'    Friend Const ICON_BIG As Integer = 1
'    Friend Const IDANI_OPEN As Integer = 1
'    Friend Const IDANI_CLOSE As Integer = 2
'    Friend Const IDANI_CAPTION As Integer = 3
'    Friend Const IDHOT_SNAPWINDOW As Integer = - 1
'    Friend Const IDHOT_SNAPDESKTOP As Integer = - 2
'    Friend Const IDC_ARROW As Integer = 32512
'    Friend Const IDC_IBEAM As Integer = 32513
'    Friend Const IDC_WAIT As Integer = 32514
'    Friend Const IDC_CROSS As Integer = 32515
'    Friend Const IDC_UPARROW As Integer = 32516
'    Friend Const IDC_SIZE As Integer = 32640
'    Friend Const IDC_ICON As Integer = 32641
'    Friend Const IDC_SIZENWSE As Integer = 32642
'    Friend Const IDC_SIZENESW As Integer = 32643
'    Friend Const IDC_SIZEWE As Integer = 32644
'    Friend Const IDC_SIZENS As Integer = 32645
'    Friend Const IDC_SIZEALL As Integer = 32646
'    Friend Const IDC_NO As Integer = 32648
'    Friend Const IDC_APPSTARTING As Integer = 32650
'    Friend Const IDC_HELP As Integer = 32651
'    Friend Const IMAGE_BITMAP As Integer = 0
'    Friend Const IMAGE_ICON As Integer = 1
'    Friend Const IMAGE_CURSOR As Integer = 2
'    Friend Const IMAGE_ENHMETAFILE As Integer = 3
'    Friend Const IDI_APPLICATION As Integer = 32512
'    Friend Const IDI_HAND As Integer = 32513
'    Friend Const IDI_QUESTION As Integer = 32514
'    Friend Const IDI_EXCLAMATION As Integer = 32515
'    Friend Const IDI_ASTERISK As Integer = 32516
'    Friend Const IDI_WINLOGO As Integer = 32517
'    Friend Const IDI_WARNING As Integer = 32515
'    Friend Const IDI_ERROR As Integer = 32513
'    Friend Const IDI_INFORMATION As Integer = 32516
'    Friend Const IDOK As Integer = 1
'    Friend Const IDCANCEL As Integer = 2
'    Friend Const IDABORT As Integer = 3
'    Friend Const IDRETRY As Integer = 4
'    Friend Const IDIGNORE As Integer = 5
'    Friend Const IDYES As Integer = 6
'    Friend Const IDNO As Integer = 7
'    Friend Const IDCLOSE As Integer = 8
'    Friend Const IDHELP As Integer = 9
'    Friend Const IDH_NO_HELP As Integer = 28440
'    Friend Const IDH_MISSING_CONTEXT As Integer = 28441
'    Friend Const IDH_GENERIC_HELP_BUTTON As Integer = 28442
'    Friend Const IDH_OK As Integer = 28443
'    Friend Const IDH_CANCEL As Integer = 28444
'    Friend Const IDH_HELP As Integer = 28445
'    Friend Const ICC_LISTVIEW_CLASSES As Integer = &H1
'    Friend Const ICC_TREEVIEW_CLASSES As Integer = &H2
'    Friend Const ICC_BAR_CLASSES As Integer = &H4
'    Friend Const ICC_TAB_CLASSES As Integer = &H8
'    Friend Const ICC_UPDOWN_CLASS As Integer = &H10
'    Friend Const ICC_PROGRESS_CLASS As Integer = &H20
'    Friend Const ICC_HOTKEY_CLASS As Integer = &H40
'    Friend Const ICC_ANIMATE_CLASS As Integer = &H80
'    Friend Const ICC_WIN95_CLASSES As Integer = &HFF
'    Friend Const ICC_DATE_CLASSES As Integer = &H100
'    Friend Const ICC_USEREX_CLASSES As Integer = &H200
'    Friend Const ICC_COOL_CLASSES As Integer = &H400
'    Friend Const ILC_MASK As Integer = &H1
'    Friend Const ILC_COLOR As Integer = &H0
'    Friend Const ILC_COLORDDB As Integer = &HFE
'    Friend Const ILC_COLOR4 As Integer = &H4
'    Friend Const ILC_COLOR8 As Integer = &H8
'    Friend Const ILC_COLOR16 As Integer = &H10
'    Friend Const ILC_COLOR24 As Integer = &H18
'    Friend Const ILC_COLOR32 As Integer = &H20
'    Friend Const ILC_PALETTE As Integer = &H800
'    Friend Const ILD_NORMAL As Integer = &H0
'    Friend Const ILD_TRANSPARENT As Integer = &H1
'    Friend Const ILD_MASK As Integer = &H10
'    Friend Const ILD_IMAGE As Integer = &H20
'    Friend Const ILD_ROP As Integer = &H40
'    Friend Const ILD_BLEND25 As Integer = &H2
'    Friend Const ILD_BLEND50 As Integer = &H4
'    Friend Const ILD_OVERLAYMASK As Integer = &HF00
'    Friend Const ILD_SELECTED As Integer = &H4
'    Friend Const ILD_FOCUS As Integer = &H2
'    Friend Const ILD_BLEND As Integer = &H4
'    Friend Const ILCF_MOVE As Integer = &H0
'    Friend Const ILCF_SWAP As Integer = &H1
'    Friend Const IDB_STD_SMALL_COLOR As Integer = 0
'    Friend Const IDB_STD_LARGE_COLOR As Integer = 1
'    Friend Const IDB_VIEW_SMALL_COLOR As Integer = 4
'    Friend Const IDB_VIEW_LARGE_COLOR As Integer = 5
'    Friend Const IDB_HIST_SMALL_COLOR As Integer = 8
'    Friend Const IDB_HIST_LARGE_COLOR As Integer = 9
'    Friend Const I_INDENTCALLBACK As Integer = - 1
'    Friend Const I_IMAGECALLBACK As Integer = - 1
'    Friend Const I_CHILDRENCALLBACK As Integer = - 1
'    Friend Const IO_COMPLETION_ALL_ACCESS As Integer = &HF0000 Or &H100000 Or &H3
'    Friend Const INVALID_HANDLE_VALUE As Integer = - 1
'    Friend Const IPN_FIRST As Integer = 0 - 860
'    Friend Const IPN_LAST As Integer = 0 - 879
    
    
'    Friend Const JOYERR_BASE As Integer = 160
'    Friend Const JOYERR_NOERROR As Integer = 0
'    Friend Const JOYERR_PARMS As Integer = 160 + 5
'    Friend Const JOYERR_NOCANDO As Integer = 160 + 6
'    Friend Const JOYERR_UNPLUGGED As Integer = 160 + 7
'    Friend Const JOY_BUTTON1 As Integer = &H1
'    Friend Const JOY_BUTTON2 As Integer = &H2
'    Friend Const JOY_BUTTON3 As Integer = &H4
'    Friend Const JOY_BUTTON4 As Integer = &H8
'    Friend Const JOY_BUTTON1CHG As Integer = &H100
'    Friend Const JOY_BUTTON2CHG As Integer = &H200
'    Friend Const JOY_BUTTON3CHG As Integer = &H400
'    Friend Const JOY_BUTTON4CHG As Integer = &H800
'    Friend Const JOY_BUTTON5 As Integer = &H10
'    Friend Const JOY_BUTTON6 As Integer = &H20
'    Friend Const JOY_BUTTON7 As Integer = &H40
'    Friend Const JOY_BUTTON8 As Integer = &H80
'    Friend Const JOY_BUTTON9 As Integer = &H100
'    Friend Const JOY_BUTTON10 As Integer = &H200
'    Friend Const JOY_BUTTON11 As Integer = &H400
'    Friend Const JOY_BUTTON12 As Integer = &H800
'    Friend Const JOY_BUTTON13 As Integer = &H1000
'    Friend Const JOY_BUTTON14 As Integer = &H2000
'    Friend Const JOY_BUTTON15 As Integer = &H4000
'    Friend Const JOY_BUTTON16 As Integer = &H8000
'    Friend Const JOY_BUTTON17 As Integer = &H10000
'    Friend Const JOY_BUTTON18 As Integer = &H20000
'    Friend Const JOY_BUTTON19 As Integer = &H40000
'    Friend Const JOY_BUTTON20 As Integer = &H80000
'    Friend Const JOY_BUTTON21 As Integer = &H100000
'    Friend Const JOY_BUTTON22 As Integer = &H200000
'    Friend Const JOY_BUTTON23 As Integer = &H400000
'    Friend Const JOY_BUTTON24 As Integer = &H800000
'    Friend Const JOY_BUTTON25 As Integer = &H1000000
'    Friend Const JOY_BUTTON26 As Integer = &H2000000
'    Friend Const JOY_BUTTON27 As Integer = &H4000000
'    Friend Const JOY_BUTTON28 As Integer = &H8000000
'    Friend Const JOY_BUTTON29 As Integer = &H10000000
'    Friend Const JOY_BUTTON30 As Integer = &H20000000
'    Friend Const JOY_BUTTON31 As Integer = &H40000000
'    Friend Const JOY_BUTTON32 As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        JOY_BUTTON31 = 0x40000000,
'    '        JOY_BUTTON32 = unchecked((int)0x80000000),
'    '------------------------^--- GenCode(token): unexpected token type
'    Friend Const JOY_POVFORWARD As Integer = 0
'    Friend Const JOY_POVRIGHT As Integer = 9000
'    Friend Const JOY_POVBACKWARD As Integer = 18000
'    Friend Const JOY_POVLEFT As Integer = 27000
'    Friend Const JOY_RETURNX As Integer = &H1
'    Friend Const JOY_RETURNY As Integer = &H2
'    Friend Const JOY_RETURNZ As Integer = &H4
'    Friend Const JOY_RETURNR As Integer = &H8
'    Friend Const JOY_RETURNU As Integer = &H10
'    Friend Const JOY_RETURNV As Integer = &H20
'    Friend Const JOY_RETURNPOV As Integer = &H40
'    Friend Const JOY_RETURNBUTTONS As Integer = &H80
'    Friend Const JOY_RETURNRAWDATA As Integer = &H100
'    Friend Const JOY_RETURNPOVCTS As Integer = &H200
'    Friend Const JOY_RETURNCENTERED As Integer = &H400
'    Friend Const JOY_USEDEADZONE As Integer = &H800
'    Friend Const JOY_CAL_READALWAYS As Integer = &H10000
'    Friend Const JOY_CAL_READXYONLY As Integer = &H20000
'    Friend Const JOY_CAL_READ3 As Integer = &H40000
'    Friend Const JOY_CAL_READ4 As Integer = &H80000
'    Friend Const JOY_CAL_READXONLY As Integer = &H100000
'    Friend Const JOY_CAL_READYONLY As Integer = &H200000
'    Friend Const JOY_CAL_READ5 As Integer = &H400000
'    Friend Const JOY_CAL_READ6 As Integer = &H800000
'    Friend Const JOY_CAL_READZONLY As Integer = &H1000000
'    Friend Const JOY_CAL_READRONLY As Integer = &H2000000
'    Friend Const JOY_CAL_READUONLY As Integer = &H4000000
'    Friend Const JOY_CAL_READVONLY As Integer = &H8000000
'    Friend Const JOYSTICKID1 As Integer = 0
'    Friend Const JOYSTICKID2 As Integer = 1
'    Friend Const JOYCAPS_HASZ As Integer = &H1
'    Friend Const JOYCAPS_HASR As Integer = &H2
'    Friend Const JOYCAPS_HASU As Integer = &H4
'    Friend Const JOYCAPS_HASV As Integer = &H8
'    Friend Const JOYCAPS_HASPOV As Integer = &H10
'    Friend Const JOYCAPS_POV4DIR As Integer = &H20
'    Friend Const JOYCAPS_POVCTS As Integer = &H40
'    Friend Const JOHAB_CHARSET As Integer = 130
'    Friend Const JOB_CONTROL_PAUSE As Integer = 1
'    Friend Const JOB_CONTROL_RESUME As Integer = 2
'    Friend Const JOB_CONTROL_CANCEL As Integer = 3
'    Friend Const JOB_CONTROL_RESTART As Integer = 4
'    Friend Const JOB_CONTROL_DELETE As Integer = 5
'    Friend Const JOB_CONTROL_SENT_TO_PRINTER As Integer = 6
'    Friend Const JOB_CONTROL_LAST_PAGE_EJECTED As Integer = 7
'    Friend Const JOB_STATUS_PAUSED As Integer = &H1
'    Friend Const JOB_STATUS_ERROR As Integer = &H2
'    Friend Const JOB_STATUS_DELETING As Integer = &H4
'    Friend Const JOB_STATUS_SPOOLING As Integer = &H8
'    Friend Const JOB_STATUS_PRINTING As Integer = &H10
'    Friend Const JOB_STATUS_OFFLINE As Integer = &H20
'    Friend Const JOB_STATUS_PAPEROUT As Integer = &H40
'    Friend Const JOB_STATUS_PRINTED As Integer = &H80
'    Friend Const JOB_STATUS_DELETED As Integer = &H100
'    Friend Const JOB_STATUS_BLOCKED_DEVQ As Integer = &H200
'    Friend Const JOB_STATUS_USER_INTERVENTION As Integer = &H400
'    Friend Const JOB_STATUS_RESTART As Integer = &H800
'    Friend Const JOB_POSITION_UNSPECIFIED As Integer = 0
'    Friend Const JOB_NOTIFY_TYPE As Integer = &H1
'    Friend Const JOB_NOTIFY_FIELD_PRINTER_NAME As Integer = &H0
'    Friend Const JOB_NOTIFY_FIELD_MACHINE_NAME As Integer = &H1
'    Friend Const JOB_NOTIFY_FIELD_PORT_NAME As Integer = &H2
'    Friend Const JOB_NOTIFY_FIELD_USER_NAME As Integer = &H3
'    Friend Const JOB_NOTIFY_FIELD_NOTIFY_NAME As Integer = &H4
'    Friend Const JOB_NOTIFY_FIELD_DATATYPE As Integer = &H5
'    Friend Const JOB_NOTIFY_FIELD_PRINT_PROCESSOR As Integer = &H6
'    Friend Const JOB_NOTIFY_FIELD_PARAMETERS As Integer = &H7
'    Friend Const JOB_NOTIFY_FIELD_DRIVER_NAME As Integer = &H8
'    Friend Const JOB_NOTIFY_FIELD_DEVMODE As Integer = &H9
'    Friend Const JOB_NOTIFY_FIELD_STATUS As Integer = &HA
'    Friend Const JOB_NOTIFY_FIELD_STATUS_STRING As Integer = &HB
'    Friend Const JOB_NOTIFY_FIELD_SECURITY_DESCRIPTOR As Integer = &HC
'    Friend Const JOB_NOTIFY_FIELD_DOCUMENT As Integer = &HD
'    Friend Const JOB_NOTIFY_FIELD_PRIORITY As Integer = &HE
'    Friend Const JOB_NOTIFY_FIELD_POSITION As Integer = &HF
'    Friend Const JOB_NOTIFY_FIELD_SUBMITTED As Integer = &H10
'    Friend Const JOB_NOTIFY_FIELD_START_TIME As Integer = &H11
'    Friend Const JOB_NOTIFY_FIELD_UNTIL_TIME As Integer = &H12
'    Friend Const JOB_NOTIFY_FIELD_TIME As Integer = &H13
'    Friend Const JOB_NOTIFY_FIELD_TOTAL_PAGES As Integer = &H14
'    Friend Const JOB_NOTIFY_FIELD_PAGES_PRINTED As Integer = &H15
'    Friend Const JOB_NOTIFY_FIELD_TOTAL_BYTES As Integer = &H16
'    Friend Const JOB_NOTIFY_FIELD_BYTES_PRINTED As Integer = &H17
'    Friend Const JOB_ACCESS_ADMINISTER As Integer = &H10
    
    
    
'    Friend Const KEY_EVENT As Integer = &H1
'    Friend Const KP_IV As Integer = 1
'    Friend Const KP_SALT As Integer = 2
'    Friend Const KP_PADDING As Integer = 3
'    Friend Const KP_MODE As Integer = 4
'    Friend Const KP_MODE_BITS As Integer = 5
'    Friend Const KP_PERMISSIONS As Integer = 6
'    Friend Const KP_ALGID As Integer = 7
'    Friend Const KP_BLOCKLEN As Integer = 8
'    Friend Const KEY_QUERY_VALUE As Integer = &H1
'    Friend Const KEY_SET_VALUE As Integer = &H2
'    Friend Const KEY_CREATE_SUB_KEY As Integer = &H4
'    Friend Const KEY_ENUMERATE_SUB_KEYS As Integer = &H8
'    Friend Const KEY_NOTIFY As Integer = &H10
'    Friend Const KEY_CREATE_LINK As Integer = &H20
'    Friend Const KF_EXTENDED As Integer = &H100
'    Friend Const KF_DLGMODE As Integer = &H800
'    Friend Const KF_MENUMODE As Integer = &H1000
'    Friend Const KF_ALTDOWN As Integer = &H2000
'    Friend Const KF_REPEAT As Integer = &H4000
'    Friend Const KF_UP As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        KF_REPEAT = 0x4000,
'    '        KF_UP = unchecked((int)0x8000),
'    '-----------------^--- GenCode(token): unexpected token type
'    Friend Const KLF_ACTIVATE As Integer = &H1
'    Friend Const KLF_SUBSTITUTE_OK As Integer = &H2
'    Friend Const KLF_UNLOADPREVIOUS As Integer = &H4
'    Friend Const KLF_REORDER As Integer = &H8
'    Friend Const KLF_REPLACELANG As Integer = &H10
'    Friend Const KLF_NOTELLSHELL As Integer = &H80
'    Friend Const KL_NAMELENGTH As Integer = 9
'    Friend Const KEYEVENTF_EXTENDEDKEY As Integer = &H1
'    Friend Const KEYEVENTF_KEYUP As Integer = &H2
    
    
'    Friend Const lst1 As Integer = &H460
'    Friend Const lst2 As Integer = &H461
'    Friend Const lst3 As Integer = &H462
'    Friend Const lst4 As Integer = &H463
'    Friend Const lst5 As Integer = &H464
'    Friend Const lst6 As Integer = &H465
'    Friend Const lst7 As Integer = &H466
'    Friend Const lst8 As Integer = &H467
'    Friend Const lst9 As Integer = &H468
'    Friend Const lst10 As Integer = &H469
'    Friend Const lst11 As Integer = &H46A
'    Friend Const lst12 As Integer = &H46B
'    Friend Const lst13 As Integer = &H46C
'    Friend Const lst14 As Integer = &H46D
'    Friend Const lst15 As Integer = &H46E
'    Friend Const lst16 As Integer = &H46F
'    Friend Const LZERROR_BADINHANDLE As Integer = - 1
'    Friend Const LZERROR_BADOUTHANDLE As Integer = - 2
'    Friend Const LZERROR_READ As Integer = - 3
'    Friend Const LZERROR_WRITE As Integer = - 4
'    Friend Const LZERROR_GLOBALLOC As Integer = - 5
'    Friend Const LZERROR_GLOBLOCK As Integer = - 6
'    Friend Const LZERROR_BADVALUE As Integer = - 7
'    Friend Const LZERROR_UNKNOWNALG As Integer = - 8
'    Friend Const LISTEN_OUTSTANDING As Integer = &H1
'    Friend Const LMEM_FIXED As Integer = &H0
'    Friend Const LMEM_MOVEABLE As Integer = &H2
'    Friend Const LMEM_NOCOMPACT As Integer = &H10
'    Friend Const LMEM_NODISCARD As Integer = &H20
'    Friend Const LMEM_ZEROINIT As Integer = &H40
'    Friend Const LMEM_MODIFY As Integer = &H80
'    Friend Const LMEM_DISCARDABLE As Integer = &HF00
'    Friend Const LMEM_VALID_FLAGS As Integer = &HF72
'    Friend Const LMEM_INVALID_HANDLE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        LMEM_VALID_FLAGS = 0x0F72,
'    '        LMEM_INVALID_HANDLE = unchecked((int)0x8000),
'    '-------------------------------^--- GenCode(token): unexpected token type
'    Friend Const LHND As Integer = &H2 Or &H40
'    Friend Const LPTR As Integer = &H0 Or &H40
'    Friend Const LMEM_DISCARDED As Integer = &H4000
'    Friend Const LMEM_LOCKCOUNT As Integer = &HFF
'    Friend Const LOAD_DLL_DEBUG_EVENT As Integer = 6
'    Friend Const LPTx As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        LOAD_DLL_DEBUG_EVENT = 6,
'    '        LPTx = unchecked((int)0x80),
'    '----------------^--- GenCode(token): unexpected token type
'    Friend Const LOCKFILE_FAIL_IMMEDIATELY As Integer = &H1
'    Friend Const LOCKFILE_EXCLUSIVE_LOCK As Integer = &H2
'    Friend Const LOAD_LIBRARY_AS_DATAFILE As Integer = &H2
'    Friend Const LOAD_WITH_ALTERED_SEARCH_PATH As Integer = &H8
'    Friend Const LOGON32_LOGON_INTERACTIVE As Integer = 2
'    Friend Const LOGON32_LOGON_NETWORK As Integer = 3
'    Friend Const LOGON32_LOGON_BATCH As Integer = 4
'    Friend Const LOGON32_LOGON_SERVICE As Integer = 5
'    Friend Const LOGON32_PROVIDER_DEFAULT As Integer = 0
'    Friend Const LOGON32_PROVIDER_WINNT35 As Integer = 1
'    Friend Const LOGON32_PROVIDER_WINNT40 As Integer = 2
'    Friend Const LEFT_ALT_PRESSED As Integer = &H2
'    Friend Const LEFT_CTRL_PRESSED As Integer = &H8
'    Friend Const LCS_CALIBRATED_RGB As Integer = &H0
'    Friend Const LCS_DEVICE_RGB As Integer = &H1
'    Friend Const LCS_DEVICE_CMYK As Integer = &H2
'    Friend Const LCS_GM_BUSINESS As Integer = &H1
'    Friend Const LCS_GM_GRAPHICS As Integer = &H2
'    Friend Const LCS_GM_IMAGES As Integer = &H4
'    Friend Const LF_FACESIZE As Integer = 32
'    Friend Const LF_FULLFACESIZE As Integer = 64
'    Friend Const LTGRAY_BRUSH As Integer = 1
'    Friend Const LINECAPS As Integer = 30
'    Friend Const LOGPIXELSX As Integer = 88
'    Friend Const LOGPIXELSY As Integer = 90
'    Friend Const LC_NONE As Integer = 0
'    Friend Const LC_POLYLINE As Integer = 2
'    Friend Const LC_MARKER As Integer = 4
'    Friend Const LC_POLYMARKER As Integer = 8
'    Friend Const LC_WIDE As Integer = 16
'    Friend Const LC_STYLED As Integer = 32
'    Friend Const LC_WIDESTYLED As Integer = 64
'    Friend Const LC_INTERIORS As Integer = 128
'    Friend Const LPD_DOUBLEBUFFER As Integer = &H1
'    Friend Const LPD_STEREO As Integer = &H2
'    Friend Const LPD_SUPPORT_GDI As Integer = &H10
'    Friend Const LPD_SUPPORT_OPENGL As Integer = &H20
'    Friend Const LPD_SHARE_DEPTH As Integer = &H40
'    Friend Const LPD_SHARE_STENCIL As Integer = &H80
'    Friend Const LPD_SHARE_ACCUM As Integer = &H100
'    Friend Const LPD_SWAP_EXCHANGE As Integer = &H200
'    Friend Const LPD_SWAP_COPY As Integer = &H400
'    Friend Const LPD_TRANSPARENT As Integer = &H1000
'    Friend Const LPD_TYPE_RGBA As Integer = 0
'    Friend Const LPD_TYPE_COLORINDEX As Integer = 1
'    Friend Const LPSTR_TEXTCALLBACK As Integer = - 1
'    Friend Const LCMAP_LOWERCASE As Integer = &H100
'    Friend Const LCMAP_UPPERCASE As Integer = &H200
'    Friend Const LCMAP_SORTKEY As Integer = &H400
'    Friend Const LCMAP_BYTEREV As Integer = &H800
'    Friend Const LCMAP_HIRAGANA As Integer = &H100000
'    Friend Const LCMAP_KATAKANA As Integer = &H200000
'    Friend Const LCMAP_HALFWIDTH As Integer = &H400000
'    Friend Const LCMAP_FULLWIDTH As Integer = &H800000
'    Friend Const LCMAP_LINGUISTIC_CASING As Integer = &H1000000
'    Friend Const LCMAP_SIMPLIFIED_CHINESE As Integer = &H2000000
'    Friend Const LCMAP_TRADITIONAL_CHINESE As Integer = &H4000000
'    Friend Const LCID_INSTALLED As Integer = &H1
'    Friend Const LCID_SUPPORTED As Integer = &H2
'    Friend Const LOCALE_NOUSEROVERRIDE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        LCID_SUPPORTED = 0x00000002,
'    '        LOCALE_NOUSEROVERRIDE = unchecked((int)0x80000000),
'    '---------------------------------^--- GenCode(token): unexpected token type
'    Friend Const LOCALE_USE_CP_ACP As Integer = &H40000000
'    Friend Const LOCALE_ILANGUAGE As Integer = &H1
'    Friend Const LOCALE_SLANGUAGE As Integer = &H2
'    Friend Const LOCALE_SENGLANGUAGE As Integer = &H1001
'    Friend Const LOCALE_SABBREVLANGNAME As Integer = &H3
'    Friend Const LOCALE_SNATIVELANGNAME As Integer = &H4
'    Friend Const LOCALE_ICOUNTRY As Integer = &H5
'    Friend Const LOCALE_SCOUNTRY As Integer = &H6
'    Friend Const LOCALE_SENGCOUNTRY As Integer = &H1002
'    Friend Const LOCALE_SABBREVCTRYNAME As Integer = &H7
'    Friend Const LOCALE_SNATIVECTRYNAME As Integer = &H8
'    Friend Const LOCALE_IDEFAULTLANGUAGE As Integer = &H9
'    Friend Const LOCALE_IDEFAULTCOUNTRY As Integer = &HA
'    Friend Const LOCALE_IDEFAULTCODEPAGE As Integer = &HB
'    Friend Const LOCALE_IDEFAULTANSICODEPAGE As Integer = &H1004
'    Friend Const LOCALE_IDEFAULTMACCODEPAGE As Integer = &H1011
'    Friend Const LOCALE_SLIST As Integer = &HC
'    Friend Const LOCALE_IMEASURE As Integer = &HD
'    Friend Const LOCALE_SDECIMAL As Integer = &HE
'    Friend Const LOCALE_STHOUSAND As Integer = &HF
'    Friend Const LOCALE_SGROUPING As Integer = &H10
'    Friend Const LOCALE_IDIGITS As Integer = &H11
'    Friend Const LOCALE_ILZERO As Integer = &H12
'    Friend Const LOCALE_INEGNUMBER As Integer = &H1010
'    Friend Const LOCALE_SNATIVEDIGITS As Integer = &H13
'    Friend Const LOCALE_SCURRENCY As Integer = &H14
'    Friend Const LOCALE_SINTLSYMBOL As Integer = &H15
'    Friend Const LOCALE_SMONDECIMALSEP As Integer = &H16
'    Friend Const LOCALE_SMONTHOUSANDSEP As Integer = &H17
'    Friend Const LOCALE_SMONGROUPING As Integer = &H18
'    Friend Const LOCALE_ICURRDIGITS As Integer = &H19
'    Friend Const LOCALE_IINTLCURRDIGITS As Integer = &H1A
'    Friend Const LOCALE_ICURRENCY As Integer = &H1B
'    Friend Const LOCALE_INEGCURR As Integer = &H1C
'    Friend Const LOCALE_SDATE As Integer = &H1D
'    Friend Const LOCALE_STIME As Integer = &H1E
'    Friend Const LOCALE_SSHORTDATE As Integer = &H1F
'    Friend Const LOCALE_SLONGDATE As Integer = &H20
'    Friend Const LOCALE_STIMEFORMAT As Integer = &H1003
'    Friend Const LOCALE_IDATE As Integer = &H21
'    Friend Const LOCALE_ILDATE As Integer = &H22
'    Friend Const LOCALE_ITIME As Integer = &H23
'    Friend Const LOCALE_ITIMEMARKPOSN As Integer = &H1005
'    Friend Const LOCALE_ICENTURY As Integer = &H24
'    Friend Const LOCALE_ITLZERO As Integer = &H25
'    Friend Const LOCALE_IDAYLZERO As Integer = &H26
'    Friend Const LOCALE_IMONLZERO As Integer = &H27
'    Friend Const LOCALE_S1159 As Integer = &H28
'    Friend Const LOCALE_S2359 As Integer = &H29
'    Friend Const LOCALE_ICALENDARTYPE As Integer = &H1009
'    Friend Const LOCALE_IOPTIONALCALENDAR As Integer = &H100B
'    Friend Const LOCALE_IFIRSTDAYOFWEEK As Integer = &H100C
'    Friend Const LOCALE_IFIRSTWEEKOFYEAR As Integer = &H100D
'    Friend Const LOCALE_SDAYNAME1 As Integer = &H2A
'    Friend Const LOCALE_SDAYNAME2 As Integer = &H2B
'    Friend Const LOCALE_SDAYNAME3 As Integer = &H2C
'    Friend Const LOCALE_SDAYNAME4 As Integer = &H2D
'    Friend Const LOCALE_SDAYNAME5 As Integer = &H2E
'    Friend Const LOCALE_SDAYNAME6 As Integer = &H2F
'    Friend Const LOCALE_SDAYNAME7 As Integer = &H30
'    Friend Const LOCALE_SABBREVDAYNAME1 As Integer = &H31
'    Friend Const LOCALE_SABBREVDAYNAME2 As Integer = &H32
'    Friend Const LOCALE_SABBREVDAYNAME3 As Integer = &H33
'    Friend Const LOCALE_SABBREVDAYNAME4 As Integer = &H34
'    Friend Const LOCALE_SABBREVDAYNAME5 As Integer = &H35
'    Friend Const LOCALE_SABBREVDAYNAME6 As Integer = &H36
'    Friend Const LOCALE_SABBREVDAYNAME7 As Integer = &H37
'    Friend Const LOCALE_SMONTHNAME1 As Integer = &H38
'    Friend Const LOCALE_SMONTHNAME2 As Integer = &H39
'    Friend Const LOCALE_SMONTHNAME3 As Integer = &H3A
'    Friend Const LOCALE_SMONTHNAME4 As Integer = &H3B
'    Friend Const LOCALE_SMONTHNAME5 As Integer = &H3C
'    Friend Const LOCALE_SMONTHNAME6 As Integer = &H3D
'    Friend Const LOCALE_SMONTHNAME7 As Integer = &H3E
'    Friend Const LOCALE_SMONTHNAME8 As Integer = &H3F
'    Friend Const LOCALE_SMONTHNAME9 As Integer = &H40
'    Friend Const LOCALE_SMONTHNAME10 As Integer = &H41
'    Friend Const LOCALE_SMONTHNAME11 As Integer = &H42
'    Friend Const LOCALE_SMONTHNAME12 As Integer = &H43
'    Friend Const LOCALE_SMONTHNAME13 As Integer = &H100E
'    Friend Const LOCALE_SABBREVMONTHNAME1 As Integer = &H44
'    Friend Const LOCALE_SABBREVMONTHNAME2 As Integer = &H45
'    Friend Const LOCALE_SABBREVMONTHNAME3 As Integer = &H46
'    Friend Const LOCALE_SABBREVMONTHNAME4 As Integer = &H47
'    Friend Const LOCALE_SABBREVMONTHNAME5 As Integer = &H48
'    Friend Const LOCALE_SABBREVMONTHNAME6 As Integer = &H49
'    Friend Const LOCALE_SABBREVMONTHNAME7 As Integer = &H4A
'    Friend Const LOCALE_SABBREVMONTHNAME8 As Integer = &H4B
'    Friend Const LOCALE_SABBREVMONTHNAME9 As Integer = &H4C
'    Friend Const LOCALE_SABBREVMONTHNAME10 As Integer = &H4D
'    Friend Const LOCALE_SABBREVMONTHNAME11 As Integer = &H4E
'    Friend Const LOCALE_SABBREVMONTHNAME12 As Integer = &H4F
'    Friend Const LOCALE_SABBREVMONTHNAME13 As Integer = &H100F
'    Friend Const LOCALE_SPOSITIVESIGN As Integer = &H50
'    Friend Const LOCALE_SNEGATIVESIGN As Integer = &H51
'    Friend Const LOCALE_IPOSSIGNPOSN As Integer = &H52
'    Friend Const LOCALE_INEGSIGNPOSN As Integer = &H53
'    Friend Const LOCALE_IPOSSYMPRECEDES As Integer = &H54
'    Friend Const LOCALE_IPOSSEPBYSPACE As Integer = &H55
'    Friend Const LOCALE_INEGSYMPRECEDES As Integer = &H56
'    Friend Const LOCALE_INEGSEPBYSPACE As Integer = &H57
'    Friend Const LOCALE_FONTSIGNATURE As Integer = &H58
'    Friend Const LOCALE_SISO639LANGNAME As Integer = &H59
'    Friend Const LOCALE_SISO3166CTRYNAME As Integer = &H5A
'    Friend Const LANG_NEUTRAL As Integer = &H0
'    Friend Const LANG_AFRIKAANS As Integer = &H36
'    Friend Const LANG_ALBANIAN As Integer = &H1C
'    Friend Const LANG_ARABIC As Integer = &H1
'    Friend Const LANG_BASQUE As Integer = &H2D
'    Friend Const LANG_BELARUSIAN As Integer = &H23
'    Friend Const LANG_BULGARIAN As Integer = &H2
'    Friend Const LANG_CATALAN As Integer = &H3
'    Friend Const LANG_CHINESE As Integer = &H4
'    Friend Const LANG_CROATIAN As Integer = &H1A
'    Friend Const LANG_CZECH As Integer = &H5
'    Friend Const LANG_DANISH As Integer = &H6
'    Friend Const LANG_DUTCH As Integer = &H13
'    Friend Const LANG_ENGLISH As Integer = &H9
'    Friend Const LANG_ESTONIAN As Integer = &H25
'    Friend Const LANG_FAEROESE As Integer = &H38
'    Friend Const LANG_FARSI As Integer = &H29
'    Friend Const LANG_FINNISH As Integer = &HB
'    Friend Const LANG_FRENCH As Integer = &HC
'    Friend Const LANG_GERMAN As Integer = &H7
'    Friend Const LANG_GREEK As Integer = &H8
'    Friend Const LANG_HEBREW As Integer = &HD
'    Friend Const LANG_HUNGARIAN As Integer = &HE
'    Friend Const LANG_ICELANDIC As Integer = &HF
'    Friend Const LANG_INDONESIAN As Integer = &H21
'    Friend Const LANG_ITALIAN As Integer = &H10
'    Friend Const LANG_JAPANESE As Integer = &H11
'    Friend Const LANG_KOREAN As Integer = &H12
'    Friend Const LANG_LATVIAN As Integer = &H26
'    Friend Const LANG_LITHUANIAN As Integer = &H27
'    Friend Const LANG_NORWEGIAN As Integer = &H14
'    Friend Const LANG_POLISH As Integer = &H15
'    Friend Const LANG_PORTUGUESE As Integer = &H16
'    Friend Const LANG_ROMANIAN As Integer = &H18
'    Friend Const LANG_RUSSIAN As Integer = &H19
'    Friend Const LANG_SERBIAN As Integer = &H1A
'    Friend Const LANG_SLOVAK As Integer = &H1B
'    Friend Const LANG_SLOVENIAN As Integer = &H24
'    Friend Const LANG_SPANISH As Integer = &HA
'    Friend Const LANG_SWEDISH As Integer = &H1D
'    Friend Const LANG_THAI As Integer = &H1E
'    Friend Const LANG_TURKISH As Integer = &H1F
'    Friend Const LANG_UKRAINIAN As Integer = &H22
'    Friend Const LANG_VIETNAMESE As Integer = &H2A
'    Friend Const LR_DEFAULTCOLOR As Integer = &H0
'    Friend Const LR_MONOCHROME As Integer = &H1
'    Friend Const LR_COLOR As Integer = &H2
'    Friend Const LR_COPYRETURNORG As Integer = &H4
'    Friend Const LR_COPYDELETEORG As Integer = &H8
'    Friend Const LR_LOADFROMFILE As Integer = &H10
'    Friend Const LR_LOADTRANSPARENT As Integer = &H20
'    Friend Const LR_DEFAULTSIZE As Integer = &H40
'    Friend Const LR_VGACOLOR As Integer = &H80
'    Friend Const LR_LOADMAP3DCOLORS As Integer = &H1000
'    Friend Const LR_CREATEDIBSECTION As Integer = &H2000
'    Friend Const LR_COPYFROMRESOURCE As Integer = &H4000
'    Friend Const LR_SHARED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        LR_COPYFROMRESOURCE = 0x4000,
'    '        LR_SHARED = unchecked((int)0x8000),
'    '---------------------^--- GenCode(token): unexpected token type
'    Friend Const LB_CTLCODE As Integer = 0
'    Friend Const LB_OKAY As Integer = 0
'    Friend Const LB_ERR As Integer = - 1
'    Friend Const LB_ERRSPACE As Integer = - 2
'    Friend Const LBN_ERRSPACE As Integer = - 2
'    Friend Const LBN_SELCHANGE As Integer = 1
'    Friend Const LBN_DBLCLK As Integer = 2
'    Friend Const LBN_SELCANCEL As Integer = 3
'    Friend Const LBN_SETFOCUS As Integer = 4
'    Friend Const LBN_KILLFOCUS As Integer = 5
'    Friend Const LB_ADDSTRING As Integer = &H180
'    Friend Const LB_INSERTSTRING As Integer = &H181
'    Friend Const LB_DELETESTRING As Integer = &H182
'    Friend Const LB_SELITEMRANGEEX As Integer = &H183
'    Friend Const LB_RESETCONTENT As Integer = &H184
'    Friend Const LB_SETSEL As Integer = &H185
'    Friend Const LB_SETCURSEL As Integer = &H186
'    Friend Const LB_GETSEL As Integer = &H187
'    Friend Const LB_GETCURSEL As Integer = &H188
'    Friend Const LB_GETTEXT As Integer = &H189
'    Friend Const LB_GETTEXTLEN As Integer = &H18A
'    Friend Const LB_GETCOUNT As Integer = &H18B
'    Friend Const LB_SELECTSTRING As Integer = &H18C
'    Friend Const LB_DIR As Integer = &H18D
'    Friend Const LB_GETTOPINDEX As Integer = &H18E
'    Friend Const LB_FINDSTRING As Integer = &H18F
'    Friend Const LB_GETSELCOUNT As Integer = &H190
'    Friend Const LB_GETSELITEMS As Integer = &H191
'    Friend Const LB_SETTABSTOPS As Integer = &H192
'    Friend Const LB_GETHORIZONTALEXTENT As Integer = &H193
'    Friend Const LB_SETHORIZONTALEXTENT As Integer = &H194
'    Friend Const LB_SETCOLUMNWIDTH As Integer = &H195
'    Friend Const LB_ADDFILE As Integer = &H196
'    Friend Const LB_SETTOPINDEX As Integer = &H197
'    Friend Const LB_GETITEMRECT As Integer = &H198
'    Friend Const LB_GETITEMDATA As Integer = &H199
'    Friend Const LB_SETITEMDATA As Integer = &H19A
'    Friend Const LB_SELITEMRANGE As Integer = &H19B
'    Friend Const LB_SETANCHORINDEX As Integer = &H19C
'    Friend Const LB_GETANCHORINDEX As Integer = &H19D
'    Friend Const LB_SETCARETINDEX As Integer = &H19E
'    Friend Const LB_GETCARETINDEX As Integer = &H19F
'    Friend Const LB_SETITEMHEIGHT As Integer = &H1A0
'    Friend Const LB_GETITEMHEIGHT As Integer = &H1A1
'    Friend Const LB_FINDSTRINGEXACT As Integer = &H1A2
'    Friend Const LB_SETLOCALE As Integer = &H1A5
'    Friend Const LB_GETLOCALE As Integer = &H1A6
'    Friend Const LB_SETCOUNT As Integer = &H1A7
'    Friend Const LB_INITSTORAGE As Integer = &H1A8
'    Friend Const LB_ITEMFROMPOINT As Integer = &H1A9
'    Friend Const LB_MSGMAX As Integer = &H1B0
'    ' LB_MSGMAX = 0x01A8;
'    Friend Const LBS_NOTIFY As Integer = &H1
'    Friend Const LBS_SORT As Integer = &H2
'    Friend Const LBS_NOREDRAW As Integer = &H4
'    Friend Const LBS_MULTIPLESEL As Integer = &H8
'    Friend Const LBS_OWNERDRAWFIXED As Integer = &H10
'    Friend Const LBS_OWNERDRAWVARIABLE As Integer = &H20
'    Friend Const LBS_HASSTRINGS As Integer = &H40
'    Friend Const LBS_USETABSTOPS As Integer = &H80
'    Friend Const LBS_NOINTEGRALHEIGHT As Integer = &H100
'    Friend Const LBS_MULTICOLUMN As Integer = &H200
'    Friend Const LBS_WANTKEYBOARDINPUT As Integer = &H400
'    Friend Const LBS_EXTENDEDSEL As Integer = &H800
'    Friend Const LBS_DISABLENOSCROLL As Integer = &H1000
'    Friend Const LBS_NODATA As Integer = &H2000
'    Friend Const LBS_NOSEL As Integer = &H4000
'    Friend Const LBS_STANDARD As Integer = &H1 Or &H2 Or &H200000 Or &H800000
'    Friend Const LVM_FIRST As Integer = &H1000
'    Friend Const LVN_FIRST As Integer = 0 - 100
'    Friend Const LVN_LAST As Integer = 0 - 199
'    Friend Const LVS_ICON As Integer = &H0
'    Friend Const LVS_REPORT As Integer = &H1
'    Friend Const LVS_SMALLICON As Integer = &H2
'    Friend Const LVS_LIST As Integer = &H3
'    Friend Const LVS_TYPEMASK As Integer = &H3
'    Friend Const LVS_SINGLESEL As Integer = &H4
'    Friend Const LVS_SHOWSELALWAYS As Integer = &H8
'    Friend Const LVS_SORTASCENDING As Integer = &H10
'    Friend Const LVS_SORTDESCENDING As Integer = &H20
'    Friend Const LVS_SHAREIMAGELISTS As Integer = &H40
'    Friend Const LVS_NOLABELWRAP As Integer = &H80
'    Friend Const LVS_AUTOARRANGE As Integer = &H100
'    Friend Const LVS_EDITLABELS As Integer = &H200
'    Friend Const LVS_OWNERDATA As Integer = &H1000
'    Friend Const LVS_NOSCROLL As Integer = &H2000
'    Friend Const LVS_TYPESTYLEMASK As Integer = &HFC00
'    Friend Const LVS_ALIGNTOP As Integer = &H0
'    Friend Const LVS_ALIGNLEFT As Integer = &H800
'    Friend Const LVS_ALIGNMASK As Integer = &HC00
'    Friend Const LVS_OWNERDRAWFIXED As Integer = &H400
'    Friend Const LVS_NOCOLUMNHEADER As Integer = &H4000
'    Friend Const LVS_NOSORTHEADER As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        LVS_NOCOLUMNHEADER = 0x4000,
'    '        LVS_NOSORTHEADER = unchecked((int)0x8000),
'    '----------------------------^--- GenCode(token): unexpected token type
'    Friend Const LVM_GETBKCOLOR As Integer = &H1000 + 0
'    Friend Const LVM_SETBKCOLOR As Integer = &H1000 + 1
'    Friend Const LVM_GETIMAGELIST As Integer = &H1000 + 2
'    Friend Const LVSIL_NORMAL As Integer = 0
'    Friend Const LVSIL_SMALL As Integer = 1
'    Friend Const LVSIL_STATE As Integer = 2
'    Friend Const LVM_SETIMAGELIST As Integer = &H1000 + 3
'    Friend Const LVM_GETITEMCOUNT As Integer = &H1000 + 4
'    Friend Const LVIF_TEXT As Integer = &H1
'    Friend Const LVIF_IMAGE As Integer = &H2
'    Friend Const LVIF_PARAM As Integer = &H4
'    Friend Const LVIF_STATE As Integer = &H8
'    Friend Const LVIF_INDENT As Integer = &H10
'    Friend Const LVIF_NORECOMPUTE As Integer = &H800
'    Friend Const LVIS_FOCUSED As Integer = &H1
'    Friend Const LVIS_SELECTED As Integer = &H2
'    Friend Const LVIS_CUT As Integer = &H4
'    Friend Const LVIS_DROPHILITED As Integer = &H8
'    Friend Const LVIS_OVERLAYMASK As Integer = &HF00
'    Friend Const LVIS_STATEIMAGEMASK As Integer = &HF000
'    Friend Const LVM_GETITEMA As Integer = &H1000 + 5
'    Friend Const LVM_GETITEMW As Integer = &H1000 + 75
'    Friend Const LVM_SETITEMA As Integer = &H1000 + 6
'    Friend Const LVM_SETITEMW As Integer = &H1000 + 76
'    Friend Const LVM_INSERTITEMA As Integer = &H1000 + 7
'    Friend Const LVM_INSERTITEMW As Integer = &H1000 + 77
'    Friend Const LVM_DELETEITEM As Integer = &H1000 + 8
'    Friend Const LVM_DELETEALLITEMS As Integer = &H1000 + 9
'    Friend Const LVM_GETCALLBACKMASK As Integer = &H1000 + 10
'    Friend Const LVM_SETCALLBACKMASK As Integer = &H1000 + 11
'    Friend Const LVNI_ALL As Integer = &H0
'    Friend Const LVNI_FOCUSED As Integer = &H1
'    Friend Const LVNI_SELECTED As Integer = &H2
'    Friend Const LVNI_CUT As Integer = &H4
'    Friend Const LVNI_DROPHILITED As Integer = &H8
'    Friend Const LVNI_ABOVE As Integer = &H100
'    Friend Const LVNI_BELOW As Integer = &H200
'    Friend Const LVNI_TOLEFT As Integer = &H400
'    Friend Const LVNI_TORIGHT As Integer = &H800
'    Friend Const LVM_GETNEXTITEM As Integer = &H1000 + 12
'    Friend Const LVFI_PARAM As Integer = &H1
'    Friend Const LVFI_STRING As Integer = &H2
'    Friend Const LVFI_PARTIAL As Integer = &H8
'    Friend Const LVFI_WRAP As Integer = &H20
'    Friend Const LVFI_NEARESTXY As Integer = &H40
'    Friend Const LVM_FINDITEMA As Integer = &H1000 + 13
'    Friend Const LVM_FINDITEMW As Integer = &H1000 + 83
'    Friend Const LVIR_BOUNDS As Integer = 0
'    Friend Const LVIR_ICON As Integer = 1
'    Friend Const LVIR_LABEL As Integer = 2
'    Friend Const LVIR_SELECTBOUNDS As Integer = 3
'    Friend Const LVM_GETITEMRECT As Integer = &H1000 + 14
'    Friend Const LVM_SETITEMPOSITION As Integer = &H1000 + 15
'    Friend Const LVM_GETITEMPOSITION As Integer = &H1000 + 16
'    Friend Const LVM_GETSTRINGWIDTHA As Integer = &H1000 + 17
'    Friend Const LVM_GETSTRINGWIDTHW As Integer = &H1000 + 87
'    Friend Const LVHT_NOWHERE As Integer = &H1
'    Friend Const LVHT_ONITEMICON As Integer = &H2
'    Friend Const LVHT_ONITEMLABEL As Integer = &H4
'    Friend Const LVHT_ONITEMSTATEICON As Integer = &H8
'    Friend Const LVHT_ONITEM As Integer = &H2 Or &H4 Or &H8
'    Friend Const LVHT_ABOVE As Integer = &H8
'    Friend Const LVHT_BELOW As Integer = &H10
'    Friend Const LVHT_TORIGHT As Integer = &H20
'    Friend Const LVHT_TOLEFT As Integer = &H40
'    Friend Const LVM_HITTEST As Integer = &H1000 + 18
'    Friend Const LVM_ENSUREVISIBLE As Integer = &H1000 + 19
'    Friend Const LVM_SCROLL As Integer = &H1000 + 20
'    Friend Const LVM_REDRAWITEMS As Integer = &H1000 + 21
'    Friend Const LVA_DEFAULT As Integer = &H0
'    Friend Const LVA_ALIGNLEFT As Integer = &H1
'    Friend Const LVA_ALIGNTOP As Integer = &H2
'    Friend Const LVA_SNAPTOGRID As Integer = &H5
'    Friend Const LVM_ARRANGE As Integer = &H1000 + 22
        Friend Const LVM_EDITLABELA As Integer = &H1000 + 23
        Friend Const LVM_EDITLABELW As Integer = &H1000 + 118
'    Friend Const LVM_GETEDITCONTROL As Integer = &H1000 + 24
'    Friend Const LVCF_FMT As Integer = &H1
'    Friend Const LVCF_WIDTH As Integer = &H2
'    Friend Const LVCF_TEXT As Integer = &H4
'    Friend Const LVCF_SUBITEM As Integer = &H8
'    Friend Const LVCF_IMAGE As Integer = &H10
'    Friend Const LVCF_ORDER As Integer = &H20
'    Friend Const LVCFMT_LEFT As Integer = &H0
'    Friend Const LVCFMT_RIGHT As Integer = &H1
'    Friend Const LVCFMT_CENTER As Integer = &H2
'    Friend Const LVCFMT_JUSTIFYMASK As Integer = &H3
'    Friend Const LVCFMT_IMAGE As Integer = &H800
'    Friend Const LVCFMT_BITMAP_ON_RIGHT As Integer = &H1000
'    Friend Const LVCFMT_COL_HAS_IMAGES As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        LVCFMT_BITMAP_ON_RIGHT = 0x1000,
'    '        LVCFMT_COL_HAS_IMAGES = unchecked((int)0x8000),
'    '---------------------------------^--- GenCode(token): unexpected token type
'    Friend Const LVM_GETCOLUMNA As Integer = &H1000 + 25
'    Friend Const LVM_GETCOLUMNW As Integer = &H1000 + 95
'    Friend Const LVM_SETCOLUMNA As Integer = &H1000 + 26
'    Friend Const LVM_SETCOLUMNW As Integer = &H1000 + 96
'    Friend Const LVM_INSERTCOLUMNA As Integer = &H1000 + 27
'    Friend Const LVM_INSERTCOLUMNW As Integer = &H1000 + 97
'    Friend Const LVM_DELETECOLUMN As Integer = &H1000 + 28
'    Friend Const LVM_GETCOLUMNWIDTH As Integer = &H1000 + 29
'    Friend Const LVSCW_AUTOSIZE As Integer = - 1
'    Friend Const LVSCW_AUTOSIZE_USEHEADER As Integer = - 2
'    Friend Const LVM_SETCOLUMNWIDTH As Integer = &H1000 + 30
     Friend Const LVM_GETHEADER As Integer = &H1000 + 31
'    Friend Const LVM_CREATEDRAGIMAGE As Integer = &H1000 + 33
'    Friend Const LVM_GETVIEWRECT As Integer = &H1000 + 34
'    Friend Const LVM_GETTEXTCOLOR As Integer = &H1000 + 35
'    Friend Const LVM_SETTEXTCOLOR As Integer = &H1000 + 36
'    Friend Const LVM_GETTEXTBKCOLOR As Integer = &H1000 + 37
'    Friend Const LVM_SETTEXTBKCOLOR As Integer = &H1000 + 38
'    Friend Const LVM_GETTOPINDEX As Integer = &H1000 + 39
'    Friend Const LVM_GETCOUNTPERPAGE As Integer = &H1000 + 40
'    Friend Const LVM_GETORIGIN As Integer = &H1000 + 41
'    Friend Const LVM_UPDATE As Integer = &H1000 + 42
'    Friend Const LVM_SETITEMSTATE As Integer = &H1000 + 43
'    Friend Const LVM_GETITEMSTATE As Integer = &H1000 + 44
'    Friend Const LVM_GETITEMTEXTA As Integer = &H1000 + 45
'    Friend Const LVM_GETITEMTEXTW As Integer = &H1000 + 115
'    Friend Const LVM_SETITEMTEXTA As Integer = &H1000 + 46
'    Friend Const LVM_SETITEMTEXTW As Integer = &H1000 + 116
'    Friend Const LVSICF_NOINVALIDATEALL As Integer = &H1
'    Friend Const LVSICF_NOSCROLL As Integer = &H2
'    Friend Const LVM_SETITEMCOUNT As Integer = &H1000 + 47
'    Friend Const LVM_SORTITEMS As Integer = &H1000 + 48
'    Friend Const LVM_SETITEMPOSITION32 As Integer = &H1000 + 49
'    Friend Const LVM_GETSELECTEDCOUNT As Integer = &H1000 + 50
'    Friend Const LVM_GETITEMSPACING As Integer = &H1000 + 51
'    Friend Const LVM_GETISEARCHSTRINGA As Integer = &H1000 + 52
'    Friend Const LVM_GETISEARCHSTRINGW As Integer = &H1000 + 117
'    Friend Const LVM_SETICONSPACING As Integer = &H1000 + 53
'    Friend Const LVM_SETEXTENDEDLISTVIEWSTYLE As Integer = &H1000 + 54
'    Friend Const LVM_GETEXTENDEDLISTVIEWSTYLE As Integer = &H1000 + 55
'    Friend Const LVS_EX_GRIDLINES As Integer = &H1
'    Friend Const LVS_EX_SUBITEMIMAGES As Integer = &H2
'    Friend Const LVS_EX_CHECKBOXES As Integer = &H4
'    Friend Const LVS_EX_TRACKSELECT As Integer = &H8
'    Friend Const LVS_EX_HEADERDRAGDROP As Integer = &H10
'    Friend Const LVS_EX_FULLROWSELECT As Integer = &H20
'    Friend Const LVS_EX_ONECLICKACTIVATE As Integer = &H40
'    Friend Const LVS_EX_TWOCLICKACTIVATE As Integer = &H80
'    Friend Const LVS_EX_FLATSB As Integer = &H100
'    Friend Const LVS_EX_REGIONAL As Integer = &H200
'    Friend Const LVS_EX_INFOTIP As Integer = &H400
'    Friend Const LVS_EX_UNDERLINEHOT As Integer = &H800
'    Friend Const LVS_EX_UNDERLINECOLD As Integer = &H1000
'    Friend Const LVS_EX_MULTIWORKAREAS As Integer = &H2000
'    Friend Const LVM_GETSUBITEMRECT As Integer = &H1000 + 56
'    Friend Const LVM_SUBITEMHITTEST As Integer = &H1000 + 57
'    Friend Const LVM_SETCOLUMNORDERARRAY As Integer = &H1000 + 58
'    Friend Const LVM_GETCOLUMNORDERARRAY As Integer = &H1000 + 59
'    Friend Const LVM_SETHOTITEM As Integer = &H1000 + 60
'    Friend Const LVM_GETHOTITEM As Integer = &H1000 + 61
'    Friend Const LVM_SETHOTCURSOR As Integer = &H1000 + 62
'    Friend Const LVM_GETHOTCURSOR As Integer = &H1000 + 63
'    Friend Const LVM_APPROXIMATEVIEWRECT As Integer = &H1000 + 64
'    Friend Const LVM_SETWORKAREA As Integer = &H1000 + 65
'    Friend Const LVN_ITEMCHANGING As Integer = 0 - 100 - 0
'    Friend Const LVN_ITEMCHANGED As Integer = 0 - 100 - 1
'    Friend Const LVN_INSERTITEM As Integer = 0 - 100 - 2
'    Friend Const LVN_DELETEITEM As Integer = 0 - 100 - 3
'    Friend Const LVN_DELETEALLITEMS As Integer = 0 - 100 - 4
'    Friend Const LVN_BEGINLABELEDITA As Integer = 0 - 100 - 5
'    Friend Const LVN_BEGINLABELEDITW As Integer = 0 - 100 - 75
'    Friend Const LVN_ENDLABELEDITA As Integer = 0 - 100 - 6
'    Friend Const LVN_ENDLABELEDITW As Integer = 0 - 100 - 76
'    Friend Const LVN_COLUMNCLICK As Integer = 0 - 100 - 8
'    Friend Const LVN_BEGINDRAG As Integer = 0 - 100 - 9
'    Friend Const LVN_BEGINRDRAG As Integer = 0 - 100 - 11
'    Friend Const LVN_ODCACHEHINT As Integer = 0 - 100 - 13
'    Friend Const LVN_ODFINDITEMA As Integer = 0 - 100 - 52
'    Friend Const LVN_ODFINDITEMW As Integer = 0 - 100 - 79
'    Friend Const LVN_ITEMACTIVATE As Integer = 0 - 100 - 14
'    Friend Const LVN_ODSTATECHANGED As Integer = 0 - 100 - 15
'    Friend Const LVN_GETDISPINFOA As Integer = 0 - 100 - 50
'    Friend Const LVN_GETDISPINFOW As Integer = 0 - 100 - 77
'    Friend Const LVN_SETDISPINFOA As Integer = 0 - 100 - 51
'    Friend Const LVN_SETDISPINFOW As Integer = 0 - 100 - 78
'    Friend Const LVIF_DI_SETITEM As Integer = &H1000
'    Friend Const LVN_KEYDOWN As Integer = 0 - 100 - 55
'    Friend Const LWA_COLORKEY As Integer = &H1
'    Friend Const LWA_ALPHA As Integer = &H2
'    Friend Const LVN_MARQUEEBEGIN As Integer = 0 - 100 - 56
'    ' nt5 begin 
'    ' nt5 end 
    
    
    
    
    
'    Friend Const MSGF_DDEMGR As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '
'    '        public const int MSGF_DDEMGR = unchecked((int)0x8001),
'    '----------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const MH_CREATE As Integer = 1
'    Friend Const MH_KEEP As Integer = 2
'    Friend Const MH_DELETE As Integer = 3
'    Friend Const MH_CLEANUP As Integer = 4
'    Friend Const MAX_MONITORS As Integer = 4
'    Friend Const MF_HSZ_INFO As Integer = &H1000000
'    Friend Const MF_SENDMSGS As Integer = &H2000000
'    Friend Const MF_POSTMSGS As Integer = &H4000000
'    Friend Const MF_CALLBACKS As Integer = &H8000000
'    Friend Const MF_ERRORS As Integer = &H10000000
'    Friend Const MF_LINKS As Integer = &H20000000
'    Friend Const MF_CONV As Integer = &H40000000
'    Friend Const MF_MASK As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        MF_CONV = 0x40000000,
'    '        MF_MASK = unchecked((int)0xFF000000),
'    '-------------------^--- GenCode(token): unexpected token type
'    Friend Const MULTIFILEOPENORD As Integer = 1537
'    Friend Const MOD_ALT As Integer = &H1
'    Friend Const MOD_CONTROL As Integer = &H2
'    Friend Const MOD_SHIFT As Integer = &H4
'    Friend Const MOD_LEFT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        MOD_SHIFT = 0x0004,
'    '        MOD_LEFT = unchecked((int)0x8000),
'    '--------------------^--- GenCode(token): unexpected token type
'    Friend Const MOD_RIGHT As Integer = &H4000
'    Friend Const MOD_ON_KEYUP As Integer = &H800
'    Friend Const MOD_IGNORE_ALL_MODIFIER As Integer = &H400
'    Friend Const MDMVOLFLAG_LOW As Integer = &H1
'    Friend Const MDMVOLFLAG_MEDIUM As Integer = &H2
'    Friend Const MDMVOLFLAG_HIGH As Integer = &H4
'    Friend Const MDMVOL_LOW As Integer = &H0
'    Friend Const MDMVOL_MEDIUM As Integer = &H1
'    Friend Const MDMVOL_HIGH As Integer = &H2
'    Friend Const MDMSPKRFLAG_OFF As Integer = &H1
'    Friend Const MDMSPKRFLAG_DIAL As Integer = &H2
'    Friend Const MDMSPKRFLAG_ON As Integer = &H4
'    Friend Const MDMSPKRFLAG_CALLSETUP As Integer = &H8
'    Friend Const MDMSPKR_OFF As Integer = &H0
'    Friend Const MDMSPKR_DIAL As Integer = &H1
'    Friend Const MDMSPKR_ON As Integer = &H2
'    Friend Const MDMSPKR_CALLSETUP As Integer = &H3
'    Friend Const MDM_COMPRESSION As Integer = &H1
'    Friend Const MDM_ERROR_CONTROL As Integer = &H2
'    Friend Const MDM_FORCED_EC As Integer = &H4
'    Friend Const MDM_CELLULAR As Integer = &H8
'    Friend Const MDM_FLOWCONTROL_HARD As Integer = &H10
'    Friend Const MDM_FLOWCONTROL_SOFT As Integer = &H20
'    Friend Const MDM_CCITT_OVERRIDE As Integer = &H40
'    Friend Const MDM_SPEED_ADJUST As Integer = &H80
'    Friend Const MDM_TONE_DIAL As Integer = &H100
'    Friend Const MDM_BLIND_DIAL As Integer = &H200
'    Friend Const MDM_V23_OVERRIDE As Integer = &H400
'    Friend Const MAXPNAMELEN As Integer = 32
'    Friend Const MAXERRORLENGTH As Integer = 256
'    Friend Const MAX_JOYSTICKOEMVXDNAME As Integer = 260
'    Friend Const MM_MICROSOFT As Integer = 1
'    Friend Const MM_MIDI_MAPPER As Integer = 1
'    Friend Const MM_WAVE_MAPPER As Integer = 2
'    Friend Const MM_SNDBLST_MIDIOUT As Integer = 3
'    Friend Const MM_SNDBLST_MIDIIN As Integer = 4
'    Friend Const MM_SNDBLST_SYNTH As Integer = 5
'    Friend Const MM_SNDBLST_WAVEOUT As Integer = 6
'    Friend Const MM_SNDBLST_WAVEIN As Integer = 7
'    Friend Const MM_ADLIB As Integer = 9
'    Friend Const MM_MPU401_MIDIOUT As Integer = 10
'    Friend Const MM_MPU401_MIDIIN As Integer = 11
'    Friend Const MM_PC_JOYSTICK As Integer = 12
'    Friend Const MM_JOY1MOVE As Integer = &H3A0
'    Friend Const MM_JOY2MOVE As Integer = &H3A1
'    Friend Const MM_JOY1ZMOVE As Integer = &H3A2
'    Friend Const MM_JOY2ZMOVE As Integer = &H3A3
'    Friend Const MM_JOY1BUTTONDOWN As Integer = &H3B5
'    Friend Const MM_JOY2BUTTONDOWN As Integer = &H3B6
'    Friend Const MM_JOY1BUTTONUP As Integer = &H3B7
'    Friend Const MM_JOY2BUTTONUP As Integer = &H3B8
'    Friend Const MM_MCINOTIFY As Integer = &H3B9
'    Friend Const MM_WOM_OPEN As Integer = &H3BB
'    Friend Const MM_WOM_CLOSE As Integer = &H3BC
'    Friend Const MM_WOM_DONE As Integer = &H3BD
'    Friend Const MM_WIM_OPEN As Integer = &H3BE
'    Friend Const MM_WIM_CLOSE As Integer = &H3BF
'    Friend Const MM_WIM_DATA As Integer = &H3C0
'    Friend Const MM_MIM_OPEN As Integer = &H3C1
'    Friend Const MM_MIM_CLOSE As Integer = &H3C2
'    Friend Const MM_MIM_DATA As Integer = &H3C3
'    Friend Const MM_MIM_LONGDATA As Integer = &H3C4
'    Friend Const MM_MIM_ERROR As Integer = &H3C5
'    Friend Const MM_MIM_LONGERROR As Integer = &H3C6
'    Friend Const MM_MOM_OPEN As Integer = &H3C7
'    Friend Const MM_MOM_CLOSE As Integer = &H3C8
'    Friend Const MM_MOM_DONE As Integer = &H3C9
'    Friend Const MM_DRVM_OPEN As Integer = &H3D0
'    Friend Const MM_DRVM_CLOSE As Integer = &H3D1
'    Friend Const MM_DRVM_DATA As Integer = &H3D2
'    Friend Const MM_DRVM_ERROR As Integer = &H3D3
'    Friend Const MM_STREAM_OPEN As Integer = &H3D4
'    Friend Const MM_STREAM_CLOSE As Integer = &H3D5
'    Friend Const MM_STREAM_DONE As Integer = &H3D6
'    Friend Const MM_STREAM_ERROR As Integer = &H3D7
'    Friend Const MM_MOM_POSITIONCB As Integer = &H3CA
'    Friend Const MM_MCISIGNAL As Integer = &H3CB
'    Friend Const MM_MIM_MOREDATA As Integer = &H3CC
'    Friend Const MM_MIXM_LINE_CHANGE As Integer = &H3D0
'    Friend Const MM_MIXM_CONTROL_CHANGE As Integer = &H3D1
'    Friend Const MMSYSERR_BASE As Integer = 0
'    Friend Const MIDIERR_BASE As Integer = 64
'    Friend Const MCIERR_BASE As Integer = 256
'    Friend Const MIXERR_BASE As Integer = 1024
'    Friend Const MCI_STRING_OFFSET As Integer = 512
'    Friend Const MCI_VD_OFFSET As Integer = 1024
'    Friend Const MCI_CD_OFFSET As Integer = 1088
'    Friend Const MCI_WAVE_OFFSET As Integer = 1152
'    Friend Const MCI_SEQ_OFFSET As Integer = 1216
'    Friend Const MMSYSERR_NOERROR As Integer = 0
'    Friend Const MMSYSERR_ERROR As Integer = 0 + 1
'    Friend Const MMSYSERR_BADDEVICEID As Integer = 0 + 2
'    Friend Const MMSYSERR_NOTENABLED As Integer = 0 + 3
'    Friend Const MMSYSERR_ALLOCATED As Integer = 0 + 4
'    Friend Const MMSYSERR_INVALHANDLE As Integer = 0 + 5
'    Friend Const MMSYSERR_NODRIVER As Integer = 0 + 6
'    Friend Const MMSYSERR_NOMEM As Integer = 0 + 7
'    Friend Const MMSYSERR_NOTSUPPORTED As Integer = 0 + 8
'    Friend Const MMSYSERR_BADERRNUM As Integer = 0 + 9
'    Friend Const MMSYSERR_INVALFLAG As Integer = 0 + 10
'    Friend Const MMSYSERR_INVALPARAM As Integer = 0 + 11
'    Friend Const MMSYSERR_HANDLEBUSY As Integer = 0 + 12
'    Friend Const MMSYSERR_INVALIDALIAS As Integer = 0 + 13
'    Friend Const MMSYSERR_BADDB As Integer = 0 + 14
'    Friend Const MMSYSERR_KEYNOTFOUND As Integer = 0 + 15
'    Friend Const MMSYSERR_READERROR As Integer = 0 + 16
'    Friend Const MMSYSERR_WRITEERROR As Integer = 0 + 17
'    Friend Const MMSYSERR_DELETEERROR As Integer = 0 + 18
'    Friend Const MMSYSERR_VALNOTFOUND As Integer = 0 + 19
'    Friend Const MMSYSERR_NODRIVERCB As Integer = 0 + 20
'    Friend Const MMSYSERR_LASTERROR As Integer = 0 + 20
'    Friend Const MIDIERR_UNPREPARED As Integer = 64 + 0
'    Friend Const MIDIERR_STILLPLAYING As Integer = 64 + 1
'    Friend Const MIDIERR_NOMAP As Integer = 64 + 2
'    Friend Const MIDIERR_NOTREADY As Integer = 64 + 3
'    Friend Const MIDIERR_NODEVICE As Integer = 64 + 4
'    Friend Const MIDIERR_INVALIDSETUP As Integer = 64 + 5
'    Friend Const MIDIERR_BADOPENMODE As Integer = 64 + 6
'    Friend Const MIDIERR_DONT_CONTINUE As Integer = 64 + 7
'    Friend Const MIDIERR_LASTERROR As Integer = 64 + 7
'    Friend Const MIDIPATCHSIZE As Integer = 128
'    Friend Const MIM_OPEN As Integer = &H3C1
'    Friend Const MIM_CLOSE As Integer = &H3C2
'    Friend Const MIM_DATA As Integer = &H3C3
'    Friend Const MIM_LONGDATA As Integer = &H3C4
'    Friend Const MIM_ERROR As Integer = &H3C5
'    Friend Const MIM_LONGERROR As Integer = &H3C6
'    Friend Const MOM_OPEN As Integer = &H3C7
'    Friend Const MOM_CLOSE As Integer = &H3C8
'    Friend Const MOM_DONE As Integer = &H3C9
'    Friend Const MIM_MOREDATA As Integer = &H3CC
'    Friend Const MOM_POSITIONCB As Integer = &H3CA
'    Friend Const MIDI_IO_STATUS As Integer = &H20
'    Friend Const MIDI_CACHE_ALL As Integer = 1
'    Friend Const MIDI_CACHE_BESTFIT As Integer = 2
'    Friend Const MIDI_CACHE_QUERY As Integer = 3
'    Friend Const MIDI_UNCACHE As Integer = 4
'    Friend Const MOD_MIDIPORT As Integer = 1
'    Friend Const MOD_SYNTH As Integer = 2
'    Friend Const MOD_SQSYNTH As Integer = 3
'    Friend Const MOD_FMSYNTH As Integer = 4
'    Friend Const MOD_MAPPER As Integer = 5
'    Friend Const MIDICAPS_VOLUME As Integer = &H1
'    Friend Const MIDICAPS_LRVOLUME As Integer = &H2
'    Friend Const MIDICAPS_CACHE As Integer = &H4
'    Friend Const MIDICAPS_STREAM As Integer = &H8
'    Friend Const MHDR_DONE As Integer = &H1
'    Friend Const MHDR_PREPARED As Integer = &H2
'    Friend Const MHDR_INQUEUE As Integer = &H4
'    Friend Const MHDR_ISSTRM As Integer = &H8
'    Friend Const MEVT_F_SHORT As Integer = &H0
'    Friend Const MEVT_F_LONG As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        MEVT_F_SHORT = 0x00000000,
'    '        MEVT_F_LONG = unchecked((int)0x80000000),
'    '-----------------------^--- GenCode(token): unexpected token type
'    Friend Const MEVT_F_CALLBACK As Integer = &H40000000
'    Friend Const MEVT_SHORTMSG As Integer = &H0
'    Friend Const MEVT_TEMPO As Integer = &H1
'    Friend Const MEVT_NOP As Integer = &H2
'    Friend Const MEVT_LONGMSG As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                     MEVT_NOP = (0x02),
'    '                                                MEVT_LONGMSG = (unchecked((int)0x80)),
'    '-----------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const MEVT_COMMENT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                MEVT_LONGMSG = (unchecked((int)0x80)),
'    '                                                               MEVT_COMMENT = (unchecked((int)0x82)),
'    '--------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const MEVT_VERSION As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                               MEVT_COMMENT = (unchecked((int)0x82)),
'    '                                                                              MEVT_VERSION = (unchecked((int)0x84)),
'    '-----------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const MIDISTRM_ERROR As Integer = - 2
'    Friend Const MIDIPROP_SET As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                             MIDISTRM_ERROR = (-2),
'    '                                                                                                              MIDIPROP_SET = unchecked((int)0x80000000),
'    '------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const MIDIPROP_GET As Integer = &H40000000
'    Friend Const MIDIPROP_TIMEDIV As Integer = &H1
'    Friend Const MIDIPROP_TEMPO As Integer = &H2
'    Friend Const MIXER_SHORT_NAME_CHARS As Integer = 16
'    Friend Const MIXER_LONG_NAME_CHARS As Integer = 64
'    Friend Const MIXERR_INVALLINE As Integer = 1024 + 0
'    Friend Const MIXERR_INVALCONTROL As Integer = 1024 + 1
'    Friend Const MIXERR_INVALVALUE As Integer = 1024 + 2
'    Friend Const MIXERR_LASTERROR As Integer = 1024 + 2
'    Friend Const MIXER_OBJECTF_HANDLE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                     MIXERR_LASTERROR = (1024+2),
'    '                                                                                        MIXER_OBJECTF_HANDLE = unchecked((int)0x80000000),
'    '----------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const MIXER_OBJECTF_MIXER As Integer = &H0
'    Friend Const MIXER_OBJECTF_HMIXER As Integer = Or &H0
'    '
'    'Note:  Error processing original source shown below
'    '        MIXER_OBJECTF_MIXER = 0x00000000,
'    '        MIXER_OBJECTF_HMIXER = (unchecked((int)0x80000000)|0x00000000),
'    '---------------------------------^--- GenCode(token): unexpected token type
'    Friend Const MIXER_OBJECTF_WAVEOUT As Integer = &H10000000
'    Friend Const MIXER_OBJECTF_HWAVEOUT As Integer = Or &H10000000
'    '
'    'Note:  Error processing original source shown below
'    '                               MIXER_OBJECTF_WAVEOUT = 0x10000000,
'    '        MIXER_OBJECTF_HWAVEOUT = (unchecked((int)0x80000000)|0x10000000),
'    '-----------------------------------^--- GenCode(token): unexpected token type
'    Friend Const MIXER_OBJECTF_WAVEIN As Integer = &H20000000
'    Friend Const MIXER_OBJECTF_HWAVEIN As Integer = Or &H20000000
'    '
'    'Note:  Error processing original source shown below
'    '                                 MIXER_OBJECTF_WAVEIN = 0x20000000,
'    '        MIXER_OBJECTF_HWAVEIN = (unchecked((int)0x80000000)|0x20000000),
'    '----------------------------------^--- GenCode(token): unexpected token type
'    Friend Const MIXER_OBJECTF_MIDIOUT As Integer = &H30000000
'    Friend Const MIXER_OBJECTF_HMIDIOUT As Integer = Or &H30000000
'    '
'    'Note:  Error processing original source shown below
'    '                                MIXER_OBJECTF_MIDIOUT = 0x30000000,
'    '        MIXER_OBJECTF_HMIDIOUT = (unchecked((int)0x80000000)|0x30000000),
'    '-----------------------------------^--- GenCode(token): unexpected token type
'    Friend Const MIXER_OBJECTF_MIDIIN As Integer = &H40000000
'    Friend Const MIXER_OBJECTF_HMIDIIN As Integer = Or &H40000000
'    '
'    'Note:  Error processing original source shown below
'    '                                 MIXER_OBJECTF_MIDIIN = 0x40000000,
'    '        MIXER_OBJECTF_HMIDIIN = (unchecked((int)0x80000000)|0x40000000),
'    '----------------------------------^--- GenCode(token): unexpected token type
'    Friend Const MIXER_OBJECTF_AUX As Integer = &H50000000
'    Friend Const MIXERLINE_LINEF_ACTIVE As Integer = &H1
'    Friend Const MIXERLINE_LINEF_DISCONNECTED As Integer = &H8000
'    Friend Const MIXERLINE_LINEF_SOURCE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        MIXERLINE_LINEF_DISCONNECTED = 0x00008000,
'    '        MIXERLINE_LINEF_SOURCE = unchecked((int)0x80000000),
'    '----------------------------------^--- GenCode(token): unexpected token type
'    Friend Const MIXERLINE_COMPONENTTYPE_DST_FIRST As Integer = &H0
'    Friend Const MIXERLINE_COMPONENTTYPE_DST_UNDEFINED As Integer = &H0 + 0
'    Friend Const MIXERLINE_COMPONENTTYPE_DST_DIGITAL As Integer = &H0 + 1
'    Friend Const MIXERLINE_COMPONENTTYPE_DST_LINE As Integer = &H0 + 2
'    Friend Const MIXERLINE_COMPONENTTYPE_DST_MONITOR As Integer = &H0 + 3
'    Friend Const MIXERLINE_COMPONENTTYPE_DST_SPEAKERS As Integer = &H0 + 4
'    Friend Const MIXERLINE_COMPONENTTYPE_DST_HEADPHONES As Integer = &H0 + 5
'    Friend Const MIXERLINE_COMPONENTTYPE_DST_TELEPHONE As Integer = &H0 + 6
'    Friend Const MIXERLINE_COMPONENTTYPE_DST_WAVEIN As Integer = &H0 + 7
'    Friend Const MIXERLINE_COMPONENTTYPE_DST_VOICEIN As Integer = &H0 + 8
'    Friend Const MIXERLINE_COMPONENTTYPE_DST_LAST As Integer = &H0 + 8
'    Friend Const MIXERLINE_COMPONENTTYPE_SRC_FIRST As Integer = &H1000
'    Friend Const MIXERLINE_COMPONENTTYPE_SRC_UNDEFINED As Integer = &H1000 + 0
'    Friend Const MIXERLINE_COMPONENTTYPE_SRC_DIGITAL As Integer = &H1000 + 1
'    Friend Const MIXERLINE_COMPONENTTYPE_SRC_LINE As Integer = &H1000 + 2
'    Friend Const MIXERLINE_COMPONENTTYPE_SRC_MICROPHONE As Integer = &H1000 + 3
'    Friend Const MIXERLINE_COMPONENTTYPE_SRC_SYNTHESIZER As Integer = &H1000 + 4
'    Friend Const MIXERLINE_COMPONENTTYPE_SRC_COMPACTDISC As Integer = &H1000 + 5
'    Friend Const MIXERLINE_COMPONENTTYPE_SRC_TELEPHONE As Integer = &H1000 + 6
'    Friend Const MIXERLINE_COMPONENTTYPE_SRC_PCSPEAKER As Integer = &H1000 + 7
'    Friend Const MIXERLINE_COMPONENTTYPE_SRC_WAVEOUT As Integer = &H1000 + 8
'    Friend Const MIXERLINE_COMPONENTTYPE_SRC_AUXILIARY As Integer = &H1000 + 9
'    Friend Const MIXERLINE_COMPONENTTYPE_SRC_ANALOG As Integer = &H1000 + 10
'    Friend Const MIXERLINE_COMPONENTTYPE_SRC_LAST As Integer = &H1000 + 10
'    Friend Const MIXERLINE_TARGETTYPE_UNDEFINED As Integer = 0
'    Friend Const MIXERLINE_TARGETTYPE_WAVEOUT As Integer = 1
'    Friend Const MIXERLINE_TARGETTYPE_WAVEIN As Integer = 2
'    Friend Const MIXERLINE_TARGETTYPE_MIDIOUT As Integer = 3
'    Friend Const MIXERLINE_TARGETTYPE_MIDIIN As Integer = 4
'    Friend Const MIXERLINE_TARGETTYPE_AUX As Integer = 5
'    Friend Const MIXER_GETLINEINFOF_DESTINATION As Integer = &H0
'    Friend Const MIXER_GETLINEINFOF_SOURCE As Integer = &H1
'    Friend Const MIXER_GETLINEINFOF_LINEID As Integer = &H2
'    Friend Const MIXER_GETLINEINFOF_COMPONENTTYPE As Integer = &H3
'    Friend Const MIXER_GETLINEINFOF_TARGETTYPE As Integer = &H4
'    Friend Const MIXER_GETLINEINFOF_QUERYMASK As Integer = &HF
'    Friend Const MIXERCONTROL_CONTROLF_UNIFORM As Integer = &H1
'    Friend Const MIXERCONTROL_CONTROLF_MULTIPLE As Integer = &H2
'    Friend Const MIXERCONTROL_CONTROLF_DISABLED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        MIXERCONTROL_CONTROLF_MULTIPLE = 0x00000002,
'    '        MIXERCONTROL_CONTROLF_DISABLED = unchecked((int)0x80000000),
'    '------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const MIXERCONTROL_CT_CLASS_MASK As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        MIXERCONTROL_CONTROLF_DISABLED = unchecked((int)0x80000000),
'    '        MIXERCONTROL_CT_CLASS_MASK = unchecked((int)0xF0000000),
'    '--------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const MIXERCONTROL_CT_CLASS_CUSTOM As Integer = &H0
'    Friend Const MIXERCONTROL_CT_CLASS_METER As Integer = &H10000000
'    Friend Const MIXERCONTROL_CT_CLASS_SWITCH As Integer = &H20000000
'    Friend Const MIXERCONTROL_CT_CLASS_NUMBER As Integer = &H30000000
'    Friend Const MIXERCONTROL_CT_CLASS_SLIDER As Integer = &H40000000
'    Friend Const MIXERCONTROL_CT_CLASS_FADER As Integer = &H50000000
'    Friend Const MIXERCONTROL_CT_CLASS_TIME As Integer = &H60000000
'    Friend Const MIXERCONTROL_CT_CLASS_LIST As Integer = &H70000000
'    Friend Const MIXERCONTROL_CT_SUBCLASS_MASK As Integer = &HF000000
'    Friend Const MIXERCONTROL_CT_SC_SWITCH_BOOLEAN As Integer = &H0
'    Friend Const MIXERCONTROL_CT_SC_SWITCH_BUTTON As Integer = &H1000000
'    Friend Const MIXERCONTROL_CT_SC_METER_POLLED As Integer = &H0
'    Friend Const MIXERCONTROL_CT_SC_TIME_MICROSECS As Integer = &H0
'    Friend Const MIXERCONTROL_CT_SC_TIME_MILLISECS As Integer = &H1000000
'    Friend Const MIXERCONTROL_CT_SC_LIST_SINGLE As Integer = &H0
'    Friend Const MIXERCONTROL_CT_SC_LIST_MULTIPLE As Integer = &H1000000
'    Friend Const MIXERCONTROL_CT_UNITS_MASK As Integer = &HFF0000
'    Friend Const MIXERCONTROL_CT_UNITS_CUSTOM As Integer = &H0
'    Friend Const MIXERCONTROL_CT_UNITS_BOOLEAN As Integer = &H10000
'    Friend Const MIXERCONTROL_CT_UNITS_SIGNED As Integer = &H20000
'    Friend Const MIXERCONTROL_CT_UNITS_UNSIGNED As Integer = &H30000
'    Friend Const MIXERCONTROL_CT_UNITS_DECIBELS As Integer = &H40000
'    Friend Const MIXERCONTROL_CT_UNITS_PERCENT As Integer = &H50000
'    Friend Const MIXERCONTROL_CONTROLTYPE_CUSTOM As Integer = &H0 Or &H0
'    Friend Const MIXERCONTROL_CONTROLTYPE_BOOLEANMETER As Integer = &H10000000 Or &H0 Or &H10000
'    Friend Const MIXERCONTROL_CONTROLTYPE_SIGNEDMETER As Integer = &H10000000 Or &H0 Or &H20000
'    Friend Const MIXERCONTROL_CONTROLTYPE_PEAKMETER As Integer = (&H10000000 Or &H0 Or &H20000) + 1
'    Friend Const MIXERCONTROL_CONTROLTYPE_UNSIGNEDMETER As Integer = &H10000000 Or &H0 Or &H30000
'    Friend Const MIXERCONTROL_CONTROLTYPE_BOOLEAN As Integer = &H20000000 Or &H0 Or &H10000
'    Friend Const MIXERCONTROL_CONTROLTYPE_ONOFF As Integer = (&H20000000 Or &H0 Or &H10000) + 1
'    Friend Const MIXERCONTROL_CONTROLTYPE_MUTE As Integer = (&H20000000 Or &H0 Or &H10000) + 2
'    Friend Const MIXERCONTROL_CONTROLTYPE_MONO As Integer = (&H20000000 Or &H0 Or &H10000) + 3
'    Friend Const MIXERCONTROL_CONTROLTYPE_LOUDNESS As Integer = (&H20000000 Or &H0 Or &H10000) + 4
'    Friend Const MIXERCONTROL_CONTROLTYPE_STEREOENH As Integer = (&H20000000 Or &H0 Or &H10000) + 5
'    Friend Const MIXERCONTROL_CONTROLTYPE_BUTTON As Integer = &H20000000 Or &H1000000 Or &H10000
'    Friend Const MIXERCONTROL_CONTROLTYPE_DECIBELS As Integer = &H30000000 Or &H40000
'    Friend Const MIXERCONTROL_CONTROLTYPE_SIGNED As Integer = &H30000000 Or &H20000
'    Friend Const MIXERCONTROL_CONTROLTYPE_UNSIGNED As Integer = &H30000000 Or &H30000
'    Friend Const MIXERCONTROL_CONTROLTYPE_PERCENT As Integer = &H30000000 Or &H50000
'    Friend Const MIXERCONTROL_CONTROLTYPE_SLIDER As Integer = &H40000000 Or &H20000
'    Friend Const MIXERCONTROL_CONTROLTYPE_PAN As Integer = (&H40000000 Or &H20000) + 1
'    Friend Const MIXERCONTROL_CONTROLTYPE_QSOUNDPAN As Integer = (&H40000000 Or &H20000) + 2
'    Friend Const MIXERCONTROL_CONTROLTYPE_FADER As Integer = &H50000000 Or &H30000
'    Friend Const MIXERCONTROL_CONTROLTYPE_VOLUME As Integer = (&H50000000 Or &H30000) + 1
'    Friend Const MIXERCONTROL_CONTROLTYPE_BASS As Integer = (&H50000000 Or &H30000) + 2
'    Friend Const MIXERCONTROL_CONTROLTYPE_TREBLE As Integer = (&H50000000 Or &H30000) + 3
'    Friend Const MIXERCONTROL_CONTROLTYPE_EQUALIZER As Integer = (&H50000000 Or &H30000) + 4
'    Friend Const MIXERCONTROL_CONTROLTYPE_SINGLESELECT As Integer = &H70000000 Or &H0 Or &H10000
'    Friend Const MIXERCONTROL_CONTROLTYPE_MUX As Integer = (&H70000000 Or &H0 Or &H10000) + 1
'    Friend Const MIXERCONTROL_CONTROLTYPE_MULTIPLESELECT As Integer = &H70000000 Or &H1000000 Or &H10000
'    Friend Const MIXERCONTROL_CONTROLTYPE_MIXER As Integer = (&H70000000 Or &H1000000 Or &H10000) + 1
'    Friend Const MIXERCONTROL_CONTROLTYPE_MICROTIME As Integer = &H60000000 Or &H0 Or &H30000
'    Friend Const MIXERCONTROL_CONTROLTYPE_MILLITIME As Integer = &H60000000 Or &H1000000 Or &H30000
'    Friend Const MIXER_GETLINECONTROLSF_ALL As Integer = &H0
'    Friend Const MIXER_GETLINECONTROLSF_ONEBYID As Integer = &H1
'    Friend Const MIXER_GETLINECONTROLSF_ONEBYTYPE As Integer = &H2
'    Friend Const MIXER_GETLINECONTROLSF_QUERYMASK As Integer = &HF
'    Friend Const MIXER_GETCONTROLDETAILSF_VALUE As Integer = &H0
'    Friend Const MIXER_GETCONTROLDETAILSF_LISTTEXT As Integer = &H1
'    Friend Const MIXER_GETCONTROLDETAILSF_QUERYMASK As Integer = &HF
'    Friend Const MIXER_SETCONTROLDETAILSF_VALUE As Integer = &H0
'    Friend Const MIXER_SETCONTROLDETAILSF_CUSTOM As Integer = &H1
'    Friend Const MIXER_SETCONTROLDETAILSF_QUERYMASK As Integer = &HF
'    Friend Const MMIOERR_BASE As Integer = 256
'    Friend Const MMIOERR_FILENOTFOUND As Integer = 256 + 1
'    Friend Const MMIOERR_OUTOFMEMORY As Integer = 256 + 2
'    Friend Const MMIOERR_CANNOTOPEN As Integer = 256 + 3
'    Friend Const MMIOERR_CANNOTCLOSE As Integer = 256 + 4
'    Friend Const MMIOERR_CANNOTREAD As Integer = 256 + 5
'    Friend Const MMIOERR_CANNOTWRITE As Integer = 256 + 6
'    Friend Const MMIOERR_CANNOTSEEK As Integer = 256 + 7
'    Friend Const MMIOERR_CANNOTEXPAND As Integer = 256 + 8
'    Friend Const MMIOERR_CHUNKNOTFOUND As Integer = 256 + 9
'    Friend Const MMIOERR_UNBUFFERED As Integer = 256 + 10
'    Friend Const MMIOERR_PATHNOTFOUND As Integer = 256 + 11
'    Friend Const MMIOERR_ACCESSDENIED As Integer = 256 + 12
'    Friend Const MMIOERR_SHARINGVIOLATION As Integer = 256 + 13
'    Friend Const MMIOERR_NETWORKERROR As Integer = 256 + 14
'    Friend Const MMIOERR_TOOMANYOPENFILES As Integer = 256 + 15
'    Friend Const MMIOERR_INVALIDFILE As Integer = 256 + 16
'    Friend Const MMIO_RWMODE As Integer = &H3
'    Friend Const MMIO_SHAREMODE As Integer = &H70
'    Friend Const MMIO_CREATE As Integer = &H1000
'    Friend Const MMIO_PARSE As Integer = &H100
'    Friend Const MMIO_DELETE As Integer = &H200
'    Friend Const MMIO_EXIST As Integer = &H4000
'    Friend Const MMIO_ALLOCBUF As Integer = &H10000
'    Friend Const MMIO_GETTEMP As Integer = &H20000
'    Friend Const MMIO_DIRTY As Integer = &H10000000
'    Friend Const MMIO_READ As Integer = &H0
'    Friend Const MMIO_WRITE As Integer = &H1
'    Friend Const MMIO_READWRITE As Integer = &H2
'    Friend Const MMIO_COMPAT As Integer = &H0
'    Friend Const MMIO_EXCLUSIVE As Integer = &H10
'    Friend Const MMIO_DENYWRITE As Integer = &H20
'    Friend Const MMIO_DENYREAD As Integer = &H30
'    Friend Const MMIO_DENYNONE As Integer = &H40
'    Friend Const MMIO_FHOPEN As Integer = &H10
'    Friend Const MMIO_EMPTYBUF As Integer = &H10
'    Friend Const MMIO_TOUPPER As Integer = &H10
'    Friend Const MMIO_INSTALLPROC As Integer = &H10000
'    Friend Const MMIO_GLOBALPROC As Integer = &H10000000
'    Friend Const MMIO_REMOVEPROC As Integer = &H20000
'    Friend Const MMIO_UNICODEPROC As Integer = &H1000000
'    Friend Const MMIO_FINDPROC As Integer = &H40000
'    Friend Const MMIO_FINDCHUNK As Integer = &H10
'    Friend Const MMIO_FINDRIFF As Integer = &H20
'    Friend Const MMIO_FINDLIST As Integer = &H40
'    Friend Const MMIO_CREATERIFF As Integer = &H20
'    Friend Const MMIO_CREATELIST As Integer = &H40
'    Friend Const MMIOM_READ As Integer = &H0
'    Friend Const MMIOM_WRITE As Integer = &H1
'    Friend Const MMIOM_SEEK As Integer = 2
'    Friend Const MMIOM_OPEN As Integer = 3
'    Friend Const MMIOM_CLOSE As Integer = 4
'    Friend Const MMIOM_WRITEFLUSH As Integer = 5
'    Friend Const MMIOM_RENAME As Integer = 6
'    Friend Const MMIOM_USER As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        MMIOM_RENAME = 6,
'    '        MMIOM_USER = unchecked((int)0x8000),
'    '----------------------^--- GenCode(token): unexpected token type
'    Friend Const MMIO_DEFAULTBUFFER As Integer = 8192
'    Friend Const MCIERR_INVALID_DEVICE_ID As Integer = 256 + 1
'    Friend Const MCIERR_UNRECOGNIZED_KEYWORD As Integer = 256 + 3
'    Friend Const MCIERR_UNRECOGNIZED_COMMAND As Integer = 256 + 5
'    Friend Const MCIERR_HARDWARE As Integer = 256 + 6
'    Friend Const MCIERR_INVALID_DEVICE_NAME As Integer = 256 + 7
'    Friend Const MCIERR_OUT_OF_MEMORY As Integer = 256 + 8
'    Friend Const MCIERR_DEVICE_OPEN As Integer = 256 + 9
'    Friend Const MCIERR_CANNOT_LOAD_DRIVER As Integer = 256 + 10
'    Friend Const MCIERR_MISSING_COMMAND_STRING As Integer = 256 + 11
'    Friend Const MCIERR_PARAM_OVERFLOW As Integer = 256 + 12
'    Friend Const MCIERR_MISSING_STRING_ARGUMENT As Integer = 256 + 13
'    Friend Const MCIERR_BAD_INTEGER As Integer = 256 + 14
'    Friend Const MCIERR_PARSER_INTERNAL As Integer = 256 + 15
'    Friend Const MCIERR_DRIVER_INTERNAL As Integer = 256 + 16
'    Friend Const MCIERR_MISSING_PARAMETER As Integer = 256 + 17
'    Friend Const MCIERR_UNSUPPORTED_FUNCTION As Integer = 256 + 18
'    Friend Const MCIERR_FILE_NOT_FOUND As Integer = 256 + 19
'    Friend Const MCIERR_DEVICE_NOT_READY As Integer = 256 + 20
'    Friend Const MCIERR_INTERNAL As Integer = 256 + 21
'    Friend Const MCIERR_DRIVER As Integer = 256 + 22
'    Friend Const MCIERR_CANNOT_USE_ALL As Integer = 256 + 23
'    Friend Const MCIERR_MULTIPLE As Integer = 256 + 24
'    Friend Const MCIERR_EXTENSION_NOT_FOUND As Integer = 256 + 25
'    Friend Const MCIERR_OUTOFRANGE As Integer = 256 + 26
'    Friend Const MCIERR_FLAGS_NOT_COMPATIBLE As Integer = 256 + 28
'    Friend Const MCIERR_FILE_NOT_SAVED As Integer = 256 + 30
'    Friend Const MCIERR_DEVICE_TYPE_REQUIRED As Integer = 256 + 31
'    Friend Const MCIERR_DEVICE_LOCKED As Integer = 256 + 32
'    Friend Const MCIERR_DUPLICATE_ALIAS As Integer = 256 + 33
'    Friend Const MCIERR_MUST_USE_SHAREABLE As Integer = 256 + 35
'    Friend Const MCIERR_MISSING_DEVICE_NAME As Integer = 256 + 36
'    Friend Const MCIERR_BAD_TIME_FORMAT As Integer = 256 + 37
'    Friend Const MCIERR_NO_CLOSING_QUOTE As Integer = 256 + 38
'    Friend Const MCIERR_DUPLICATE_FLAGS As Integer = 256 + 39
'    Friend Const MCIERR_INVALID_FILE As Integer = 256 + 40
'    Friend Const MCIERR_NULL_PARAMETER_BLOCK As Integer = 256 + 41
'    Friend Const MCIERR_UNNAMED_RESOURCE As Integer = 256 + 42
'    Friend Const MCIERR_NEW_REQUIRES_ALIAS As Integer = 256 + 43
'    Friend Const MCIERR_NOTIFY_ON_AUTO_OPEN As Integer = 256 + 44
'    Friend Const MCIERR_NO_ELEMENT_ALLOWED As Integer = 256 + 45
'    Friend Const MCIERR_NONAPPLICABLE_FUNCTION As Integer = 256 + 46
'    Friend Const MCIERR_ILLEGAL_FOR_AUTO_OPEN As Integer = 256 + 47
'    Friend Const MCIERR_FILENAME_REQUIRED As Integer = 256 + 48
'    Friend Const MCIERR_EXTRA_CHARACTERS As Integer = 256 + 49
'    Friend Const MCIERR_DEVICE_NOT_INSTALLED As Integer = 256 + 50
'    Friend Const MCIERR_GET_CD As Integer = 256 + 51
'    Friend Const MCIERR_SET_CD As Integer = 256 + 52
'    Friend Const MCIERR_SET_DRIVE As Integer = 256 + 53
'    Friend Const MCIERR_DEVICE_LENGTH As Integer = 256 + 54
'    Friend Const MCIERR_DEVICE_ORD_LENGTH As Integer = 256 + 55
'    Friend Const MCIERR_NO_INTEGER As Integer = 256 + 56
'    Friend Const MCIERR_WAVE_OUTPUTSINUSE As Integer = 256 + 64
'    Friend Const MCIERR_WAVE_SETOUTPUTINUSE As Integer = 256 + 65
'    Friend Const MCIERR_WAVE_INPUTSINUSE As Integer = 256 + 66
'    Friend Const MCIERR_WAVE_SETINPUTINUSE As Integer = 256 + 67
'    Friend Const MCIERR_WAVE_OUTPUTUNSPECIFIED As Integer = 256 + 68
'    Friend Const MCIERR_WAVE_INPUTUNSPECIFIED As Integer = 256 + 69
'    Friend Const MCIERR_WAVE_OUTPUTSUNSUITABLE As Integer = 256 + 70
'    Friend Const MCIERR_WAVE_SETOUTPUTUNSUITABLE As Integer = 256 + 71
'    Friend Const MCIERR_WAVE_INPUTSUNSUITABLE As Integer = 256 + 72
'    Friend Const MCIERR_WAVE_SETINPUTUNSUITABLE As Integer = 256 + 73
'    Friend Const MCIERR_SEQ_DIV_INCOMPATIBLE As Integer = 256 + 80
'    Friend Const MCIERR_SEQ_PORT_INUSE As Integer = 256 + 81
'    Friend Const MCIERR_SEQ_PORT_NONEXISTENT As Integer = 256 + 82
'    Friend Const MCIERR_SEQ_PORT_MAPNODEVICE As Integer = 256 + 83
'    Friend Const MCIERR_SEQ_PORT_MISCERROR As Integer = 256 + 84
'    Friend Const MCIERR_SEQ_TIMER As Integer = 256 + 85
'    Friend Const MCIERR_SEQ_PORTUNSPECIFIED As Integer = 256 + 86
'    Friend Const MCIERR_SEQ_NOMIDIPRESENT As Integer = 256 + 87
'    Friend Const MCIERR_NO_WINDOW As Integer = 256 + 90
'    Friend Const MCIERR_CREATEWINDOW As Integer = 256 + 91
'    Friend Const MCIERR_FILE_READ As Integer = 256 + 92
'    Friend Const MCIERR_FILE_WRITE As Integer = 256 + 93
'    Friend Const MCIERR_NO_IDENTITY As Integer = 256 + 94
'    Friend Const MCIERR_CUSTOM_DRIVER_BASE As Integer = 256 + 256
'    Friend Const MCI_FIRST As Integer = &H800
'    Friend Const MCI_OPEN As Integer = &H803
'    Friend Const MCI_CLOSE As Integer = &H804
'    Friend Const MCI_ESCAPE As Integer = &H805
'    Friend Const MCI_PLAY As Integer = &H806
'    Friend Const MCI_SEEK As Integer = &H807
'    Friend Const MCI_STOP As Integer = &H808
'    Friend Const MCI_PAUSE As Integer = &H809
'    Friend Const MCI_INFO As Integer = &H80A
'    Friend Const MCI_GETDEVCAPS As Integer = &H80B
'    Friend Const MCI_SPIN As Integer = &H80C
'    Friend Const MCI_SET As Integer = &H80D
'    Friend Const MCI_STEP As Integer = &H80E
'    Friend Const MCI_RECORD As Integer = &H80F
'    Friend Const MCI_SYSINFO As Integer = &H810
'    Friend Const MCI_BREAK As Integer = &H811
'    Friend Const MCI_SAVE As Integer = &H813
'    Friend Const MCI_STATUS As Integer = &H814
'    Friend Const MCI_CUE As Integer = &H830
'    Friend Const MCI_REALIZE As Integer = &H840
'    Friend Const MCI_WINDOW As Integer = &H841
'    Friend Const MCI_PUT As Integer = &H842
'    Friend Const MCI_WHERE As Integer = &H843
'    Friend Const MCI_FREEZE As Integer = &H844
'    Friend Const MCI_UNFREEZE As Integer = &H845
'    Friend Const MCI_LOAD As Integer = &H850
'    Friend Const MCI_CUT As Integer = &H851
'    Friend Const MCI_COPY As Integer = &H852
'    Friend Const MCI_PASTE As Integer = &H853
'    Friend Const MCI_UPDATE As Integer = &H854
'    Friend Const MCI_RESUME As Integer = &H855
'    Friend Const MCI_DELETE As Integer = &H856
'    Friend Const MCI_USER_MESSAGES As Integer = &H800 + &H400
'    Friend Const MCI_LAST As Integer = &HFFF
'    Friend Const MCI_DEVTYPE_VCR As Integer = 513
'    Friend Const MCI_DEVTYPE_VIDEODISC As Integer = 514
'    Friend Const MCI_DEVTYPE_OVERLAY As Integer = 515
'    Friend Const MCI_DEVTYPE_CD_AUDIO As Integer = 516
'    Friend Const MCI_DEVTYPE_DAT As Integer = 517
'    Friend Const MCI_DEVTYPE_SCANNER As Integer = 518
'    Friend Const MCI_DEVTYPE_ANIMATION As Integer = 519
'    Friend Const MCI_DEVTYPE_DIGITAL_VIDEO As Integer = 520
'    Friend Const MCI_DEVTYPE_OTHER As Integer = 521
'    Friend Const MCI_DEVTYPE_WAVEFORM_AUDIO As Integer = 522
'    Friend Const MCI_DEVTYPE_SEQUENCER As Integer = 523
'    Friend Const MCI_DEVTYPE_FIRST As Integer = 513
'    Friend Const MCI_DEVTYPE_LAST As Integer = 523
'    Friend Const MCI_DEVTYPE_FIRST_USER As Integer = &H1000
'    Friend Const MCI_MODE_NOT_READY As Integer = 512 + 12
'    Friend Const MCI_MODE_STOP As Integer = 512 + 13
'    Friend Const MCI_MODE_PLAY As Integer = 512 + 14
'    Friend Const MCI_MODE_RECORD As Integer = 512 + 15
'    Friend Const MCI_MODE_SEEK As Integer = 512 + 16
'    Friend Const MCI_MODE_PAUSE As Integer = 512 + 17
'    Friend Const MCI_MODE_OPEN As Integer = 512 + 18
'    Friend Const MCI_FORMAT_MILLISECONDS As Integer = 0
'    Friend Const MCI_FORMAT_HMS As Integer = 1
'    Friend Const MCI_FORMAT_MSF As Integer = 2
'    Friend Const MCI_FORMAT_FRAMES As Integer = 3
'    Friend Const MCI_FORMAT_SMPTE_24 As Integer = 4
'    Friend Const MCI_FORMAT_SMPTE_25 As Integer = 5
'    Friend Const MCI_FORMAT_SMPTE_30 As Integer = 6
'    Friend Const MCI_FORMAT_SMPTE_30DROP As Integer = 7
'    Friend Const MCI_FORMAT_BYTES As Integer = 8
'    Friend Const MCI_FORMAT_SAMPLES As Integer = 9
'    Friend Const MCI_FORMAT_TMSF As Integer = 10
'    Friend Const MCI_NOTIFY_SUCCESSFUL As Integer = &H1
'    Friend Const MCI_NOTIFY_SUPERSEDED As Integer = &H2
'    Friend Const MCI_NOTIFY_ABORTED As Integer = &H4
'    Friend Const MCI_NOTIFY_FAILURE As Integer = &H8
'    Friend Const MCI_NOTIFY As Integer = &H1
'    Friend Const MCI_WAIT As Integer = &H2
'    Friend Const MCI_FROM As Integer = &H4
'    Friend Const MCI_TO As Integer = &H8
'    Friend Const MCI_TRACK As Integer = &H10
'    Friend Const MCI_OPEN_SHAREABLE As Integer = &H100
'    Friend Const MCI_OPEN_ELEMENT As Integer = &H200
'    Friend Const MCI_OPEN_ALIAS As Integer = &H400
'    Friend Const MCI_OPEN_ELEMENT_ID As Integer = &H800
'    Friend Const MCI_OPEN_TYPE_ID As Integer = &H1000
'    Friend Const MCI_OPEN_TYPE As Integer = &H2000
'    Friend Const MCI_SEEK_TO_START As Integer = &H100
'    Friend Const MCI_SEEK_TO_END As Integer = &H200
'    Friend Const MCI_STATUS_ITEM As Integer = &H100
'    Friend Const MCI_STATUS_START As Integer = &H200
'    Friend Const MCI_STATUS_LENGTH As Integer = &H1
'    Friend Const MCI_STATUS_POSITION As Integer = &H2
'    Friend Const MCI_STATUS_NUMBER_OF_TRACKS As Integer = &H3
'    Friend Const MCI_STATUS_MODE As Integer = &H4
'    Friend Const MCI_STATUS_MEDIA_PRESENT As Integer = &H5
'    Friend Const MCI_STATUS_TIME_FORMAT As Integer = &H6
'    Friend Const MCI_STATUS_READY As Integer = &H7
'    Friend Const MCI_STATUS_CURRENT_TRACK As Integer = &H8
'    Friend Const MCI_INFO_PRODUCT As Integer = &H100
'    Friend Const MCI_INFO_FILE As Integer = &H200
'    Friend Const MCI_INFO_MEDIA_UPC As Integer = &H400
'    Friend Const MCI_INFO_MEDIA_IDENTITY As Integer = &H800
'    Friend Const MCI_INFO_NAME As Integer = &H1000
'    Friend Const MCI_INFO_COPYRIGHT As Integer = &H2000
'    Friend Const MCI_GETDEVCAPS_ITEM As Integer = &H100
'    Friend Const MCI_GETDEVCAPS_CAN_RECORD As Integer = &H1
'    Friend Const MCI_GETDEVCAPS_HAS_AUDIO As Integer = &H2
'    Friend Const MCI_GETDEVCAPS_HAS_VIDEO As Integer = &H3
'    Friend Const MCI_GETDEVCAPS_DEVICE_TYPE As Integer = &H4
'    Friend Const MCI_GETDEVCAPS_USES_FILES As Integer = &H5
'    Friend Const MCI_GETDEVCAPS_COMPOUND_DEVICE As Integer = &H6
'    Friend Const MCI_GETDEVCAPS_CAN_EJECT As Integer = &H7
'    Friend Const MCI_GETDEVCAPS_CAN_PLAY As Integer = &H8
'    Friend Const MCI_GETDEVCAPS_CAN_SAVE As Integer = &H9
'    Friend Const MCI_SYSINFO_QUANTITY As Integer = &H100
'    Friend Const MCI_SYSINFO_OPEN As Integer = &H200
'    Friend Const MCI_SYSINFO_NAME As Integer = &H400
'    Friend Const MCI_SYSINFO_INSTALLNAME As Integer = &H800
'    Friend Const MCI_SET_DOOR_OPEN As Integer = &H100
'    Friend Const MCI_SET_DOOR_CLOSED As Integer = &H200
'    Friend Const MCI_SET_TIME_FORMAT As Integer = &H400
'    Friend Const MCI_SET_AUDIO As Integer = &H800
'    Friend Const MCI_SET_VIDEO As Integer = &H1000
'    Friend Const MCI_SET_ON As Integer = &H2000
'    Friend Const MCI_SET_OFF As Integer = &H4000
'    Friend Const MCI_SET_AUDIO_ALL As Integer = &H0
'    Friend Const MCI_SET_AUDIO_LEFT As Integer = &H1
'    Friend Const MCI_SET_AUDIO_RIGHT As Integer = &H2
'    Friend Const MCI_BREAK_KEY As Integer = &H100
'    Friend Const MCI_BREAK_HWND As Integer = &H200
'    Friend Const MCI_BREAK_OFF As Integer = &H400
'    Friend Const MCI_RECORD_INSERT As Integer = &H100
'    Friend Const MCI_RECORD_OVERWRITE As Integer = &H200
'    Friend Const MCI_SAVE_FILE As Integer = &H100
'    Friend Const MCI_LOAD_FILE As Integer = &H100
'    Friend Const MCI_VD_MODE_PARK As Integer = 1024 + 1
'    Friend Const MCI_VD_MEDIA_CLV As Integer = 1024 + 2
'    Friend Const MCI_VD_MEDIA_CAV As Integer = 1024 + 3
'    Friend Const MCI_VD_MEDIA_OTHER As Integer = 1024 + 4
'    Friend Const MCI_VD_FORMAT_TRACK As Integer = &H4001
'    Friend Const MCI_VD_PLAY_REVERSE As Integer = &H10000
'    Friend Const MCI_VD_PLAY_FAST As Integer = &H20000
'    Friend Const MCI_VD_PLAY_SPEED As Integer = &H40000
'    Friend Const MCI_VD_PLAY_SCAN As Integer = &H80000
'    Friend Const MCI_VD_PLAY_SLOW As Integer = &H100000
'    Friend Const MCI_VD_SEEK_REVERSE As Integer = &H10000
'    Friend Const MCI_VD_STATUS_SPEED As Integer = &H4002
'    Friend Const MCI_VD_STATUS_FORWARD As Integer = &H4003
'    Friend Const MCI_VD_STATUS_MEDIA_TYPE As Integer = &H4004
'    Friend Const MCI_VD_STATUS_SIDE As Integer = &H4005
'    Friend Const MCI_VD_STATUS_DISC_SIZE As Integer = &H4006
'    Friend Const MCI_VD_GETDEVCAPS_CLV As Integer = &H10000
'    Friend Const MCI_VD_GETDEVCAPS_CAV As Integer = &H20000
'    Friend Const MCI_VD_SPIN_UP As Integer = &H10000
'    Friend Const MCI_VD_SPIN_DOWN As Integer = &H20000
'    Friend Const MCI_VD_GETDEVCAPS_CAN_REVERSE As Integer = &H4002
'    Friend Const MCI_VD_GETDEVCAPS_FAST_RATE As Integer = &H4003
'    Friend Const MCI_VD_GETDEVCAPS_SLOW_RATE As Integer = &H4004
'    Friend Const MCI_VD_GETDEVCAPS_NORMAL_RATE As Integer = &H4005
'    Friend Const MCI_VD_STEP_FRAMES As Integer = &H10000
'    Friend Const MCI_VD_STEP_REVERSE As Integer = &H20000
'    Friend Const MCI_VD_ESCAPE_STRING As Integer = &H100
'    Friend Const MCI_CDA_STATUS_TYPE_TRACK As Integer = &H4001
'    Friend Const MCI_CDA_TRACK_AUDIO As Integer = 1088 + 0
'    Friend Const MCI_CDA_TRACK_OTHER As Integer = 1088 + 1
'    Friend Const MCI_WAVE_PCM As Integer = 1152 + 0
'    Friend Const MCI_WAVE_MAPPER As Integer = 1152 + 1
'    Friend Const MCI_WAVE_OPEN_BUFFER As Integer = &H10000
'    Friend Const MCI_WAVE_SET_FORMATTAG As Integer = &H10000
'    Friend Const MCI_WAVE_SET_CHANNELS As Integer = &H20000
'    Friend Const MCI_WAVE_SET_SAMPLESPERSEC As Integer = &H40000
'    Friend Const MCI_WAVE_SET_AVGBYTESPERSEC As Integer = &H80000
'    Friend Const MCI_WAVE_SET_BLOCKALIGN As Integer = &H100000
'    Friend Const MCI_WAVE_SET_BITSPERSAMPLE As Integer = &H200000
'    Friend Const MCI_WAVE_INPUT As Integer = &H400000
'    Friend Const MCI_WAVE_OUTPUT As Integer = &H800000
'    Friend Const MCI_WAVE_STATUS_FORMATTAG As Integer = &H4001
'    Friend Const MCI_WAVE_STATUS_CHANNELS As Integer = &H4002
'    Friend Const MCI_WAVE_STATUS_SAMPLESPERSEC As Integer = &H4003
'    Friend Const MCI_WAVE_STATUS_AVGBYTESPERSEC As Integer = &H4004
'    Friend Const MCI_WAVE_STATUS_BLOCKALIGN As Integer = &H4005
'    Friend Const MCI_WAVE_STATUS_BITSPERSAMPLE As Integer = &H4006
'    Friend Const MCI_WAVE_STATUS_LEVEL As Integer = &H4007
'    Friend Const MCI_WAVE_SET_ANYINPUT As Integer = &H4000000
'    Friend Const MCI_WAVE_SET_ANYOUTPUT As Integer = &H8000000
'    Friend Const MCI_WAVE_GETDEVCAPS_INPUTS As Integer = &H4001
'    Friend Const MCI_WAVE_GETDEVCAPS_OUTPUTS As Integer = &H4002
'    Friend Const MCI_SEQ_DIV_PPQN As Integer = 0 + 1216
'    Friend Const MCI_SEQ_DIV_SMPTE_24 As Integer = 1 + 1216
'    Friend Const MCI_SEQ_DIV_SMPTE_25 As Integer = 2 + 1216
'    Friend Const MCI_SEQ_DIV_SMPTE_30DROP As Integer = 3 + 1216
'    Friend Const MCI_SEQ_DIV_SMPTE_30 As Integer = 4 + 1216
'    Friend Const MCI_SEQ_FORMAT_SONGPTR As Integer = &H4001
'    Friend Const MCI_SEQ_FILE As Integer = &H4002
'    Friend Const MCI_SEQ_MIDI As Integer = &H4003
'    Friend Const MCI_SEQ_SMPTE As Integer = &H4004
'    Friend Const MCI_SEQ_NONE As Integer = 65533
'    Friend Const MCI_SEQ_MAPPER As Integer = 65535
'    Friend Const MCI_SEQ_STATUS_TEMPO As Integer = &H4002
'    Friend Const MCI_SEQ_STATUS_PORT As Integer = &H4003
'    Friend Const MCI_SEQ_STATUS_SLAVE As Integer = &H4007
'    Friend Const MCI_SEQ_STATUS_MASTER As Integer = &H4008
'    Friend Const MCI_SEQ_STATUS_OFFSET As Integer = &H4009
'    Friend Const MCI_SEQ_STATUS_DIVTYPE As Integer = &H400A
'    Friend Const MCI_SEQ_STATUS_NAME As Integer = &H400B
'    Friend Const MCI_SEQ_STATUS_COPYRIGHT As Integer = &H400C
'    Friend Const MCI_SEQ_SET_TEMPO As Integer = &H10000
'    Friend Const MCI_SEQ_SET_PORT As Integer = &H20000
'    Friend Const MCI_SEQ_SET_SLAVE As Integer = &H40000
'    Friend Const MCI_SEQ_SET_MASTER As Integer = &H80000
'    Friend Const MCI_SEQ_SET_OFFSET As Integer = &H1000000
'    Friend Const MCI_ANIM_OPEN_WS As Integer = &H10000
'    Friend Const MCI_ANIM_OPEN_PARENT As Integer = &H20000
'    Friend Const MCI_ANIM_OPEN_NOSTATIC As Integer = &H40000
'    Friend Const MCI_ANIM_PLAY_SPEED As Integer = &H10000
'    Friend Const MCI_ANIM_PLAY_REVERSE As Integer = &H20000
'    Friend Const MCI_ANIM_PLAY_FAST As Integer = &H40000
'    Friend Const MCI_ANIM_PLAY_SLOW As Integer = &H80000
'    Friend Const MCI_ANIM_PLAY_SCAN As Integer = &H100000
'    Friend Const MCI_ANIM_STEP_REVERSE As Integer = &H10000
'    Friend Const MCI_ANIM_STEP_FRAMES As Integer = &H20000
'    Friend Const MCI_ANIM_STATUS_SPEED As Integer = &H4001
'    Friend Const MCI_ANIM_STATUS_FORWARD As Integer = &H4002
'    Friend Const MCI_ANIM_STATUS_HWND As Integer = &H4003
'    Friend Const MCI_ANIM_STATUS_HPAL As Integer = &H4004
'    Friend Const MCI_ANIM_STATUS_STRETCH As Integer = &H4005
'    Friend Const MCI_ANIM_INFO_TEXT As Integer = &H10000
'    Friend Const MCI_ANIM_GETDEVCAPS_CAN_REVERSE As Integer = &H4001
'    Friend Const MCI_ANIM_GETDEVCAPS_FAST_RATE As Integer = &H4002
'    Friend Const MCI_ANIM_GETDEVCAPS_SLOW_RATE As Integer = &H4003
'    Friend Const MCI_ANIM_GETDEVCAPS_NORMAL_RATE As Integer = &H4004
'    Friend Const MCI_ANIM_GETDEVCAPS_PALETTES As Integer = &H4006
'    Friend Const MCI_ANIM_GETDEVCAPS_CAN_STRETCH As Integer = &H4007
'    Friend Const MCI_ANIM_GETDEVCAPS_MAX_WINDOWS As Integer = &H4008
'    Friend Const MCI_ANIM_REALIZE_NORM As Integer = &H10000
'    Friend Const MCI_ANIM_REALIZE_BKGD As Integer = &H20000
'    Friend Const MCI_ANIM_WINDOW_HWND As Integer = &H10000
'    Friend Const MCI_ANIM_WINDOW_STATE As Integer = &H40000
'    Friend Const MCI_ANIM_WINDOW_TEXT As Integer = &H80000
'    Friend Const MCI_ANIM_WINDOW_ENABLE_STRETCH As Integer = &H100000
'    Friend Const MCI_ANIM_WINDOW_DISABLE_STRETCH As Integer = &H200000
'    Friend Const MCI_ANIM_WINDOW_DEFAULT As Integer = &H0
'    Friend Const MCI_ANIM_RECT As Integer = &H10000
'    Friend Const MCI_ANIM_PUT_SOURCE As Integer = &H20000
'    Friend Const MCI_ANIM_PUT_DESTINATION As Integer = &H40000
'    Friend Const MCI_ANIM_WHERE_SOURCE As Integer = &H20000
'    Friend Const MCI_ANIM_WHERE_DESTINATION As Integer = &H40000
'    Friend Const MCI_ANIM_UPDATE_HDC As Integer = &H20000
'    Friend Const MCI_OVLY_OPEN_WS As Integer = &H10000
'    Friend Const MCI_OVLY_OPEN_PARENT As Integer = &H20000
'    Friend Const MCI_OVLY_STATUS_HWND As Integer = &H4001
'    Friend Const MCI_OVLY_STATUS_STRETCH As Integer = &H4002
'    Friend Const MCI_OVLY_INFO_TEXT As Integer = &H10000
'    Friend Const MCI_OVLY_GETDEVCAPS_CAN_STRETCH As Integer = &H4001
'    Friend Const MCI_OVLY_GETDEVCAPS_CAN_FREEZE As Integer = &H4002
'    Friend Const MCI_OVLY_GETDEVCAPS_MAX_WINDOWS As Integer = &H4003
'    Friend Const MCI_OVLY_WINDOW_HWND As Integer = &H10000
'    Friend Const MCI_OVLY_WINDOW_STATE As Integer = &H40000
'    Friend Const MCI_OVLY_WINDOW_TEXT As Integer = &H80000
'    Friend Const MCI_OVLY_WINDOW_ENABLE_STRETCH As Integer = &H100000
'    Friend Const MCI_OVLY_WINDOW_DISABLE_STRETCH As Integer = &H200000
'    Friend Const MCI_OVLY_WINDOW_DEFAULT As Integer = &H0
'    Friend Const MCI_OVLY_RECT As Integer = &H10000
'    Friend Const MCI_OVLY_PUT_SOURCE As Integer = &H20000
'    Friend Const MCI_OVLY_PUT_DESTINATION As Integer = &H40000
'    Friend Const MCI_OVLY_PUT_FRAME As Integer = &H80000
'    Friend Const MCI_OVLY_PUT_VIDEO As Integer = &H100000
'    Friend Const MCI_OVLY_WHERE_SOURCE As Integer = &H20000
'    Friend Const MCI_OVLY_WHERE_DESTINATION As Integer = &H40000
'    Friend Const MCI_OVLY_WHERE_FRAME As Integer = &H80000
'    Friend Const MCI_OVLY_WHERE_VIDEO As Integer = &H100000
'    Friend Const MAX_LANA As Integer = 254
'    Friend Const MARSHALINTERFACE_MIN As Integer = 500
'    Friend Const MEMBERID_NIL As Integer = - 1
'    Friend Const MK_ALT As Integer = &H20
'    Friend Const MAXPROPPAGES As Integer = 100
'    Friend Const MARKPARITY As Integer = 3
'    Friend Const MS_CTS_ON As Integer = &H10
'    Friend Const MS_DSR_ON As Integer = &H20
'    Friend Const MS_RING_ON As Integer = &H40
'    Friend Const MS_RLSD_ON As Integer = &H80
'    Friend Const MAXINTATOM As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                             MS_RLSD_ON = (0x0080),
'    '                                                          MAXINTATOM = unchecked((int)0xC000),
'    '------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const MOVEFILE_REPLACE_EXISTING As Integer = &H1
'    Friend Const MOVEFILE_COPY_ALLOWED As Integer = &H2
'    Friend Const MOVEFILE_DELAY_UNTIL_REBOOT As Integer = &H4
'    Friend Const MOVEFILE_WRITE_THROUGH As Integer = &H8
'    Friend Const MAX_COMPUTERNAME_LENGTH As Integer = 15
'    Friend Const MAX_PROFILE_LEN As Integer = 80
'    Friend Const MOUSE_MOVED As Integer = &H1
'    Friend Const MOUSE_EVENT As Integer = &H2
'    Friend Const MENU_EVENT As Integer = &H8
'    Friend Const MAXUIDLEN As Integer = 64
     Friend Const MAX_PATH As Integer = 260
'    Friend Const MARSHAL_E_FIRST As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        MAX_PATH = 260,
'    '        MARSHAL_E_FIRST = unchecked((int)0x80040120),
'    '---------------------------^--- GenCode(token): unexpected token type
'    Friend Const MARSHAL_E_LAST As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        MARSHAL_E_FIRST = unchecked((int)0x80040120),
'    '        MARSHAL_E_LAST = unchecked((int)0x8004012F),
'    '--------------------------^--- GenCode(token): unexpected token type
'    Friend Const MARSHAL_S_FIRST As Integer = &H40120
'    Friend Const MARSHAL_S_LAST As Integer = &H4012F
'    Friend Const MK_E_FIRST As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        MARSHAL_S_LAST = 0x0004012F,
'    '        MK_E_FIRST = unchecked((int)0x800401E0),
'    '----------------------^--- GenCode(token): unexpected token type
'    Friend Const MK_E_LAST As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        MK_E_FIRST = unchecked((int)0x800401E0),
'    '        MK_E_LAST = unchecked((int)0x800401EF),
'    '---------------------^--- GenCode(token): unexpected token type
'    Friend Const MK_S_FIRST As Integer = &H401E0
'    Friend Const MK_S_LAST As Integer = &H401EF
'    Friend Const MK_E_CONNECTMANUALLY As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        MK_S_LAST = 0x000401EF,
'    '        MK_E_CONNECTMANUALLY = unchecked((int)0x800401E0),
'    '--------------------------------^--- GenCode(token): unexpected token type
'    Friend Const MK_E_EXCEEDEDDEADLINE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        MK_E_CONNECTMANUALLY = unchecked((int)0x800401E0),
'    '        MK_E_EXCEEDEDDEADLINE = unchecked((int)0x800401E1),
'    '---------------------------------^--- GenCode(token): unexpected token type
'    Friend Const MK_E_NEEDGENERIC As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        MK_E_EXCEEDEDDEADLINE = unchecked((int)0x800401E1),
'    '        MK_E_NEEDGENERIC = unchecked((int)0x800401E2),
'    '----------------------------^--- GenCode(token): unexpected token type
'    Friend Const MK_E_UNAVAILABLE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        MK_E_NEEDGENERIC = unchecked((int)0x800401E2),
'    '        MK_E_UNAVAILABLE = unchecked((int)0x800401E3),
'    '----------------------------^--- GenCode(token): unexpected token type
'    Friend Const MK_E_SYNTAX As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        MK_E_UNAVAILABLE = unchecked((int)0x800401E3),
'    '        MK_E_SYNTAX = unchecked((int)0x800401E4),
'    '-----------------------^--- GenCode(token): unexpected token type
'    Friend Const MK_E_NOOBJECT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        MK_E_SYNTAX = unchecked((int)0x800401E4),
'    '        MK_E_NOOBJECT = unchecked((int)0x800401E5),
'    '-------------------------^--- GenCode(token): unexpected token type
'    Friend Const MK_E_INVALIDEXTENSION As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        MK_E_NOOBJECT = unchecked((int)0x800401E5),
'    '        MK_E_INVALIDEXTENSION = unchecked((int)0x800401E6),
'    '---------------------------------^--- GenCode(token): unexpected token type
'    Friend Const MK_E_INTERMEDIATEINTERFACENOTSUPPORTED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        MK_E_INVALIDEXTENSION = unchecked((int)0x800401E6),
'    '        MK_E_INTERMEDIATEINTERFACENOTSUPPORTED = unchecked((int)0x800401E7),
'    '--------------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const MK_E_NOTBINDABLE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        MK_E_INTERMEDIATEINTERFACENOTSUPPORTED = unchecked((int)0x800401E7),
'    '        MK_E_NOTBINDABLE = unchecked((int)0x800401E8),
'    '----------------------------^--- GenCode(token): unexpected token type
'    Friend Const MK_E_NOTBOUND As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        MK_E_NOTBINDABLE = unchecked((int)0x800401E8),
'    '        MK_E_NOTBOUND = unchecked((int)0x800401E9),
'    '-------------------------^--- GenCode(token): unexpected token type
'    Friend Const MK_E_CANTOPENFILE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        MK_E_NOTBOUND = unchecked((int)0x800401E9),
'    '        MK_E_CANTOPENFILE = unchecked((int)0x800401EA),
'    '-----------------------------^--- GenCode(token): unexpected token type
'    Friend Const MK_E_MUSTBOTHERUSER As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        MK_E_CANTOPENFILE = unchecked((int)0x800401EA),
'    '        MK_E_MUSTBOTHERUSER = unchecked((int)0x800401EB),
'    '-------------------------------^--- GenCode(token): unexpected token type
'    Friend Const MK_E_NOINVERSE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        MK_E_MUSTBOTHERUSER = unchecked((int)0x800401EB),
'    '        MK_E_NOINVERSE = unchecked((int)0x800401EC),
'    '--------------------------^--- GenCode(token): unexpected token type
'    Friend Const MK_E_NOSTORAGE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        MK_E_NOINVERSE = unchecked((int)0x800401EC),
'    '        MK_E_NOSTORAGE = unchecked((int)0x800401ED),
'    '--------------------------^--- GenCode(token): unexpected token type
'    Friend Const MK_E_NOPREFIX As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        MK_E_NOSTORAGE = unchecked((int)0x800401ED),
'    '        MK_E_NOPREFIX = unchecked((int)0x800401EE),
'    '-------------------------^--- GenCode(token): unexpected token type
'    Friend Const MK_E_ENUMERATION_FAILED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        MK_E_NOPREFIX = unchecked((int)0x800401EE),
'    '        MK_E_ENUMERATION_FAILED = unchecked((int)0x800401EF),
'    '-----------------------------------^--- GenCode(token): unexpected token type
'    Friend Const MK_S_REDUCED_TO_SELF As Integer = &H401E2
'    Friend Const MK_S_ME As Integer = &H401E4
'    Friend Const MK_S_HIM As Integer = &H401E5
'    Friend Const MK_S_US As Integer = &H401E6
'    Friend Const MK_S_MONIKERALREADYREGISTERED As Integer = &H401E7
'    Friend Const MK_E_NO_NORMALIZED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        MK_S_MONIKERALREADYREGISTERED = 0x000401E7,
'    '        MK_E_NO_NORMALIZED = unchecked((int)0x80080007),
'    '------------------------------^--- GenCode(token): unexpected token type
'    Friend Const MEM_E_INVALID_ROOT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        MK_E_NO_NORMALIZED = unchecked((int)0x80080007),
'    '        MEM_E_INVALID_ROOT = unchecked((int)0x80080009),
'    '------------------------------^--- GenCode(token): unexpected token type
'    Friend Const MEM_E_INVALID_LINK As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        MEM_E_INVALID_ROOT = unchecked((int)0x80080009),
'    '        MEM_E_INVALID_LINK = unchecked((int)0x80080010),
'    '------------------------------^--- GenCode(token): unexpected token type
'    Friend Const MEM_E_INVALID_SIZE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        MEM_E_INVALID_LINK = unchecked((int)0x80080010),
'    '        MEM_E_INVALID_SIZE = unchecked((int)0x80080011),
'    '------------------------------^--- GenCode(token): unexpected token type
'    Friend Const MAXSTRETCHBLTMODE As Integer = 4
'    Friend Const META_SETBKCOLOR As Integer = &H201
'    Friend Const META_SETBKMODE As Integer = &H102
'    Friend Const META_SETMAPMODE As Integer = &H103
'    Friend Const META_SETROP2 As Integer = &H104
'    Friend Const META_SETRELABS As Integer = &H105
'    Friend Const META_SETPOLYFILLMODE As Integer = &H106
'    Friend Const META_SETSTRETCHBLTMODE As Integer = &H107
'    Friend Const META_SETTEXTCHAREXTRA As Integer = &H108
'    Friend Const META_SETTEXTCOLOR As Integer = &H209
'    Friend Const META_SETTEXTJUSTIFICATION As Integer = &H20A
'    Friend Const META_SETWINDOWORG As Integer = &H20B
'    Friend Const META_SETWINDOWEXT As Integer = &H20C
'    Friend Const META_SETVIEWPORTORG As Integer = &H20D
'    Friend Const META_SETVIEWPORTEXT As Integer = &H20E
'    Friend Const META_OFFSETWINDOWORG As Integer = &H20F
'    Friend Const META_SCALEWINDOWEXT As Integer = &H410
'    Friend Const META_OFFSETVIEWPORTORG As Integer = &H211
'    Friend Const META_SCALEVIEWPORTEXT As Integer = &H412
'    Friend Const META_LINETO As Integer = &H213
'    Friend Const META_MOVETO As Integer = &H214
'    Friend Const META_EXCLUDECLIPRECT As Integer = &H415
'    Friend Const META_INTERSECTCLIPRECT As Integer = &H416
'    Friend Const META_ARC As Integer = &H817
'    Friend Const META_ELLIPSE As Integer = &H418
'    Friend Const META_FLOODFILL As Integer = &H419
'    Friend Const META_PIE As Integer = &H81A
'    Friend Const META_RECTANGLE As Integer = &H41B
'    Friend Const META_ROUNDRECT As Integer = &H61C
'    Friend Const META_PATBLT As Integer = &H61D
'    Friend Const META_SAVEDC As Integer = &H1E
'    Friend Const META_SETPIXEL As Integer = &H41F
'    Friend Const META_OFFSETCLIPRGN As Integer = &H220
'    Friend Const META_TEXTOUT As Integer = &H521
'    Friend Const META_BITBLT As Integer = &H922
'    Friend Const META_STRETCHBLT As Integer = &HB23
'    Friend Const META_POLYGON As Integer = &H324
'    Friend Const META_POLYLINE As Integer = &H325
'    Friend Const META_ESCAPE As Integer = &H626
'    Friend Const META_RESTOREDC As Integer = &H127
'    Friend Const META_FILLREGION As Integer = &H228
'    Friend Const META_FRAMEREGION As Integer = &H429
'    Friend Const META_INVERTREGION As Integer = &H12A
'    Friend Const META_PAINTREGION As Integer = &H12B
'    Friend Const META_SELECTCLIPREGION As Integer = &H12C
'    Friend Const META_SELECTOBJECT As Integer = &H12D
'    Friend Const META_SETTEXTALIGN As Integer = &H12E
'    Friend Const META_CHORD As Integer = &H830
'    Friend Const META_SETMAPPERFLAGS As Integer = &H231
'    Friend Const META_EXTTEXTOUT As Integer = &HA32
'    Friend Const META_SETDIBTODEV As Integer = &HD33
'    Friend Const META_SELECTPALETTE As Integer = &H234
'    Friend Const META_REALIZEPALETTE As Integer = &H35
'    Friend Const META_ANIMATEPALETTE As Integer = &H436
'    Friend Const META_SETPALENTRIES As Integer = &H37
'    Friend Const META_POLYPOLYGON As Integer = &H538
'    Friend Const META_RESIZEPALETTE As Integer = &H139
'    Friend Const META_DIBBITBLT As Integer = &H940
'    Friend Const META_DIBSTRETCHBLT As Integer = &HB41
'    Friend Const META_DIBCREATEPATTERNBRUSH As Integer = &H142
'    Friend Const META_STRETCHDIB As Integer = &HF43
'    Friend Const META_EXTFLOODFILL As Integer = &H548
'    Friend Const META_DELETEOBJECT As Integer = &H1F0
'    Friend Const META_CREATEPALETTE As Integer = &HF7
'    Friend Const META_CREATEPATTERNBRUSH As Integer = &H1F9
'    Friend Const META_CREATEPENINDIRECT As Integer = &H2FA
'    Friend Const META_CREATEFONTINDIRECT As Integer = &H2FB
'    Friend Const META_CREATEBRUSHINDIRECT As Integer = &H2FC
'    Friend Const META_CREATEREGION As Integer = &H6FF
'    Friend Const MFCOMMENT As Integer = 15
'    Friend Const MOUSETRAILS As Integer = 39
'    Friend Const MWT_IDENTITY As Integer = 1
'    Friend Const MWT_LEFTMULTIPLY As Integer = 2
'    Friend Const MWT_RIGHTMULTIPLY As Integer = 3
'    Friend Const MWT_MIN As Integer = 1
'    Friend Const MWT_MAX As Integer = 3
'    Friend Const MONO_FONT As Integer = 8
'    Friend Const MAC_CHARSET As Integer = 77
'    Friend Const MM_TEXT As Integer = 1
'    Friend Const MM_LOMETRIC As Integer = 2
'    Friend Const MM_HIMETRIC As Integer = 3
'    Friend Const MM_LOENGLISH As Integer = 4
'    Friend Const MM_HIENGLISH As Integer = 5
'    Friend Const MM_TWIPS As Integer = 6
'    Friend Const MM_ISOTROPIC As Integer = 7
'    Friend Const MM_ANISOTROPIC As Integer = 8
'    Friend Const MM_MIN As Integer = 1
'    Friend Const MM_MAX As Integer = 8
'    Friend Const MM_MAX_FIXEDSCALE As Integer = 6
'    Friend Const MAX_LEADBYTES As Integer = 12
'    Friend Const MAX_DEFAULTCHAR As Integer = 2
'    Friend Const MB_PRECOMPOSED As Integer = &H1
'    Friend Const MB_COMPOSITE As Integer = &H2
'    Friend Const MB_USEGLYPHCHARS As Integer = &H4
'    Friend Const MB_ERR_INVALID_CHARS As Integer = &H8
'    Friend Const MAP_FOLDCZONE As Integer = &H10
'    Friend Const MAP_PRECOMPOSED As Integer = &H20
'    Friend Const MAP_COMPOSITE As Integer = &H40
'    Friend Const MAP_FOLDDIGITS As Integer = &H80
'    Friend Const MAXLONGLONG As Long = __unknown
'    Friend Const MINCHAR As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        public const long MAXLONGLONG = (0x7fffffffffffffffL);
'    '        public const int MINCHAR = unchecked((int)0x80),
'    '------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const MAXCHAR As Integer = &H7F
'    Friend Const MINSHORT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        MAXCHAR = 0x7f,
'    '        MINSHORT = unchecked((int)0x8000),
'    '--------------------^--- GenCode(token): unexpected token type
'    Friend Const MAXSHORT As Integer = &H7FFF
'    Friend Const MINLONG As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        MAXSHORT = 0x7fff,
'    '        MINLONG = unchecked((int)0x80000000),
'    '-------------------^--- GenCode(token): unexpected token type
'    Friend Const MAXLONG As Integer = &H7FFFFFFF
'    Friend Const MAXBYTE As Integer = &HFF
'    Friend Const MAXWORD As Integer = &HFFFF
'    Friend Const MAXDWORD As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        MAXWORD = 0xffff,
'    '        MAXDWORD = unchecked((int)0xFfffffff),
'    '--------------------^--- GenCode(token): unexpected token type
'    Friend Const MAXIMUM_WAIT_OBJECTS As Integer = 64
'    Friend Const MAXIMUM_SUSPEND_COUNT As Integer = &H7F
'    Friend Const MAXIMUM_PROCESSORS As Integer = 32
'    Friend Const MUTANT_QUERY_STATE As Integer = &H1
'    Friend Const MEM_COMMIT As Integer = &H1000
'    Friend Const MEM_RESERVE As Integer = &H2000
'    Friend Const MEM_DECOMMIT As Integer = &H4000
'    Friend Const MEM_RELEASE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        MEM_DECOMMIT = 0x4000,
'    '        MEM_RELEASE = unchecked((int)0x8000),
'    '-----------------------^--- GenCode(token): unexpected token type
'    Friend Const MEM_FREE As Integer = &H10000
'    Friend Const MEM_PRIVATE As Integer = &H20000
'    Friend Const MEM_MAPPED As Integer = &H40000
'    Friend Const MEM_RESET As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        MEM_MAPPED = 0x40000,
'    '        MEM_RESET = unchecked((int)0x80000),
'    '---------------------^--- GenCode(token): unexpected token type
'    Friend Const MEM_TOP_DOWN As Integer = &H100000
'    Friend Const MEM_IMAGE As Integer = &H1000000
'    Friend Const MAILSLOT_NO_MESSAGE As Integer = - 1
'    Friend Const MAILSLOT_WAIT_FOREVER As Integer = - 1
'    Friend Const MAXIMUM_ALLOWED As Integer = &H2000000
'    Friend Const MESSAGE_RESOURCE_UNICODE As Integer = &H1
'    Friend Const MAX_PRIORITY As Integer = 99
'    Friend Const MIN_PRIORITY As Integer = 1
'    Friend Const MSGF_DIALOGBOX As Integer = 0
'    Friend Const MSGF_MESSAGEBOX As Integer = 1
'    Friend Const MSGF_MENU As Integer = 2
'    Friend Const MSGF_MOVE As Integer = 3
'    Friend Const MSGF_SIZE As Integer = 4
'    Friend Const MSGF_SCROLLBAR As Integer = 5
'    Friend Const MSGF_NEXTWINDOW As Integer = 6
'    Friend Const MSGF_MAINLOOP As Integer = 8
'    Friend Const MSGF_MAX As Integer = 8
'    Friend Const MSGF_USER As Integer = 4096
'    Friend Const MENULOOP_WINDOW As Integer = 0
'    Friend Const MENULOOP_POPUP As Integer = 1
'    Friend Const MA_ACTIVATE As Integer = 1
'    Friend Const MA_ACTIVATEANDEAT As Integer = 2
'    Friend Const MA_NOACTIVATE As Integer = 3
'    Friend Const MA_NOACTIVATEANDEAT As Integer = 4
'    Friend Const MK_LBUTTON As Integer = &H1
'    Friend Const MK_RBUTTON As Integer = &H2
'    Friend Const MK_SHIFT As Integer = &H4
'    Friend Const MK_CONTROL As Integer = &H8
'    Friend Const MK_MBUTTON As Integer = &H10
'    Friend Const MOD_WIN As Integer = &H8
'    Friend Const MOUSEEVENTF_MOVE As Integer = &H1
'    Friend Const MOUSEEVENTF_LEFTDOWN As Integer = &H2
'    Friend Const MOUSEEVENTF_LEFTUP As Integer = &H4
'    Friend Const MOUSEEVENTF_RIGHTDOWN As Integer = &H8
'    Friend Const MOUSEEVENTF_RIGHTUP As Integer = &H10
'    Friend Const MOUSEEVENTF_MIDDLEDOWN As Integer = &H20
'    Friend Const MOUSEEVENTF_MIDDLEUP As Integer = &H40
'    Friend Const MOUSEEVENTF_WHEEL As Integer = &H800
'    Friend Const MOUSEEVENTF_ABSOLUTE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        MOUSEEVENTF_WHEEL = 0x0800,
'    '        MOUSEEVENTF_ABSOLUTE = unchecked((int)0x8000),
'    '--------------------------------^--- GenCode(token): unexpected token type
'    Friend Const MWMO_WAITALL As Integer = &H1
'    Friend Const MWMO_ALERTABLE As Integer = &H2
'    Friend Const MNC_IGNORE As Integer = 0
'    Friend Const MNC_CLOSE As Integer = 1
'    Friend Const MNC_EXECUTE As Integer = 2
'    Friend Const MNC_SELECT As Integer = 3
'    Friend Const MIIM_STATE As Integer = &H1
'    Friend Const MIIM_ID As Integer = &H2
'    Friend Const MIIM_SUBMENU As Integer = &H4
'    Friend Const MIIM_CHECKMARKS As Integer = &H8
'    Friend Const MIIM_TYPE As Integer = &H10
'    Friend Const MIIM_DATA As Integer = &H20
'    Friend Const MB_OK As Integer = &H0
'    Friend Const MB_OKCANCEL As Integer = &H1
'    Friend Const MB_ABORTRETRYIGNORE As Integer = &H2
'    Friend Const MB_YESNOCANCEL As Integer = &H3
'    Friend Const MB_YESNO As Integer = &H4
'    Friend Const MB_RETRYCANCEL As Integer = &H5
'    Friend Const MB_ICONHAND As Integer = &H10
'    Friend Const MB_ICONQUESTION As Integer = &H20
'    Friend Const MB_ICONEXCLAMATION As Integer = &H30
'    Friend Const MB_ICONASTERISK As Integer = &H40
'    Friend Const MB_USERICON As Integer = &H80
'    Friend Const MB_ICONWARNING As Integer = &H30
'    Friend Const MB_ICONERROR As Integer = &H10
'    Friend Const MB_ICONINFORMATION As Integer = &H40
'    Friend Const MB_DEFBUTTON1 As Integer = &H0
'    Friend Const MB_DEFBUTTON2 As Integer = &H100
'    Friend Const MB_DEFBUTTON3 As Integer = &H200
'    Friend Const MB_DEFBUTTON4 As Integer = &H300
'    Friend Const MB_APPLMODAL As Integer = &H0
'    Friend Const MB_SYSTEMMODAL As Integer = &H1000
'    Friend Const MB_TASKMODAL As Integer = &H2000
'    Friend Const MB_HELP As Integer = &H4000
'    Friend Const MB_NOFOCUS As Integer = &H8000
'    Friend Const MB_SETFOREGROUND As Integer = &H10000
'    Friend Const MB_DEFAULT_DESKTOP_ONLY As Integer = &H20000
'    Friend Const MB_TOPMOST As Integer = &H40000
'    Friend Const MB_RIGHT As Integer = &H80000
'    Friend Const MB_RTLREADING As Integer = &H100000
'    Friend Const MB_SERVICE_NOTIFICATION As Integer = &H200000 '
'    'Note:  Error processing original source shown below
'    '        public const long MAXLONGLONG = (0x7fffffffffffffffL);
'    '        public const int MINCHAR = unchecked((int)0x80),
'    '--------------------------------------------------------------^--- Numeric constant overflow
'    ' MB_SERVICE_NOTIFICATION = 0x00040000;
'    Friend Const MB_SERVICE_NOTIFICATION_NT3X As Integer = &H40000
'    Friend Const MB_TYPEMASK As Integer = &HF
'    Friend Const MB_ICONMASK As Integer = &HF0
'    Friend Const MB_DEFMASK As Integer = &HF00
'    Friend Const MB_MODEMASK As Integer = &H3000
'    Friend Const MB_MISCMASK As Integer = &HC000
'    Friend Const MF_INSERT As Integer = &H0
'    Friend Const MF_CHANGE As Integer = &H80
'    Friend Const MF_APPEND As Integer = &H100
'    Friend Const MF_DELETE As Integer = &H200
'    Friend Const MF_REMOVE As Integer = &H1000
'    Friend Const MF_BYCOMMAND As Integer = &H0
'    Friend Const MF_BYPOSITION As Integer = &H400
'    Friend Const MF_SEPARATOR As Integer = &H800
'    Friend Const MF_ENABLED As Integer = &H0
'    Friend Const MF_GRAYED As Integer = &H1
'    Friend Const MF_DISABLED As Integer = &H2
'    Friend Const MF_UNCHECKED As Integer = &H0
'    Friend Const MF_CHECKED As Integer = &H8
'    Friend Const MF_USECHECKBITMAPS As Integer = &H200
'    Friend Const MF_STRING As Integer = &H0
'    Friend Const MF_BITMAP As Integer = &H4
'    Friend Const MF_OWNERDRAW As Integer = &H100
'    Friend Const MF_POPUP As Integer = &H10
'    Friend Const MF_MENUBARBREAK As Integer = &H20
'    Friend Const MF_MENUBREAK As Integer = &H40
'    Friend Const MF_UNHILITE As Integer = &H0
'    Friend Const MF_HILITE As Integer = &H80
'    Friend Const MF_DEFAULT As Integer = &H1000
'    Friend Const MF_SYSMENU As Integer = &H2000
'    Friend Const MF_HELP As Integer = &H4000
'    Friend Const MF_RIGHTJUSTIFY As Integer = &H4000
'    Friend Const MF_MOUSESELECT As Integer = &H8000
'    Friend Const MF_END As Integer = &H80
'    Friend Const MFT_STRING As Integer = &H0
'    Friend Const MFT_BITMAP As Integer = &H4
'    Friend Const MFT_MENUBARBREAK As Integer = &H20
'    Friend Const MFT_MENUBREAK As Integer = &H40
'    Friend Const MFT_OWNERDRAW As Integer = &H100
'    Friend Const MFT_RADIOCHECK As Integer = &H200
'    Friend Const MFT_SEPARATOR As Integer = &H800
'    Friend Const MFT_RIGHTORDER As Integer = &H2000
'    Friend Const MFT_RIGHTJUSTIFY As Integer = &H4000
'    Friend Const MFS_GRAYED As Integer = &H3
'    Friend Const MFS_DISABLED As Integer = &H3
'    Friend Const MFS_CHECKED As Integer = &H8
'    Friend Const MFS_HILITE As Integer = &H80
'    Friend Const MFS_ENABLED As Integer = &H0
'    Friend Const MFS_UNCHECKED As Integer = &H0
'    Friend Const MFS_UNHILITE As Integer = &H0
'    Friend Const MFS_DEFAULT As Integer = &H1000
'    Friend Const MDIS_ALLCHILDSTYLES As Integer = &H1
'    Friend Const MDITILE_VERTICAL As Integer = &H0
'    Friend Const MDITILE_HORIZONTAL As Integer = &H1
'    Friend Const MDITILE_SKIPDISABLED As Integer = &H2
'    Friend Const METRICS_USEDEFAULT As Integer = - 1
'    Friend Const MKF_MOUSEKEYSON As Integer = &H1
'    Friend Const MKF_AVAILABLE As Integer = &H2
'    Friend Const MKF_HOTKEYACTIVE As Integer = &H4
'    Friend Const MKF_CONFIRMHOTKEY As Integer = &H8
'    Friend Const MKF_HOTKEYSOUND As Integer = &H10
'    Friend Const MKF_INDICATOR As Integer = &H20
'    Friend Const MKF_MODIFIERS As Integer = &H40
'    Friend Const MKF_REPLACENUMBERS As Integer = &H80
'    Friend Const MCN_FIRST As Integer = 0 - 750
'    Friend Const MCN_LAST As Integer = 0 - 759
'    Friend Const MSGF_COMMCTRL_BEGINDRAG As Integer = &H4200
'    Friend Const MSGF_COMMCTRL_SIZEHEADER As Integer = &H4201
'    Friend Const MSGF_COMMCTRL_DRAGSELECT As Integer = &H4202
'    Friend Const MSGF_COMMCTRL_TOOLBARCUST As Integer = &H4203
'    Friend Const MINSYSCOMMAND As Integer = &HF000
'    Friend Const MCM_FIRST As Integer = &H1000
'    Friend Const MCM_GETCURSEL As Integer = &H1000 + 1
'    Friend Const MCM_SETCURSEL As Integer = &H1000 + 2
'    Friend Const MCM_GETMAXSELCOUNT As Integer = &H1000 + 3
'    Friend Const MCM_SETMAXSELCOUNT As Integer = &H1000 + 4
'    Friend Const MCM_GETSELRANGE As Integer = &H1000 + 5
'    Friend Const MCM_SETSELRANGE As Integer = &H1000 + 6
'    Friend Const MCM_GETMONTHRANGE As Integer = &H1000 + 7
'    Friend Const MCM_SETDAYSTATE As Integer = &H1000 + 8
'    Friend Const MCM_GETMINREQRECT As Integer = &H1000 + 9
'    Friend Const MCM_SETCOLOR As Integer = &H1000 + 10
'    Friend Const MCM_GETCOLOR As Integer = &H1000 + 11
'    Friend Const MCM_SETTODAY As Integer = &H1000 + 12
'    Friend Const MCM_GETTODAY As Integer = &H1000 + 13
'    Friend Const MCM_HITTEST As Integer = &H1000 + 14
'    Friend Const MCM_SETFIRSTDAYOFWEEK As Integer = &H1000 + 15
'    Friend Const MCM_GETFIRSTDAYOFWEEK As Integer = &H1000 + 16
'    Friend Const MCM_GETRANGE As Integer = &H1000 + 17
'    Friend Const MCM_SETRANGE As Integer = &H1000 + 18
'    Friend Const MCM_GETMONTHDELTA As Integer = &H1000 + 19
'    Friend Const MCM_SETMONTHDELTA As Integer = &H1000 + 20
'    Friend Const MCM_GETMAXTODAYWIDTH As Integer = &H1000 + 21
'    Friend Const MCHT_TITLE As Integer = &H10000
'    Friend Const MCHT_CALENDAR As Integer = &H20000
'    Friend Const MCHT_TODAYLINK As Integer = &H30000
'    Friend Const MCHT_NEXT As Integer = &H1000000
'    Friend Const MCHT_PREV As Integer = &H2000000
'    Friend Const MCHT_NOWHERE As Integer = &H0
'    Friend Const MCHT_TITLEBK As Integer = &H10000
'    Friend Const MCHT_TITLEMONTH As Integer = &H10000 Or &H1
'    Friend Const MCHT_TITLEYEAR As Integer = &H10000 Or &H2
'    Friend Const MCHT_TITLEBTNNEXT As Integer = &H10000 Or &H1000000 Or &H3
'    Friend Const MCHT_TITLEBTNPREV As Integer = &H10000 Or &H2000000 Or &H3
'    Friend Const MCHT_CALENDARBK As Integer = &H20000
'    Friend Const MCHT_CALENDARDATE As Integer = &H20000 Or &H1
'    Friend Const MCHT_CALENDARDATENEXT As Integer = &H20000 Or &H1 Or &H1000000
'    Friend Const MCHT_CALENDARDATEPREV As Integer = &H20000 Or &H1 Or &H2000000
'    Friend Const MCHT_CALENDARDAY As Integer = &H20000 Or &H2
'    Friend Const MCHT_CALENDARWEEKNUM As Integer = &H20000 Or &H3
'    Friend Const MCSC_BACKGROUND As Integer = 0
'    Friend Const MCSC_TEXT As Integer = 1
'    Friend Const MCSC_TITLEBK As Integer = 2
'    Friend Const MCSC_TITLETEXT As Integer = 3
'    Friend Const MCSC_MONTHBK As Integer = 4
'    Friend Const MCSC_TRAILINGTEXT As Integer = 5
'    Friend Const MCN_SELCHANGE As Integer = 0 - 750 + 1
'    Friend Const MCN_GETDAYSTATE As Integer = 0 - 750 + 3
'    Friend Const MCN_SELECT As Integer = 0 - 750 + 4
'    Friend Const MCS_DAYSTATE As Integer = &H1
'    Friend Const MCS_MULTISELECT As Integer = &H2
'    Friend Const MCS_WEEKNUMBERS As Integer = &H4
'    Friend Const MCS_NOTODAYCIRCLE As Integer = &H8
'    Friend Const MUTEX_MODIFY_STATE As Integer = &H1
'    Friend Const MERGECOPY As Integer = &HC000CA
'    Friend Const MERGEPAINT As Integer = &HBB0226
    
    
    
    
    
'    Friend Const NEWFILEOPENORD As Integer = 1547
'    Friend Const NI_OPENCANDIDATE As Integer = &H10
'    Friend Const NI_CLOSECANDIDATE As Integer = &H11
'    Friend Const NI_SELECTCANDIDATESTR As Integer = &H12
'    Friend Const NI_CHANGECANDIDATELIST As Integer = &H13
'    Friend Const NI_FINALIZECONVERSIONRESULT As Integer = &H14
'    Friend Const NI_COMPOSITIONSTR As Integer = &H15
'    Friend Const NI_SETCANDIDATE_PAGESTART As Integer = &H16
'    Friend Const NI_SETCANDIDATE_PAGESIZE As Integer = &H17
'    Friend Const NEWTRANSPARENT As Integer = 3
'    Friend Const NCBNAMSZ As Integer = 16
'    Friend Const NAME_FLAGS_MASK As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        NCBNAMSZ = 16,
'    '        NAME_FLAGS_MASK = unchecked((int)0x87),
'    '---------------------------^--- GenCode(token): unexpected token type
'    Friend Const NCBCALL As Integer = &H10
'    Friend Const NCBLISTEN As Integer = &H11
'    Friend Const NCBHANGUP As Integer = &H12
'    Friend Const NCBSEND As Integer = &H14
'    Friend Const NCBRECV As Integer = &H15
'    Friend Const NCBRECVANY As Integer = &H16
'    Friend Const NCBCHAINSEND As Integer = &H17
'    Friend Const NCBDGSEND As Integer = &H20
'    Friend Const NCBDGRECV As Integer = &H21
'    Friend Const NCBDGSENDBC As Integer = &H22
'    Friend Const NCBDGRECVBC As Integer = &H23
'    Friend Const NCBADDNAME As Integer = &H30
'    Friend Const NCBDELNAME As Integer = &H31
'    Friend Const NCBRESET As Integer = &H32
'    Friend Const NCBASTAT As Integer = &H33
'    Friend Const NCBSSTAT As Integer = &H34
'    Friend Const NCBCANCEL As Integer = &H35
'    Friend Const NCBADDGRNAME As Integer = &H36
'    Friend Const NCBENUM As Integer = &H37
'    Friend Const NCBUNLINK As Integer = &H70
'    Friend Const NCBSENDNA As Integer = &H71
'    Friend Const NCBCHAINSENDNA As Integer = &H72
'    Friend Const NCBLANSTALERT As Integer = &H73
'    Friend Const NCBACTION As Integer = &H77
'    Friend Const NCBFINDNAME As Integer = &H78
'    Friend Const NCBTRACE As Integer = &H79
'    Friend Const NRC_GOODRET As Integer = &H0
'    Friend Const NRC_BUFLEN As Integer = &H1
'    Friend Const NRC_ILLCMD As Integer = &H3
'    Friend Const NRC_CMDTMO As Integer = &H5
'    Friend Const NRC_INCOMP As Integer = &H6
'    Friend Const NRC_BADDR As Integer = &H7
'    Friend Const NRC_SNUMOUT As Integer = &H8
'    Friend Const NRC_NORES As Integer = &H9
'    Friend Const NRC_SCLOSED As Integer = &HA
'    Friend Const NRC_CMDCAN As Integer = &HB
'    Friend Const NRC_DUPNAME As Integer = &HD
'    Friend Const NRC_NAMTFUL As Integer = &HE
'    Friend Const NRC_ACTSES As Integer = &HF
'    Friend Const NRC_LOCTFUL As Integer = &H11
'    Friend Const NRC_REMTFUL As Integer = &H12
'    Friend Const NRC_ILLNN As Integer = &H13
'    Friend Const NRC_NOCALL As Integer = &H14
'    Friend Const NRC_NOWILD As Integer = &H15
'    Friend Const NRC_INUSE As Integer = &H16
'    Friend Const NRC_NAMERR As Integer = &H17
'    Friend Const NRC_SABORT As Integer = &H18
'    Friend Const NRC_NAMCONF As Integer = &H19
'    Friend Const NRC_IFBUSY As Integer = &H21
'    Friend Const NRC_TOOMANY As Integer = &H22
'    Friend Const NRC_BRIDGE As Integer = &H23
'    Friend Const NRC_CANOCCR As Integer = &H24
'    Friend Const NRC_CANCEL As Integer = &H26
'    Friend Const NRC_DUPENV As Integer = &H30
'    Friend Const NRC_ENVNOTDEF As Integer = &H34
'    Friend Const NRC_OSRESNOTAV As Integer = &H35
'    Friend Const NRC_MAXAPPS As Integer = &H36
'    Friend Const NRC_NOSAPS As Integer = &H37
'    Friend Const NRC_NORESOURCES As Integer = &H38
'    Friend Const NRC_INVADDRESS As Integer = &H39
'    Friend Const NRC_INVDDID As Integer = &H3B
'    Friend Const NRC_LOCKFAIL As Integer = &H3C
'    Friend Const NRC_OPENERR As Integer = &H3F
'    Friend Const NRC_SYSTEM As Integer = &H40
'    Friend Const NRC_PENDING As Integer = &HFF
'    Friend Const NUMPRS_LEADING_WHITE As Integer = &H1
'    Friend Const NUMPRS_TRAILING_WHITE As Integer = &H2
'    Friend Const NUMPRS_LEADING_PLUS As Integer = &H4
'    Friend Const NUMPRS_TRAILING_PLUS As Integer = &H8
'    Friend Const NUMPRS_LEADING_MINUS As Integer = &H10
'    Friend Const NUMPRS_TRAILING_MINUS As Integer = &H20
'    Friend Const NUMPRS_HEX_OCT As Integer = &H40
'    Friend Const NUMPRS_PARENS As Integer = &H80
'    Friend Const NUMPRS_DECIMAL As Integer = &H100
'    Friend Const NUMPRS_THOUSANDS As Integer = &H200
'    Friend Const NUMPRS_CURRENCY As Integer = &H400
'    Friend Const NUMPRS_EXPONENT As Integer = &H800
'    Friend Const NUMPRS_USE_ALL As Integer = &H1000
'    Friend Const NUMPRS_STD As Integer = &H1FFF
'    Friend Const NUMPRS_NEG As Integer = &H10000
'    Friend Const NUMPRS_INEXACT As Integer = &H20000
'    Friend Const NT351_INTERFACE_SIZE As Integer = &H40
'    Friend Const NIM_ADD As Integer = &H0
'    Friend Const NIM_MODIFY As Integer = &H1
'    Friend Const NIM_DELETE As Integer = &H2
'    Friend Const NIF_MESSAGE As Integer = &H1
'    Friend Const NIF_ICON As Integer = &H2
'    Friend Const NIF_TIP As Integer = &H4
'    Friend Const NONZEROLHND As Integer = &H2
'    Friend Const NONZEROLPTR As Integer = &H0
'    Friend Const NORMAL_PRIORITY_CLASS As Integer = &H20
'    Friend Const NOPARITY As Integer = 0
'    Friend Const NMPWAIT_WAIT_FOREVER As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        NOPARITY = 0,
'    '        NMPWAIT_WAIT_FOREVER = unchecked((int)0xFfffffff),
'    '--------------------------------^--- GenCode(token): unexpected token type
'    Friend Const NMPWAIT_NOWAIT As Integer = &H1
'    Friend Const NMPWAIT_USE_DEFAULT_WAIT As Integer = &H0
'    Friend Const NUMLOCK_ON As Integer = &H20
'    Friend Const NULL As Integer = 0
'    Friend Const NO_ERROR As Integer = 0
'    Friend Const NOERROR As Integer = 0
'    Friend Const NTE_BAD_UID As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        NOERROR = 0,
'    '        NTE_BAD_UID = unchecked((int)0x80090001),
'    '-----------------------^--- GenCode(token): unexpected token type
'    Friend Const NTE_BAD_HASH As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        NTE_BAD_UID = unchecked((int)0x80090001),
'    '        NTE_BAD_HASH = unchecked((int)0x80090002),
'    '------------------------^--- GenCode(token): unexpected token type
'    Friend Const NTE_BAD_KEY As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        NTE_BAD_HASH = unchecked((int)0x80090002),
'    '        NTE_BAD_KEY = unchecked((int)0x80090003),
'    '-----------------------^--- GenCode(token): unexpected token type
'    Friend Const NTE_BAD_LEN As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        NTE_BAD_KEY = unchecked((int)0x80090003),
'    '        NTE_BAD_LEN = unchecked((int)0x80090004),
'    '-----------------------^--- GenCode(token): unexpected token type
'    Friend Const NTE_BAD_DATA As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        NTE_BAD_LEN = unchecked((int)0x80090004),
'    '        NTE_BAD_DATA = unchecked((int)0x80090005),
'    '------------------------^--- GenCode(token): unexpected token type
'    Friend Const NTE_BAD_SIGNATURE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        NTE_BAD_DATA = unchecked((int)0x80090005),
'    '        NTE_BAD_SIGNATURE = unchecked((int)0x80090006),
'    '-----------------------------^--- GenCode(token): unexpected token type
'    Friend Const NTE_BAD_VER As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        NTE_BAD_SIGNATURE = unchecked((int)0x80090006),
'    '        NTE_BAD_VER = unchecked((int)0x80090007),
'    '-----------------------^--- GenCode(token): unexpected token type
'    Friend Const NTE_BAD_ALGID As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        NTE_BAD_VER = unchecked((int)0x80090007),
'    '        NTE_BAD_ALGID = unchecked((int)0x80090008),
'    '-------------------------^--- GenCode(token): unexpected token type
'    Friend Const NTE_BAD_FLAGS As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        NTE_BAD_ALGID = unchecked((int)0x80090008),
'    '        NTE_BAD_FLAGS = unchecked((int)0x80090009),
'    '-------------------------^--- GenCode(token): unexpected token type
'    Friend Const NTE_BAD_TYPE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        NTE_BAD_FLAGS = unchecked((int)0x80090009),
'    '        NTE_BAD_TYPE = unchecked((int)0x8009000A),
'    '------------------------^--- GenCode(token): unexpected token type
'    Friend Const NTE_BAD_KEY_STATE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        NTE_BAD_TYPE = unchecked((int)0x8009000A),
'    '        NTE_BAD_KEY_STATE = unchecked((int)0x8009000B),
'    '-----------------------------^--- GenCode(token): unexpected token type
'    Friend Const NTE_BAD_HASH_STATE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        NTE_BAD_KEY_STATE = unchecked((int)0x8009000B),
'    '        NTE_BAD_HASH_STATE = unchecked((int)0x8009000C),
'    '------------------------------^--- GenCode(token): unexpected token type
'    Friend Const NTE_NO_KEY As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        NTE_BAD_HASH_STATE = unchecked((int)0x8009000C),
'    '        NTE_NO_KEY = unchecked((int)0x8009000D),
'    '----------------------^--- GenCode(token): unexpected token type
'    Friend Const NTE_NO_MEMORY As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        NTE_NO_KEY = unchecked((int)0x8009000D),
'    '        NTE_NO_MEMORY = unchecked((int)0x8009000E),
'    '-------------------------^--- GenCode(token): unexpected token type
'    Friend Const NTE_EXISTS As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        NTE_NO_MEMORY = unchecked((int)0x8009000E),
'    '        NTE_EXISTS = unchecked((int)0x8009000F),
'    '----------------------^--- GenCode(token): unexpected token type
'    Friend Const NTE_PERM As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        NTE_EXISTS = unchecked((int)0x8009000F),
'    '        NTE_PERM = unchecked((int)0x80090010),
'    '--------------------^--- GenCode(token): unexpected token type
'    Friend Const NTE_NOT_FOUND As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        NTE_PERM = unchecked((int)0x80090010),
'    '        NTE_NOT_FOUND = unchecked((int)0x80090011),
'    '-------------------------^--- GenCode(token): unexpected token type
'    Friend Const NTE_DOUBLE_ENCRYPT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        NTE_NOT_FOUND = unchecked((int)0x80090011),
'    '        NTE_DOUBLE_ENCRYPT = unchecked((int)0x80090012),
'    '------------------------------^--- GenCode(token): unexpected token type
'    Friend Const NTE_BAD_PROVIDER As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        NTE_DOUBLE_ENCRYPT = unchecked((int)0x80090012),
'    '        NTE_BAD_PROVIDER = unchecked((int)0x80090013),
'    '----------------------------^--- GenCode(token): unexpected token type
'    Friend Const NTE_BAD_PROV_TYPE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        NTE_BAD_PROVIDER = unchecked((int)0x80090013),
'    '        NTE_BAD_PROV_TYPE = unchecked((int)0x80090014),
'    '-----------------------------^--- GenCode(token): unexpected token type
'    Friend Const NTE_BAD_PUBLIC_KEY As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        NTE_BAD_PROV_TYPE = unchecked((int)0x80090014),
'    '        NTE_BAD_PUBLIC_KEY = unchecked((int)0x80090015),
'    '------------------------------^--- GenCode(token): unexpected token type
'    Friend Const NTE_BAD_KEYSET As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        NTE_BAD_PUBLIC_KEY = unchecked((int)0x80090015),
'    '        NTE_BAD_KEYSET = unchecked((int)0x80090016),
'    '--------------------------^--- GenCode(token): unexpected token type
'    Friend Const NTE_PROV_TYPE_NOT_DEF As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        NTE_BAD_KEYSET = unchecked((int)0x80090016),
'    '        NTE_PROV_TYPE_NOT_DEF = unchecked((int)0x80090017),
'    '---------------------------------^--- GenCode(token): unexpected token type
'    Friend Const NTE_PROV_TYPE_ENTRY_BAD As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        NTE_PROV_TYPE_NOT_DEF = unchecked((int)0x80090017),
'    '        NTE_PROV_TYPE_ENTRY_BAD = unchecked((int)0x80090018),
'    '-----------------------------------^--- GenCode(token): unexpected token type
'    Friend Const NTE_KEYSET_NOT_DEF As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        NTE_PROV_TYPE_ENTRY_BAD = unchecked((int)0x80090018),
'    '        NTE_KEYSET_NOT_DEF = unchecked((int)0x80090019),
'    '------------------------------^--- GenCode(token): unexpected token type
'    Friend Const NTE_KEYSET_ENTRY_BAD As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        NTE_KEYSET_NOT_DEF = unchecked((int)0x80090019),
'    '        NTE_KEYSET_ENTRY_BAD = unchecked((int)0x8009001A),
'    '--------------------------------^--- GenCode(token): unexpected token type
'    Friend Const NTE_PROV_TYPE_NO_MATCH As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        NTE_KEYSET_ENTRY_BAD = unchecked((int)0x8009001A),
'    '        NTE_PROV_TYPE_NO_MATCH = unchecked((int)0x8009001B),
'    '----------------------------------^--- GenCode(token): unexpected token type
'    Friend Const NTE_SIGNATURE_FILE_BAD As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        NTE_PROV_TYPE_NO_MATCH = unchecked((int)0x8009001B),
'    '        NTE_SIGNATURE_FILE_BAD = unchecked((int)0x8009001C),
'    '----------------------------------^--- GenCode(token): unexpected token type
'    Friend Const NTE_PROVIDER_DLL_FAIL As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        NTE_SIGNATURE_FILE_BAD = unchecked((int)0x8009001C),
'    '        NTE_PROVIDER_DLL_FAIL = unchecked((int)0x8009001D),
'    '---------------------------------^--- GenCode(token): unexpected token type
'    Friend Const NTE_PROV_DLL_NOT_FOUND As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        NTE_PROVIDER_DLL_FAIL = unchecked((int)0x8009001D),
'    '        NTE_PROV_DLL_NOT_FOUND = unchecked((int)0x8009001E),
'    '----------------------------------^--- GenCode(token): unexpected token type
'    Friend Const NTE_BAD_KEYSET_PARAM As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        NTE_PROV_DLL_NOT_FOUND = unchecked((int)0x8009001E),
'    '        NTE_BAD_KEYSET_PARAM = unchecked((int)0x8009001F),
'    '--------------------------------^--- GenCode(token): unexpected token type
'    Friend Const NTE_FAIL As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        NTE_BAD_KEYSET_PARAM = unchecked((int)0x8009001F),
'    '        NTE_FAIL = unchecked((int)0x80090020),
'    '--------------------^--- GenCode(token): unexpected token type
'    Friend Const NTE_SYS_ERR As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        NTE_FAIL = unchecked((int)0x80090020),
'    '        NTE_SYS_ERR = unchecked((int)0x80090021),
'    '-----------------------^--- GenCode(token): unexpected token type
'    Friend Const NTE_OP_OK As Integer = 0
'    Friend Const NULLREGION As Integer = 1
'    Friend Const NEWFRAME As Integer = 1
'    Friend Const NEXTBAND As Integer = 3
'    Friend Const NTM_REGULAR As Integer = &H40
'    Friend Const NTM_BOLD As Integer = &H20
'    Friend Const NTM_ITALIC As Integer = &H1
'    Friend Const NONANTIALIASED_QUALITY As Integer = 3
'    Friend Const NULL_BRUSH As Integer = 5
'    Friend Const NULL_PEN As Integer = 8
'    Friend Const NUMBRUSHES As Integer = 16
'    Friend Const NUMPENS As Integer = 18
'    Friend Const NUMMARKERS As Integer = 20
'    Friend Const NUMFONTS As Integer = 22
'    Friend Const NUMCOLORS As Integer = 24
'    Friend Const NUMRESERVED As Integer = 106
'    Friend Const NETPROPERTY_PERSISTENT As Integer = 1
'    Friend Const NETINFO_DLL16 As Integer = &H1
'    Friend Const NETINFO_DISKRED As Integer = &H4
'    Friend Const NETINFO_PRINTERRED As Integer = &H8
'    Friend Const NORM_IGNORECASE As Integer = &H1
'    Friend Const NORM_IGNORENONSPACE As Integer = &H2
'    Friend Const NORM_IGNORESYMBOLS As Integer = &H4
'    Friend Const NORM_IGNOREKANATYPE As Integer = &H10000
'    Friend Const NORM_IGNOREWIDTH As Integer = &H20000
'    Friend Const NLS_VALID_LOCALE_MASK As Integer = &HFFFFF
'    Friend Const NO_PROPAGATE_INHERIT_ACE As Integer = &H4
'    Friend Const N_BTMASK As Integer = &HF
'    Friend Const N_TMASK As Integer = &H30
'    Friend Const N_TMASK1 As Integer = &HC0
'    Friend Const N_TMASK2 As Integer = &HF0
'    Friend Const N_BTSHFT As Integer = 4
'    Friend Const N_TSHIFT As Integer = 2
'    Friend Const NO_PRIORITY As Integer = 0
'    Friend Const NFR_ANSI As Integer = 1
'    Friend Const NFR_UNICODE As Integer = 2
'    Friend Const NF_QUERY As Integer = 3
'    Friend Const NF_REQUERY As Integer = 4
'    Friend Const NM_FIRST As Integer = 0 - 0
'    Friend Const NM_LAST As Integer = 0 - 99
'    Friend Const NM_OUTOFMEMORY As Integer = 0 - 0 - 1
'    Friend Const NM_CLICK As Integer = 0 - 0 - 2
'    Friend Const NM_DBLCLK As Integer = 0 - 0 - 3
'    Friend Const NM_RETURN As Integer = 0 - 0 - 4
'    Friend Const NM_RCLICK As Integer = 0 - 0 - 5
'    Friend Const NM_RDBLCLK As Integer = 0 - 0 - 6
'    Friend Const NM_SETFOCUS As Integer = 0 - 0 - 7
'    Friend Const NM_KILLFOCUS As Integer = 0 - 0 - 8
'    Friend Const NM_CUSTOMDRAW As Integer = 0 - 0 - 12
'    Friend Const NM_HOVER As Integer = 0 - 0 - 13
'    Friend Const NM_RELEASEDCAPTURE As Integer = 0 - 0 - 16
'    Friend Const NOTSRCCOPY As Integer = &H330008
'    Friend Const NOTSRCERASE As Integer = &H1100A6
    
    
    
'    Friend Const OFN_READONLY As Integer = &H1
'    Friend Const OFN_OVERWRITEPROMPT As Integer = &H2
'    Friend Const OFN_HIDEREADONLY As Integer = &H4
'    Friend Const OFN_NOCHANGEDIR As Integer = &H8
'    Friend Const OFN_SHOWHELP As Integer = &H10
'    Friend Const OFN_ENABLEHOOK As Integer = &H20
'    Friend Const OFN_ENABLETEMPLATE As Integer = &H40
'    Friend Const OFN_ENABLETEMPLATEHANDLE As Integer = &H80
'    Friend Const OFN_NOVALIDATE As Integer = &H100
'    Friend Const OFN_ALLOWMULTISELECT As Integer = &H200
'    Friend Const OFN_EXTENSIONDIFFERENT As Integer = &H400
'    Friend Const OFN_PATHMUSTEXIST As Integer = &H800
'    Friend Const OFN_FILEMUSTEXIST As Integer = &H1000
'    Friend Const OFN_CREATEPROMPT As Integer = &H2000
'    Friend Const OFN_SHAREAWARE As Integer = &H4000
'    Friend Const OFN_NOREADONLYRETURN As Integer = &H8000
'    Friend Const OFN_NOTESTFILECREATE As Integer = &H10000
'    Friend Const OFN_NONETWORKBUTTON As Integer = &H20000
'    Friend Const OFN_NOLONGNAMES As Integer = &H40000
'    Friend Const OFN_EXPLORER As Integer = &H80000
'    Friend Const OFN_NODEREFERENCELINKS As Integer = &H100000
'    Friend Const OFN_ENABLEINCLUDENOTIFY As Integer = &H400000
'    Friend Const OFN_ENABLESIZING As Integer = &H800000
'    Friend Const OFN_LONGNAMES As Integer = &H200000
'    Friend Const OFN_SHAREFALLTHROUGH As Integer = 2
'    Friend Const OFN_SHARENOWARN As Integer = 1
'    Friend Const OFN_SHAREWARN As Integer = 0
'    Friend Const OFN_USESHELLITEM As Integer = &H1000000
'    Friend Const OFN_DONTADDTORECENT As Integer = &H2000000
'    Friend Const OFN_FORCESHOWHIDDEN As Integer = &H10000000
'    Friend Const OLEIVERB_PRIMARY As Integer = 0
'    Friend Const OLEIVERB_SHOW As Integer = - 1
'    Friend Const OLEIVERB_OPEN As Integer = - 2
'    Friend Const OLEIVERB_HIDE As Integer = - 3
'    Friend Const OLEIVERB_UIACTIVATE As Integer = - 4
'    Friend Const OLEIVERB_INPLACEACTIVATE As Integer = - 5
'    Friend Const OLEIVERB_DISCARDUNDOSTATE As Integer = - 6
'    Friend Const OLEIVERB_PROPERTIES As Integer = - 7
'    Friend Const OLECREATE_LEAVERUNNING As Integer = &H1
'    Friend Const OPEN_EXISTING As Integer = 3
'    Friend Const OPEN_ALWAYS As Integer = 4
'    Friend Const OUTPUT_DEBUG_STRING_EVENT As Integer = 8
'    Friend Const ODDPARITY As Integer = 1
'    Friend Const ONESTOPBIT As Integer = 0
'    Friend Const ONE5STOPBITS As Integer = 1
'    Friend Const OF_READ As Integer = &H0
'    Friend Const OF_WRITE As Integer = &H1
'    Friend Const OF_READWRITE As Integer = &H2
'    Friend Const OF_SHARE_COMPAT As Integer = &H0
'    Friend Const OF_SHARE_EXCLUSIVE As Integer = &H10
'    Friend Const OF_SHARE_DENY_WRITE As Integer = &H20
'    Friend Const OF_SHARE_DENY_READ As Integer = &H30
'    Friend Const OF_SHARE_DENY_NONE As Integer = &H40
'    Friend Const OF_PARSE As Integer = &H100
'    Friend Const OF_DELETE As Integer = &H200
'    Friend Const OF_VERIFY As Integer = &H400
'    Friend Const OF_CANCEL As Integer = &H800
'    Friend Const OF_CREATE As Integer = &H1000
'    Friend Const OF_PROMPT As Integer = &H2000
'    Friend Const OF_EXIST As Integer = &H4000
'    Friend Const OF_REOPEN As Integer = &H8000
'    Friend Const OFS_MAXPATHNAME As Integer = 128
'    Friend Const OR_INVALID_OXID As Integer = 1910
'    Friend Const OR_INVALID_OID As Integer = 1911
'    Friend Const OR_INVALID_SET As Integer = 1912
'    Friend Const OLE_E_OLEVERB As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        OR_INVALID_SET = 1912,
'    '        OLE_E_OLEVERB = unchecked((int)0x80040000),
'    '-------------------------^--- GenCode(token): unexpected token type
'    Friend Const OLE_E_ADVF As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        OLE_E_OLEVERB = unchecked((int)0x80040000),
'    '        OLE_E_ADVF = unchecked((int)0x80040001),
'    '----------------------^--- GenCode(token): unexpected token type
'    Friend Const OLE_E_ENUM_NOMORE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        OLE_E_ADVF = unchecked((int)0x80040001),
'    '        OLE_E_ENUM_NOMORE = unchecked((int)0x80040002),
'    '-----------------------------^--- GenCode(token): unexpected token type
'    Friend Const OLE_E_ADVISENOTSUPPORTED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        OLE_E_ENUM_NOMORE = unchecked((int)0x80040002),
'    '        OLE_E_ADVISENOTSUPPORTED = unchecked((int)0x80040003),
'    '------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const OLE_E_NOCONNECTION As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        OLE_E_ADVISENOTSUPPORTED = unchecked((int)0x80040003),
'    '        OLE_E_NOCONNECTION = unchecked((int)0x80040004),
'    '------------------------------^--- GenCode(token): unexpected token type
'    Friend Const OLE_E_NOTRUNNING As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        OLE_E_NOCONNECTION = unchecked((int)0x80040004),
'    '        OLE_E_NOTRUNNING = unchecked((int)0x80040005),
'    '----------------------------^--- GenCode(token): unexpected token type
'    Friend Const OLE_E_NOCACHE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        OLE_E_NOTRUNNING = unchecked((int)0x80040005),
'    '        OLE_E_NOCACHE = unchecked((int)0x80040006),
'    '-------------------------^--- GenCode(token): unexpected token type
'    Friend Const OLE_E_BLANK As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        OLE_E_NOCACHE = unchecked((int)0x80040006),
'    '        OLE_E_BLANK = unchecked((int)0x80040007),
'    '-----------------------^--- GenCode(token): unexpected token type
'    Friend Const OLE_E_CLASSDIFF As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        OLE_E_BLANK = unchecked((int)0x80040007),
'    '        OLE_E_CLASSDIFF = unchecked((int)0x80040008),
'    '---------------------------^--- GenCode(token): unexpected token type
'    Friend Const OLE_E_CANT_GETMONIKER As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        OLE_E_CLASSDIFF = unchecked((int)0x80040008),
'    '        OLE_E_CANT_GETMONIKER = unchecked((int)0x80040009),
'    '---------------------------------^--- GenCode(token): unexpected token type
'    Friend Const OLE_E_CANT_BINDTOSOURCE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        OLE_E_CANT_GETMONIKER = unchecked((int)0x80040009),
'    '        OLE_E_CANT_BINDTOSOURCE = unchecked((int)0x8004000A),
'    '-----------------------------------^--- GenCode(token): unexpected token type
'    Friend Const OLE_E_STATIC As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        OLE_E_CANT_BINDTOSOURCE = unchecked((int)0x8004000A),
'    '        OLE_E_STATIC = unchecked((int)0x8004000B),
'    '------------------------^--- GenCode(token): unexpected token type
'    Friend Const OLE_E_PROMPTSAVECANCELLED As Integer = 
'    '
'    'Note:  Error processing original source shown below
        '    '        OLE_E_STATIC = unchecked((int)0x8004000B),

        Friend Const OLE_E_PROMPTSAVECANCELLED As Integer = &H8004000C

'    '-------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const OLE_E_INVALIDRECT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        OLE_E_PROMPTSAVECANCELLED = unchecked((int)0x8004000C),
'    '        OLE_E_INVALIDRECT = unchecked((int)0x8004000D),
'    '-----------------------------^--- GenCode(token): unexpected token type
'    Friend Const OLE_E_WRONGCOMPOBJ As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        OLE_E_INVALIDRECT = unchecked((int)0x8004000D),
'    '        OLE_E_WRONGCOMPOBJ = unchecked((int)0x8004000E),
'    '------------------------------^--- GenCode(token): unexpected token type
'    Friend Const OLE_E_INVALIDHWND As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        OLE_E_WRONGCOMPOBJ = unchecked((int)0x8004000E),
'    '        OLE_E_INVALIDHWND = unchecked((int)0x8004000F),
'    '-----------------------------^--- GenCode(token): unexpected token type
'    Friend Const OLE_E_NOT_INPLACEACTIVE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        OLE_E_INVALIDHWND = unchecked((int)0x8004000F),
'    '        OLE_E_NOT_INPLACEACTIVE = unchecked((int)0x80040010),
'    '-----------------------------------^--- GenCode(token): unexpected token type
'    Friend Const OLE_E_CANTCONVERT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        OLE_E_NOT_INPLACEACTIVE = unchecked((int)0x80040010),
'    '        OLE_E_CANTCONVERT = unchecked((int)0x80040011),
'    '-----------------------------^--- GenCode(token): unexpected token type
'    Friend Const OLE_E_NOSTORAGE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        OLE_E_CANTCONVERT = unchecked((int)0x80040011),
'    '        OLE_E_NOSTORAGE = unchecked((int)0x80040012),
'    '---------------------------^--- GenCode(token): unexpected token type
     Friend Const OLECMDERR_E_NOTSUPPORTED As Integer = &H80040100
'    '
'    'Note:  Error processing original source shown below
'    '        OLE_E_NOSTORAGE = unchecked((int)0x80040012),
'    '        OLECMDERR_E_NOTSUPPORTED = unchecked((int)0x80040100),
'    '------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const OLECMDERR_E_DISABLED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        OLECMDERR_E_NOTSUPPORTED = unchecked((int)0x80040100),
'    '        OLECMDERR_E_DISABLED  = unchecked((int)0x80040101),
'    '---------------------------------^--- GenCode(token): unexpected token type
'    Friend Const OLECMDERR_E_NOHELP As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        OLECMDERR_E_DISABLED  = unchecked((int)0x80040101),
'    '        OLECMDERR_E_NOHELP  = unchecked((int)0x80040102),
'    '-------------------------------^--- GenCode(token): unexpected token type
'    Friend Const OLECMDERR_E_CANCELED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        OLECMDERR_E_NOHELP  = unchecked((int)0x80040102),
'    '        OLECMDERR_E_CANCELED  = unchecked((int)0x80040103),
'    '---------------------------------^--- GenCode(token): unexpected token type
'    Friend Const OLECMDERR_E_UNKNOWNGROUP As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        OLECMDERR_E_CANCELED  = unchecked((int)0x80040103),
'    '        OLECMDERR_E_UNKNOWNGROUP  = unchecked((int)0x80040104),
'    '-------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const OLEMISC_RECOMPOSEONRESIZE As Integer = &H1
'    Friend Const OLEMISC_ONLYICONIC As Integer = &H2
'    Friend Const OLEMISC_INSERTNOTREPLACE As Integer = &H4
'    Friend Const OLEMISC_STATIC As Integer = &H8
'    Friend Const OLEMISC_CANTLINKINSIDE As Integer = &H10
'    Friend Const OLEMISC_CANLINKBYOLE1 As Integer = &H20
'    Friend Const OLEMISC_ISLINKOBJECT As Integer = &H40
'    Friend Const OLEMISC_INSIDEOUT As Integer = &H80
'    Friend Const OLEMISC_ACTIVATEWHENVISIBLE As Integer = &H100
'    Friend Const OLEMISC_RENDERINGISDEVICEINDEPENDENT As Integer = &H200
'    Friend Const OLEMISC_INVISIBLEATRUNTIME As Integer = &H400
'    Friend Const OLEMISC_ALWAYSRUN As Integer = &H800
'    Friend Const OLEMISC_ACTSLIKEBUTTON As Integer = &H1000
'    Friend Const OLEMISC_ACTSLIKELABEL As Integer = &H2000
'    Friend Const OLEMISC_NOUIACTIVATE As Integer = &H4000
'    Friend Const OLEMISC_ALIGNABLE As Integer = &H8000
'    Friend Const OLEMISC_SIMPLEFRAME As Integer = &H10000
'    Friend Const OLEMISC_SETCLIENTSITEFIRST As Integer = &H20000
'    Friend Const OLEMISC_IMEMODE As Integer = &H40000
'    Friend Const OLEOBJ_E_FIRST As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        OLEMISC_IMEMODE = 0x00040000,
'    '        OLEOBJ_E_FIRST = unchecked((int)0x80040180),
'    '--------------------------^--- GenCode(token): unexpected token type
'    Friend Const OLEOBJ_E_LAST As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        OLEOBJ_E_FIRST = unchecked((int)0x80040180),
'    '        OLEOBJ_E_LAST = unchecked((int)0x8004018F),
'    '-------------------------^--- GenCode(token): unexpected token type
'    Friend Const OLEOBJ_S_FIRST As Integer = &H40180
'    Friend Const OLEOBJ_S_LAST As Integer = &H4018F
'    Friend Const OLEOBJ_E_NOVERBS As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        OLEOBJ_S_LAST = 0x0004018F,
'    '        OLEOBJ_E_NOVERBS = unchecked((int)0x80040180),
'    '----------------------------^--- GenCode(token): unexpected token type
'    Friend Const OLEOBJ_E_INVALIDVERB As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        OLEOBJ_E_NOVERBS = unchecked((int)0x80040180),
'    '        OLEOBJ_E_INVALIDVERB = unchecked((int)0x80040181),
'    '--------------------------------^--- GenCode(token): unexpected token type
'    Friend Const OLE_S_USEREG As Integer = &H40000
'    Friend Const OLE_S_STATIC As Integer = &H40001
'    Friend Const OLE_S_MAC_CLIPFORMAT As Integer = &H40002
'    Friend Const OLEOBJ_S_INVALIDVERB As Integer = &H40180
'    Friend Const OLEOBJ_S_CANNOT_DOVERB_NOW As Integer = &H40181
'    Friend Const OLEOBJ_S_INVALIDHWND As Integer = &H40182
'    Friend Const OPENCHANNEL As Integer = 4110
'    Friend Const OBJ_PEN As Integer = 1
'    Friend Const OBJ_BRUSH As Integer = 2
'    Friend Const OBJ_DC As Integer = 3
'    Friend Const OBJ_METADC As Integer = 4
'    Friend Const OBJ_PAL As Integer = 5
'    Friend Const OBJ_FONT As Integer = 6
'    Friend Const OBJ_BITMAP As Integer = 7
'    Friend Const OBJ_REGION As Integer = 8
'    Friend Const OBJ_METAFILE As Integer = 9
'    Friend Const OBJ_MEMDC As Integer = 10
'    Friend Const OBJ_EXTPEN As Integer = 11
'    Friend Const OBJ_ENHMETADC As Integer = 12
'    Friend Const OBJ_ENHMETAFILE As Integer = 13
'    Friend Const OUT_DEFAULT_PRECIS As Integer = 0
'    Friend Const OUT_STRING_PRECIS As Integer = 1
'    Friend Const OUT_CHARACTER_PRECIS As Integer = 2
'    Friend Const OUT_STROKE_PRECIS As Integer = 3
'    Friend Const OUT_TT_PRECIS As Integer = 4
'    Friend Const OUT_DEVICE_PRECIS As Integer = 5
'    Friend Const OUT_RASTER_PRECIS As Integer = 6
'    Friend Const OUT_TT_ONLY_PRECIS As Integer = 7
'    Friend Const OUT_OUTLINE_PRECIS As Integer = 8
'    Friend Const OUT_SCREEN_OUTLINE_PRECIS As Integer = 9
'    Friend Const OEM_CHARSET As Integer = 255
'    Friend Const OPAQUE As Integer = 2
'    Friend Const OEM_FIXED_FONT As Integer = 10
'    Friend Const OBJECT_INHERIT_ACE As Integer = &H1
'    Friend Const OWNER_SECURITY_INFORMATION As Integer = 0
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
    
    
    
    
    
    
    
    
    
    
'    Friend Const PDERR_PRINTERCODES As Integer = &H1000
'    Friend Const PDERR_SETUPFAILURE As Integer = &H1001
'    Friend Const PDERR_PARSEFAILURE As Integer = &H1002
'    Friend Const PDERR_RETDEFFAILURE As Integer = &H1003
'    Friend Const PDERR_LOADDRVFAILURE As Integer = &H1004
'    Friend Const PDERR_GETDEVMODEFAIL As Integer = &H1005
'    Friend Const PDERR_INITFAILURE As Integer = &H1006
'    Friend Const PDERR_NODEVICES As Integer = &H1007
'    Friend Const PDERR_NODEFAULTPRN As Integer = &H1008
'    Friend Const PDERR_DNDMMISMATCH As Integer = &H1009
'    Friend Const PDERR_CREATEICFAILURE As Integer = &H100A
'    Friend Const PDERR_PRINTERNOTFOUND As Integer = &H100B
'    Friend Const PDERR_DEFAULTDIFFERENT As Integer = &H100C
'    Friend Const PRINTER_FONTTYPE As Integer = &H4000
'    Friend Const PD_ALLPAGES As Integer = &H0
'    Friend Const PD_SELECTION As Integer = &H1
'    Friend Const PD_PAGENUMS As Integer = &H2
'    Friend Const PD_CURRENTPAGE As Integer = &H400000
'    Friend Const PD_NOSELECTION As Integer = &H4
'    Friend Const PD_NOPAGENUMS As Integer = &H8
'    Friend Const PD_NOCURRENTPAGE As Integer = &H800000
'    Friend Const PD_COLLATE As Integer = &H10
'    Friend Const PD_PRINTTOFILE As Integer = &H20
'    Friend Const PD_PRINTSETUP As Integer = &H40
'    Friend Const PD_NOWARNING As Integer = &H80
'    Friend Const PD_RETURNDC As Integer = &H100
'    Friend Const PD_RETURNIC As Integer = &H200
'    Friend Const PD_RETURNDEFAULT As Integer = &H400
'    Friend Const PD_SHOWHELP As Integer = &H800
'    Friend Const PD_ENABLEPRINTHOOK As Integer = &H1000
'    Friend Const PD_ENABLESETUPHOOK As Integer = &H2000
'    Friend Const PD_ENABLEPRINTTEMPLATE As Integer = &H4000
'    Friend Const PD_ENABLESETUPTEMPLATE As Integer = &H8000
'    Friend Const PD_ENABLEPRINTTEMPLATEHANDLE As Integer = &H10000
'    Friend Const PD_ENABLESETUPTEMPLATEHANDLE As Integer = &H20000
'    Friend Const PD_USEDEVMODECOPIES As Integer = &H40000
'    Friend Const PD_USEDEVMODECOPIESANDCOLLATE As Integer = &H40000
'    Friend Const PD_DISABLEPRINTTOFILE As Integer = &H80000
'    Friend Const PD_HIDEPRINTTOFILE As Integer = &H100000
'    Friend Const PD_NONETWORKBUTTON As Integer = &H200000
'    Friend Const PSD_DEFAULTMINMARGINS As Integer = &H0
'    Friend Const PSD_INWININIINTLMEASURE As Integer = &H0
'    Friend Const PSD_MINMARGINS As Integer = &H1
'    Friend Const PSD_MARGINS As Integer = &H2
'    Friend Const PSD_INTHOUSANDTHSOFINCHES As Integer = &H4
'    Friend Const PSD_INHUNDREDTHSOFMILLIMETERS As Integer = &H8
'    Friend Const PSD_DISABLEMARGINS As Integer = &H10
'    Friend Const PSD_DISABLEPRINTER As Integer = &H20
'    Friend Const PSD_NOWARNING As Integer = &H80
'    Friend Const PSD_DISABLEORIENTATION As Integer = &H100
'    Friend Const PSD_RETURNDEFAULT As Integer = &H400
'    Friend Const PSD_DISABLEPAPER As Integer = &H200
'    Friend Const PSD_SHOWHELP As Integer = &H800
'    Friend Const PSD_ENABLEPAGESETUPHOOK As Integer = &H2000
'    Friend Const PSD_ENABLEPAGESETUPTEMPLATE As Integer = &H8000
'    Friend Const PSD_ENABLEPAGESETUPTEMPLATEHANDLE As Integer = &H20000
'    Friend Const PSD_ENABLEPAGEPAINTHOOK As Integer = &H40000
'    Friend Const PSD_DISABLEPAGEPAINTING As Integer = &H80000
'    Friend Const PSD_NONETWORKBUTTON As Integer = &H200000
'    Friend Const psh1 As Integer = &H400
'    Friend Const psh2 As Integer = &H401
'    Friend Const psh3 As Integer = &H402
'    Friend Const psh4 As Integer = &H403
'    Friend Const psh5 As Integer = &H404
'    Friend Const psh6 As Integer = &H405
'    Friend Const psh7 As Integer = &H406
'    Friend Const psh8 As Integer = &H407
'    Friend Const psh9 As Integer = &H408
'    Friend Const psh10 As Integer = &H409
'    Friend Const psh11 As Integer = &H40A
'    Friend Const psh12 As Integer = &H40B
'    Friend Const psh13 As Integer = &H40C
'    Friend Const psh14 As Integer = &H40D
'    Friend Const psh15 As Integer = &H40E
'    Friend Const pshHelp As Integer = &H40E
'    Friend Const psh16 As Integer = &H40F
'    Friend Const PRINTDLGORD As Integer = 1538
'    Friend Const PRNSETUPDLGORD As Integer = 1539
'    Friend Const PAGESETUPDLGORD As Integer = 1546
'    Friend Const PARAMFLAG_NONE As Integer = 0
'    Friend Const PARAMFLAG_FIN As Integer = &H1
'    Friend Const PARAMFLAG_FOUT As Integer = &H2
'    Friend Const PARAMFLAG_FLCID As Integer = &H4
'    Friend Const PARAMFLAG_FRETVAL As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                          PARAMFLAG_FLCID = (0x4),
'    '                                                                            PARAMFLAG_FRETVAL = (unchecked((int)0x8)),
'    '--------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const PARAMFLAG_FOPT As Integer = &H10
'    Friend Const PARAMFLAG_FHASDEFAULT As Integer = &H20
'    Friend Const PROPSETFLAG_DEFAULT As Integer = 0
'    Friend Const PROPSETFLAG_NONSIMPLE As Integer = 1
'    Friend Const PROPSETFLAG_ANSI As Integer = 2
'    Friend Const PID_DICTIONARY As Integer = 0
'    Friend Const PID_CODEPAGE As Integer = &H1
'    Friend Const PID_FIRST_USABLE As Integer = &H2
'    Friend Const PID_FIRST_NAME_DEFAULT As Integer = &HFFF
'    Friend Const PID_LOCALE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                                                                                                                             PID_FIRST_NAME_DEFAULT = (0xfff),
'    '                                                                                                                                                                                                                                                                                      PID_LOCALE = (unchecked((int)0x80000000)),
'    '-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const PID_MODIFY_TIME As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                                                                                                                                                      PID_LOCALE = (unchecked((int)0x80000000)),
'    '                                                                                                                                                                                                                                                                                                   PID_MODIFY_TIME = (unchecked((int)0x80000001)),
'    '-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const PID_SECURITY As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                                                                                                                                                                   PID_MODIFY_TIME = (unchecked((int)0x80000001)),
'    '                                                                                                                                                                                                                                                                                                                     PID_SECURITY = (unchecked((int)0x80000002)),
'    '--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const PID_ILLEGAL As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                                                                                                                                                                                     PID_SECURITY = (unchecked((int)0x80000002)),
'    '                                                                                                                                                                                                                                                                                                                                    PID_ILLEGAL = (unchecked((int)0xFfffffff)),
'    '----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const PIDSI_TITLE As Integer = &H2
'    Friend Const PIDSI_SUBJECT As Integer = &H3
'    Friend Const PIDSI_AUTHOR As Integer = &H4
'    Friend Const PIDSI_KEYWORDS As Integer = &H5
'    Friend Const PIDSI_COMMENTS As Integer = &H6
'    Friend Const PIDSI_TEMPLATE As Integer = &H7
'    Friend Const PIDSI_LASTAUTHOR As Integer = &H8
'    Friend Const PIDSI_REVNUMBER As Integer = &H9
'    Friend Const PIDSI_EDITTIME As Integer = &HA
'    Friend Const PIDSI_LASTPRINTED As Integer = &HB
'    Friend Const PIDSI_CREATE_DTM As Integer = &HC
'    Friend Const PIDSI_LASTSAVE_DTM As Integer = &HD
'    Friend Const PIDSI_PAGECOUNT As Integer = &HE
'    Friend Const PIDSI_WORDCOUNT As Integer = &HF
'    Friend Const PIDSI_CHARCOUNT As Integer = &H10
'    Friend Const PIDSI_THUMBNAIL As Integer = &H11
'    Friend Const PIDSI_APPNAME As Integer = &H12
'    Friend Const PIDSI_DOC_SECURITY As Integer = &H13
'    Friend Const PRSPEC_INVALID As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        PIDSI_DOC_SECURITY = 0x00000013,
'    '        PRSPEC_INVALID = (unchecked((int)0xFfffffff)),
'    '---------------------------^--- GenCode(token): unexpected token type
'    Friend Const PRSPEC_LPWSTR As Integer = 0
'    Friend Const PRSPEC_PROPID As Integer = 1
'    Friend Const PROPSETHDR_OSVERSION_UNKNOWN As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                         PRSPEC_PROPID = (1),
'    '                                                         PROPSETHDR_OSVERSION_UNKNOWN = unchecked((int)0xFFFFFFFF),
'    '-----------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const PSP_DEFAULT As Integer = &H0
'    Friend Const PSP_DLGINDIRECT As Integer = &H1
'    Friend Const PSP_USEHICON As Integer = &H2
'    Friend Const PSP_USEICONID As Integer = &H4
'    Friend Const PSP_USETITLE As Integer = &H8
'    Friend Const PSP_RTLREADING As Integer = &H10
'    Friend Const PSP_HASHELP As Integer = &H20
'    Friend Const PSP_USEREFPARENT As Integer = &H40
'    Friend Const PSP_USECALLBACK As Integer = &H80
'    Friend Const PSPCB_RELEASE As Integer = 1
'    Friend Const PSPCB_CREATE As Integer = 2
'    Friend Const PSH_DEFAULT As Integer = &H0
'    Friend Const PSH_PROPTITLE As Integer = &H1
'    Friend Const PSH_USEHICON As Integer = &H2
'    Friend Const PSH_USEICONID As Integer = &H4
'    Friend Const PSH_PROPSHEETPAGE As Integer = &H8
'    Friend Const PSH_WIZARD As Integer = &H20
'    Friend Const PSH_USEPSTARTPAGE As Integer = &H40
'    Friend Const PSH_NOAPPLYNOW As Integer = &H80
'    Friend Const PSH_USECALLBACK As Integer = &H100
'    Friend Const PSH_HASHELP As Integer = &H200
'    Friend Const PSH_MODELESS As Integer = &H400
'    Friend Const PSH_RTLREADING As Integer = &H800
'    Friend Const PSCB_INITIALIZED As Integer = 1
'    Friend Const PSCB_PRECREATE As Integer = 2
'    Friend Const PSN_FIRST As Integer = 0 - 200
'    Friend Const PSN_LAST As Integer = 0 - 299
'    Friend Const PSN_SETACTIVE As Integer = 0 - 200 - 0
'    Friend Const PSN_KILLACTIVE As Integer = 0 - 200 - 1
'    Friend Const PSN_APPLY As Integer = 0 - 200 - 2
'    Friend Const PSN_RESET As Integer = 0 - 200 - 3
'    Friend Const PSN_HELP As Integer = 0 - 200 - 5
'    Friend Const PSN_WIZBACK As Integer = 0 - 200 - 6
'    Friend Const PSN_WIZNEXT As Integer = 0 - 200 - 7
'    Friend Const PSN_WIZFINISH As Integer = 0 - 200 - 8
'    Friend Const PSN_QUERYCANCEL As Integer = 0 - 200 - 9
'    Friend Const PSNRET_NOERROR As Integer = 0
'    Friend Const PSNRET_INVALID As Integer = 1
'    Friend Const PSNRET_INVALID_NOCHANGEPAGE As Integer = 2
'    Friend Const PSWIZB_BACK As Integer = &H1
'    Friend Const PSWIZB_NEXT As Integer = &H2
'    Friend Const PSWIZB_FINISH As Integer = &H4
'    Friend Const PSWIZB_DISABLEDFINISH As Integer = &H8
'    Friend Const PSBTN_BACK As Integer = 0
'    Friend Const PSBTN_NEXT As Integer = 1
'    Friend Const PSBTN_FINISH As Integer = 2
'    Friend Const PSBTN_OK As Integer = 3
'    Friend Const PSBTN_APPLYNOW As Integer = 4
'    Friend Const PSBTN_CANCEL As Integer = 5
'    Friend Const PSBTN_HELP As Integer = 6
'    Friend Const PSBTN_MAX As Integer = 6
'    Friend Const PROP_SM_CXDLG As Integer = 212
'    Friend Const PROP_SM_CYDLG As Integer = 188
'    Friend Const PROP_MED_CXDLG As Integer = 227
'    Friend Const PROP_MED_CYDLG As Integer = 215
'    Friend Const PROP_LG_CXDLG As Integer = 252
'    Friend Const PROP_LG_CYDLG As Integer = 218
'    Friend Const PO_DELETE As Integer = &H13
'    Friend Const PO_RENAME As Integer = &H14
'    Friend Const PO_PORTCHANGE As Integer = &H20
'    Friend Const PO_REN_PORT As Integer = &H34
'    Friend Const PROGRESS_CONTINUE As Integer = 0
'    Friend Const PROGRESS_CANCEL As Integer = 1
'    Friend Const PROGRESS_STOP As Integer = 2
'    Friend Const PROGRESS_QUIET As Integer = 3
'    Friend Const PIPE_ACCESS_INBOUND As Integer = &H1
'    Friend Const PIPE_ACCESS_OUTBOUND As Integer = &H2
'    Friend Const PIPE_ACCESS_DUPLEX As Integer = &H3
'    Friend Const PIPE_CLIENT_END As Integer = &H0
'    Friend Const PIPE_SERVER_END As Integer = &H1
'    Friend Const PIPE_WAIT As Integer = &H0
'    Friend Const PIPE_NOWAIT As Integer = &H1
'    Friend Const PIPE_READMODE_BYTE As Integer = &H0
'    Friend Const PIPE_READMODE_MESSAGE As Integer = &H2
'    Friend Const PIPE_TYPE_BYTE As Integer = &H0
'    Friend Const PIPE_TYPE_MESSAGE As Integer = &H4
'    Friend Const PIPE_UNLIMITED_INSTANCES As Integer = 255
'    Friend Const PST_UNSPECIFIED As Integer = &H0
'    Friend Const PST_RS232 As Integer = &H1
'    Friend Const PST_PARALLELPORT As Integer = &H2
'    Friend Const PST_RS422 As Integer = &H3
'    Friend Const PST_RS423 As Integer = &H4
'    Friend Const PST_RS449 As Integer = &H5
'    Friend Const PST_MODEM As Integer = &H6
'    Friend Const PST_FAX As Integer = &H21
'    Friend Const PST_SCANNER As Integer = &H22
'    Friend Const PST_NETWORK_BRIDGE As Integer = &H100
'    Friend Const PST_LAT As Integer = &H101
'    Friend Const PST_TCPIP_TELNET As Integer = &H102
'    Friend Const PST_X25 As Integer = &H103
'    Friend Const PCF_DTRDSR As Integer = &H1
'    Friend Const PCF_RTSCTS As Integer = &H2
'    Friend Const PCF_RLSD As Integer = &H4
'    Friend Const PCF_PARITY_CHECK As Integer = &H8
'    Friend Const PCF_XONXOFF As Integer = &H10
'    Friend Const PCF_SETXCHAR As Integer = &H20
'    Friend Const PCF_TOTALTIMEOUTS As Integer = &H40
'    Friend Const PCF_INTTIMEOUTS As Integer = &H80
'    Friend Const PCF_SPECIALCHARS As Integer = &H100
'    Friend Const PCF_16BITMODE As Integer = &H200
'    Friend Const PARITY_NONE As Integer = &H100
'    Friend Const PARITY_ODD As Integer = &H200
'    Friend Const PARITY_EVEN As Integer = &H400
'    Friend Const PARITY_MARK As Integer = &H800
'    Friend Const PARITY_SPACE As Integer = &H1000
'    Friend Const PROFILE_USER As Integer = &H10000000
'    Friend Const PROFILE_KERNEL As Integer = &H20000000
'    Friend Const PROFILE_SERVER As Integer = &H40000000
'    Friend Const PURGE_TXABORT As Integer = &H1
'    Friend Const PURGE_RXABORT As Integer = &H2
'    Friend Const PURGE_TXCLEAR As Integer = &H4
'    Friend Const PURGE_RXCLEAR As Integer = &H8
'    Friend Const PROCESS_HEAP_REGION As Integer = &H1
'    Friend Const PROCESS_HEAP_UNCOMMITTED_RANGE As Integer = &H2
'    Friend Const PROCESS_HEAP_ENTRY_BUSY As Integer = &H4
'    Friend Const PROCESS_HEAP_ENTRY_MOVEABLE As Integer = &H10
'    Friend Const PROCESS_HEAP_ENTRY_DDESHARE As Integer = &H20
'    Friend Const PUBLICKEYBLOB As Integer = &H6
'    Friend Const PRIVATEKEYBLOB As Integer = &H7
'    Friend Const PKCS5_PADDING As Integer = 1
'    Friend Const PP_ENUMALGS As Integer = 1
'    Friend Const PP_ENUMCONTAINERS As Integer = 2
'    Friend Const PP_IMPTYPE As Integer = 3
'    Friend Const PP_NAME As Integer = 4
'    Friend Const PP_VERSION As Integer = 5
'    Friend Const PP_CONTAINER As Integer = 6
'    Friend Const PP_CLIENT_HWND As Integer = 1
'    Friend Const PROV_RSA_FULL As Integer = 1
'    Friend Const PROV_RSA_SIG As Integer = 2
'    Friend Const PROV_DSS As Integer = 3
'    Friend Const PROV_FORTEZZA As Integer = 4
'    Friend Const PROV_MS_EXCHANGE As Integer = 5
'    Friend Const PROV_SSL As Integer = 6
'    Friend Const PROV_STT_MER As Integer = 7
'    Friend Const PROV_STT_ACQ As Integer = 8
'    Friend Const PROV_STT_BRND As Integer = 9
'    Friend Const PROV_STT_ROOT As Integer = 10
'    Friend Const PROV_STT_ISS As Integer = 11
'    Friend Const PERSIST_E_SIZEDEFINITE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        PROV_STT_ISS = 11,
'    '        PERSIST_E_SIZEDEFINITE = unchecked((int)0x800B0009),
'    '----------------------------------^--- GenCode(token): unexpected token type
'    Friend Const PERSIST_E_SIZEINDEFINITE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        PERSIST_E_SIZEDEFINITE = unchecked((int)0x800B0009),
'    '        PERSIST_E_SIZEINDEFINITE = unchecked((int)0x800B000A),
'    '------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const PERSIST_E_NOTSELFSIZING As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        PERSIST_E_SIZEINDEFINITE = unchecked((int)0x800B000A),
'    '        PERSIST_E_NOTSELFSIZING = unchecked((int)0x800B000B),
'    '-----------------------------------^--- GenCode(token): unexpected token type
'    Friend Const POLYFILL_LAST As Integer = 2
'    Friend Const PASSTHROUGH As Integer = 19
'    Friend Const POSTSCRIPT_DATA As Integer = 37
'    Friend Const POSTSCRIPT_IGNORE As Integer = 38
'    Friend Const POSTSCRIPT_PASSTHROUGH As Integer = 4115
'    Friend Const PR_JOBSTATUS As Integer = &H0
'    Friend Const PROOF_QUALITY As Integer = 2
'    Friend Const PANOSE_COUNT As Integer = 10
'    Friend Const PAN_FAMILYTYPE_INDEX As Integer = 0
'    Friend Const PAN_SERIFSTYLE_INDEX As Integer = 1
'    Friend Const PAN_WEIGHT_INDEX As Integer = 2
'    Friend Const PAN_PROPORTION_INDEX As Integer = 3
'    Friend Const PAN_CONTRAST_INDEX As Integer = 4
'    Friend Const PAN_STROKEVARIATION_INDEX As Integer = 5
'    Friend Const PAN_ARMSTYLE_INDEX As Integer = 6
'    Friend Const PAN_LETTERFORM_INDEX As Integer = 7
'    Friend Const PAN_MIDLINE_INDEX As Integer = 8
'    Friend Const PAN_XHEIGHT_INDEX As Integer = 9
'    Friend Const PAN_CULTURE_LATIN As Integer = 0
'    Friend Const PAN_ANY As Integer = 0
'    Friend Const PAN_NO_FIT As Integer = 1
'    Friend Const PAN_FAMILY_TEXT_DISPLAY As Integer = 2
'    Friend Const PAN_FAMILY_SCRIPT As Integer = 3
'    Friend Const PAN_FAMILY_DECORATIVE As Integer = 4
'    Friend Const PAN_FAMILY_PICTORIAL As Integer = 5
'    Friend Const PAN_SERIF_COVE As Integer = 2
'    Friend Const PAN_SERIF_OBTUSE_COVE As Integer = 3
'    Friend Const PAN_SERIF_SQUARE_COVE As Integer = 4
'    Friend Const PAN_SERIF_OBTUSE_SQUARE_COVE As Integer = 5
'    Friend Const PAN_SERIF_SQUARE As Integer = 6
'    Friend Const PAN_SERIF_THIN As Integer = 7
'    Friend Const PAN_SERIF_BONE As Integer = 8
'    Friend Const PAN_SERIF_EXAGGERATED As Integer = 9
'    Friend Const PAN_SERIF_TRIANGLE As Integer = 10
'    Friend Const PAN_SERIF_NORMAL_SANS As Integer = 11
'    Friend Const PAN_SERIF_OBTUSE_SANS As Integer = 12
'    Friend Const PAN_SERIF_PERP_SANS As Integer = 13
'    Friend Const PAN_SERIF_FLARED As Integer = 14
'    Friend Const PAN_SERIF_ROUNDED As Integer = 15
'    Friend Const PAN_WEIGHT_VERY_LIGHT As Integer = 2
'    Friend Const PAN_WEIGHT_LIGHT As Integer = 3
'    Friend Const PAN_WEIGHT_THIN As Integer = 4
'    Friend Const PAN_WEIGHT_BOOK As Integer = 5
'    Friend Const PAN_WEIGHT_MEDIUM As Integer = 6
'    Friend Const PAN_WEIGHT_DEMI As Integer = 7
'    Friend Const PAN_WEIGHT_BOLD As Integer = 8
'    Friend Const PAN_WEIGHT_HEAVY As Integer = 9
'    Friend Const PAN_WEIGHT_BLACK As Integer = 10
'    Friend Const PAN_WEIGHT_NORD As Integer = 11
'    Friend Const PAN_PROP_OLD_STYLE As Integer = 2
'    Friend Const PAN_PROP_MODERN As Integer = 3
'    Friend Const PAN_PROP_EVEN_WIDTH As Integer = 4
'    Friend Const PAN_PROP_EXPANDED As Integer = 5
'    Friend Const PAN_PROP_CONDENSED As Integer = 6
'    Friend Const PAN_PROP_VERY_EXPANDED As Integer = 7
'    Friend Const PAN_PROP_VERY_CONDENSED As Integer = 8
'    Friend Const PAN_PROP_MONOSPACED As Integer = 9
'    Friend Const PAN_CONTRAST_NONE As Integer = 2
'    Friend Const PAN_CONTRAST_VERY_LOW As Integer = 3
'    Friend Const PAN_CONTRAST_LOW As Integer = 4
'    Friend Const PAN_CONTRAST_MEDIUM_LOW As Integer = 5
'    Friend Const PAN_CONTRAST_MEDIUM As Integer = 6
'    Friend Const PAN_CONTRAST_MEDIUM_HIGH As Integer = 7
'    Friend Const PAN_CONTRAST_HIGH As Integer = 8
'    Friend Const PAN_CONTRAST_VERY_HIGH As Integer = 9
'    Friend Const PAN_STROKE_GRADUAL_DIAG As Integer = 2
'    Friend Const PAN_STROKE_GRADUAL_TRAN As Integer = 3
'    Friend Const PAN_STROKE_GRADUAL_VERT As Integer = 4
'    Friend Const PAN_STROKE_GRADUAL_HORZ As Integer = 5
'    Friend Const PAN_STROKE_RAPID_VERT As Integer = 6
'    Friend Const PAN_STROKE_RAPID_HORZ As Integer = 7
'    Friend Const PAN_STROKE_INSTANT_VERT As Integer = 8
'    Friend Const PAN_STRAIGHT_ARMS_HORZ As Integer = 2
'    Friend Const PAN_STRAIGHT_ARMS_WEDGE As Integer = 3
'    Friend Const PAN_STRAIGHT_ARMS_VERT As Integer = 4
'    Friend Const PAN_STRAIGHT_ARMS_SINGLE_SERIF As Integer = 5
'    Friend Const PAN_STRAIGHT_ARMS_DOUBLE_SERIF As Integer = 6
'    Friend Const PAN_BENT_ARMS_HORZ As Integer = 7
'    Friend Const PAN_BENT_ARMS_WEDGE As Integer = 8
'    Friend Const PAN_BENT_ARMS_VERT As Integer = 9
'    Friend Const PAN_BENT_ARMS_SINGLE_SERIF As Integer = 10
'    Friend Const PAN_BENT_ARMS_DOUBLE_SERIF As Integer = 11
'    Friend Const PAN_LETT_NORMAL_CONTACT As Integer = 2
'    Friend Const PAN_LETT_NORMAL_WEIGHTED As Integer = 3
'    Friend Const PAN_LETT_NORMAL_BOXED As Integer = 4
'    Friend Const PAN_LETT_NORMAL_FLATTENED As Integer = 5
'    Friend Const PAN_LETT_NORMAL_ROUNDED As Integer = 6
'    Friend Const PAN_LETT_NORMAL_OFF_CENTER As Integer = 7
'    Friend Const PAN_LETT_NORMAL_SQUARE As Integer = 8
'    Friend Const PAN_LETT_OBLIQUE_CONTACT As Integer = 9
'    Friend Const PAN_LETT_OBLIQUE_WEIGHTED As Integer = 10
'    Friend Const PAN_LETT_OBLIQUE_BOXED As Integer = 11
'    Friend Const PAN_LETT_OBLIQUE_FLATTENED As Integer = 12
'    Friend Const PAN_LETT_OBLIQUE_ROUNDED As Integer = 13
'    Friend Const PAN_LETT_OBLIQUE_OFF_CENTER As Integer = 14
'    Friend Const PAN_LETT_OBLIQUE_SQUARE As Integer = 15
'    Friend Const PAN_MIDLINE_STANDARD_TRIMMED As Integer = 2
'    Friend Const PAN_MIDLINE_STANDARD_POINTED As Integer = 3
'    Friend Const PAN_MIDLINE_STANDARD_SERIFED As Integer = 4
'    Friend Const PAN_MIDLINE_HIGH_TRIMMED As Integer = 5
'    Friend Const PAN_MIDLINE_HIGH_POINTED As Integer = 6
'    Friend Const PAN_MIDLINE_HIGH_SERIFED As Integer = 7
'    Friend Const PAN_MIDLINE_LOW_TRIMMED As Integer = 11
'    Friend Const PAN_MIDLINE_LOW_POINTED As Integer = 12
'    Friend Const PAN_MIDLINE_LOW_SERIFED As Integer = 13
'    Friend Const PAN_XHEIGHT_DUCKING_SMALL As Integer = 5
'    Friend Const PAN_XHEIGHT_DUCKING_STD As Integer = 6
'    Friend Const PAN_XHEIGHT_DUCKING_LARGE As Integer = 7
'    Friend Const PC_RESERVED As Integer = &H1
'    Friend Const PC_EXPLICIT As Integer = &H2
'    Friend Const PC_NOCOLLAPSE As Integer = &H4
'    Friend Const PT_CLOSEFIGURE As Integer = &H1
'    Friend Const PT_LINETO As Integer = &H2
'    Friend Const PT_BEZIERTO As Integer = &H4
'    Friend Const PT_MOVETO As Integer = &H6
'    Friend Const PS_SOLID As Integer = 0
'    Friend Const PS_DASH As Integer = 1
'    Friend Const PS_DOT As Integer = 2
'    Friend Const PS_DASHDOT As Integer = 3
'    Friend Const PS_DASHDOTDOT As Integer = 4
'    Friend Const PS_NULL As Integer = 5
'    Friend Const PS_INSIDEFRAME As Integer = 6
'    Friend Const PS_USERSTYLE As Integer = 7
'    Friend Const PS_ALTERNATE As Integer = 8
'    Friend Const PS_STYLE_MASK As Integer = &HF
'    Friend Const PS_ENDCAP_ROUND As Integer = &H0
'    Friend Const PS_ENDCAP_SQUARE As Integer = &H100
'    Friend Const PS_ENDCAP_FLAT As Integer = &H200
'    Friend Const PS_ENDCAP_MASK As Integer = &HF00
'    Friend Const PS_JOIN_ROUND As Integer = &H0
'    Friend Const PS_JOIN_BEVEL As Integer = &H1000
'    Friend Const PS_JOIN_MITER As Integer = &H2000
'    Friend Const PS_JOIN_MASK As Integer = &HF000
'    Friend Const PS_COSMETIC As Integer = &H0
'    Friend Const PS_GEOMETRIC As Integer = &H10000
'    Friend Const PS_TYPE_MASK As Integer = &HF0000
'    Friend Const PLANES As Integer = 14
'    Friend Const PDEVICESIZE As Integer = 26
'    Friend Const POLYGONALCAPS As Integer = 32
'    Friend Const PHYSICALWIDTH As Integer = 110
'    Friend Const PHYSICALHEIGHT As Integer = 111
'    Friend Const PHYSICALOFFSETX As Integer = 112
'    Friend Const PHYSICALOFFSETY As Integer = 113
'    Friend Const PC_NONE As Integer = 0
'    Friend Const PC_POLYGON As Integer = 1
'    Friend Const PC_RECTANGLE As Integer = 2
'    Friend Const PC_WINDPOLYGON As Integer = 4
'    Friend Const PC_TRAPEZOID As Integer = 4
'    Friend Const PC_SCANLINE As Integer = 8
'    Friend Const PC_WIDE As Integer = 16
'    Friend Const PC_STYLED As Integer = 32
'    Friend Const PC_WIDESTYLED As Integer = 64
'    Friend Const PC_INTERIORS As Integer = 128
'    Friend Const PC_POLYPOLYGON As Integer = 256
'    Friend Const PC_PATHS As Integer = 512
'    Friend Const PFD_TYPE_RGBA As Integer = 0
'    Friend Const PFD_TYPE_COLORINDEX As Integer = 1
'    Friend Const PFD_MAIN_PLANE As Integer = 0
'    Friend Const PFD_OVERLAY_PLANE As Integer = 1
'    Friend Const PFD_UNDERLAY_PLANE As Integer = - 1
'    Friend Const PFD_DOUBLEBUFFER As Integer = &H1
'    Friend Const PFD_STEREO As Integer = &H2
'    Friend Const PFD_DRAW_TO_WINDOW As Integer = &H4
'    Friend Const PFD_DRAW_TO_BITMAP As Integer = &H8
'    Friend Const PFD_SUPPORT_GDI As Integer = &H10
'    Friend Const PFD_SUPPORT_OPENGL As Integer = &H20
'    Friend Const PFD_GENERIC_FORMAT As Integer = &H40
'    Friend Const PFD_NEED_PALETTE As Integer = &H80
'    Friend Const PFD_NEED_SYSTEM_PALETTE As Integer = &H100
'    Friend Const PFD_SWAP_EXCHANGE As Integer = &H200
'    Friend Const PFD_SWAP_COPY As Integer = &H400
'    Friend Const PFD_SWAP_LAYER_BUFFERS As Integer = &H800
'    Friend Const PFD_GENERIC_ACCELERATED As Integer = &H1000
'    Friend Const PFD_DEPTH_DONTCARE As Integer = &H20000000
'    Friend Const PFD_DOUBLEBUFFER_DONTCARE As Integer = &H40000000
'    Friend Const PFD_STEREO_DONTCARE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        PFD_DOUBLEBUFFER_DONTCARE = 0x40000000,
'    '        PFD_STEREO_DONTCARE = unchecked((int)0x80000000),
'    '-------------------------------^--- GenCode(token): unexpected token type
'    Friend Const PP_DISPLAYERRORS As Integer = &H1
'    Friend Const PROCESS_TERMINATE As Integer = &H1
'    Friend Const PROCESS_CREATE_THREAD As Integer = &H2
'    Friend Const PROCESS_VM_OPERATION As Integer = &H8
'    Friend Const PROCESS_VM_READ As Integer = &H10
'    Friend Const PROCESS_VM_WRITE As Integer = &H20
'    Friend Const PROCESS_DUP_HANDLE As Integer = &H40
'    Friend Const PROCESS_CREATE_PROCESS As Integer = &H80
'    Friend Const PROCESS_SET_QUOTA As Integer = &H100
'    Friend Const PROCESS_SET_INFORMATION As Integer = &H200
'    Friend Const PROCESS_QUERY_INFORMATION As Integer = &H400
'    Friend Const PROCESSOR_INTEL_386 As Integer = 386
'    Friend Const PROCESSOR_INTEL_486 As Integer = 486
'    Friend Const PROCESSOR_INTEL_PENTIUM As Integer = 586
'    Friend Const PROCESSOR_MIPS_R4000 As Integer = 4000
'    Friend Const PROCESSOR_ALPHA_21064 As Integer = 21064
'    Friend Const PROCESSOR_ARCHITECTURE_INTEL As Integer = 0
'    Friend Const PROCESSOR_ARCHITECTURE_MIPS As Integer = 1
'    Friend Const PROCESSOR_ARCHITECTURE_ALPHA As Integer = 2
'    Friend Const PROCESSOR_ARCHITECTURE_PPC As Integer = 3
'    Friend Const PROCESSOR_ARCHITECTURE_UNKNOWN As Integer = &HFFFF
'    Friend Const PF_FLOATING_POINT_PRECISION_ERRATA As Integer = 0
'    Friend Const PF_FLOATING_POINT_EMULATED As Integer = 1
'    Friend Const PF_COMPARE_EXCHANGE_DOUBLE As Integer = 2
'    Friend Const PF_MMX_INSTRUCTIONS_AVAILABLE As Integer = 3
'    Friend Const PAGE_NOACCESS As Integer = &H1
'    Friend Const PAGE_READONLY As Integer = &H2
'    Friend Const PAGE_READWRITE As Integer = &H4
'    Friend Const PAGE_WRITECOPY As Integer = &H8
'    Friend Const PAGE_EXECUTE As Integer = &H10
'    Friend Const PAGE_EXECUTE_READ As Integer = &H20
'    Friend Const PAGE_EXECUTE_READWRITE As Integer = &H40
'    Friend Const PAGE_EXECUTE_WRITECOPY As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        PAGE_EXECUTE_READWRITE = 0x40,
'    '        PAGE_EXECUTE_WRITECOPY = unchecked((int)0x80),
'    '----------------------------------^--- GenCode(token): unexpected token type
'    Friend Const PAGE_GUARD As Integer = &H100
'    Friend Const PAGE_NOCACHE As Integer = &H200
'    Friend Const PRIVILEGE_SET_ALL_NECESSARY As Integer = 1
'    Friend Const PERF_DATA_VERSION As Integer = 1
'    Friend Const PERF_DATA_REVISION As Integer = 1
'    Friend Const PERF_NO_INSTANCES As Integer = - 1
'    Friend Const PERF_SIZE_DWORD As Integer = &H0
'    Friend Const PERF_SIZE_LARGE As Integer = &H100
'    Friend Const PERF_SIZE_ZERO As Integer = &H200
'    Friend Const PERF_SIZE_VARIABLE_LEN As Integer = &H300
'    Friend Const PERF_TYPE_NUMBER As Integer = &H0
'    Friend Const PERF_TYPE_COUNTER As Integer = &H400
'    Friend Const PERF_TYPE_TEXT As Integer = &H800
'    Friend Const PERF_TYPE_ZERO As Integer = &HC00
'    Friend Const PERF_NUMBER_HEX As Integer = &H0
'    Friend Const PERF_NUMBER_DECIMAL As Integer = &H10000
'    Friend Const PERF_NUMBER_DEC_1000 As Integer = &H20000
'    Friend Const PERF_COUNTER_VALUE As Integer = &H0
'    Friend Const PERF_COUNTER_RATE As Integer = &H10000
'    Friend Const PERF_COUNTER_FRACTION As Integer = &H20000
'    Friend Const PERF_COUNTER_BASE As Integer = &H30000
'    Friend Const PERF_COUNTER_ELAPSED As Integer = &H40000
'    Friend Const PERF_COUNTER_QUEUELEN As Integer = &H50000
'    Friend Const PERF_COUNTER_HISTOGRAM As Integer = &H60000
'    Friend Const PERF_TEXT_UNICODE As Integer = &H0
'    Friend Const PERF_TEXT_ASCII As Integer = &H10000
'    Friend Const PERF_TIMER_TICK As Integer = &H0
'    Friend Const PERF_TIMER_100NS As Integer = &H100000
'    Friend Const PERF_OBJECT_TIMER As Integer = &H200000
'    Friend Const PERF_DELTA_COUNTER As Integer = &H400000
'    Friend Const PERF_DELTA_BASE As Integer = &H800000
'    Friend Const PERF_INVERSE_COUNTER As Integer = &H1000000
'    Friend Const PERF_MULTI_COUNTER As Integer = &H2000000
'    Friend Const PERF_DISPLAY_NO_SUFFIX As Integer = &H0
'    Friend Const PERF_DISPLAY_PER_SEC As Integer = &H10000000
'    Friend Const PERF_DISPLAY_PERCENT As Integer = &H20000000
'    Friend Const PERF_DISPLAY_SECONDS As Integer = &H30000000
'    Friend Const PERF_DISPLAY_NOSHOW As Integer = &H40000000
'    Friend Const PERF_COUNTER_HISTOGRAM_TYPE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        PERF_DISPLAY_NOSHOW = 0x40000000,
'    '        PERF_COUNTER_HISTOGRAM_TYPE = unchecked((int)0x80000000),
'    '---------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const PERF_DETAIL_NOVICE As Integer = 100
'    Friend Const PERF_DETAIL_ADVANCED As Integer = 200
'    Friend Const PERF_DETAIL_EXPERT As Integer = 300
'    Friend Const PERF_DETAIL_WIZARD As Integer = 400
'    Friend Const PERF_NO_UNIQUE_ID As Integer = - 1
'    Friend Const PROVIDER_KEEPS_VALUE_LENGTH As Integer = &H1
'    Friend Const PRINTER_CONTROL_PAUSE As Integer = 1
'    Friend Const PRINTER_CONTROL_RESUME As Integer = 2
'    Friend Const PRINTER_CONTROL_PURGE As Integer = 3
'    Friend Const PRINTER_CONTROL_SET_STATUS As Integer = 4
'    Friend Const PRINTER_STATUS_PAUSED As Integer = &H1
'    Friend Const PRINTER_STATUS_ERROR As Integer = &H2
'    Friend Const PRINTER_STATUS_PENDING_DELETION As Integer = &H4
'    Friend Const PRINTER_STATUS_PAPER_JAM As Integer = &H8
'    Friend Const PRINTER_STATUS_PAPER_OUT As Integer = &H10
'    Friend Const PRINTER_STATUS_MANUAL_FEED As Integer = &H20
'    Friend Const PRINTER_STATUS_PAPER_PROBLEM As Integer = &H40
'    Friend Const PRINTER_STATUS_OFFLINE As Integer = &H80
'    Friend Const PRINTER_STATUS_IO_ACTIVE As Integer = &H100
'    Friend Const PRINTER_STATUS_BUSY As Integer = &H200
'    Friend Const PRINTER_STATUS_PRINTING As Integer = &H400
'    Friend Const PRINTER_STATUS_OUTPUT_BIN_FULL As Integer = &H800
'    Friend Const PRINTER_STATUS_NOT_AVAILABLE As Integer = &H1000
'    Friend Const PRINTER_STATUS_WAITING As Integer = &H2000
'    Friend Const PRINTER_STATUS_PROCESSING As Integer = &H4000
'    Friend Const PRINTER_STATUS_INITIALIZING As Integer = &H8000
'    Friend Const PRINTER_STATUS_WARMING_UP As Integer = &H10000
'    Friend Const PRINTER_STATUS_TONER_LOW As Integer = &H20000
'    Friend Const PRINTER_STATUS_NO_TONER As Integer = &H40000
'    Friend Const PRINTER_STATUS_PAGE_PUNT As Integer = &H80000
'    Friend Const PRINTER_STATUS_USER_INTERVENTION As Integer = &H100000
'    Friend Const PRINTER_STATUS_OUT_OF_MEMORY As Integer = &H200000
'    Friend Const PRINTER_STATUS_DOOR_OPEN As Integer = &H400000
'    Friend Const PRINTER_STATUS_SERVER_UNKNOWN As Integer = &H800000
'    Friend Const PRINTER_STATUS_POWER_SAVE As Integer = &H1000000
'    Friend Const PRINTER_ATTRIBUTE_QUEUED As Integer = &H1
'    Friend Const PRINTER_ATTRIBUTE_DIRECT As Integer = &H2
'    Friend Const PRINTER_ATTRIBUTE_DEFAULT As Integer = &H4
'    Friend Const PRINTER_ATTRIBUTE_SHARED As Integer = &H8
'    Friend Const PRINTER_ATTRIBUTE_NETWORK As Integer = &H10
'    Friend Const PRINTER_ATTRIBUTE_HIDDEN As Integer = &H20
'    Friend Const PRINTER_ATTRIBUTE_LOCAL As Integer = &H40
'    Friend Const PRINTER_ATTRIBUTE_ENABLE_DEVQ As Integer = &H80
'    Friend Const PRINTER_ATTRIBUTE_KEEPPRINTEDJOBS As Integer = &H100
'    Friend Const PRINTER_ATTRIBUTE_DO_COMPLETE_FIRST As Integer = &H200
'    Friend Const PRINTER_ATTRIBUTE_WORK_OFFLINE As Integer = &H400
'    Friend Const PRINTER_ATTRIBUTE_ENABLE_BIDI As Integer = &H800
'    Friend Const PRINTER_ATTRIBUTE_RAW_ONLY As Integer = &H1000
'    Friend Const PORT_TYPE_WRITE As Integer = &H1
'    Friend Const PORT_TYPE_READ As Integer = &H2
'    Friend Const PORT_TYPE_REDIRECTED As Integer = &H4
'    Friend Const PORT_TYPE_NET_ATTACHED As Integer = &H8
'    Friend Const PORT_STATUS_TYPE_ERROR As Integer = 1
'    Friend Const PORT_STATUS_TYPE_WARNING As Integer = 2
'    Friend Const PORT_STATUS_TYPE_INFO As Integer = 3
'    Friend Const PORT_STATUS_OFFLINE As Integer = 1
'    Friend Const PORT_STATUS_PAPER_JAM As Integer = 2
'    Friend Const PORT_STATUS_PAPER_OUT As Integer = 3
'    Friend Const PORT_STATUS_OUTPUT_BIN_FULL As Integer = 4
'    Friend Const PORT_STATUS_PAPER_PROBLEM As Integer = 5
'    Friend Const PORT_STATUS_NO_TONER As Integer = 6
'    Friend Const PORT_STATUS_DOOR_OPEN As Integer = 7
'    Friend Const PORT_STATUS_USER_INTERVENTION As Integer = 8
'    Friend Const PORT_STATUS_OUT_OF_MEMORY As Integer = 9
'    Friend Const PORT_STATUS_TONER_LOW As Integer = 10
'    Friend Const PORT_STATUS_WARMING_UP As Integer = 11
'    Friend Const PORT_STATUS_POWER_SAVE As Integer = 12
'    Friend Const PRINTER_ENUM_DEFAULT As Integer = &H1
'    Friend Const PRINTER_ENUM_LOCAL As Integer = &H2
'    Friend Const PRINTER_ENUM_CONNECTIONS As Integer = &H4
'    Friend Const PRINTER_ENUM_FAVORITE As Integer = &H4
'    Friend Const PRINTER_ENUM_NAME As Integer = &H8
'    Friend Const PRINTER_ENUM_REMOTE As Integer = &H10
'    Friend Const PRINTER_ENUM_SHARED As Integer = &H20
'    Friend Const PRINTER_ENUM_NETWORK As Integer = &H40
'    Friend Const PRINTER_ENUM_EXPAND As Integer = &H4000
'    Friend Const PRINTER_ENUM_CONTAINER As Integer = &H8000
'    Friend Const PRINTER_ENUM_ICONMASK As Integer = &HFF0000
'    Friend Const PRINTER_ENUM_ICON1 As Integer = &H10000
'    Friend Const PRINTER_ENUM_ICON2 As Integer = &H20000
'    Friend Const PRINTER_ENUM_ICON3 As Integer = &H40000
'    Friend Const PRINTER_ENUM_ICON4 As Integer = &H80000
'    Friend Const PRINTER_ENUM_ICON5 As Integer = &H100000
'    Friend Const PRINTER_ENUM_ICON6 As Integer = &H200000
'    Friend Const PRINTER_ENUM_ICON7 As Integer = &H400000
'    Friend Const PRINTER_ENUM_ICON8 As Integer = &H800000
'    Friend Const PRINTER_NOTIFY_TYPE As Integer = &H0
'    Friend Const PRINTER_NOTIFY_FIELD_SERVER_NAME As Integer = &H0
'    Friend Const PRINTER_NOTIFY_FIELD_PRINTER_NAME As Integer = &H1
'    Friend Const PRINTER_NOTIFY_FIELD_SHARE_NAME As Integer = &H2
'    Friend Const PRINTER_NOTIFY_FIELD_PORT_NAME As Integer = &H3
'    Friend Const PRINTER_NOTIFY_FIELD_DRIVER_NAME As Integer = &H4
'    Friend Const PRINTER_NOTIFY_FIELD_COMMENT As Integer = &H5
'    Friend Const PRINTER_NOTIFY_FIELD_LOCATION As Integer = &H6
'    Friend Const PRINTER_NOTIFY_FIELD_DEVMODE As Integer = &H7
'    Friend Const PRINTER_NOTIFY_FIELD_SEPFILE As Integer = &H8
'    Friend Const PRINTER_NOTIFY_FIELD_PRINT_PROCESSOR As Integer = &H9
'    Friend Const PRINTER_NOTIFY_FIELD_PARAMETERS As Integer = &HA
'    Friend Const PRINTER_NOTIFY_FIELD_DATATYPE As Integer = &HB
'    Friend Const PRINTER_NOTIFY_FIELD_SECURITY_DESCRIPTOR As Integer = &HC
'    Friend Const PRINTER_NOTIFY_FIELD_ATTRIBUTES As Integer = &HD
'    Friend Const PRINTER_NOTIFY_FIELD_PRIORITY As Integer = &HE
'    Friend Const PRINTER_NOTIFY_FIELD_DEFAULT_PRIORITY As Integer = &HF
'    Friend Const PRINTER_NOTIFY_FIELD_START_TIME As Integer = &H10
'    Friend Const PRINTER_NOTIFY_FIELD_UNTIL_TIME As Integer = &H11
'    Friend Const PRINTER_NOTIFY_FIELD_STATUS As Integer = &H12
'    Friend Const PRINTER_NOTIFY_FIELD_STATUS_STRING As Integer = &H13
'    Friend Const PRINTER_NOTIFY_FIELD_CJOBS As Integer = &H14
'    Friend Const PRINTER_NOTIFY_FIELD_AVERAGE_PPM As Integer = &H15
'    Friend Const PRINTER_NOTIFY_FIELD_TOTAL_PAGES As Integer = &H16
'    Friend Const PRINTER_NOTIFY_FIELD_PAGES_PRINTED As Integer = &H17
'    Friend Const PRINTER_NOTIFY_FIELD_TOTAL_BYTES As Integer = &H18
'    Friend Const PRINTER_NOTIFY_FIELD_BYTES_PRINTED As Integer = &H19
'    Friend Const PRINTER_NOTIFY_OPTIONS_REFRESH As Integer = &H1
'    Friend Const PRINTER_NOTIFY_INFO_DISCARDED As Integer = &H1
'    Friend Const PRINTER_CHANGE_ADD_PRINTER As Integer = &H1
'    Friend Const PRINTER_CHANGE_SET_PRINTER As Integer = &H2
'    Friend Const PRINTER_CHANGE_DELETE_PRINTER As Integer = &H4
'    Friend Const PRINTER_CHANGE_FAILED_CONNECTION_PRINTER As Integer = &H8
'    Friend Const PRINTER_CHANGE_PRINTER As Integer = &HFF
'    Friend Const PRINTER_CHANGE_ADD_JOB As Integer = &H100
'    Friend Const PRINTER_CHANGE_SET_JOB As Integer = &H200
'    Friend Const PRINTER_CHANGE_DELETE_JOB As Integer = &H400
'    Friend Const PRINTER_CHANGE_WRITE_JOB As Integer = &H800
'    Friend Const PRINTER_CHANGE_JOB As Integer = &HFF00
'    Friend Const PRINTER_CHANGE_ADD_FORM As Integer = &H10000
'    Friend Const PRINTER_CHANGE_SET_FORM As Integer = &H20000
'    Friend Const PRINTER_CHANGE_DELETE_FORM As Integer = &H40000
'    Friend Const PRINTER_CHANGE_FORM As Integer = &H70000
'    Friend Const PRINTER_CHANGE_ADD_PORT As Integer = &H100000
'    Friend Const PRINTER_CHANGE_CONFIGURE_PORT As Integer = &H200000
'    Friend Const PRINTER_CHANGE_DELETE_PORT As Integer = &H400000
'    Friend Const PRINTER_CHANGE_PORT As Integer = &H700000
'    Friend Const PRINTER_CHANGE_ADD_PRINT_PROCESSOR As Integer = &H1000000
'    Friend Const PRINTER_CHANGE_DELETE_PRINT_PROCESSOR As Integer = &H4000000
'    Friend Const PRINTER_CHANGE_PRINT_PROCESSOR As Integer = &H7000000
'    Friend Const PRINTER_CHANGE_ADD_PRINTER_DRIVER As Integer = &H10000000
'    Friend Const PRINTER_CHANGE_SET_PRINTER_DRIVER As Integer = &H20000000
'    Friend Const PRINTER_CHANGE_DELETE_PRINTER_DRIVER As Integer = &H40000000
'    Friend Const PRINTER_CHANGE_PRINTER_DRIVER As Integer = &H70000000
'    Friend Const PRINTER_CHANGE_TIMEOUT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        PRINTER_CHANGE_PRINTER_DRIVER = 0x70000000,
'    '        PRINTER_CHANGE_TIMEOUT = unchecked((int)0x80000000),
'    '----------------------------------^--- GenCode(token): unexpected token type
'    Friend Const PRINTER_CHANGE_ALL As Integer = &H7777FFFF
'    Friend Const PRINTER_ERROR_INFORMATION As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        PRINTER_CHANGE_ALL = 0x7777FFFF,
'    '        PRINTER_ERROR_INFORMATION = unchecked((int)0x80000000),
'    '-------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const PRINTER_ERROR_WARNING As Integer = &H40000000
'    Friend Const PRINTER_ERROR_SEVERE As Integer = &H20000000
'    Friend Const PRINTER_ERROR_OUTOFPAPER As Integer = &H1
'    Friend Const PRINTER_ERROR_JAM As Integer = &H2
'    Friend Const PRINTER_ERROR_OUTOFTONER As Integer = &H4
'    Friend Const PRINTER_ACCESS_ADMINISTER As Integer = &H4
'    Friend Const PRINTER_ACCESS_USE As Integer = &H8
'    Friend Const PWR_OK As Integer = 1
'    Friend Const PWR_FAIL As Integer = - 1
'    Friend Const PWR_SUSPENDREQUEST As Integer = 1
'    Friend Const PWR_SUSPENDRESUME As Integer = 2
'    Friend Const PWR_CRITICALRESUME As Integer = 3
'    Friend Const PRF_CHECKVISIBLE As Integer = &H1
'    Friend Const PRF_NONCLIENT As Integer = &H2
'    Friend Const PRF_CLIENT As Integer = &H4
'    Friend Const PRF_ERASEBKGND As Integer = &H8
'    Friend Const PRF_CHILDREN As Integer = &H10
'    Friend Const PRF_OWNED As Integer = &H20
'    Friend Const PM_NOREMOVE As Integer = &H0
'    Friend Const PM_REMOVE As Integer = &H1
'    Friend Const PM_NOYIELD As Integer = &H2
'    Friend Const PSM_PAGEINFO As Integer = &H400 + 100
'    Friend Const PSM_SHEETINFO As Integer = &H400 + 101
'    Friend Const PSI_SETACTIVE As Integer = &H1
'    Friend Const PSI_KILLACTIVE As Integer = &H2
'    Friend Const PSI_APPLY As Integer = &H3
'    Friend Const PSI_RESET As Integer = &H4
'    Friend Const PSI_HASHELP As Integer = &H5
'    Friend Const PSI_HELP As Integer = &H6
'    Friend Const PSI_CHANGED As Integer = &H1
'    Friend Const PSI_GUISTART As Integer = &H2
'    Friend Const PSI_REBOOT As Integer = &H3
'    Friend Const PSI_GETSIBLINGS As Integer = &H4
'    Friend Const PBS_SMOOTH As Integer = &H1
'    Friend Const PBS_VERTICAL As Integer = &H4
'    Friend Const PBM_SETRANGE As Integer = &H400 + 1
'    Friend Const PBM_SETPOS As Integer = &H400 + 2
'    Friend Const PBM_DELTAPOS As Integer = &H400 + 3
'    Friend Const PBM_SETSTEP As Integer = &H400 + 4
'    Friend Const PBM_STEPIT As Integer = &H400 + 5
'    Friend Const PBM_SETRANGE32 As Integer = &H400 + 6
'    Friend Const PBM_GETRANGE As Integer = &H400 + 7
'    Friend Const PBM_GETPOS As Integer = &H400 + 8
'    Friend Const PSM_SETCURSEL As Integer = &H400 + 101
'    Friend Const PSM_REMOVEPAGE As Integer = &H400 + 102
'    Friend Const PSM_ADDPAGE As Integer = &H400 + 103
'    Friend Const PSM_CHANGED As Integer = &H400 + 104
'    Friend Const PSM_RESTARTWINDOWS As Integer = &H400 + 105
'    Friend Const PSM_REBOOTSYSTEM As Integer = &H400 + 106
'    Friend Const PSM_CANCELTOCLOSE As Integer = &H400 + 107
'    Friend Const PSM_QUERYSIBLINGS As Integer = &H400 + 108
'    Friend Const PSM_UNCHANGED As Integer = &H400 + 109
'    Friend Const PSM_APPLY As Integer = &H400 + 110
'    Friend Const PSM_SETTITLEA As Integer = &H400 + 111
'    Friend Const PSM_SETTITLEW As Integer = &H400 + 120
'    Friend Const PSM_SETWIZBUTTONS As Integer = &H400 + 112
'    Friend Const PSM_PRESSBUTTON As Integer = &H400 + 113
'    Friend Const PSM_SETCURSELID As Integer = &H400 + 114
'    Friend Const PSM_SETFINISHTEXTA As Integer = &H400 + 115
'    Friend Const PSM_SETFINISHTEXTW As Integer = &H400 + 121
'    Friend Const PSM_GETTABCONTROL As Integer = &H400 + 116
'    Friend Const PSM_ISDIALOGMESSAGE As Integer = &H400 + 117
'    Friend Const PSM_GETCURRENTPAGEHWND As Integer = &H400 + 118
'    Friend Const PATCOPY As Integer = &HF00021
'    Friend Const PATPAINT As Integer = &HFB0A09
'    Friend Const PATINVERT As Integer = &H5A0049
'    Friend Const PGN_FIRST As Integer = 0 - 900
'    Friend Const PGN_LAST As Integer = 0 - 950
    
    
    
'    Friend Const QID_SYNC As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '
'    '        public const int QID_SYNC = unchecked((int)0xFFFFFFFF),
'    '-------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const QUERYROPSUPPORT As Integer = 40
'    Friend Const QUERYESCSUPPORT As Integer = 8
'    Friend Const QUERYDIBSUPPORT As Integer = 3073
'    Friend Const QDI_SETDIBITS As Integer = 1
'    Friend Const QDI_GETDIBITS As Integer = 2
'    Friend Const QDI_DIBTOSCREEN As Integer = 4
'    Friend Const QDI_STRETCHDIB As Integer = 8
        Friend Const QS_KEY As Integer = &H1
        Friend Const QS_MOUSEMOVE As Integer = &H2
        Friend Const QS_MOUSEBUTTON As Integer = &H4
        Friend Const QS_POSTMESSAGE As Integer = &H8
        Friend Const QS_TIMER As Integer = &H10
        Friend Const QS_PAINT As Integer = &H20
        Friend Const QS_SENDMESSAGE As Integer = &H40
        Friend Const QS_HOTKEY As Integer = &H80
        Friend Const QS_ALLPOSTMESSAGE As Integer = &H100
        Friend Const QS_MOUSE As Integer = QS_MOUSEMOVE Or QS_MOUSEBUTTON
        Friend Const QS_INPUT As Integer = QS_MOUSE Or QS_KEY
        Friend Const QS_ALLEVENTS As Integer = QS_INPUT Or QS_POSTMESSAGE Or QS_TIMER Or QS_PAINT Or QS_HOTKEY
        Friend Const QS_ALLINPUT As Integer = QS_INPUT Or QS_POSTMESSAGE Or QS_TIMER Or QS_PAINT Or QS_HOTKEY Or QS_SENDMESSAGE
    
'    Friend Const REGULAR_FONTTYPE As Integer = &H400
'    Friend Const rad1 As Integer = &H420
'    Friend Const rad2 As Integer = &H421
'    Friend Const rad3 As Integer = &H422
'    Friend Const rad4 As Integer = &H423
'    Friend Const rad5 As Integer = &H424
'    Friend Const rad6 As Integer = &H425
'    Friend Const rad7 As Integer = &H426
'    Friend Const rad8 As Integer = &H427
'    Friend Const rad9 As Integer = &H428
'    Friend Const rad10 As Integer = &H429
'    Friend Const rad11 As Integer = &H42A
'    Friend Const rad12 As Integer = &H42B
'    Friend Const rad13 As Integer = &H42C
'    Friend Const rad14 As Integer = &H42D
'    Friend Const rad15 As Integer = &H42E
'    Friend Const rad16 As Integer = &H42F
'    Friend Const rct1 As Integer = &H438
'    Friend Const rct2 As Integer = &H439
'    Friend Const rct3 As Integer = &H43A
'    Friend Const rct4 As Integer = &H43B
'    Friend Const REPLACEDLGORD As Integer = 1541
'    Friend Const REGISTERING As Integer = &H0
'    Friend Const REGISTERED As Integer = &H4
'    Friend Const RPC_C_BINDING_INFINITE_TIMEOUT As Integer = 10
'    Friend Const RPC_C_BINDING_MIN_TIMEOUT As Integer = 0
'    Friend Const RPC_C_BINDING_DEFAULT_TIMEOUT As Integer = 5
'    Friend Const RPC_C_BINDING_MAX_TIMEOUT As Integer = 9
'    Friend Const RPC_C_CANCEL_INFINITE_TIMEOUT As Integer = - 1
'    Friend Const RPC_C_LISTEN_MAX_CALLS_DEFAULT As Integer = 1234
'    Friend Const RPC_C_PROTSEQ_MAX_REQS_DEFAULT As Integer = 10
'    Friend Const RPC_C_BIND_TO_ALL_NICS As Integer = 1
'    Friend Const RPC_C_USE_INTERNET_PORT As Integer = 1
'    Friend Const RPC_C_USE_INTRANET_PORT As Integer = 2
'    Friend Const RPC_C_STATS_CALLS_IN As Integer = 0
'    Friend Const RPC_C_STATS_CALLS_OUT As Integer = 1
'    Friend Const RPC_C_STATS_PKTS_IN As Integer = 2
'    Friend Const RPC_C_STATS_PKTS_OUT As Integer = 3
'    Friend Const RPC_C_AUTHN_LEVEL_DEFAULT As Integer = 0
'    Friend Const RPC_C_AUTHN_LEVEL_NONE As Integer = 1
'    Friend Const RPC_C_AUTHN_LEVEL_CONNECT As Integer = 2
'    Friend Const RPC_C_AUTHN_LEVEL_CALL As Integer = 3
'    Friend Const RPC_C_AUTHN_LEVEL_PKT As Integer = 4
'    Friend Const RPC_C_AUTHN_LEVEL_PKT_INTEGRITY As Integer = 5
'    Friend Const RPC_C_AUTHN_LEVEL_PKT_PRIVACY As Integer = 6
'    Friend Const RPC_C_IMP_LEVEL_ANONYMOUS As Integer = 1
'    Friend Const RPC_C_IMP_LEVEL_IDENTIFY As Integer = 2
'    Friend Const RPC_C_IMP_LEVEL_IMPERSONATE As Integer = 3
'    Friend Const RPC_C_IMP_LEVEL_DELEGATE As Integer = 4
'    Friend Const RPC_C_QOS_IDENTITY_STATIC As Integer = 0
'    Friend Const RPC_C_QOS_IDENTITY_DYNAMIC As Integer = 1
'    Friend Const RPC_C_QOS_CAPABILITIES_DEFAULT As Integer = 0
'    Friend Const RPC_C_QOS_CAPABILITIES_MUTUAL_AUTH As Integer = 1
'    Friend Const RPC_C_PROTECT_LEVEL_DEFAULT As Integer = 0
'    Friend Const RPC_C_PROTECT_LEVEL_NONE As Integer = 1
'    Friend Const RPC_C_PROTECT_LEVEL_CONNECT As Integer = 2
'    Friend Const RPC_C_PROTECT_LEVEL_CALL As Integer = 3
'    Friend Const RPC_C_PROTECT_LEVEL_PKT As Integer = 4
'    Friend Const RPC_C_PROTECT_LEVEL_PKT_INTEGRITY As Integer = 5
'    Friend Const RPC_C_PROTECT_LEVEL_PKT_PRIVACY As Integer = 6
'    Friend Const RPC_C_AUTHN_NONE As Integer = 0
'    Friend Const RPC_C_AUTHN_DCE_PRIVATE As Integer = 1
'    Friend Const RPC_C_AUTHN_DCE_PUBLIC As Integer = 2
'    Friend Const RPC_C_AUTHN_DEC_PUBLIC As Integer = 4
'    Friend Const RPC_C_AUTHN_WINNT As Integer = 10
'    Friend Const RPC_C_AUTHN_DEFAULT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_C_AUTHN_WINNT = 10,
'    '        RPC_C_AUTHN_DEFAULT = unchecked((int)0xFFFFFFFF),
'    '-------------------------------^--- GenCode(token): unexpected token type
'    Friend Const RPC_C_SECURITY_QOS_VERSION As Integer = 1
'    Friend Const RPC_C_AUTHZ_NONE As Integer = 0
'    Friend Const RPC_C_AUTHZ_NAME As Integer = 1
'    Friend Const RPC_C_AUTHZ_DCE As Integer = 2
'    Friend Const RPC_C_EP_ALL_ELTS As Integer = 0
'    Friend Const RPC_C_EP_MATCH_BY_IF As Integer = 1
'    Friend Const RPC_C_EP_MATCH_BY_OBJ As Integer = 2
'    Friend Const RPC_C_EP_MATCH_BY_BOTH As Integer = 3
'    Friend Const RPC_C_VERS_ALL As Integer = 1
'    Friend Const RPC_C_VERS_COMPATIBLE As Integer = 2
'    Friend Const RPC_C_VERS_EXACT As Integer = 3
'    Friend Const RPC_C_VERS_MAJOR_ONLY As Integer = 4
'    Friend Const RPC_C_VERS_UPTO As Integer = 5
'    Friend Const RPC_C_MGMT_INQ_IF_IDS As Integer = 0
'    Friend Const RPC_C_MGMT_INQ_PRINC_NAME As Integer = 1
'    Friend Const RPC_C_MGMT_INQ_STATS As Integer = 2
'    Friend Const RPC_C_MGMT_IS_SERVER_LISTEN As Integer = 3
'    Friend Const RPC_C_MGMT_STOP_SERVER_LISTEN As Integer = 4
'    Friend Const RPC_C_PARM_MAX_PACKET_LENGTH As Integer = 1
'    Friend Const RPC_C_PARM_BUFFER_LENGTH As Integer = 2
'    Friend Const RPC_IF_AUTOLISTEN As Integer = &H1
'    Friend Const RPC_IF_OLE As Integer = &H2
'    Friend Const RPC_NCA_FLAGS_DEFAULT As Integer = &H0
'    Friend Const RPC_NCA_FLAGS_IDEMPOTENT As Integer = &H1
'    Friend Const RPC_NCA_FLAGS_BROADCAST As Integer = &H2
'    Friend Const RPC_NCA_FLAGS_MAYBE As Integer = &H4
'    Friend Const RPC_BUFFER_COMPLETE As Integer = &H1000
'    Friend Const RPC_BUFFER_PARTIAL As Integer = &H2000
'    Friend Const RPC_BUFFER_EXTRA As Integer = &H4000
'    Friend Const RPCFLG_NON_NDR As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_BUFFER_EXTRA = 0x00004000,
'    '        RPCFLG_NON_NDR = unchecked((int)0x80000000),
'    '--------------------------^--- GenCode(token): unexpected token type
'    Friend Const RPCFLG_ASYNCHRONOUS As Integer = &H40000000
'    Friend Const RPCFLG_INPUT_SYNCHRONOUS As Integer = &H20000000
'    Friend Const RPCFLG_LOCAL_CALL As Integer = &H10000000
'    Friend Const RPC_FLAGS_VALID_BIT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPCFLG_LOCAL_CALL = 0x10000000,
'    '        RPC_FLAGS_VALID_BIT = unchecked((int)0x8000);
'    '-------------------------------^--- GenCode(token): unexpected token type
'    ' RPC_FLAGS_VALID_BIT = 0x00008000;
'    Friend Const RPC_INTERFACE_HAS_PIPES As Integer = &H1
'    Friend Const RPC_C_NS_SYNTAX_DEFAULT As Integer = 0
'    Friend Const RPC_C_NS_SYNTAX_DCE As Integer = 3
'    Friend Const RPC_C_PROFILE_DEFAULT_ELT As Integer = 0
'    Friend Const RPC_C_PROFILE_ALL_ELT As Integer = 1
'    Friend Const RPC_C_PROFILE_MATCH_BY_IF As Integer = 2
'    Friend Const RPC_C_PROFILE_MATCH_BY_MBR As Integer = 3
'    Friend Const RPC_C_PROFILE_MATCH_BY_BOTH As Integer = 4
'    Friend Const RPC_C_NS_DEFAULT_EXP_AGE As Integer = - 1
'    Friend Const RTS_CONTROL_DISABLE As Integer = &H0
'    Friend Const RTS_CONTROL_ENABLE As Integer = &H1
'    Friend Const RTS_CONTROL_HANDSHAKE As Integer = &H2
'    Friend Const RTS_CONTROL_TOGGLE As Integer = &H3
'    Friend Const REALTIME_PRIORITY_CLASS As Integer = &H100
'    Friend Const RIP_EVENT As Integer = 9
'    Friend Const RESETDEV As Integer = 7
'    Friend Const RIGHT_ALT_PRESSED As Integer = &H1
'    Friend Const RIGHT_CTRL_PRESSED As Integer = &H4
'    Friend Const RIGHTMOST_BUTTON_PRESSED As Integer = &H2
'    Friend Const RPC_S_INVALID_STRING_BINDING As Integer = 1700
'    Friend Const RPC_S_WRONG_KIND_OF_BINDING As Integer = 1701
'    Friend Const RPC_S_INVALID_BINDING As Integer = 1702
'    Friend Const RPC_S_PROTSEQ_NOT_SUPPORTED As Integer = 1703
'    Friend Const RPC_S_INVALID_RPC_PROTSEQ As Integer = 1704
'    Friend Const RPC_S_INVALID_STRING_UUID As Integer = 1705
'    Friend Const RPC_S_INVALID_ENDPOINT_FORMAT As Integer = 1706
'    Friend Const RPC_S_INVALID_NET_ADDR As Integer = 1707
'    Friend Const RPC_S_NO_ENDPOINT_FOUND As Integer = 1708
'    Friend Const RPC_S_INVALID_TIMEOUT As Integer = 1709
'    Friend Const RPC_S_OBJECT_NOT_FOUND As Integer = 1710
'    Friend Const RPC_S_ALREADY_REGISTERED As Integer = 1711
'    Friend Const RPC_S_TYPE_ALREADY_REGISTERED As Integer = 1712
'    Friend Const RPC_S_ALREADY_LISTENING As Integer = 1713
'    Friend Const RPC_S_NO_PROTSEQS_REGISTERED As Integer = 1714
'    Friend Const RPC_S_NOT_LISTENING As Integer = 1715
'    Friend Const RPC_S_UNKNOWN_MGR_TYPE As Integer = 1716
'    Friend Const RPC_S_UNKNOWN_IF As Integer = 1717
'    Friend Const RPC_S_NO_BINDINGS As Integer = 1718
'    Friend Const RPC_S_NO_PROTSEQS As Integer = 1719
'    Friend Const RPC_S_CANT_CREATE_ENDPOINT As Integer = 1720
'    Friend Const RPC_S_OUT_OF_RESOURCES As Integer = 1721
'    Friend Const RPC_S_SERVER_UNAVAILABLE As Integer = 1722
'    Friend Const RPC_S_SERVER_TOO_BUSY As Integer = 1723
'    Friend Const RPC_S_INVALID_NETWORK_OPTIONS As Integer = 1724
'    Friend Const RPC_S_NO_CALL_ACTIVE As Integer = 1725
'    Friend Const RPC_S_CALL_FAILED As Integer = 1726
'    Friend Const RPC_S_CALL_FAILED_DNE As Integer = 1727
'    Friend Const RPC_S_PROTOCOL_ERROR As Integer = 1728
'    Friend Const RPC_S_UNSUPPORTED_TRANS_SYN As Integer = 1730
'    Friend Const RPC_S_UNSUPPORTED_TYPE As Integer = 1732
'    Friend Const RPC_S_INVALID_TAG As Integer = 1733
'    Friend Const RPC_S_INVALID_BOUND As Integer = 1734
'    Friend Const RPC_S_NO_ENTRY_NAME As Integer = 1735
'    Friend Const RPC_S_INVALID_NAME_SYNTAX As Integer = 1736
'    Friend Const RPC_S_UNSUPPORTED_NAME_SYNTAX As Integer = 1737
'    Friend Const RPC_S_UUID_NO_ADDRESS As Integer = 1739
'    Friend Const RPC_S_DUPLICATE_ENDPOINT As Integer = 1740
'    Friend Const RPC_S_UNKNOWN_AUTHN_TYPE As Integer = 1741
'    Friend Const RPC_S_MAX_CALLS_TOO_SMALL As Integer = 1742
'    Friend Const RPC_S_STRING_TOO_LONG As Integer = 1743
'    Friend Const RPC_S_PROTSEQ_NOT_FOUND As Integer = 1744
'    Friend Const RPC_S_PROCNUM_OUT_OF_RANGE As Integer = 1745
'    Friend Const RPC_S_BINDING_HAS_NO_AUTH As Integer = 1746
'    Friend Const RPC_S_UNKNOWN_AUTHN_SERVICE As Integer = 1747
'    Friend Const RPC_S_UNKNOWN_AUTHN_LEVEL As Integer = 1748
'    Friend Const RPC_S_INVALID_AUTH_IDENTITY As Integer = 1749
'    Friend Const RPC_S_UNKNOWN_AUTHZ_SERVICE As Integer = 1750
'    Friend Const RPC_S_NOTHING_TO_EXPORT As Integer = 1754
'    Friend Const RPC_S_INCOMPLETE_NAME As Integer = 1755
'    Friend Const RPC_S_INVALID_VERS_OPTION As Integer = 1756
'    Friend Const RPC_S_NO_MORE_MEMBERS As Integer = 1757
'    Friend Const RPC_S_NOT_ALL_OBJS_UNEXPORTED As Integer = 1758
'    Friend Const RPC_S_INTERFACE_NOT_FOUND As Integer = 1759
'    Friend Const RPC_S_ENTRY_ALREADY_EXISTS As Integer = 1760
'    Friend Const RPC_S_ENTRY_NOT_FOUND As Integer = 1761
'    Friend Const RPC_S_NAME_SERVICE_UNAVAILABLE As Integer = 1762
'    Friend Const RPC_S_INVALID_NAF_ID As Integer = 1763
'    Friend Const RPC_S_CANNOT_SUPPORT As Integer = 1764
'    Friend Const RPC_S_NO_CONTEXT_AVAILABLE As Integer = 1765
'    Friend Const RPC_S_INTERNAL_ERROR As Integer = 1766
'    Friend Const RPC_S_ZERO_DIVIDE As Integer = 1767
'    Friend Const RPC_S_ADDRESS_ERROR As Integer = 1768
'    Friend Const RPC_S_FP_DIV_ZERO As Integer = 1769
'    Friend Const RPC_S_FP_UNDERFLOW As Integer = 1770
'    Friend Const RPC_S_FP_OVERFLOW As Integer = 1771
'    Friend Const RPC_X_NO_MORE_ENTRIES As Integer = 1772
'    Friend Const RPC_X_SS_CHAR_TRANS_OPEN_FAIL As Integer = 1773
'    Friend Const RPC_X_SS_CHAR_TRANS_SHORT_FILE As Integer = 1774
'    Friend Const RPC_X_SS_IN_NULL_CONTEXT As Integer = 1775
'    Friend Const RPC_X_SS_CONTEXT_DAMAGED As Integer = 1777
'    Friend Const RPC_X_SS_HANDLES_MISMATCH As Integer = 1778
'    Friend Const RPC_X_SS_CANNOT_GET_CALL_HANDLE As Integer = 1779
'    Friend Const RPC_X_NULL_REF_POINTER As Integer = 1780
'    Friend Const RPC_X_ENUM_VALUE_OUT_OF_RANGE As Integer = 1781
'    Friend Const RPC_X_BYTE_COUNT_TOO_SMALL As Integer = 1782
'    Friend Const RPC_X_BAD_STUB_DATA As Integer = 1783
'    Friend Const RPC_S_CALL_IN_PROGRESS As Integer = 1791
'    Friend Const RPC_S_NO_MORE_BINDINGS As Integer = 1806
'    Friend Const RPC_S_NO_INTERFACES As Integer = 1817
'    Friend Const RPC_S_CALL_CANCELLED As Integer = 1818
'    Friend Const RPC_S_BINDING_INCOMPLETE As Integer = 1819
'    Friend Const RPC_S_COMM_FAILURE As Integer = 1820
'    Friend Const RPC_S_UNSUPPORTED_AUTHN_LEVEL As Integer = 1821
'    Friend Const RPC_S_NO_PRINC_NAME As Integer = 1822
'    Friend Const RPC_S_NOT_RPC_ERROR As Integer = 1823
'    Friend Const RPC_S_UUID_LOCAL_ONLY As Integer = 1824
'    Friend Const RPC_S_SEC_PKG_ERROR As Integer = 1825
'    Friend Const RPC_S_NOT_CANCELLED As Integer = 1826
'    Friend Const RPC_X_INVALID_ES_ACTION As Integer = 1827
'    Friend Const RPC_X_WRONG_ES_VERSION As Integer = 1828
'    Friend Const RPC_X_WRONG_STUB_VERSION As Integer = 1829
'    Friend Const RPC_X_INVALID_PIPE_OBJECT As Integer = 1830
'    Friend Const RPC_X_INVALID_PIPE_OPERATION As Integer = 1831
'    Friend Const RPC_X_WRONG_PIPE_VERSION As Integer = 1832
'    Friend Const RPC_S_GROUP_MEMBER_NOT_FOUND As Integer = 1898
'    Friend Const RPC_S_INVALID_OBJECT As Integer = 1900
'    Friend Const RPC_S_SEND_INCOMPLETE As Integer = 1913
'    Friend Const REGDB_E_FIRST As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_S_SEND_INCOMPLETE = 1913,
'    '        REGDB_E_FIRST = unchecked((int)0x80040150),
'    '-------------------------^--- GenCode(token): unexpected token type
'    Friend Const REGDB_E_LAST As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        REGDB_E_FIRST = unchecked((int)0x80040150),
'    '        REGDB_E_LAST = unchecked((int)0x8004015F),
'    '------------------------^--- GenCode(token): unexpected token type
'    Friend Const REGDB_S_FIRST As Integer = &H40150
'    Friend Const REGDB_S_LAST As Integer = &H4015F
'    Friend Const REGDB_E_READREGDB As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        REGDB_S_LAST = 0x0004015F,
'    '        REGDB_E_READREGDB = unchecked((int)0x80040150),
'    '-----------------------------^--- GenCode(token): unexpected token type
'    Friend Const REGDB_E_WRITEREGDB As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        REGDB_E_READREGDB = unchecked((int)0x80040150),
'    '        REGDB_E_WRITEREGDB = unchecked((int)0x80040151),
'    '------------------------------^--- GenCode(token): unexpected token type
'    Friend Const REGDB_E_KEYMISSING As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        REGDB_E_WRITEREGDB = unchecked((int)0x80040151),
'    '        REGDB_E_KEYMISSING = unchecked((int)0x80040152),
'    '------------------------------^--- GenCode(token): unexpected token type
'    Friend Const REGDB_E_INVALIDVALUE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        REGDB_E_KEYMISSING = unchecked((int)0x80040152),
'    '        REGDB_E_INVALIDVALUE = unchecked((int)0x80040153),
'    '--------------------------------^--- GenCode(token): unexpected token type
'    Friend Const REGDB_E_CLASSNOTREG As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        REGDB_E_INVALIDVALUE = unchecked((int)0x80040153),
'    '        REGDB_E_CLASSNOTREG = unchecked((int)0x80040154),
'    '-------------------------------^--- GenCode(token): unexpected token type
'    Friend Const REGDB_E_IIDNOTREG As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        REGDB_E_CLASSNOTREG = unchecked((int)0x80040154),
'    '        REGDB_E_IIDNOTREG = unchecked((int)0x80040155),
'    '-----------------------------^--- GenCode(token): unexpected token type
'    Friend Const RPC_E_CALL_REJECTED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        REGDB_E_IIDNOTREG = unchecked((int)0x80040155),
'    '        RPC_E_CALL_REJECTED = unchecked((int)0x80010001),
'    '-------------------------------^--- GenCode(token): unexpected token type
'    Friend Const RPC_E_CALL_CANCELED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_CALL_REJECTED = unchecked((int)0x80010001),
'    '        RPC_E_CALL_CANCELED = unchecked((int)0x80010002),
'    '-------------------------------^--- GenCode(token): unexpected token type
'    Friend Const RPC_E_CANTPOST_INSENDCALL As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_CALL_CANCELED = unchecked((int)0x80010002),
'    '        RPC_E_CANTPOST_INSENDCALL = unchecked((int)0x80010003),
'    '-------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const RPC_E_CANTCALLOUT_INASYNCCALL As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_CANTPOST_INSENDCALL = unchecked((int)0x80010003),
'    '        RPC_E_CANTCALLOUT_INASYNCCALL = unchecked((int)0x80010004),
'    '-----------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const RPC_E_CONNECTION_TERMINATED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_CANTCALLOUT_INASYNCCALL = unchecked((int)0x80010004),
'    '        RPC_E_CONNECTION_TERMINATED = unchecked((int)0x80010006),
'    '---------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const RPC_E_SERVER_DIED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_CONNECTION_TERMINATED = unchecked((int)0x80010006),
'    '        RPC_E_SERVER_DIED = unchecked((int)0x80010007),
'    '-----------------------------^--- GenCode(token): unexpected token type
'    Friend Const RPC_E_CLIENT_DIED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_SERVER_DIED = unchecked((int)0x80010007),
'    '        RPC_E_CLIENT_DIED = unchecked((int)0x80010008),
'    '-----------------------------^--- GenCode(token): unexpected token type
'    Friend Const RPC_E_INVALID_DATAPACKET As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_CLIENT_DIED = unchecked((int)0x80010008),
'    '        RPC_E_INVALID_DATAPACKET = unchecked((int)0x80010009),
'    '------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const RPC_E_CANTTRANSMIT_CALL As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_INVALID_DATAPACKET = unchecked((int)0x80010009),
'    '        RPC_E_CANTTRANSMIT_CALL = unchecked((int)0x8001000A),
'    '-----------------------------------^--- GenCode(token): unexpected token type
'    Friend Const RPC_E_CLIENT_CANTMARSHAL_DATA As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_CANTTRANSMIT_CALL = unchecked((int)0x8001000A),
'    '        RPC_E_CLIENT_CANTMARSHAL_DATA = unchecked((int)0x8001000B),
'    '-----------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const RPC_E_CLIENT_CANTUNMARSHAL_DATA As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_CLIENT_CANTMARSHAL_DATA = unchecked((int)0x8001000B),
'    '        RPC_E_CLIENT_CANTUNMARSHAL_DATA = unchecked((int)0x8001000C),
'    '-------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const RPC_E_SERVER_CANTMARSHAL_DATA As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_CLIENT_CANTUNMARSHAL_DATA = unchecked((int)0x8001000C),
'    '        RPC_E_SERVER_CANTMARSHAL_DATA = unchecked((int)0x8001000D),
'    '-----------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const RPC_E_SERVER_CANTUNMARSHAL_DATA As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_SERVER_CANTMARSHAL_DATA = unchecked((int)0x8001000D),
'    '        RPC_E_SERVER_CANTUNMARSHAL_DATA = unchecked((int)0x8001000E),
'    '-------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const RPC_E_INVALID_DATA As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_SERVER_CANTUNMARSHAL_DATA = unchecked((int)0x8001000E),
'    '        RPC_E_INVALID_DATA = unchecked((int)0x8001000F),
'    '------------------------------^--- GenCode(token): unexpected token type
'    Friend Const RPC_E_INVALID_PARAMETER As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_INVALID_DATA = unchecked((int)0x8001000F),
'    '        RPC_E_INVALID_PARAMETER = unchecked((int)0x80010010),
'    '-----------------------------------^--- GenCode(token): unexpected token type
'    Friend Const RPC_E_CANTCALLOUT_AGAIN As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_INVALID_PARAMETER = unchecked((int)0x80010010),
'    '        RPC_E_CANTCALLOUT_AGAIN = unchecked((int)0x80010011),
'    '-----------------------------------^--- GenCode(token): unexpected token type
'    Friend Const RPC_E_SERVER_DIED_DNE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_CANTCALLOUT_AGAIN = unchecked((int)0x80010011),
'    '        RPC_E_SERVER_DIED_DNE = unchecked((int)0x80010012),
'    '---------------------------------^--- GenCode(token): unexpected token type
'    Friend Const RPC_E_SYS_CALL_FAILED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_SERVER_DIED_DNE = unchecked((int)0x80010012),
'    '        RPC_E_SYS_CALL_FAILED = unchecked((int)0x80010100),
'    '---------------------------------^--- GenCode(token): unexpected token type
'    Friend Const RPC_E_OUT_OF_RESOURCES As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_SYS_CALL_FAILED = unchecked((int)0x80010100),
'    '        RPC_E_OUT_OF_RESOURCES = unchecked((int)0x80010101),
'    '----------------------------------^--- GenCode(token): unexpected token type
'    Friend Const RPC_E_ATTEMPTED_MULTITHREAD As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_OUT_OF_RESOURCES = unchecked((int)0x80010101),
'    '        RPC_E_ATTEMPTED_MULTITHREAD = unchecked((int)0x80010102),
'    '---------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const RPC_E_NOT_REGISTERED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_ATTEMPTED_MULTITHREAD = unchecked((int)0x80010102),
'    '        RPC_E_NOT_REGISTERED = unchecked((int)0x80010103),
'    '--------------------------------^--- GenCode(token): unexpected token type
'    Friend Const RPC_E_FAULT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_NOT_REGISTERED = unchecked((int)0x80010103),
'    '        RPC_E_FAULT = unchecked((int)0x80010104),
'    '-----------------------^--- GenCode(token): unexpected token type
'    Friend Const RPC_E_SERVERFAULT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_FAULT = unchecked((int)0x80010104),
'    '        RPC_E_SERVERFAULT = unchecked((int)0x80010105),
'    '-----------------------------^--- GenCode(token): unexpected token type
'    Friend Const RPC_E_CHANGED_MODE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_SERVERFAULT = unchecked((int)0x80010105),
'    '        RPC_E_CHANGED_MODE = unchecked((int)0x80010106),
'    '------------------------------^--- GenCode(token): unexpected token type
'    Friend Const RPC_E_INVALIDMETHOD As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_CHANGED_MODE = unchecked((int)0x80010106),
'    '        RPC_E_INVALIDMETHOD = unchecked((int)0x80010107),
'    '-------------------------------^--- GenCode(token): unexpected token type
'    Friend Const RPC_E_DISCONNECTED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_INVALIDMETHOD = unchecked((int)0x80010107),
'    '        RPC_E_DISCONNECTED = unchecked((int)0x80010108),
'    '------------------------------^--- GenCode(token): unexpected token type
'    Friend Const RPC_E_RETRY As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_DISCONNECTED = unchecked((int)0x80010108),
'    '        RPC_E_RETRY = unchecked((int)0x80010109),
'    '-----------------------^--- GenCode(token): unexpected token type
'    Friend Const RPC_E_SERVERCALL_RETRYLATER As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_RETRY = unchecked((int)0x80010109),
'    '        RPC_E_SERVERCALL_RETRYLATER = unchecked((int)0x8001010A),
'    '---------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const RPC_E_SERVERCALL_REJECTED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_SERVERCALL_RETRYLATER = unchecked((int)0x8001010A),
'    '        RPC_E_SERVERCALL_REJECTED = unchecked((int)0x8001010B),
'    '-------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const RPC_E_INVALID_CALLDATA As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_SERVERCALL_REJECTED = unchecked((int)0x8001010B),
'    '        RPC_E_INVALID_CALLDATA = unchecked((int)0x8001010C),
'    '----------------------------------^--- GenCode(token): unexpected token type
'    Friend Const RPC_E_CANTCALLOUT_ININPUTSYNCCALL As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_INVALID_CALLDATA = unchecked((int)0x8001010C),
'    '        RPC_E_CANTCALLOUT_ININPUTSYNCCALL = unchecked((int)0x8001010D),
'    '---------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const RPC_E_WRONG_THREAD As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_CANTCALLOUT_ININPUTSYNCCALL = unchecked((int)0x8001010D),
'    '        RPC_E_WRONG_THREAD = unchecked((int)0x8001010E),
'    '------------------------------^--- GenCode(token): unexpected token type
'    Friend Const RPC_E_THREAD_NOT_INIT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_WRONG_THREAD = unchecked((int)0x8001010E),
'    '        RPC_E_THREAD_NOT_INIT = unchecked((int)0x8001010F),
'    '---------------------------------^--- GenCode(token): unexpected token type
'    Friend Const RPC_E_VERSION_MISMATCH As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_THREAD_NOT_INIT = unchecked((int)0x8001010F),
'    '        RPC_E_VERSION_MISMATCH = unchecked((int)0x80010110),
'    '----------------------------------^--- GenCode(token): unexpected token type
'    Friend Const RPC_E_INVALID_HEADER As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_VERSION_MISMATCH = unchecked((int)0x80010110),
'    '        RPC_E_INVALID_HEADER = unchecked((int)0x80010111),
'    '--------------------------------^--- GenCode(token): unexpected token type
'    Friend Const RPC_E_INVALID_EXTENSION As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_INVALID_HEADER = unchecked((int)0x80010111),
'    '        RPC_E_INVALID_EXTENSION = unchecked((int)0x80010112),
'    '-----------------------------------^--- GenCode(token): unexpected token type
'    Friend Const RPC_E_INVALID_IPID As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_INVALID_EXTENSION = unchecked((int)0x80010112),
'    '        RPC_E_INVALID_IPID = unchecked((int)0x80010113),
'    '------------------------------^--- GenCode(token): unexpected token type
'    Friend Const RPC_E_INVALID_OBJECT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_INVALID_IPID = unchecked((int)0x80010113),
'    '        RPC_E_INVALID_OBJECT = unchecked((int)0x80010114),
'    '--------------------------------^--- GenCode(token): unexpected token type
'    Friend Const RPC_S_CALLPENDING As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_INVALID_OBJECT = unchecked((int)0x80010114),
'    '        RPC_S_CALLPENDING = unchecked((int)0x80010115),
'    '-----------------------------^--- GenCode(token): unexpected token type
'    Friend Const RPC_S_WAITONTIMER As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_S_CALLPENDING = unchecked((int)0x80010115),
'    '        RPC_S_WAITONTIMER = unchecked((int)0x80010116),
'    '-----------------------------^--- GenCode(token): unexpected token type
'    Friend Const RPC_E_CALL_COMPLETE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_S_WAITONTIMER = unchecked((int)0x80010116),
'    '        RPC_E_CALL_COMPLETE = unchecked((int)0x80010117),
'    '-------------------------------^--- GenCode(token): unexpected token type
'    Friend Const RPC_E_UNSECURE_CALL As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_CALL_COMPLETE = unchecked((int)0x80010117),
'    '        RPC_E_UNSECURE_CALL = unchecked((int)0x80010118),
'    '-------------------------------^--- GenCode(token): unexpected token type
'    Friend Const RPC_E_TOO_LATE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_UNSECURE_CALL = unchecked((int)0x80010118),
'    '        RPC_E_TOO_LATE = unchecked((int)0x80010119),
'    '--------------------------^--- GenCode(token): unexpected token type
'    Friend Const RPC_E_NO_GOOD_SECURITY_PACKAGES As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_TOO_LATE = unchecked((int)0x80010119),
'    '        RPC_E_NO_GOOD_SECURITY_PACKAGES = unchecked((int)0x8001011A),
'    '-------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const RPC_E_ACCESS_DENIED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_NO_GOOD_SECURITY_PACKAGES = unchecked((int)0x8001011A),
'    '        RPC_E_ACCESS_DENIED = unchecked((int)0x8001011B),
'    '-------------------------------^--- GenCode(token): unexpected token type
'    Friend Const RPC_E_REMOTE_DISABLED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_ACCESS_DENIED = unchecked((int)0x8001011B),
'    '        RPC_E_REMOTE_DISABLED = unchecked((int)0x8001011C),
'    '---------------------------------^--- GenCode(token): unexpected token type
'    Friend Const RPC_E_INVALID_OBJREF As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_REMOTE_DISABLED = unchecked((int)0x8001011C),
'    '        RPC_E_INVALID_OBJREF = unchecked((int)0x8001011D),
'    '--------------------------------^--- GenCode(token): unexpected token type
'    Friend Const RPC_E_UNEXPECTED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RPC_E_INVALID_OBJREF = unchecked((int)0x8001011D),
'    '        RPC_E_UNEXPECTED = unchecked((int)0x8001FFFF),
'    '----------------------------^--- GenCode(token): unexpected token type
'    Friend Const R2_BLACK As Integer = 1
'    Friend Const R2_NOTMERGEPEN As Integer = 2
'    Friend Const R2_MASKNOTPEN As Integer = 3
'    Friend Const R2_NOTCOPYPEN As Integer = 4
'    Friend Const R2_MASKPENNOT As Integer = 5
'    Friend Const R2_NOT As Integer = 6
'    Friend Const R2_XORPEN As Integer = 7
'    Friend Const R2_NOTMASKPEN As Integer = 8
'    Friend Const R2_MASKPEN As Integer = 9
'    Friend Const R2_NOTXORPEN As Integer = 10
'    Friend Const R2_NOP As Integer = 11
'    Friend Const R2_MERGENOTPEN As Integer = 12
'    Friend Const R2_COPYPEN As Integer = 13
'    Friend Const R2_MERGEPENNOT As Integer = 14
'    Friend Const R2_MERGEPEN As Integer = 15
'    Friend Const R2_WHITE As Integer = 16
'    Friend Const R2_LAST As Integer = 16
'    Friend Const RGN_ERROR As Integer = 0
'    Friend Const RGN_AND As Integer = 1
'    Friend Const RGN_OR As Integer = 2
'    Friend Const RGN_XOR As Integer = 3
'    Friend Const RGN_DIFF As Integer = 4
'    Friend Const RGN_COPY As Integer = 5
'    Friend Const RGN_MIN As Integer = 1
'    Friend Const RGN_MAX As Integer = 5
'    Friend Const RESTORE_CTM As Integer = 4100
'    Friend Const RUSSIAN_CHARSET As Integer = 204
'    Friend Const RASTER_FONTTYPE As Integer = &H1
'    Friend Const RELATIVE As Integer = 2
'    Friend Const RASTERCAPS As Integer = 38
'    Friend Const RC_BITBLT As Integer = 1
'    Friend Const RC_BANDING As Integer = 2
'    Friend Const RC_SCALING As Integer = 4
'    Friend Const RC_BITMAP64 As Integer = 8
'    Friend Const RC_GDI20_OUTPUT As Integer = &H10
'    Friend Const RC_GDI20_STATE As Integer = &H20
'    Friend Const RC_SAVEBITMAP As Integer = &H40
'    Friend Const RC_DI_BITMAP As Integer = &H80
'    Friend Const RC_PALETTE As Integer = &H100
'    Friend Const RC_DIBTODEV As Integer = &H200
'    Friend Const RC_BIGFONT As Integer = &H400
'    Friend Const RC_STRETCHBLT As Integer = &H800
'    Friend Const RC_FLOODFILL As Integer = &H1000
'    Friend Const RC_STRETCHDIB As Integer = &H2000
'    Friend Const RC_OP_DX_OUTPUT As Integer = &H4000
'    Friend Const RC_DEVBITS As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RC_OP_DX_OUTPUT = 0x4000,
'    '        RC_DEVBITS = unchecked((int)0x8000),
'    '----------------------^--- GenCode(token): unexpected token type
'    Friend Const RDH_RECTANGLES As Integer = 1
'    Friend Const RESOURCE_CONNECTED As Integer = &H1
'    Friend Const RESOURCE_GLOBALNET As Integer = &H2
'    Friend Const RESOURCE_REMEMBERED As Integer = &H3
'    Friend Const RESOURCE_RECENT As Integer = &H4
'    Friend Const RESOURCE_CONTEXT As Integer = &H5
'    Friend Const RESOURCETYPE_ANY As Integer = &H0
'    Friend Const RESOURCETYPE_DISK As Integer = &H1
'    Friend Const RESOURCETYPE_PRINT As Integer = &H2
'    Friend Const RESOURCETYPE_RESERVED As Integer = &H8
'    Friend Const RESOURCETYPE_UNKNOWN As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RESOURCETYPE_RESERVED = 0x00000008,
'    '        RESOURCETYPE_UNKNOWN = unchecked((int)0xFFFFFFFF),
'    '--------------------------------^--- GenCode(token): unexpected token type
'    Friend Const RESOURCEUSAGE_CONNECTABLE As Integer = &H1
'    Friend Const RESOURCEUSAGE_CONTAINER As Integer = &H2
'    Friend Const RESOURCEUSAGE_NOLOCALDEVICE As Integer = &H4
'    Friend Const RESOURCEUSAGE_SIBLING As Integer = &H8
'    Friend Const RESOURCEUSAGE_ATTACHED As Integer = &H10
'    Friend Const RESOURCEUSAGE_ALL As Integer = &H1 Or &H2 Or &H10
'    Friend Const RESOURCEUSAGE_RESERVED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RESOURCEUSAGE_ALL = (0x00000001|0x00000002|0x00000010),
'    '                            RESOURCEUSAGE_RESERVED = unchecked((int)0x80000000),
'    '------------------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const RESOURCEDISPLAYTYPE_GENERIC As Integer = &H0
'    Friend Const RESOURCEDISPLAYTYPE_DOMAIN As Integer = &H1
'    Friend Const RESOURCEDISPLAYTYPE_SERVER As Integer = &H2
'    Friend Const RESOURCEDISPLAYTYPE_SHARE As Integer = &H3
'    Friend Const RESOURCEDISPLAYTYPE_FILE As Integer = &H4
'    Friend Const RESOURCEDISPLAYTYPE_GROUP As Integer = &H5
'    Friend Const RESOURCEDISPLAYTYPE_NETWORK As Integer = &H6
'    Friend Const RESOURCEDISPLAYTYPE_ROOT As Integer = &H7
'    Friend Const RESOURCEDISPLAYTYPE_SHAREADMIN As Integer = &H8
'    Friend Const RESOURCEDISPLAYTYPE_DIRECTORY As Integer = &H9
'    Friend Const RESOURCEDISPLAYTYPE_TREE As Integer = &HA
'    Friend Const RESOURCEDISPLAYTYPE_NDSCONTAINER As Integer = &HB
'    Friend Const REMOTE_NAME_INFO_LEVEL As Integer = &H2
'    Friend Const RP_LOGON As Integer = &H1
'    Friend Const RP_INIFILE As Integer = &H2
'    Friend Const READ_CONTROL As Integer = &H20000
'    Friend Const RTL_CRITSECT_TYPE As Integer = 0
'    Friend Const RTL_RESOURCE_TYPE As Integer = 1
'    Friend Const REG_OPTION_RESERVED As Integer = &H0
'    Friend Const REG_OPTION_NON_VOLATILE As Integer = &H0
'    Friend Const REG_OPTION_VOLATILE As Integer = &H1
'    Friend Const REG_OPTION_CREATE_LINK As Integer = &H2
'    Friend Const REG_OPTION_BACKUP_RESTORE As Integer = &H4
'    Friend Const REG_OPTION_OPEN_LINK As Integer = &H8
'    Friend Const REG_CREATED_NEW_KEY As Integer = &H1
'    Friend Const REG_OPENED_EXISTING_KEY As Integer = &H2
'    Friend Const REG_WHOLE_HIVE_VOLATILE As Integer = &H1
'    Friend Const REG_REFRESH_HIVE As Integer = &H2
'    Friend Const REG_NO_LAZY_FLUSH As Integer = &H4
'    Friend Const REG_NOTIFY_CHANGE_NAME As Integer = &H1
'    Friend Const REG_NOTIFY_CHANGE_ATTRIBUTES As Integer = &H2
'    Friend Const REG_NOTIFY_CHANGE_LAST_SET As Integer = &H4
'    Friend Const REG_NOTIFY_CHANGE_SECURITY As Integer = &H8
'    Friend Const REG_NONE As Integer = 0
'    Friend Const REG_SZ As Integer = 1
'    Friend Const REG_EXPAND_SZ As Integer = 2
'    Friend Const REG_BINARY As Integer = 3
'    Friend Const REG_DWORD As Integer = 4
'    Friend Const REG_DWORD_LITTLE_ENDIAN As Integer = 4
'    Friend Const REG_DWORD_BIG_ENDIAN As Integer = 5
'    Friend Const REG_LINK As Integer = 6
'    Friend Const REG_MULTI_SZ As Integer = 7
'    Friend Const REG_RESOURCE_LIST As Integer = 8
'    Friend Const REG_FULL_RESOURCE_DESCRIPTOR As Integer = 9
'    Friend Const REG_RESOURCE_REQUIREMENTS_LIST As Integer = 10
'    Friend Const RT_CURSOR As Integer = 1
'    Friend Const RT_BITMAP As Integer = 2
'    Friend Const RT_ICON As Integer = 3
'    Friend Const RT_MENU As Integer = 4
'    Friend Const RT_DIALOG As Integer = 5
'    Friend Const RT_STRING As Integer = 6
'    Friend Const RT_FONTDIR As Integer = 7
'    Friend Const RT_FONT As Integer = 8
'    Friend Const RT_ACCELERATOR As Integer = 9
'    Friend Const RT_RCDATA As Integer = 10
'    Friend Const RT_MESSAGETABLE As Integer = 11
'    Friend Const RT_GROUP_CURSOR As Integer = 1 + 11
'    Friend Const RT_GROUP_ICON As Integer = 3 + 11
'    Friend Const RT_VERSION As Integer = 16
'    Friend Const RT_DLGINCLUDE As Integer = 17
'    Friend Const RT_PLUGPLAY As Integer = 19
'    Friend Const RT_VXD As Integer = 20
'    Friend Const RT_ANICURSOR As Integer = 21
'    Friend Const RT_ANIICON As Integer = 22
'    Friend Const RDW_INVALIDATE As Integer = &H1
'    Friend Const RDW_INTERNALPAINT As Integer = &H2
'    Friend Const RDW_ERASE As Integer = &H4
'    Friend Const RDW_VALIDATE As Integer = &H8
'    Friend Const RDW_NOINTERNALPAINT As Integer = &H10
'    Friend Const RDW_NOERASE As Integer = &H20
'    Friend Const RDW_NOCHILDREN As Integer = &H40
'    Friend Const RDW_ALLCHILDREN As Integer = &H80
'    Friend Const RDW_UPDATENOW As Integer = &H100
'    Friend Const RDW_ERASENOW As Integer = &H200
'    Friend Const RDW_FRAME As Integer = &H400
'    Friend Const RDW_NOFRAME As Integer = &H800
'    Friend Const RES_ICON As Integer = 1
'    Friend Const RES_CURSOR As Integer = 2
'    Friend Const ROTFLAGS_REGISTRATIONKEEPSALIVE As Integer = &H1
'    Friend Const ROTFLAGS_ALLOWANYCLIENT As Integer = &H2
'    Friend Const ROT_COMPARE_MAX As Integer = 2048
'    Friend Const RBN_FIRST As Integer = 0 - 831
'    Friend Const RBN_LAST As Integer = 0 - 859
'    Friend Const RBNM_ID As Integer = &H1
'    Friend Const RBNM_STYLE As Integer = &H2
'    Friend Const RBNM_LPARAM As Integer = &H4
'    Friend Const RBIM_IMAGELIST As Integer = &H1
'    Friend Const RBS_TOOLTIPS As Integer = &H100
'    Friend Const RBS_VARHEIGHT As Integer = &H200
'    Friend Const RBS_BANDBORDERS As Integer = &H400
'    Friend Const RBS_FIXEDORDER As Integer = &H800
'    Friend Const RBS_REGISTERDROP As Integer = &H1000
'    Friend Const RBS_AUTOSIZE As Integer = &H2000
'    Friend Const RBS_VERTICALGRIPPER As Integer = &H4000
'    Friend Const RBS_DBLCLKTOGGLE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        RBS_VERTICALGRIPPER = 0x4000,
'    '        RBS_DBLCLKTOGGLE = unchecked((int)0x8000),
'    '----------------------------^--- GenCode(token): unexpected token type
'    Friend Const RBBS_BREAK As Integer = &H1
'    Friend Const RBBS_FIXEDSIZE As Integer = &H2
'    Friend Const RBBS_CHILDEDGE As Integer = &H4
'    Friend Const RBBS_HIDDEN As Integer = &H8
'    Friend Const RBBS_NOVERT As Integer = &H10
'    Friend Const RBBS_FIXEDBMP As Integer = &H20
'    Friend Const RBBS_VARIABLEHEIGHT As Integer = &H40
'    Friend Const RBBS_GRIPPERALWAYS As Integer = &H80
'    Friend Const RBBS_NOGRIPPER As Integer = &H100
'    Friend Const RBBIM_STYLE As Integer = &H1
'    Friend Const RBBIM_COLORS As Integer = &H2
'    Friend Const RBBIM_TEXT As Integer = &H4
'    Friend Const RBBIM_IMAGE As Integer = &H8
'    Friend Const RBBIM_CHILD As Integer = &H10
'    Friend Const RBBIM_CHILDSIZE As Integer = &H20
'    Friend Const RBBIM_SIZE As Integer = &H40
'    Friend Const RBBIM_BACKGROUND As Integer = &H80
'    Friend Const RBBIM_ID As Integer = &H100
'    Friend Const RBBIM_IDEALSIZE As Integer = &H200
'    Friend Const RBBIM_LPARAM As Integer = &H400
'    Friend Const RBBIM_HEADERSIZE As Integer = &H800
'    Friend Const RB_INSERTBANDA As Integer = &H400 + 1
'    Friend Const RB_DELETEBAND As Integer = &H400 + 2
'    Friend Const RB_GETBARINFO As Integer = &H400 + 3
'    Friend Const RB_SETBARINFO As Integer = &H400 + 4
'    Friend Const RB_GETBANDINFO_OLD As Integer = &H400 + 5
'    Friend Const RB_SETBANDINFOA As Integer = &H400 + 6
'    Friend Const RB_SETPARENT As Integer = &H400 + 7
'    Friend Const RB_HITTEST As Integer = &H400 + 8
'    Friend Const RB_GETRECT As Integer = &H400 + 9
'    Friend Const RB_INSERTBANDW As Integer = &H400 + 10
'    Friend Const RB_SETBANDINFOW As Integer = &H400 + 11
'    Friend Const RB_GETBANDCOUNT As Integer = &H400 + 12
'    Friend Const RB_GETROWCOUNT As Integer = &H400 + 13
'    Friend Const RB_GETROWHEIGHT As Integer = &H400 + 14
'    Friend Const RB_IDTOINDEX As Integer = &H400 + 16
'    Friend Const RB_GETTOOLTIPS As Integer = &H400 + 17
'    Friend Const RB_SETTOOLTIPS As Integer = &H400 + 18
'    Friend Const RB_SETBKCOLOR As Integer = &H400 + 19
'    Friend Const RB_GETBKCOLOR As Integer = &H400 + 20
'    Friend Const RB_SETTEXTCOLOR As Integer = &H400 + 21
'    Friend Const RB_GETTEXTCOLOR As Integer = &H400 + 22
'    Friend Const RB_SIZETORECT As Integer = &H400 + 23
'    Friend Const RB_BEGINDRAG As Integer = &H400 + 24
'    Friend Const RB_ENDDRAG As Integer = &H400 + 25
'    Friend Const RB_DRAGMOVE As Integer = &H400 + 26
'    Friend Const RB_GETBARHEIGHT As Integer = &H400 + 27
'    Friend Const RB_GETBANDINFOW As Integer = &H400 + 28
'    Friend Const RB_GETBANDINFOA As Integer = &H400 + 29
'    Friend Const RB_MINIMIZEBAND As Integer = &H400 + 30
'    Friend Const RB_MAXIMIZEBAND As Integer = &H400 + 31
'    Friend Const RB_GETBANDBORDERS As Integer = &H400 + 34
'    Friend Const RB_SHOWBAND As Integer = &H400 + 35
'    Friend Const RB_SETPALETTE As Integer = &H400 + 37
'    Friend Const RB_GETPALETTE As Integer = &H400 + 38
'    Friend Const RB_MOVEBAND As Integer = &H400 + 39
'    Friend Const RB_SETCOLORSCHEME As Integer = win.CCM_SETCOLORSCHEME
'    Friend Const RB_GETCOLORSCHEME As Integer = win.CCM_GETCOLORSCHEME
'    Friend Const RB_GETDROPTARGET As Integer = win.CCM_GETDROPTARGET
'    Friend Const RB_SETUNICODEFORMAT As Integer = win.CCM_SETUNICODEFORMAT
'    Friend Const RB_GETUNICODEFORMAT As Integer = win.CCM_GETUNICODEFORMAT
'    Friend Const RBN_HEIGHTCHANGE As Integer = 0 - 831 - 0
'    Friend Const RBN_GETOBJECT As Integer = 0 - 831 - 1
'    Friend Const RBN_LAYOUTCHANGED As Integer = 0 - 831 - 2
'    Friend Const RBN_AUTOSIZE As Integer = 0 - 831 - 3
'    Friend Const RBN_BEGINDRAG As Integer = 0 - 831 - 4
'    Friend Const RBN_ENDDRAG As Integer = 0 - 831 - 5
'    Friend Const RBN_DELETINGBAND As Integer = 0 - 831 - 6
'    Friend Const RBN_DELETEDBAND As Integer = 0 - 831 - 7
'    Friend Const RBN_CHILDSIZE As Integer = 0 - 831 - 8
'    Friend Const RBHT_NOWHERE As Integer = &H1
'    Friend Const RBHT_CAPTION As Integer = &H2
'    Friend Const RBHT_CLIENT As Integer = &H3
'    Friend Const RBHT_GRABBER As Integer = &H4
'    Friend Const RPC_S_OK As Integer = 0
'    Friend Const RPC_S_INVALID_ARG As Integer = 87
'    Friend Const RPC_S_OUT_OF_MEMORY As Integer = 14
'    Friend Const RPC_S_OUT_OF_THREADS As Integer = 164
'    Friend Const RPC_S_INVALID_LEVEL As Integer = 87
'    Friend Const RPC_S_BUFFER_TOO_SMALL As Integer = 122
'    Friend Const RPC_S_INVALID_SECURITY_DESC As Integer = 1338
'    Friend Const RPC_S_ACCESS_DENIED As Integer = 5
'    Friend Const RPC_S_SERVER_OUT_OF_MEMORY As Integer = 1130
'    Friend Const RPC_X_NO_MEMORY As Integer = 14
'    Friend Const RPC_X_INVALID_BOUND As Integer = 1734
'    Friend Const RPC_X_INVALID_TAG As Integer = 1733
'    Friend Const RPC_X_ENUM_VALUE_TOO_LARGE As Integer = 1781
'    Friend Const RPC_X_SS_CONTEXT_MISMATCH As Integer = 6
'    Friend Const RPC_X_INVALID_BUFFER As Integer = 1784
    
    
    
    
    
    
    
'    Friend Const SIMULATED_FONTTYPE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '
'    '        public const int SIMULATED_FONTTYPE = unchecked((int)0x8000),
'    '-----------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const SCREEN_FONTTYPE As Integer = &H2000
'    Friend Const ST_CONNECTED As Integer = &H1
'    Friend Const ST_ADVISE As Integer = &H2
'    Friend Const ST_ISLOCAL As Integer = &H4
'    Friend Const ST_BLOCKED As Integer = &H8
'    Friend Const ST_CLIENT As Integer = &H10
'    Friend Const ST_TERMINATED As Integer = &H20
'    Friend Const ST_INLIST As Integer = &H40
'    Friend Const ST_BLOCKNEXT As Integer = &H80
'    Friend Const ST_ISSELF As Integer = &H100
'    Friend Const stc1 As Integer = &H440
'    Friend Const stc2 As Integer = &H441
'    Friend Const stc3 As Integer = &H442
'    Friend Const stc4 As Integer = &H443
'    Friend Const stc5 As Integer = &H444
'    Friend Const stc6 As Integer = &H445
'    Friend Const stc7 As Integer = &H446
'    Friend Const stc8 As Integer = &H447
'    Friend Const stc9 As Integer = &H448
'    Friend Const stc10 As Integer = &H449
'    Friend Const stc11 As Integer = &H44A
'    Friend Const stc12 As Integer = &H44B
'    Friend Const stc13 As Integer = &H44C
'    Friend Const stc14 As Integer = &H44D
'    Friend Const stc15 As Integer = &H44E
'    Friend Const stc16 As Integer = &H44F
'    Friend Const stc17 As Integer = &H450
'    Friend Const stc18 As Integer = &H451
'    Friend Const stc19 As Integer = &H452
'    Friend Const stc20 As Integer = &H453
'    Friend Const stc21 As Integer = &H454
'    Friend Const stc22 As Integer = &H455
'    Friend Const stc23 As Integer = &H456
'    Friend Const stc24 As Integer = &H457
'    Friend Const stc25 As Integer = &H458
'    Friend Const stc26 As Integer = &H459
'    Friend Const stc27 As Integer = &H45A
'    Friend Const stc28 As Integer = &H45B
'    Friend Const stc29 As Integer = &H45C
'    Friend Const stc30 As Integer = &H45D
'    Friend Const stc31 As Integer = &H45E
'    Friend Const stc32 As Integer = &H45F
'    Friend Const scr1 As Integer = &H490
'    Friend Const scr2 As Integer = &H491
'    Friend Const scr3 As Integer = &H492
'    Friend Const scr4 As Integer = &H493
'    Friend Const scr5 As Integer = &H494
'    Friend Const scr6 As Integer = &H495
'    Friend Const scr7 As Integer = &H496
'    Friend Const scr8 As Integer = &H497
'    Friend Const STYLE_DESCRIPTION_SIZE As Integer = 32
'    Friend Const SCS_CAP_COMPSTR As Integer = &H1
'    Friend Const SCS_CAP_MAKEREAD As Integer = &H2
'    Friend Const SELECT_CAP_CONVERSION As Integer = &H1
'    Friend Const SELECT_CAP_SENTENCE As Integer = &H2
'    Friend Const SCS_SETSTR As Integer = &H1 Or &H8
'    Friend Const SCS_CHANGEATTR As Integer = &H2 Or &H10
'    Friend Const SCS_CHANGECLAUSE As Integer = &H4 Or &H20
'    Friend Const SOFTKEYBOARD_TYPE_T1 As Integer = &H1
'    Friend Const SOFTKEYBOARD_TYPE_C1 As Integer = &H2
'    Friend Const SND_SYNC As Integer = &H0
'    Friend Const SND_ASYNC As Integer = &H1
'    Friend Const SND_NODEFAULT As Integer = &H2
'    Friend Const SND_MEMORY As Integer = &H4
'    Friend Const SND_LOOP As Integer = &H8
'    Friend Const SND_NOSTOP As Integer = &H10
'    Friend Const SND_NOWAIT As Integer = &H2000
'    Friend Const SND_ALIAS As Integer = &H10000
'    Friend Const SND_ALIAS_ID As Integer = &H110000
'    Friend Const SND_FILENAME As Integer = &H20000
'    Friend Const SND_RESOURCE As Integer = &H40004
'    Friend Const SND_PURGE As Integer = &H40
'    Friend Const SND_APPLICATION As Integer = &H80
'    Friend Const SND_ALIAS_START As Integer = 0
'    Friend Const SEEK_SET As Integer = 0
'    Friend Const SEEK_CUR As Integer = 1
'    Friend Const SEEK_END As Integer = 2
'    Friend Const SELECTDIB As Integer = 41
'    Friend Const SC_SCREENSAVE As Integer = &HF140
'    Friend Const SO_CONNDATA As Integer = &H7000
'    Friend Const SO_CONNOPT As Integer = &H7001
'    Friend Const SO_DISCDATA As Integer = &H7002
'    Friend Const SO_DISCOPT As Integer = &H7003
'    Friend Const SO_CONNDATALEN As Integer = &H7004
'    Friend Const SO_CONNOPTLEN As Integer = &H7005
'    Friend Const SO_DISCDATALEN As Integer = &H7006
'    Friend Const SO_DISCOPTLEN As Integer = &H7007
'    Friend Const SO_OPENTYPE As Integer = &H7008
'    Friend Const SO_SYNCHRONOUS_ALERT As Integer = &H10
'    Friend Const SO_SYNCHRONOUS_NONALERT As Integer = &H20
'    Friend Const SO_MAXDG As Integer = &H7009
'    Friend Const SO_MAXPATHDG As Integer = &H700A
'    Friend Const SO_UPDATE_ACCEPT_CONTEXT As Integer = &H700B
'    Friend Const SO_CONNECT_TIME As Integer = &H700C
'    Friend Const SESSION_ESTABLISHED As Integer = &H3
'    Friend Const SESSION_ABORTED As Integer = &H6
'    Friend Const STGM_DIRECT As Integer = &H0
'    Friend Const STGM_TRANSACTED As Integer = &H10000
'    Friend Const STGM_SIMPLE As Integer = &H8000000
'    Friend Const STGM_READ As Integer = &H0
'    Friend Const STGM_WRITE As Integer = &H1
'    Friend Const STGM_READWRITE As Integer = &H2
'    Friend Const STGM_SHARE_DENY_NONE As Integer = &H40
'    Friend Const STGM_SHARE_DENY_READ As Integer = &H30
'    Friend Const STGM_SHARE_DENY_WRITE As Integer = &H20
'    Friend Const STGM_SHARE_EXCLUSIVE As Integer = &H10
'    Friend Const STGM_PRIORITY As Integer = &H40000
'    Friend Const STGM_DELETEONRELEASE As Integer = &H4000000
'    Friend Const STGM_NOSCRATCH As Integer = &H100000
'    Friend Const STGM_CREATE As Integer = &H1000
'    Friend Const STGM_CONVERT As Integer = &H20000
'    Friend Const STGM_FAILIFTHERE As Integer = &H0
'    Friend Const STGM_NOSNAPSHOT As Integer = &H200000
'    Friend Const STGTY_REPEAT As Integer = &H100
'    Friend Const STG_TOEND As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        STGTY_REPEAT = 0x00000100,
'    '        STG_TOEND = unchecked((int)0xFFFFFFFF),
'    '---------------------^--- GenCode(token): unexpected token type
'    Friend Const STG_LAYOUT_SEQUENTIAL As Integer = &H0
'    Friend Const STG_LAYOUT_INTERLEAVED As Integer = &H1
'    Friend Const STDOLE_MAJORVERNUM As Integer = &H1
'    Friend Const STDOLE_MINORVERNUM As Integer = &H0
'    Friend Const STDOLE_LCID As Integer = &H0
'    Friend Const STDOLE2_MAJORVERNUM As Integer = &H2
'    Friend Const STDOLE2_MINORVERNUM As Integer = &H0
'    Friend Const STDOLE2_LCID As Integer = &H0
'    Friend Const SEC_WINNT_AUTH_IDENTITY_ANSI As Integer = &H1
'    Friend Const SEC_WINNT_AUTH_IDENTITY_UNICODE As Integer = &H2
'    Friend Const SE_ERR_FNF As Integer = 2
'    Friend Const SE_ERR_PNF As Integer = 3
'    Friend Const SE_ERR_ACCESSDENIED As Integer = 5
'    Friend Const SE_ERR_OOM As Integer = 8
'    Friend Const SE_ERR_DLLNOTFOUND As Integer = 32
'    Friend Const SE_ERR_SHARE As Integer = 26
'    Friend Const SE_ERR_ASSOCINCOMPLETE As Integer = 27
'    Friend Const SE_ERR_DDETIMEOUT As Integer = 28
'    Friend Const SE_ERR_DDEFAIL As Integer = 29
'    Friend Const SE_ERR_DDEBUSY As Integer = 30
'    Friend Const SE_ERR_NOASSOC As Integer = 31
'    Friend Const SEE_MASK_CLASSNAME As Integer = &H1
'    Friend Const SEE_MASK_CLASSKEY As Integer = &H3
'    Friend Const SEE_MASK_IDLIST As Integer = &H4
'    Friend Const SEE_MASK_INVOKEIDLIST As Integer = &HC
'    Friend Const SEE_MASK_ICON As Integer = &H10
'    Friend Const SEE_MASK_HOTKEY As Integer = &H20
'    Friend Const SEE_MASK_NOCLOSEPROCESS As Integer = &H40
'    Friend Const SEE_MASK_CONNECTNETDRV As Integer = &H80
'    Friend Const SEE_MASK_FLAG_DDEWAIT As Integer = &H100
'    Friend Const SEE_MASK_DOENVSUBST As Integer = &H200
'    Friend Const SEE_MASK_FLAG_NO_UI As Integer = &H400
'    Friend Const SEE_MASK_UNICODE As Integer = &H4000
'    Friend Const SEE_MASK_NO_CONSOLE As Integer = &H8000
'    Friend Const SEE_MASK_ASYNCOK As Integer = &H100000
'    Friend Const SHGFI_ICON As Long = &H100L
'    Friend Const SHGFI_DISPLAYNAME As Long = &H200L
'    Friend Const SHGFI_TYPENAME As Long = &H400L
'    Friend Const SHGFI_ATTRIBUTES As Long = &H800L
'    Friend Const SHGFI_ICONLOCATION As Long = &H1000L
'    Friend Const SHGFI_EXETYPE As Long = &H2000L
'    Friend Const SHGFI_SYSICONINDEX As Long = &H4000L
'    Friend Const SHGFI_LINKOVERLAY As Long = &H8000L
'    Friend Const SHGFI_SELECTED As Long = &H10000L
'    Friend Const SHGFI_LARGEICON As Long = &H0L
'    Friend Const SHGFI_SMALLICON As Long = &H1L
'    Friend Const SHGFI_OPENICON As Long = &H2L
'    Friend Const SHGFI_SHELLICONSIZE As Long = &H4L
'    Friend Const SHGFI_PIDL As Long = &H8L
'    Friend Const SHGFI_USEFILEATTRIBUTES As Long = &H10L
'    Friend Const SHGNLI_PIDL As Long = &H1L
'    Friend Const SHGNLI_PREFIXNAME As Long = &H2L
'    Friend Const SHGNLI_NOUNIQUE As Long = &H4L
'    Friend Const SECURITY_CONTEXT_TRACKING As Integer = &H40000
'    Friend Const SECURITY_EFFECTIVE_ONLY As Integer = &H80000
'    Friend Const SECURITY_SQOS_PRESENT As Integer = &H100000
'    Friend Const SECURITY_VALID_SQOS_FLAGS As Integer = &H1F0000
'    Friend Const SP_SERIALCOMM As Integer = &H1
'    Friend Const SP_PARITY As Integer = &H1
'    Friend Const SP_BAUD As Integer = &H2
'    Friend Const SP_DATABITS As Integer = &H4
'    Friend Const SP_STOPBITS As Integer = &H8
'    Friend Const SP_HANDSHAKING As Integer = &H10
'    Friend Const SP_PARITY_CHECK As Integer = &H20
'    Friend Const SP_RLSD As Integer = &H40
'    Friend Const STOPBITS_10 As Integer = &H1
'    Friend Const STOPBITS_15 As Integer = &H2
'    Friend Const STOPBITS_20 As Integer = &H4
'    Friend Const SPACEPARITY As Integer = 4
'    Friend Const SETXOFF As Integer = 1
'    Friend Const SETXON As Integer = 2
'    Friend Const SETRTS As Integer = 3
'    Friend Const SETDTR As Integer = 5
'    Friend Const SETBREAK As Integer = 8
'    Friend Const S_QUEUEEMPTY As Integer = 0
'    Friend Const S_THRESHOLD As Integer = 1
'    Friend Const S_ALLTHRESHOLD As Integer = 2
'    Friend Const S_NORMAL As Integer = 0
'    Friend Const S_LEGATO As Integer = 1
'    Friend Const S_STACCATO As Integer = 2
'    Friend Const S_PERIOD512 As Integer = 0
'    Friend Const S_PERIOD1024 As Integer = 1
'    Friend Const S_PERIOD2048 As Integer = 2
'    Friend Const S_PERIODVOICE As Integer = 3
'    Friend Const S_WHITE512 As Integer = 4
'    Friend Const S_WHITE1024 As Integer = 5
'    Friend Const S_WHITE2048 As Integer = 6
'    Friend Const S_WHITEVOICE As Integer = 7
'    Friend Const S_SERDVNA As Integer = - 1
'    Friend Const S_SEROFM As Integer = - 2
'    Friend Const S_SERMACT As Integer = - 3
'    Friend Const S_SERQFUL As Integer = - 4
'    Friend Const S_SERBDNT As Integer = - 5
'    Friend Const S_SERDLN As Integer = - 6
'    Friend Const S_SERDCC As Integer = - 7
'    Friend Const S_SERDTP As Integer = - 8
'    Friend Const S_SERDVL As Integer = - 9
'    Friend Const S_SERDMD As Integer = - 10
'    Friend Const S_SERDSH As Integer = - 11
'    Friend Const S_SERDPT As Integer = - 12
'    Friend Const S_SERDFQ As Integer = - 13
'    Friend Const S_SERDDR As Integer = - 14
'    Friend Const S_SERDSR As Integer = - 15
'    Friend Const S_SERDST As Integer = - 16
'    Friend Const SCS_32BIT_BINARY As Integer = 0
'    Friend Const SCS_DOS_BINARY As Integer = 1
'    Friend Const SCS_WOW_BINARY As Integer = 2
'    Friend Const SCS_PIF_BINARY As Integer = 3
'    Friend Const SCS_POSIX_BINARY As Integer = 4
'    Friend Const SCS_OS216_BINARY As Integer = 5
'    Friend Const SEM_FAILCRITICALERRORS As Integer = &H1
'    Friend Const SEM_NOGPFAULTERRORBOX As Integer = &H2
'    Friend Const SEM_NOALIGNMENTFAULTEXCEPT As Integer = &H4
'    Friend Const SEM_NOOPENFILEERRORBOX As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        SEM_NOALIGNMENTFAULTEXCEPT = 0x0004,
'    '        SEM_NOOPENFILEERRORBOX = unchecked((int)0x8000),
'    '----------------------------------^--- GenCode(token): unexpected token type
'    Friend Const SET_TAPE_MEDIA_INFORMATION As Integer = 0
'    Friend Const SET_TAPE_DRIVE_INFORMATION As Integer = 1
'    Friend Const STREAM_NORMAL_ATTRIBUTE As Integer = &H0
'    Friend Const STREAM_MODIFIED_WHEN_READ As Integer = &H1
'    Friend Const STREAM_CONTAINS_SECURITY As Integer = &H2
'    Friend Const STREAM_CONTAINS_PROPERTIES As Integer = &H4
'    Friend Const STARTF_USESHOWWINDOW As Integer = &H1
'    Friend Const STARTF_USESIZE As Integer = &H2
'    Friend Const STARTF_USEPOSITION As Integer = &H4
'    Friend Const STARTF_USECOUNTCHARS As Integer = &H8
'    Friend Const STARTF_USEFILLATTRIBUTE As Integer = &H10
'    Friend Const STARTF_RUNFULLSCREEN As Integer = &H20
'    Friend Const STARTF_FORCEONFEEDBACK As Integer = &H40
'    Friend Const STARTF_FORCEOFFFEEDBACK As Integer = &H80
'    Friend Const STARTF_USESTDHANDLES As Integer = &H100
'    Friend Const STARTF_USEHOTKEY As Integer = &H200
'    Friend Const SHUTDOWN_NORETRY As Integer = &H1
'    Friend Const SHIFT_PRESSED As Integer = &H10
'    Friend Const SCROLLLOCK_ON As Integer = &H40
'    Friend Const SIMPLEBLOB As Integer = &H1
'    Friend Const SEVERITY_SUCCESS As Integer = 0
'    Friend Const SEVERITY_ERROR As Integer = 1
'    Friend Const STG_E_INVALIDFUNCTION As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        SEVERITY_ERROR = 1,
'    '        STG_E_INVALIDFUNCTION = unchecked((int)0x80030001),
'    '---------------------------------^--- GenCode(token): unexpected token type
'    Friend Const STG_E_FILENOTFOUND As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        STG_E_INVALIDFUNCTION = unchecked((int)0x80030001),
'    '        STG_E_FILENOTFOUND = unchecked((int)0x80030002),
'    '------------------------------^--- GenCode(token): unexpected token type
'    Friend Const STG_E_PATHNOTFOUND As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        STG_E_FILENOTFOUND = unchecked((int)0x80030002),
'    '        STG_E_PATHNOTFOUND = unchecked((int)0x80030003),
'    '------------------------------^--- GenCode(token): unexpected token type
'    Friend Const STG_E_TOOMANYOPENFILES As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        STG_E_PATHNOTFOUND = unchecked((int)0x80030003),
'    '        STG_E_TOOMANYOPENFILES = unchecked((int)0x80030004),
'    '----------------------------------^--- GenCode(token): unexpected token type
'    Friend Const STG_E_ACCESSDENIED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        STG_E_TOOMANYOPENFILES = unchecked((int)0x80030004),
'    '        STG_E_ACCESSDENIED = unchecked((int)0x80030005),
'    '------------------------------^--- GenCode(token): unexpected token type
'    Friend Const STG_E_INVALIDHANDLE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        STG_E_ACCESSDENIED = unchecked((int)0x80030005),
'    '        STG_E_INVALIDHANDLE = unchecked((int)0x80030006),
'    '-------------------------------^--- GenCode(token): unexpected token type
'    Friend Const STG_E_INSUFFICIENTMEMORY As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        STG_E_INVALIDHANDLE = unchecked((int)0x80030006),
'    '        STG_E_INSUFFICIENTMEMORY = unchecked((int)0x80030008),
'    '------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const STG_E_INVALIDPOINTER As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        STG_E_INSUFFICIENTMEMORY = unchecked((int)0x80030008),
'    '        STG_E_INVALIDPOINTER = unchecked((int)0x80030009),
'    '--------------------------------^--- GenCode(token): unexpected token type
'    Friend Const STG_E_NOMOREFILES As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        STG_E_INVALIDPOINTER = unchecked((int)0x80030009),
'    '        STG_E_NOMOREFILES = unchecked((int)0x80030012),
'    '-----------------------------^--- GenCode(token): unexpected token type
'    Friend Const STG_E_DISKISWRITEPROTECTED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        STG_E_NOMOREFILES = unchecked((int)0x80030012),
'    '        STG_E_DISKISWRITEPROTECTED = unchecked((int)0x80030013),
'    '--------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const STG_E_SEEKERROR As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        STG_E_DISKISWRITEPROTECTED = unchecked((int)0x80030013),
'    '        STG_E_SEEKERROR = unchecked((int)0x80030019),
'    '---------------------------^--- GenCode(token): unexpected token type
'    Friend Const STG_E_WRITEFAULT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        STG_E_SEEKERROR = unchecked((int)0x80030019),
'    '        STG_E_WRITEFAULT = unchecked((int)0x8003001D),
'    '----------------------------^--- GenCode(token): unexpected token type
'    Friend Const STG_E_READFAULT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        STG_E_WRITEFAULT = unchecked((int)0x8003001D),
'    '        STG_E_READFAULT = unchecked((int)0x8003001E),
'    '---------------------------^--- GenCode(token): unexpected token type
'    Friend Const STG_E_SHAREVIOLATION As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        STG_E_READFAULT = unchecked((int)0x8003001E),
'    '        STG_E_SHAREVIOLATION = unchecked((int)0x80030020),
'    '--------------------------------^--- GenCode(token): unexpected token type
'    Friend Const STG_E_LOCKVIOLATION As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        STG_E_SHAREVIOLATION = unchecked((int)0x80030020),
'    '        STG_E_LOCKVIOLATION = unchecked((int)0x80030021),
'    '-------------------------------^--- GenCode(token): unexpected token type
'    Friend Const STG_E_FILEALREADYEXISTS As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        STG_E_LOCKVIOLATION = unchecked((int)0x80030021),
'    '        STG_E_FILEALREADYEXISTS = unchecked((int)0x80030050),
'    '-----------------------------------^--- GenCode(token): unexpected token type
'    Friend Const STG_E_INVALIDPARAMETER As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        STG_E_FILEALREADYEXISTS = unchecked((int)0x80030050),
'    '        STG_E_INVALIDPARAMETER = unchecked((int)0x80030057),
'    '----------------------------------^--- GenCode(token): unexpected token type
'    Friend Const STG_E_MEDIUMFULL As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        STG_E_INVALIDPARAMETER = unchecked((int)0x80030057),
'    '        STG_E_MEDIUMFULL = unchecked((int)0x80030070),
'    '----------------------------^--- GenCode(token): unexpected token type
'    Friend Const STG_E_PROPSETMISMATCHED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        STG_E_MEDIUMFULL = unchecked((int)0x80030070),
'    '        STG_E_PROPSETMISMATCHED = unchecked((int)0x800300F0),
'    '-----------------------------------^--- GenCode(token): unexpected token type
'    Friend Const STG_E_ABNORMALAPIEXIT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        STG_E_PROPSETMISMATCHED = unchecked((int)0x800300F0),
'    '        STG_E_ABNORMALAPIEXIT = unchecked((int)0x800300FA),
'    '---------------------------------^--- GenCode(token): unexpected token type
'    Friend Const STG_E_INVALIDHEADER As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        STG_E_ABNORMALAPIEXIT = unchecked((int)0x800300FA),
'    '        STG_E_INVALIDHEADER = unchecked((int)0x800300FB),
'    '-------------------------------^--- GenCode(token): unexpected token type
'    Friend Const STG_E_INVALIDNAME As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        STG_E_INVALIDHEADER = unchecked((int)0x800300FB),
'    '        STG_E_INVALIDNAME = unchecked((int)0x800300FC),
'    '-----------------------------^--- GenCode(token): unexpected token type
'    Friend Const STG_E_UNKNOWN As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        STG_E_INVALIDNAME = unchecked((int)0x800300FC),
'    '        STG_E_UNKNOWN = unchecked((int)0x800300FD),
'    '-------------------------^--- GenCode(token): unexpected token type
'    Friend Const STG_E_UNIMPLEMENTEDFUNCTION As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        STG_E_UNKNOWN = unchecked((int)0x800300FD),
'    '        STG_E_UNIMPLEMENTEDFUNCTION = unchecked((int)0x800300FE),
'    '---------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const STG_E_INVALIDFLAG As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        STG_E_UNIMPLEMENTEDFUNCTION = unchecked((int)0x800300FE),
'    '        STG_E_INVALIDFLAG = unchecked((int)0x800300FF),
'    '-----------------------------^--- GenCode(token): unexpected token type
'    Friend Const STG_E_INUSE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        STG_E_INVALIDFLAG = unchecked((int)0x800300FF),
'    '        STG_E_INUSE = unchecked((int)0x80030100),
'    '-----------------------^--- GenCode(token): unexpected token type
'    Friend Const STG_E_NOTCURRENT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        STG_E_INUSE = unchecked((int)0x80030100),
'    '        STG_E_NOTCURRENT = unchecked((int)0x80030101),
'    '----------------------------^--- GenCode(token): unexpected token type
'    Friend Const STG_E_REVERTED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        STG_E_NOTCURRENT = unchecked((int)0x80030101),
'    '        STG_E_REVERTED = unchecked((int)0x80030102),
'    '--------------------------^--- GenCode(token): unexpected token type
'    Friend Const STG_E_CANTSAVE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        STG_E_REVERTED = unchecked((int)0x80030102),
'    '        STG_E_CANTSAVE = unchecked((int)0x80030103),
'    '--------------------------^--- GenCode(token): unexpected token type
'    Friend Const STG_E_OLDFORMAT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        STG_E_CANTSAVE = unchecked((int)0x80030103),
'    '        STG_E_OLDFORMAT = unchecked((int)0x80030104),
'    '---------------------------^--- GenCode(token): unexpected token type
'    Friend Const STG_E_OLDDLL As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        STG_E_OLDFORMAT = unchecked((int)0x80030104),
'    '        STG_E_OLDDLL = unchecked((int)0x80030105),
'    '------------------------^--- GenCode(token): unexpected token type
'    Friend Const STG_E_SHAREREQUIRED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        STG_E_OLDDLL = unchecked((int)0x80030105),
'    '        STG_E_SHAREREQUIRED = unchecked((int)0x80030106),
'    '-------------------------------^--- GenCode(token): unexpected token type
'    Friend Const STG_E_NOTFILEBASEDSTORAGE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        STG_E_SHAREREQUIRED = unchecked((int)0x80030106),
'    '        STG_E_NOTFILEBASEDSTORAGE = unchecked((int)0x80030107),
'    '-------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const STG_E_EXTANTMARSHALLINGS As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        STG_E_NOTFILEBASEDSTORAGE = unchecked((int)0x80030107),
'    '        STG_E_EXTANTMARSHALLINGS = unchecked((int)0x80030108),
'    '------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const STG_E_DOCFILECORRUPT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        STG_E_EXTANTMARSHALLINGS = unchecked((int)0x80030108),
'    '        STG_E_DOCFILECORRUPT = unchecked((int)0x80030109),
'    '--------------------------------^--- GenCode(token): unexpected token type
'    Friend Const STG_E_BADBASEADDRESS As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        STG_E_DOCFILECORRUPT = unchecked((int)0x80030109),
'    '        STG_E_BADBASEADDRESS = unchecked((int)0x80030110),
'    '--------------------------------^--- GenCode(token): unexpected token type
'    Friend Const STG_E_INCOMPLETE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        STG_E_BADBASEADDRESS = unchecked((int)0x80030110),
'    '        STG_E_INCOMPLETE = unchecked((int)0x80030201),
'    '----------------------------^--- GenCode(token): unexpected token type
'    Friend Const STG_E_TERMINATED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        STG_E_INCOMPLETE = unchecked((int)0x80030201),
'    '        STG_E_TERMINATED = unchecked((int)0x80030202),
'    '----------------------------^--- GenCode(token): unexpected token type
'    Friend Const STG_S_CONVERTED As Integer = &H30200
'    Friend Const STG_S_BLOCK As Integer = &H30201
'    Friend Const STG_S_RETRYNOW As Integer = &H30202
'    Friend Const STG_S_MONITORING As Integer = &H30203
'    Friend Const SIMPLEREGION As Integer = 2
'    Friend Const STRETCH_ANDSCANS As Integer = 1
'    Friend Const STRETCH_ORSCANS As Integer = 2
'    Friend Const STRETCH_DELETESCANS As Integer = 3
'    Friend Const STRETCH_HALFTONE As Integer = 4
'    Friend Const SETCOLORTABLE As Integer = 4
'    Friend Const SETABORTPROC As Integer = 9
'    Friend Const STARTDOC As Integer = 10
'    Friend Const SETCOPYCOUNT As Integer = 17
'    Friend Const SELECTPAPERSOURCE As Integer = 18
'    Friend Const SETLINECAP As Integer = 21
'    Friend Const SETLINEJOIN As Integer = 22
'    Friend Const SETMITERLIMIT As Integer = 23
'    Friend Const SETDIBSCALING As Integer = 32
'    Friend Const SETKERNTRACK As Integer = 770
'    Friend Const SETALLJUSTVALUES As Integer = 771
'    Friend Const SETCHARSET As Integer = 772
'    Friend Const STRETCHBLT As Integer = 2048
'    Friend Const SAVE_CTM As Integer = 4101
'    Friend Const SET_ARC_DIRECTION As Integer = 4102
'    Friend Const SET_BACKGROUND_COLOR As Integer = 4103
'    Friend Const SET_POLY_MODE As Integer = 4104
'    Friend Const SET_SCREEN_ANGLE As Integer = 4105
'    Friend Const SET_SPREAD As Integer = 4106
'    Friend Const SET_CLIP_BOX As Integer = 4108
'    Friend Const SET_BOUNDS As Integer = 4109
'    Friend Const SET_MIRROR_MODE As Integer = 4110
'    Friend Const SP_NOTREPORTED As Integer = &H4000
'    Friend Const SP_ERROR As Integer = - 1
'    Friend Const SP_APPABORT As Integer = - 2
'    Friend Const SP_USERABORT As Integer = - 3
'    Friend Const SP_OUTOFDISK As Integer = - 4
'    Friend Const SP_OUTOFMEMORY As Integer = - 5
'    Friend Const SYMBOL_CHARSET As Integer = 2
'    Friend Const SHIFTJIS_CHARSET As Integer = 128
'    Friend Const SYSTEM_FONT As Integer = 13
'    Friend Const SYSTEM_FIXED_FONT As Integer = 16
'    Friend Const STOCK_LAST As Integer = 17
'    ' STOCK_LAST = 16;
'    Friend Const SIZEPALETTE As Integer = 104
'    Friend Const SCALINGFACTORX As Integer = 114
'    Friend Const SCALINGFACTORY As Integer = 115
'    Friend Const SYSPAL_ERROR As Integer = 0
'    Friend Const SYSPAL_STATIC As Integer = 1
'    Friend Const SYSPAL_NOSTATIC As Integer = 2
'    Friend Const SORT_STRINGSORT As Integer = &H1000
'    Friend Const SUBLANG_NEUTRAL As Integer = &H0
'    Friend Const SUBLANG_DEFAULT As Integer = &H1
'    Friend Const SUBLANG_SYS_DEFAULT As Integer = &H2
'    Friend Const SUBLANG_ARABIC_SAUDI_ARABIA As Integer = &H1
'    Friend Const SUBLANG_ARABIC_IRAQ As Integer = &H2
'    Friend Const SUBLANG_ARABIC_EGYPT As Integer = &H3
'    Friend Const SUBLANG_ARABIC_LIBYA As Integer = &H4
'    Friend Const SUBLANG_ARABIC_ALGERIA As Integer = &H5
'    Friend Const SUBLANG_ARABIC_MOROCCO As Integer = &H6
'    Friend Const SUBLANG_ARABIC_TUNISIA As Integer = &H7
'    Friend Const SUBLANG_ARABIC_OMAN As Integer = &H8
'    Friend Const SUBLANG_ARABIC_YEMEN As Integer = &H9
'    Friend Const SUBLANG_ARABIC_SYRIA As Integer = &HA
'    Friend Const SUBLANG_ARABIC_JORDAN As Integer = &HB
'    Friend Const SUBLANG_ARABIC_LEBANON As Integer = &HC
'    Friend Const SUBLANG_ARABIC_KUWAIT As Integer = &HD
'    Friend Const SUBLANG_ARABIC_UAE As Integer = &HE
'    Friend Const SUBLANG_ARABIC_BAHRAIN As Integer = &HF
'    Friend Const SUBLANG_ARABIC_QATAR As Integer = &H10
'    Friend Const SUBLANG_CHINESE_TRADITIONAL As Integer = &H1
'    Friend Const SUBLANG_CHINESE_SIMPLIFIED As Integer = &H2
'    Friend Const SUBLANG_CHINESE_HONGKONG As Integer = &H3
'    Friend Const SUBLANG_CHINESE_SINGAPORE As Integer = &H4
'    Friend Const SUBLANG_DUTCH As Integer = &H1
'    Friend Const SUBLANG_DUTCH_BELGIAN As Integer = &H2
'    Friend Const SUBLANG_ENGLISH_US As Integer = &H1
'    Friend Const SUBLANG_ENGLISH_UK As Integer = &H2
'    Friend Const SUBLANG_ENGLISH_AUS As Integer = &H3
'    Friend Const SUBLANG_ENGLISH_CAN As Integer = &H4
'    Friend Const SUBLANG_ENGLISH_NZ As Integer = &H5
'    Friend Const SUBLANG_ENGLISH_EIRE As Integer = &H6
'    Friend Const SUBLANG_ENGLISH_SOUTH_AFRICA As Integer = &H7
'    Friend Const SUBLANG_ENGLISH_JAMAICA As Integer = &H8
'    Friend Const SUBLANG_ENGLISH_CARIBBEAN As Integer = &H9
'    Friend Const SUBLANG_ENGLISH_BELIZE As Integer = &HA
'    Friend Const SUBLANG_ENGLISH_TRINIDAD As Integer = &HB
'    Friend Const SUBLANG_FRENCH As Integer = &H1
'    Friend Const SUBLANG_FRENCH_BELGIAN As Integer = &H2
'    Friend Const SUBLANG_FRENCH_CANADIAN As Integer = &H3
'    Friend Const SUBLANG_FRENCH_SWISS As Integer = &H4
'    Friend Const SUBLANG_FRENCH_LUXEMBOURG As Integer = &H5
'    Friend Const SUBLANG_GERMAN As Integer = &H1
'    Friend Const SUBLANG_GERMAN_SWISS As Integer = &H2
'    Friend Const SUBLANG_GERMAN_AUSTRIAN As Integer = &H3
'    Friend Const SUBLANG_GERMAN_LUXEMBOURG As Integer = &H4
'    Friend Const SUBLANG_GERMAN_LIECHTENSTEIN As Integer = &H5
'    Friend Const SUBLANG_ITALIAN As Integer = &H1
'    Friend Const SUBLANG_ITALIAN_SWISS As Integer = &H2
'    Friend Const SUBLANG_KOREAN As Integer = &H1
'    Friend Const SUBLANG_KOREAN_JOHAB As Integer = &H2
'    Friend Const SUBLANG_NORWEGIAN_BOKMAL As Integer = &H1
'    Friend Const SUBLANG_NORWEGIAN_NYNORSK As Integer = &H2
'    Friend Const SUBLANG_PORTUGUESE As Integer = &H2
'    Friend Const SUBLANG_PORTUGUESE_BRAZILIAN As Integer = &H1
'    Friend Const SUBLANG_SERBIAN_LATIN As Integer = &H2
'    Friend Const SUBLANG_SERBIAN_CYRILLIC As Integer = &H3
'    Friend Const SUBLANG_SPANISH As Integer = &H1
'    Friend Const SUBLANG_SPANISH_MEXICAN As Integer = &H2
'    Friend Const SUBLANG_SPANISH_MODERN As Integer = &H3
'    Friend Const SUBLANG_SPANISH_GUATEMALA As Integer = &H4
'    Friend Const SUBLANG_SPANISH_COSTA_RICA As Integer = &H5
'    Friend Const SUBLANG_SPANISH_PANAMA As Integer = &H6
'    Friend Const SUBLANG_SPANISH_DOMINICAN_REPUBLIC As Integer = &H7
'    Friend Const SUBLANG_SPANISH_VENEZUELA As Integer = &H8
'    Friend Const SUBLANG_SPANISH_COLOMBIA As Integer = &H9
'    Friend Const SUBLANG_SPANISH_PERU As Integer = &HA
'    Friend Const SUBLANG_SPANISH_ARGENTINA As Integer = &HB
'    Friend Const SUBLANG_SPANISH_ECUADOR As Integer = &HC
'    Friend Const SUBLANG_SPANISH_CHILE As Integer = &HD
'    Friend Const SUBLANG_SPANISH_URUGUAY As Integer = &HE
'    Friend Const SUBLANG_SPANISH_PARAGUAY As Integer = &HF
'    Friend Const SUBLANG_SPANISH_BOLIVIA As Integer = &H10
'    Friend Const SUBLANG_SPANISH_EL_SALVADOR As Integer = &H11
'    Friend Const SUBLANG_SPANISH_HONDURAS As Integer = &H12
'    Friend Const SUBLANG_SPANISH_NICARAGUA As Integer = &H13
'    Friend Const SUBLANG_SPANISH_PUERTO_RICO As Integer = &H14
'    Friend Const SUBLANG_SWEDISH As Integer = &H1
'    Friend Const SUBLANG_SWEDISH_FINLAND As Integer = &H2
'    Friend Const SORT_DEFAULT As Integer = &H0
'    Friend Const SORT_JAPANESE_XJIS As Integer = &H0
'    Friend Const SORT_JAPANESE_UNICODE As Integer = &H1
'    Friend Const SORT_CHINESE_BIG5 As Integer = &H0
'    Friend Const SORT_CHINESE_PRCP As Integer = &H0
'    Friend Const SORT_CHINESE_UNICODE As Integer = &H1
'    Friend Const SORT_CHINESE_PRC As Integer = &H2
'    Friend Const SORT_KOREAN_KSC As Integer = &H0
'    Friend Const SORT_KOREAN_UNICODE As Integer = &H1
'    Friend Const SORT_GERMAN_PHONE_BOOK As Integer = &H1
'    Friend Const STATUS_WAIT_0 As Integer = &H0
'    Friend Const STATUS_ABANDONED_WAIT_0 As Integer = &H80
'    Friend Const STATUS_USER_APC As Integer = &HC0
'    Friend Const STATUS_TIMEOUT As Integer = &H102
'    Friend Const STATUS_PENDING As Integer = &H103
'    Friend Const STATUS_SEGMENT_NOTIFICATION As Integer = &H40000005
'    Friend Const STATUS_GUARD_PAGE_VIOLATION As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                      STATUS_SEGMENT_NOTIFICATION = (0x40000005),
'    '                                                                                                                                    STATUS_GUARD_PAGE_VIOLATION = (unchecked((int)0x80000001)),
'    '--------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const STATUS_DATATYPE_MISALIGNMENT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                    STATUS_GUARD_PAGE_VIOLATION = (unchecked((int)0x80000001)),
'    '                                                                                                                                                                  STATUS_DATATYPE_MISALIGNMENT = (unchecked((int)0x80000002)),
'    '---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const STATUS_BREAKPOINT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                                  STATUS_DATATYPE_MISALIGNMENT = (unchecked((int)0x80000002)),
'    '                                                                                                                                                                                                 STATUS_BREAKPOINT = (unchecked((int)0x80000003)),
'    '-----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const STATUS_SINGLE_STEP As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                                                                 STATUS_BREAKPOINT = (unchecked((int)0x80000003)),
'    '                                                                                                                                                                                                                     STATUS_SINGLE_STEP = (unchecked((int)0x80000004)),
'    '--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const STATUS_ACCESS_VIOLATION As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                                                                                     STATUS_SINGLE_STEP = (unchecked((int)0x80000004)),
'    '                                                                                                                                                                                                                                          STATUS_ACCESS_VIOLATION = (unchecked((int)0xC0000005)),
'    '----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const STATUS_IN_PAGE_ERROR As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                                                                                                          STATUS_ACCESS_VIOLATION = (unchecked((int)0xC0000005)),
'    '                                                                                                                                                                                                                                                                    STATUS_IN_PAGE_ERROR = (unchecked((int)0xC0000006)),
'    '---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const STATUS_INVALID_HANDLE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                                                                                                                                    STATUS_IN_PAGE_ERROR = (unchecked((int)0xC0000006)),
'    '                                                                                                                                                                                                                                                                                           STATUS_INVALID_HANDLE = (unchecked((int)0xC0000008)),
'    '---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const STATUS_NO_MEMORY As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                                                                                                                                                           STATUS_INVALID_HANDLE = (unchecked((int)0xC0000008)),
'    '                                                                                                                                                                                                                                                                                                                   STATUS_NO_MEMORY = (unchecked((int)0xC0000017)),
'    '----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const STATUS_ILLEGAL_INSTRUCTION As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                                                                                                                                                                                   STATUS_NO_MEMORY = (unchecked((int)0xC0000017)),
'    '                                                                                                                                                                                                                                                                                                                                      STATUS_ILLEGAL_INSTRUCTION = (unchecked((int)0xC000001D)),
'    '---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const STATUS_NONCONTINUABLE_EXCEPTION As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                                                                                                                                                                                                      STATUS_ILLEGAL_INSTRUCTION = (unchecked((int)0xC000001D)),
'    '                                                                                                                                                                                                                                                                                                                                                                   STATUS_NONCONTINUABLE_EXCEPTION = (unchecked((int)0xC0000025)),
'    '-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const STATUS_INVALID_DISPOSITION As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                                                                                                                                                                                                                                   STATUS_NONCONTINUABLE_EXCEPTION = (unchecked((int)0xC0000025)),
'    '                                                                                                                                                                                                                                                                                                                                                                                                     STATUS_INVALID_DISPOSITION = (unchecked((int)0xC0000026)),
'    '------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const STATUS_ARRAY_BOUNDS_EXCEEDED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                                                                                                                                                                                                                                                                     STATUS_INVALID_DISPOSITION = (unchecked((int)0xC0000026)),
'    '                                                                                                                                                                                                                                                                                                                                                                                                                                  STATUS_ARRAY_BOUNDS_EXCEEDED = (unchecked((int)0xC000008C)),
'    '-------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const STATUS_FLOAT_DENORMAL_OPERAND As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                                                                                                                                                                                                                                                                                                  STATUS_ARRAY_BOUNDS_EXCEEDED = (unchecked((int)0xC000008C)),
'    '                                                                                                                                                                                                                                                                                                                                                                                                                                                                 STATUS_FLOAT_DENORMAL_OPERAND = (unchecked((int)0xC000008D)),
'    '---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const STATUS_FLOAT_DIVIDE_BY_ZERO As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                                                                                                                                                                                                                                                                                                                                 STATUS_FLOAT_DENORMAL_OPERAND = (unchecked((int)0xC000008D)),
'    '                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                 STATUS_FLOAT_DIVIDE_BY_ZERO = (unchecked((int)0xC000008E)),
'    '---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const STATUS_FLOAT_INEXACT_RESULT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                 STATUS_FLOAT_DIVIDE_BY_ZERO = (unchecked((int)0xC000008E)),
'    '                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                               STATUS_FLOAT_INEXACT_RESULT = (unchecked((int)0xC000008F)),
'    '---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const STATUS_FLOAT_INVALID_OPERATION As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                               STATUS_FLOAT_INEXACT_RESULT = (unchecked((int)0xC000008F)),
'    '                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                             STATUS_FLOAT_INVALID_OPERATION = (unchecked((int)0xC0000090)),
'    '------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const STATUS_FLOAT_OVERFLOW As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                             STATUS_FLOAT_INVALID_OPERATION = (unchecked((int)0xC0000090)),
'    '                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                              STATUS_FLOAT_OVERFLOW = (unchecked((int)0xC0000091)),
'    '------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const STATUS_FLOAT_STACK_CHECK As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                              STATUS_FLOAT_OVERFLOW = (unchecked((int)0xC0000091)),
'    '                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                      STATUS_FLOAT_STACK_CHECK = (unchecked((int)0xC0000092)),
'    '---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const STATUS_FLOAT_UNDERFLOW As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                      STATUS_FLOAT_STACK_CHECK = (unchecked((int)0xC0000092)),
'    '                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                 STATUS_FLOAT_UNDERFLOW = (unchecked((int)0xC0000093)),
'    '----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const STATUS_INTEGER_DIVIDE_BY_ZERO As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                 STATUS_FLOAT_UNDERFLOW = (unchecked((int)0xC0000093)),
'    '                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                          STATUS_INTEGER_DIVIDE_BY_ZERO = (unchecked((int)0xC0000094)),
'    '------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const STATUS_INTEGER_OVERFLOW As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                          STATUS_INTEGER_DIVIDE_BY_ZERO = (unchecked((int)0xC0000094)),
'    '                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                          STATUS_INTEGER_OVERFLOW = (unchecked((int)0xC0000095)),
'    '--------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const STATUS_PRIVILEGED_INSTRUCTION As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                          STATUS_INTEGER_OVERFLOW = (unchecked((int)0xC0000095)),
'    '                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    STATUS_PRIVILEGED_INSTRUCTION = (unchecked((int)0xC0000096)),
'    '----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const STATUS_STACK_OVERFLOW As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    STATUS_PRIVILEGED_INSTRUCTION = (unchecked((int)0xC0000096)),
'    '                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    STATUS_STACK_OVERFLOW = (unchecked((int)0xC00000FD)),
'    '----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const STATUS_CONTROL_C_EXIT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                    STATUS_STACK_OVERFLOW = (unchecked((int)0xC00000FD)),
'    '                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                                            STATUS_CONTROL_C_EXIT = (unchecked((int)0xC000013A)),
'    '----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const SIZE_OF_80387_REGISTERS As Integer = 80
'    Friend Const SEMAPHORE_MODIFY_STATE As Integer = &H2
'    Friend Const SECTION_QUERY As Integer = &H1
'    Friend Const SECTION_MAP_WRITE As Integer = &H2
'    Friend Const SECTION_MAP_READ As Integer = &H4
'    Friend Const SECTION_MAP_EXECUTE As Integer = &H8
'    Friend Const SECTION_EXTEND_SIZE As Integer = &H10
'    Friend Const SEC_FILE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        SECTION_EXTEND_SIZE = 0x0010,
'    '        SEC_FILE = unchecked((int)0x800000),
'    '--------------------^--- GenCode(token): unexpected token type
'    Friend Const SEC_IMAGE As Integer = &H1000000
'    Friend Const SEC_RESERVE As Integer = &H4000000
'    Friend Const SEC_COMMIT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        SEC_RESERVE = 0x4000000,
'    '        SEC_COMMIT = unchecked((int)0x8000000),
'    '----------------------^--- GenCode(token): unexpected token type
'    Friend Const SEC_NOCACHE As Integer = &H10000000
'    Friend Const SYNCHRONIZE As Integer = &H100000
'    Friend Const STANDARD_RIGHTS_REQUIRED As Integer = &HF0000
'    Friend Const STANDARD_RIGHTS_READ As Integer = &H20000
'    Friend Const STANDARD_RIGHTS_WRITE As Integer = &H20000
'    Friend Const STANDARD_RIGHTS_EXECUTE As Integer = &H20000
'    Friend Const STANDARD_RIGHTS_ALL As Integer = &H1F0000
'    Friend Const SPECIFIC_RIGHTS_ALL As Integer = &HFFFF
'    Friend Const SID_REVISION As Integer = 1
'    Friend Const SID_MAX_SUB_AUTHORITIES As Integer = 15
'    Friend Const SID_RECOMMENDED_SUB_AUTHORITIES As Integer = 1
'    Friend Const SECURITY_NULL_RID As Integer = &H0
'    Friend Const SECURITY_WORLD_RID As Integer = &H0
'    Friend Const SECURITY_LOCAL_RID As Integer = 0
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
'    Friend Const SECURITY_DYNAMIC_TRACKING As Boolean = True
'    Friend Const SECURITY_STATIC_TRACKING As Boolean = False
'    Friend Const SACL_SECURITY_INFORMATION As Integer = 0
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
'    Friend Const SC_GROUP_IDENTIFIERW As Char = "+"c
'    Friend Const SC_GROUP_IDENTIFIERA As Char = "+"c
'    Friend Const SERVICE_NO_CHANGE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        SC_GROUP_IDENTIFIERA = '+';
'    '        public const int SERVICE_NO_CHANGE = unchecked((int)0xFfffffff),
'    '----------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const SERVICE_ACTIVE As Integer = &H1
'    Friend Const SERVICE_INACTIVE As Integer = &H2
'    Friend Const SERVICE_CONTROL_STOP As Integer = &H1
'    Friend Const SERVICE_CONTROL_PAUSE As Integer = &H2
'    Friend Const SERVICE_CONTROL_CONTINUE As Integer = &H3
'    Friend Const SERVICE_CONTROL_INTERROGATE As Integer = &H4
'    Friend Const SERVICE_CONTROL_SHUTDOWN As Integer = &H5
'    Friend Const SERVICE_STOPPED As Integer = &H1
'    Friend Const SERVICE_START_PENDING As Integer = &H2
'    Friend Const SERVICE_STOP_PENDING As Integer = &H3
'    Friend Const SERVICE_RUNNING As Integer = &H4
'    Friend Const SERVICE_CONTINUE_PENDING As Integer = &H5
'    Friend Const SERVICE_PAUSE_PENDING As Integer = &H6
'    Friend Const SERVICE_PAUSED As Integer = &H7
'    Friend Const SERVICE_ACCEPT_STOP As Integer = &H1
'    Friend Const SERVICE_ACCEPT_PAUSE_CONTINUE As Integer = &H2
'    Friend Const SERVICE_ACCEPT_SHUTDOWN As Integer = &H4
'    Friend Const SC_MANAGER_CONNECT As Integer = &H1
'    Friend Const SC_MANAGER_CREATE_SERVICE As Integer = &H2
'    Friend Const SC_MANAGER_ENUMERATE_SERVICE As Integer = &H4
'    Friend Const SC_MANAGER_LOCK As Integer = &H8
'    Friend Const SC_MANAGER_QUERY_LOCK_STATUS As Integer = &H10
'    Friend Const SC_MANAGER_MODIFY_BOOT_CONFIG As Integer = &H20
'    Friend Const SERVICE_QUERY_CONFIG As Integer = &H1
'    Friend Const SERVICE_CHANGE_CONFIG As Integer = &H2
'    Friend Const SERVICE_QUERY_STATUS As Integer = &H4
'    Friend Const SERVICE_ENUMERATE_DEPENDENTS As Integer = &H8
'    Friend Const SERVICE_START As Integer = &H10
'    Friend Const SERVICE_STOP As Integer = &H20
'    Friend Const SERVICE_PAUSE_CONTINUE As Integer = &H40
'    Friend Const SERVICE_INTERROGATE As Integer = &H80
'    Friend Const SERVICE_USER_DEFINED_CONTROL As Integer = &H100
'    Friend Const SB_HORZ As Integer = 0
'    Friend Const SB_VERT As Integer = 1
'    Friend Const SB_CTL As Integer = 2
'    Friend Const SB_BOTH As Integer = 3
'    Friend Const SB_LINEUP As Integer = 0
'    Friend Const SB_LINELEFT As Integer = 0
'    Friend Const SB_LINEDOWN As Integer = 1
'    Friend Const SB_LINERIGHT As Integer = 1
'    Friend Const SB_PAGEUP As Integer = 2
'    Friend Const SB_PAGELEFT As Integer = 2
'    Friend Const SB_PAGEDOWN As Integer = 3
'    Friend Const SB_PAGERIGHT As Integer = 3
'    Friend Const SB_THUMBPOSITION As Integer = 4
'    Friend Const SB_THUMBTRACK As Integer = 5
'    Friend Const SB_TOP As Integer = 6
'    Friend Const SB_LEFT As Integer = 6
'    Friend Const SB_BOTTOM As Integer = 7
'    Friend Const SB_RIGHT As Integer = 7
'    Friend Const SB_ENDSCROLL As Integer = 8
'    Friend Const SW_HIDE As Integer = 0
'    Friend Const SW_SHOWNORMAL As Integer = 1
'    Friend Const SW_NORMAL As Integer = 1
'    Friend Const SW_SHOWMINIMIZED As Integer = 2
'    Friend Const SW_SHOWMAXIMIZED As Integer = 3
'    Friend Const SW_MAXIMIZE As Integer = 3
'    Friend Const SW_SHOWNOACTIVATE As Integer = 4
'    Friend Const SW_SHOW As Integer = 5
'    Friend Const SW_MINIMIZE As Integer = 6
'    Friend Const SW_SHOWMINNOACTIVE As Integer = 7
'    Friend Const SW_SHOWNA As Integer = 8
'    Friend Const SW_RESTORE As Integer = 9
'    Friend Const SW_SHOWDEFAULT As Integer = 10
'    Friend Const SW_MAX As Integer = 10
'    Friend Const SHOW_OPENWINDOW As Integer = 1
'    Friend Const SHOW_ICONWINDOW As Integer = 2
'    Friend Const SHOW_FULLSCREEN As Integer = 3
'    Friend Const SHOW_OPENNOACTIVATE As Integer = 4
'    Friend Const SW_PARENTCLOSING As Integer = 1
'    Friend Const SW_OTHERZOOM As Integer = 2
'    Friend Const SW_PARENTOPENING As Integer = 3
'    Friend Const SW_OTHERUNZOOM As Integer = 4
'    Friend Const ST_BEGINSWP As Integer = 0
'    Friend Const ST_ENDSWP As Integer = 1
'    Friend Const SMTO_NORMAL As Integer = &H0
'    Friend Const SMTO_BLOCK As Integer = &H1
'    Friend Const SMTO_ABORTIFHUNG As Integer = &H2
'    Friend Const SIZE_RESTORED As Integer = 0
'    Friend Const SIZE_MINIMIZED As Integer = 1
'    Friend Const SIZE_MAXIMIZED As Integer = 2
'    Friend Const SIZE_MAXSHOW As Integer = 3
'    Friend Const SIZE_MAXHIDE As Integer = 4
'    Friend Const SIZENORMAL As Integer = 0
'    Friend Const SIZEICONIC As Integer = 1
'    Friend Const SIZEFULLSCREEN As Integer = 2
'    Friend Const SIZEZOOMSHOW As Integer = 3
'    Friend Const SIZEZOOMHIDE As Integer = 4
'    Friend Const SWP_NOSIZE As Integer = &H1
'    Friend Const SWP_NOMOVE As Integer = &H2
'    Friend Const SWP_NOZORDER As Integer = &H4
'    Friend Const SWP_NOREDRAW As Integer = &H8
'    Friend Const SWP_NOACTIVATE As Integer = &H10
'    Friend Const SWP_FRAMECHANGED As Integer = &H20
'    Friend Const SWP_SHOWWINDOW As Integer = &H40
'    Friend Const SWP_HIDEWINDOW As Integer = &H80
'    Friend Const SWP_NOCOPYBITS As Integer = &H100
'    Friend Const SWP_NOOWNERZORDER As Integer = &H200
'    Friend Const SWP_NOSENDCHANGING As Integer = &H400
'    Friend Const SWP_DRAWFRAME As Integer = &H20
'    Friend Const SWP_NOREPOSITION As Integer = &H200
'    Friend Const SWP_DEFERERASE As Integer = &H2000
'    Friend Const SWP_ASYNCWINDOWPOS As Integer = &H4000
'    Friend Const SM_CXSCREEN As Integer = 0
'    Friend Const SM_CYSCREEN As Integer = 1
'    Friend Const SM_CXVSCROLL As Integer = 2
'    Friend Const SM_CYHSCROLL As Integer = 3
'    Friend Const SM_CYCAPTION As Integer = 4
'    Friend Const SM_CXBORDER As Integer = 5
'    Friend Const SM_CYBORDER As Integer = 6
'    Friend Const SM_CXDLGFRAME As Integer = 7
'    Friend Const SM_CYDLGFRAME As Integer = 8
'    Friend Const SM_CYVTHUMB As Integer = 9
'    Friend Const SM_CXHTHUMB As Integer = 10
'    Friend Const SM_CXICON As Integer = 11
'    Friend Const SM_CYICON As Integer = 12
'    Friend Const SM_CXCURSOR As Integer = 13
'    Friend Const SM_CYCURSOR As Integer = 14
'    Friend Const SM_CYMENU As Integer = 15
'    Friend Const SM_CXFULLSCREEN As Integer = 16
'    Friend Const SM_CYFULLSCREEN As Integer = 17
'    Friend Const SM_CYKANJIWINDOW As Integer = 18
'    Friend Const SM_MOUSEPRESENT As Integer = 19
'    Friend Const SM_CYVSCROLL As Integer = 20
'    Friend Const SM_CXHSCROLL As Integer = 21
'    Friend Const SM_DEBUG As Integer = 22
'    Friend Const SM_SWAPBUTTON As Integer = 23
'    Friend Const SM_RESERVED1 As Integer = 24
'    Friend Const SM_RESERVED2 As Integer = 25
'    Friend Const SM_RESERVED3 As Integer = 26
'    Friend Const SM_RESERVED4 As Integer = 27
'    Friend Const SM_CXMIN As Integer = 28
'    Friend Const SM_CYMIN As Integer = 29
'    Friend Const SM_CXSIZE As Integer = 30
'    Friend Const SM_CYSIZE As Integer = 31
'    Friend Const SM_CXFRAME As Integer = 32
'    Friend Const SM_CYFRAME As Integer = 33
'    Friend Const SM_CXMINTRACK As Integer = 34
'    Friend Const SM_CYMINTRACK As Integer = 35
'    Friend Const SM_CXDOUBLECLK As Integer = 36
'    Friend Const SM_CYDOUBLECLK As Integer = 37
'    Friend Const SM_CXICONSPACING As Integer = 38
'    Friend Const SM_CYICONSPACING As Integer = 39
'    Friend Const SM_MENUDROPALIGNMENT As Integer = 40
'    Friend Const SM_PENWINDOWS As Integer = 41
'    Friend Const SM_DBCSENABLED As Integer = 42
'    Friend Const SM_CMOUSEBUTTONS As Integer = 43
'    Friend Const SM_CXFIXEDFRAME As Integer = 7
'    Friend Const SM_CYFIXEDFRAME As Integer = 8
'    Friend Const SM_CXSIZEFRAME As Integer = 32
'    Friend Const SM_CYSIZEFRAME As Integer = 33
'    Friend Const SM_SECURE As Integer = 44
'    Friend Const SM_CXEDGE As Integer = 45
'    Friend Const SM_CYEDGE As Integer = 46
'    Friend Const SM_CXMINSPACING As Integer = 47
'    Friend Const SM_CYMINSPACING As Integer = 48
'    Friend Const SM_CXSMICON As Integer = 49
'    Friend Const SM_CYSMICON As Integer = 50
'    Friend Const SM_CYSMCAPTION As Integer = 51
'    Friend Const SM_CXSMSIZE As Integer = 52
'    Friend Const SM_CYSMSIZE As Integer = 53
'    Friend Const SM_CXMENUSIZE As Integer = 54
'    Friend Const SM_CYMENUSIZE As Integer = 55
'    Friend Const SM_ARRANGE As Integer = 56
'    Friend Const SM_CXMINIMIZED As Integer = 57
'    Friend Const SM_CYMINIMIZED As Integer = 58
'    Friend Const SM_CXMAXTRACK As Integer = 59
'    Friend Const SM_CYMAXTRACK As Integer = 60
'    Friend Const SM_CXMAXIMIZED As Integer = 61
'    Friend Const SM_CYMAXIMIZED As Integer = 62
'    Friend Const SM_NETWORK As Integer = 63
'    Friend Const SM_CLEANBOOT As Integer = 67
'    Friend Const SM_CXDRAG As Integer = 68
'    Friend Const SM_CYDRAG As Integer = 69
'    Friend Const SM_SHOWSOUNDS As Integer = 70
'    Friend Const SM_CXMENUCHECK As Integer = 71
'    Friend Const SM_CYMENUCHECK As Integer = 72
'    Friend Const SM_SLOWMACHINE As Integer = 73
'    Friend Const SM_MIDEASTENABLED As Integer = 74
'    Friend Const SM_MOUSEWHEELPRESENT As Integer = 75
'    Friend Const SM_XVIRTUALSCREEN As Integer = 76
'    Friend Const SM_YVIRTUALSCREEN As Integer = 77
'    Friend Const SM_CXVIRTUALSCREEN As Integer = 78
'    Friend Const SM_CYVIRTUALSCREEN As Integer = 79
'    Friend Const SM_CMONITORS As Integer = 80
'    Friend Const SM_SAMEDISPLAYFORMAT As Integer = 81
'    Friend Const SM_CMETRICS As Integer = 83
'    Friend Const SW_SCROLLCHILDREN As Integer = &H1
'    Friend Const SW_INVALIDATE As Integer = &H2
'    Friend Const SW_ERASE As Integer = &H4
'    Friend Const SC_SIZE As Integer = &HF000
'    Friend Const SC_MOVE As Integer = &HF010
'    Friend Const SC_MINIMIZE As Integer = &HF020
'    Friend Const SC_MAXIMIZE As Integer = &HF030
'    Friend Const SC_NEXTWINDOW As Integer = &HF040
'    Friend Const SC_PREVWINDOW As Integer = &HF050
'    Friend Const SC_CLOSE As Integer = &HF060
'    Friend Const SC_VSCROLL As Integer = &HF070
'    Friend Const SC_HSCROLL As Integer = &HF080
'    Friend Const SC_MOUSEMENU As Integer = &HF090
'    Friend Const SC_KEYMENU As Integer = &HF100
'    Friend Const SC_ARRANGE As Integer = &HF110
'    Friend Const SC_RESTORE As Integer = &HF120
'    Friend Const SC_TASKLIST As Integer = &HF130
'    Friend Const SC_HOTKEY As Integer = &HF150
'    Friend Const SC_DEFAULT As Integer = &HF160
'    Friend Const SC_MONITORPOWER As Integer = &HF170
        Friend Const SC_CONTEXTHELP As Integer = &HF180
'    Friend Const SC_SEPARATOR As Integer = &HF00F
'    Friend Const SC_ICON As Integer = &HF020
'    Friend Const SC_ZOOM As Integer = &HF030
'    Friend Const SS_LEFT As Integer = &H0
'    Friend Const SS_CENTER As Integer = &H1
'    Friend Const SS_RIGHT As Integer = &H2
'    Friend Const SS_ICON As Integer = &H3
'    Friend Const SS_BLACKRECT As Integer = &H4
'    Friend Const SS_GRAYRECT As Integer = &H5
'    Friend Const SS_WHITERECT As Integer = &H6
'    Friend Const SS_BLACKFRAME As Integer = &H7
'    Friend Const SS_GRAYFRAME As Integer = &H8
'    Friend Const SS_WHITEFRAME As Integer = &H9
'    Friend Const SS_USERITEM As Integer = &HA
'    Friend Const SS_SIMPLE As Integer = &HB
'    Friend Const SS_LEFTNOWORDWRAP As Integer = &HC
'    Friend Const SS_OWNERDRAW As Integer = &HD
'    Friend Const SS_BITMAP As Integer = &HE
'    Friend Const SS_ENHMETAFILE As Integer = &HF
'    Friend Const SS_ETCHEDHORZ As Integer = &H10
'    Friend Const SS_ETCHEDVERT As Integer = &H11
'    Friend Const SS_ETCHEDFRAME As Integer = &H12
'    Friend Const SS_TYPEMASK As Integer = &H1F
'    Friend Const SS_NOPREFIX As Integer = &H80
'    Friend Const SS_NOTIFY As Integer = &H100
'    Friend Const SS_CENTERIMAGE As Integer = &H200
'    Friend Const SS_RIGHTJUST As Integer = &H400
'    Friend Const SS_REALSIZEIMAGE As Integer = &H800
'    Friend Const SS_SUNKEN As Integer = &H1000
'    Friend Const SS_ENDELLIPSIS As Integer = &H4000
'    Friend Const SS_PATHELLIPSIS As Integer = &H8000
'    Friend Const SS_WORDELLIPSIS As Integer = &HC000
'    Friend Const SS_ELLIPSISMASK As Integer = &HC000
'    Friend Const STM_SETICON As Integer = &H170
'    Friend Const STM_GETICON As Integer = &H171
'    Friend Const STM_SETIMAGE As Integer = &H172
'    Friend Const STM_GETIMAGE As Integer = &H173
'    Friend Const STN_CLICKED As Integer = 0
'    Friend Const STN_DBLCLK As Integer = 1
'    Friend Const STN_ENABLE As Integer = 2
'    Friend Const STN_DISABLE As Integer = 3
'    Friend Const STM_MSGMAX As Integer = &H174
'    Friend Const SBS_HORZ As Integer = &H0
'    Friend Const SBS_VERT As Integer = &H1
'    Friend Const SBS_TOPALIGN As Integer = &H2
'    Friend Const SBS_LEFTALIGN As Integer = &H2
'    Friend Const SBS_BOTTOMALIGN As Integer = &H4
'    Friend Const SBS_RIGHTALIGN As Integer = &H4
'    Friend Const SBS_SIZEBOXTOPLEFTALIGN As Integer = &H2
'    Friend Const SBS_SIZEBOXBOTTOMRIGHTALIGN As Integer = &H4
'    Friend Const SBS_SIZEBOX As Integer = &H8
'    Friend Const SBS_SIZEGRIP As Integer = &H10
'    Friend Const SBM_SETPOS As Integer = &HE0
'    Friend Const SBM_GETPOS As Integer = &HE1
'    Friend Const SBM_SETRANGE As Integer = &HE2
'    Friend Const SBM_SETRANGEREDRAW As Integer = &HE6
'    Friend Const SBM_GETRANGE As Integer = &HE3
'    Friend Const SBM_ENABLE_ARROWS As Integer = &HE4
'    Friend Const SBM_SETSCROLLINFO As Integer = &HE9
'    Friend Const SBM_GETSCROLLINFO As Integer = &HEA
'    Friend Const SIF_RANGE As Integer = &H1
'    Friend Const SIF_PAGE As Integer = &H2
'    Friend Const SIF_POS As Integer = &H4
'    Friend Const SIF_DISABLENOSCROLL As Integer = &H8
'    Friend Const SIF_TRACKPOS As Integer = &H10
'    Friend Const SIF_ALL As Integer = &H1 Or &H2 Or &H4 Or &H10
'    Friend Const SPI_GETBEEP As Integer = 1
'    Friend Const SPI_SETBEEP As Integer = 2
'    Friend Const SPI_GETMOUSE As Integer = 3
'    Friend Const SPI_SETMOUSE As Integer = 4
'    Friend Const SPI_GETBORDER As Integer = 5
'    Friend Const SPI_SETBORDER As Integer = 6
'    Friend Const SPI_GETKEYBOARDSPEED As Integer = 10
'    Friend Const SPI_SETKEYBOARDSPEED As Integer = 11
'    Friend Const SPI_LANGDRIVER As Integer = 12
'    Friend Const SPI_ICONHORIZONTALSPACING As Integer = 13
'    Friend Const SPI_GETSCREENSAVETIMEOUT As Integer = 14
'    Friend Const SPI_SETSCREENSAVETIMEOUT As Integer = 15
'    Friend Const SPI_GETSCREENSAVEACTIVE As Integer = 16
'    Friend Const SPI_SETSCREENSAVEACTIVE As Integer = 17
'    Friend Const SPI_GETGRIDGRANULARITY As Integer = 18
'    Friend Const SPI_SETGRIDGRANULARITY As Integer = 19
'    Friend Const SPI_SETDESKWALLPAPER As Integer = 20
'    Friend Const SPI_SETDESKPATTERN As Integer = 21
'    Friend Const SPI_GETKEYBOARDDELAY As Integer = 22
'    Friend Const SPI_SETKEYBOARDDELAY As Integer = 23
'    Friend Const SPI_ICONVERTICALSPACING As Integer = 24
'    Friend Const SPI_GETICONTITLEWRAP As Integer = 25
'    Friend Const SPI_SETICONTITLEWRAP As Integer = 26
'    Friend Const SPI_GETMENUDROPALIGNMENT As Integer = 27
'    Friend Const SPI_SETMENUDROPALIGNMENT As Integer = 28
'    Friend Const SPI_SETDOUBLECLKWIDTH As Integer = 29
'    Friend Const SPI_SETDOUBLECLKHEIGHT As Integer = 30
'    Friend Const SPI_GETICONTITLELOGFONT As Integer = 31
'    Friend Const SPI_SETDOUBLECLICKTIME As Integer = 32
'    Friend Const SPI_SETMOUSEBUTTONSWAP As Integer = 33
'    Friend Const SPI_SETICONTITLELOGFONT As Integer = 34
'    Friend Const SPI_GETFASTTASKSWITCH As Integer = 35
'    Friend Const SPI_SETFASTTASKSWITCH As Integer = 36
'    Friend Const SPI_SETDRAGFULLWINDOWS As Integer = 37
'    Friend Const SPI_GETDRAGFULLWINDOWS As Integer = 38
'    Friend Const SPI_GETNONCLIENTMETRICS As Integer = 41
'    Friend Const SPI_SETNONCLIENTMETRICS As Integer = 42
'    Friend Const SPI_GETMINIMIZEDMETRICS As Integer = 43
'    Friend Const SPI_SETMINIMIZEDMETRICS As Integer = 44
'    Friend Const SPI_GETICONMETRICS As Integer = 45
'    Friend Const SPI_SETICONMETRICS As Integer = 46
'    Friend Const SPI_SETWORKAREA As Integer = 47
'    Friend Const SPI_GETWORKAREA As Integer = 48
'    Friend Const SPI_SETPENWINDOWS As Integer = 49
'    Friend Const SPI_GETHIGHCONTRAST As Integer = 66
'    Friend Const SPI_SETHIGHCONTRAST As Integer = 67
'    Friend Const SPI_GETKEYBOARDPREF As Integer = 68
'    Friend Const SPI_SETKEYBOARDPREF As Integer = 69
     Friend Const SPI_GETSCREENREADER As Integer = 70
'    Friend Const SPI_SETSCREENREADER As Integer = 71
'    Friend Const SPI_GETANIMATION As Integer = 72
'    Friend Const SPI_SETANIMATION As Integer = 73
'    Friend Const SPI_GETFONTSMOOTHING As Integer = 74
'    Friend Const SPI_SETFONTSMOOTHING As Integer = 75
'    Friend Const SPI_SETDRAGWIDTH As Integer = 76
'    Friend Const SPI_SETDRAGHEIGHT As Integer = 77
'    Friend Const SPI_SETHANDHELD As Integer = 78
'    Friend Const SPI_GETLOWPOWERTIMEOUT As Integer = 79
'    Friend Const SPI_GETPOWEROFFTIMEOUT As Integer = 80
'    Friend Const SPI_SETLOWPOWERTIMEOUT As Integer = 81
'    Friend Const SPI_SETPOWEROFFTIMEOUT As Integer = 82
'    Friend Const SPI_GETLOWPOWERACTIVE As Integer = 83
'    Friend Const SPI_GETPOWEROFFACTIVE As Integer = 84
'    Friend Const SPI_SETLOWPOWERACTIVE As Integer = 85
'    Friend Const SPI_SETPOWEROFFACTIVE As Integer = 86
'    Friend Const SPI_SETCURSORS As Integer = 87
'    Friend Const SPI_SETICONS As Integer = 88
'    Friend Const SPI_GETDEFAULTINPUTLANG As Integer = 89
'    Friend Const SPI_SETDEFAULTINPUTLANG As Integer = 90
'    Friend Const SPI_SETLANGTOGGLE As Integer = 91
'    Friend Const SPI_GETWINDOWSEXTENSION As Integer = 92
'    Friend Const SPI_SETMOUSETRAILS As Integer = 93
'    Friend Const SPI_GETMOUSETRAILS As Integer = 94
'    Friend Const SPI_SCREENSAVERRUNNING As Integer = 97
'    Friend Const SPI_GETFILTERKEYS As Integer = 50
'    Friend Const SPI_SETFILTERKEYS As Integer = 51
'    Friend Const SPI_GETTOGGLEKEYS As Integer = 52
'    Friend Const SPI_SETTOGGLEKEYS As Integer = 53
'    Friend Const SPI_GETMOUSEKEYS As Integer = 54
'    Friend Const SPI_SETMOUSEKEYS As Integer = 55
'    Friend Const SPI_GETSHOWSOUNDS As Integer = 56
'    Friend Const SPI_SETSHOWSOUNDS As Integer = 57
'    Friend Const SPI_GETSTICKYKEYS As Integer = 58
'    Friend Const SPI_SETSTICKYKEYS As Integer = 59
'    Friend Const SPI_GETACCESSTIMEOUT As Integer = 60
'    Friend Const SPI_SETACCESSTIMEOUT As Integer = 61
'    Friend Const SPI_GETSERIALKEYS As Integer = 62
'    Friend Const SPI_SETSERIALKEYS As Integer = 63
'    Friend Const SPI_GETSOUNDSENTRY As Integer = 64
'    Friend Const SPI_SETSOUNDSENTRY As Integer = 65
'    Friend Const SPI_GETSNAPTODEFBUTTON As Integer = 95
'    Friend Const SPI_SETSNAPTODEFBUTTON As Integer = 96
'    Friend Const SPI_GETMOUSEHOVERWIDTH As Integer = 98
'    Friend Const SPI_SETMOUSEHOVERWIDTH As Integer = 99
'    Friend Const SPI_GETMOUSEHOVERHEIGHT As Integer = 100
'    Friend Const SPI_SETMOUSEHOVERHEIGHT As Integer = 101
'    Friend Const SPI_GETMOUSEHOVERTIME As Integer = 102
'    Friend Const SPI_SETMOUSEHOVERTIME As Integer = 103
'    Friend Const SPI_GETWHEELSCROLLLINES As Integer = 104
'    Friend Const SPI_SETWHEELSCROLLLINES As Integer = 105
'    Friend Const SPI_GETKEYBOARDCUES As Integer = &H100A
'    Friend Const SPI_GETMENUUNDERLINES As Integer = SPI_GETKEYBOARDCUES
'    Friend Const SPIF_UPDATEINIFILE As Integer = &H1
'    Friend Const SPIF_SENDWININICHANGE As Integer = &H2
'    Friend Const SPIF_SENDCHANGE As Integer = &H2
'    Friend Const SERKF_SERIALKEYSON As Integer = &H1
'    Friend Const SERKF_AVAILABLE As Integer = &H2
'    Friend Const SERKF_INDICATOR As Integer = &H4
'    Friend Const SKF_STICKYKEYSON As Integer = &H1
'    Friend Const SKF_AVAILABLE As Integer = &H2
'    Friend Const SKF_HOTKEYACTIVE As Integer = &H4
'    Friend Const SKF_CONFIRMHOTKEY As Integer = &H8
'    Friend Const SKF_HOTKEYSOUND As Integer = &H10
'    Friend Const SKF_INDICATOR As Integer = &H20
'    Friend Const SKF_AUDIBLEFEEDBACK As Integer = &H40
'    Friend Const SKF_TRISTATE As Integer = &H80
'    Friend Const SKF_TWOKEYSOFF As Integer = &H100
'    Friend Const SSGF_NONE As Integer = 0
'    Friend Const SSGF_DISPLAY As Integer = 3
'    Friend Const SSTF_NONE As Integer = 0
'    Friend Const SSTF_CHARS As Integer = 1
'    Friend Const SSTF_BORDER As Integer = 2
'    Friend Const SSTF_DISPLAY As Integer = 3
'    Friend Const SSWF_NONE As Integer = 0
'    Friend Const SSWF_TITLE As Integer = 1
'    Friend Const SSWF_WINDOW As Integer = 2
'    Friend Const SSWF_DISPLAY As Integer = 3
'    Friend Const SSWF_CUSTOM As Integer = 4
'    Friend Const SSF_SOUNDSENTRYON As Integer = &H1
'    Friend Const SSF_AVAILABLE As Integer = &H2
'    Friend Const SSF_INDICATOR As Integer = &H4
'    Friend Const SLE_ERROR As Integer = &H1
'    Friend Const SLE_MINORERROR As Integer = &H2
'    Friend Const SLE_WARNING As Integer = &H3
'    Friend Const STD_CUT As Integer = 0
'    Friend Const STD_COPY As Integer = 1
'    Friend Const STD_PASTE As Integer = 2
'    Friend Const STD_UNDO As Integer = 3
'    Friend Const STD_REDOW As Integer = 4
'    Friend Const STD_DELETE As Integer = 5
'    Friend Const STD_FILENEW As Integer = 6
'    Friend Const STD_FILEOPEN As Integer = 7
'    Friend Const STD_FILESAVE As Integer = 8
'    Friend Const STD_PRINTPRE As Integer = 9
'    Friend Const STD_PROPERTIES As Integer = 10
'    Friend Const STD_HELP As Integer = 11
'    Friend Const STD_FIND As Integer = 12
'    Friend Const STD_REPLACE As Integer = 13
'    Friend Const STD_PRINT As Integer = 14
'    Friend Const SBARS_SIZEGRIP As Integer = &H100
'    Friend Const SB_SETTEXTA As Integer = &H400 + 1
'    Friend Const SB_SETTEXTW As Integer = &H400 + 11
'    Friend Const SB_GETTEXTA As Integer = &H400 + 2
'    Friend Const SB_GETTEXTW As Integer = &H400 + 13
'    Friend Const SB_GETTEXTLENGTHA As Integer = &H400 + 3
'    Friend Const SB_GETTEXTLENGTHW As Integer = &H400 + 12
'    Friend Const SB_SETPARTS As Integer = &H400 + 4
'    Friend Const SB_GETPARTS As Integer = &H400 + 6
'    Friend Const SB_GETBORDERS As Integer = &H400 + 7
'    Friend Const SB_SETMINHEIGHT As Integer = &H400 + 8
'    Friend Const SB_SIMPLE As Integer = &H400 + 9
'    Friend Const SB_GETRECT As Integer = &H400 + 10
'    Friend Const SB_ISSIMPLE As Integer = &H400 + 14
'    Friend Const SB_SETICON As Integer = &H400 + 15
'    Friend Const SB_SETTIPTEXTA As Integer = &H400 + 16
'    Friend Const SB_SETTIPTEXTW As Integer = &H400 + 17
'    Friend Const SB_GETTIPTEXTA As Integer = &H400 + 18
'    Friend Const SB_GETTIPTEXTW As Integer = &H400 + 19
'    Friend Const SB_GETICON As Integer = &H400 + 20
'    Friend Const SB_SETBKCOLOR As Integer = win.CCM_SETBKCOLOR
'    Friend Const SB_SETUNICODEFORMAT As Integer = win.CCM_SETUNICODEFORMAT
'    Friend Const SB_GETUNICODEFORMAT As Integer = win.CCM_GETUNICODEFORMAT
'    Friend Const SBT_OWNERDRAW As Integer = &H1000
'    Friend Const SBT_NOBORDERS As Integer = &H100
'    Friend Const SBT_POPOUT As Integer = &H200
'    Friend Const SBT_RTLREADING As Integer = &H400
'    Friend Const SBT_TOOLTIPS As Integer = &H800
'    Friend Const SC_GROUP_IDENTIFIER As Char = "+"c
'    Friend Const SYSRGN As Integer = 4
'    Friend Const STILL_ACTIVE As Integer = &H103
'    Friend Const SEMAPHORE_ALL_ACCESS As Integer = &HF0000 Or &H100000 Or &H3
'    Friend Const SRCCOPY As Integer = &HCC0020
'    Friend Const SRCPAINT As Integer = &HEE0086
'    Friend Const SRCAND As Integer = &H8800C6
'    Friend Const SRCINVERT As Integer = &H660046
'    Friend Const SRCERASE As Integer = &H440328
'    Friend Const STD_INPUT_HANDLE As Integer = - 10
'    Friend Const STD_OUTPUT_HANDLE As Integer = - 11
'    Friend Const STD_ERROR_HANDLE As Integer = - 12
'    Friend Const SBN_FIRST As Integer = 0 - 880
'    Friend Const SBN_LAST As Integer = 0 - 899
'    Friend Const SBN_SIMPLEMODECHANGE As Integer = SBN_FIRST - 0
    
'    Friend Const S_OK As Integer = &H0
'    Friend Const S_FALSE As Integer = &H1
    
    
'    Friend Shared Function Succeeded(hr As Integer) As Boolean
'        Return hr >= 0
'    End Function 'Succeeded
    
    
'    Friend Shared Function Failed(hr As Integer) As Boolean
'        Return hr < 0
'    End Function 'Failed
    
    
'    Friend Const [TRUE] As Boolean = True
'    Friend Const TIMEOUT_ASYNC As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        public const bool TRUE = true;
'    '        public const int TIMEOUT_ASYNC = unchecked((int)0xFFFFFFFF),
'    '------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const TIME_MS As Integer = &H1
'    Friend Const TIME_SAMPLES As Integer = &H2
'    Friend Const TIME_BYTES As Integer = &H4
'    Friend Const TIME_SMPTE As Integer = &H8
'    Friend Const TIME_MIDI As Integer = &H10
'    Friend Const TIME_TICKS As Integer = &H20
'    Friend Const TIMERR_BASE As Integer = 96
'    Friend Const TIMERR_NOERROR As Integer = 0
'    Friend Const TIMERR_NOCANDO As Integer = 96 + 1
'    Friend Const TIMERR_STRUCT As Integer = 96 + 33
'    Friend Const TIME_ONESHOT As Integer = &H0
'    Friend Const TIME_PERIODIC As Integer = &H1
'    Friend Const TIME_CALLBACK_FUNCTION As Integer = &H0
'    Friend Const TIME_CALLBACK_EVENT_SET As Integer = &H10
'    Friend Const TIME_CALLBACK_EVENT_PULSE As Integer = &H20
'    Friend Const TCP_BSDURGENT As Integer = &H7000
'    Friend Const TF_DISCONNECT As Integer = &H1
'    Friend Const TF_REUSE_SOCKET As Integer = &H2
'    Friend Const TF_WRITE_BEHIND As Integer = &H4
'    Friend Const TRANSPORT_TYPE_CN As Integer = &H1
'    Friend Const TRANSPORT_TYPE_DG As Integer = &H2
'    Friend Const TRANSPORT_TYPE_LPC As Integer = &H4
'    Friend Const TRANSPORT_TYPE_WMSG As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TRANSPORT_TYPE_LPC = 0x4,
'    '        TRANSPORT_TYPE_WMSG = unchecked((int)0x8);
'    '-------------------------------^--- GenCode(token): unexpected token type
'    Friend Const TRUNCATE_EXISTING As Integer = 5
'    Friend Const THREAD_PRIORITY_NORMAL As Integer = 0
'    Friend Const TWOSTOPBITS As Integer = 2
'    Friend Const TC_NORMAL As Integer = 0
'    Friend Const TC_HARDERR As Integer = 1
'    Friend Const TC_GP_TRAP As Integer = 2
'    Friend Const TC_SIGNAL As Integer = 3
     Friend Const [TRUE] As Integer = 1
'    Friend Const TYPE_E_BUFFERTOOSMALL As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        // TRUE = 1;
'    '        public const int TYPE_E_BUFFERTOOSMALL = unchecked((int)0x80028016),
'    '--------------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const TYPE_E_INVDATAREAD As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        public const int TYPE_E_BUFFERTOOSMALL = unchecked((int)0x80028016),
'    '        TYPE_E_INVDATAREAD = unchecked((int)0x80028018),
'    '------------------------------^--- GenCode(token): unexpected token type
'    Friend Const TYPE_E_UNSUPFORMAT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TYPE_E_INVDATAREAD = unchecked((int)0x80028018),
'    '        TYPE_E_UNSUPFORMAT = unchecked((int)0x80028019),
'    '------------------------------^--- GenCode(token): unexpected token type
'    Friend Const TYPE_E_REGISTRYACCESS As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TYPE_E_UNSUPFORMAT = unchecked((int)0x80028019),
'    '        TYPE_E_REGISTRYACCESS = unchecked((int)0x8002801C),
'    '---------------------------------^--- GenCode(token): unexpected token type
'    Friend Const TYPE_E_LIBNOTREGISTERED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TYPE_E_REGISTRYACCESS = unchecked((int)0x8002801C),
'    '        TYPE_E_LIBNOTREGISTERED = unchecked((int)0x8002801D),
'    '-----------------------------------^--- GenCode(token): unexpected token type
'    Friend Const TYPE_E_UNDEFINEDTYPE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TYPE_E_LIBNOTREGISTERED = unchecked((int)0x8002801D),
'    '        TYPE_E_UNDEFINEDTYPE = unchecked((int)0x80028027),
'    '--------------------------------^--- GenCode(token): unexpected token type
'    Friend Const TYPE_E_QUALIFIEDNAMEDISALLOWED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TYPE_E_UNDEFINEDTYPE = unchecked((int)0x80028027),
'    '        TYPE_E_QUALIFIEDNAMEDISALLOWED = unchecked((int)0x80028028),
'    '------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const TYPE_E_INVALIDSTATE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TYPE_E_QUALIFIEDNAMEDISALLOWED = unchecked((int)0x80028028),
'    '        TYPE_E_INVALIDSTATE = unchecked((int)0x80028029),
'    '-------------------------------^--- GenCode(token): unexpected token type
'    Friend Const TYPE_E_WRONGTYPEKIND As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TYPE_E_INVALIDSTATE = unchecked((int)0x80028029),
'    '        TYPE_E_WRONGTYPEKIND = unchecked((int)0x8002802A),
'    '--------------------------------^--- GenCode(token): unexpected token type
'    Friend Const TYPE_E_ELEMENTNOTFOUND As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TYPE_E_WRONGTYPEKIND = unchecked((int)0x8002802A),
'    '        TYPE_E_ELEMENTNOTFOUND = unchecked((int)0x8002802B),
'    '----------------------------------^--- GenCode(token): unexpected token type
'    Friend Const TYPE_E_AMBIGUOUSNAME As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TYPE_E_ELEMENTNOTFOUND = unchecked((int)0x8002802B),
'    '        TYPE_E_AMBIGUOUSNAME = unchecked((int)0x8002802C),
'    '--------------------------------^--- GenCode(token): unexpected token type
'    Friend Const TYPE_E_NAMECONFLICT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TYPE_E_AMBIGUOUSNAME = unchecked((int)0x8002802C),
'    '        TYPE_E_NAMECONFLICT = unchecked((int)0x8002802D),
'    '-------------------------------^--- GenCode(token): unexpected token type
'    Friend Const TYPE_E_UNKNOWNLCID As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TYPE_E_NAMECONFLICT = unchecked((int)0x8002802D),
'    '        TYPE_E_UNKNOWNLCID = unchecked((int)0x8002802E),
'    '------------------------------^--- GenCode(token): unexpected token type
'    Friend Const TYPE_E_DLLFUNCTIONNOTFOUND As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TYPE_E_UNKNOWNLCID = unchecked((int)0x8002802E),
'    '        TYPE_E_DLLFUNCTIONNOTFOUND = unchecked((int)0x8002802F),
'    '--------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const TYPE_E_BADMODULEKIND As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TYPE_E_DLLFUNCTIONNOTFOUND = unchecked((int)0x8002802F),
'    '        TYPE_E_BADMODULEKIND = unchecked((int)0x800288BD),
'    '--------------------------------^--- GenCode(token): unexpected token type
'    Friend Const TYPE_E_SIZETOOBIG As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TYPE_E_BADMODULEKIND = unchecked((int)0x800288BD),
'    '        TYPE_E_SIZETOOBIG = unchecked((int)0x800288C5),
'    '-----------------------------^--- GenCode(token): unexpected token type
'    Friend Const TYPE_E_DUPLICATEID As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TYPE_E_SIZETOOBIG = unchecked((int)0x800288C5),
'    '        TYPE_E_DUPLICATEID = unchecked((int)0x800288C6),
'    '------------------------------^--- GenCode(token): unexpected token type
'    Friend Const TYPE_E_INVALIDID As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TYPE_E_DUPLICATEID = unchecked((int)0x800288C6),
'    '        TYPE_E_INVALIDID = unchecked((int)0x800288CF),
'    '----------------------------^--- GenCode(token): unexpected token type
'    Friend Const TYPE_E_TYPEMISMATCH As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TYPE_E_INVALIDID = unchecked((int)0x800288CF),
'    '        TYPE_E_TYPEMISMATCH = unchecked((int)0x80028CA0),
'    '-------------------------------^--- GenCode(token): unexpected token type
'    Friend Const TYPE_E_OUTOFBOUNDS As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TYPE_E_TYPEMISMATCH = unchecked((int)0x80028CA0),
'    '        TYPE_E_OUTOFBOUNDS = unchecked((int)0x80028CA1),
'    '------------------------------^--- GenCode(token): unexpected token type
'    Friend Const TYPE_E_IOERROR As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TYPE_E_OUTOFBOUNDS = unchecked((int)0x80028CA1),
'    '        TYPE_E_IOERROR = unchecked((int)0x80028CA2),
'    '--------------------------^--- GenCode(token): unexpected token type
'    Friend Const TYPE_E_CANTCREATETMPFILE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TYPE_E_IOERROR = unchecked((int)0x80028CA2),
'    '        TYPE_E_CANTCREATETMPFILE = unchecked((int)0x80028CA3),
'    '------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const TYPE_E_CANTLOADLIBRARY As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TYPE_E_CANTCREATETMPFILE = unchecked((int)0x80028CA3),
'    '        TYPE_E_CANTLOADLIBRARY = unchecked((int)0x80029C4A),
'    '----------------------------------^--- GenCode(token): unexpected token type
'    Friend Const TYPE_E_INCONSISTENTPROPFUNCS As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TYPE_E_CANTLOADLIBRARY = unchecked((int)0x80029C4A),
'    '        TYPE_E_INCONSISTENTPROPFUNCS = unchecked((int)0x80029C83),
'    '----------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const TYPE_E_CIRCULARTYPE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TYPE_E_INCONSISTENTPROPFUNCS = unchecked((int)0x80029C83),
'    '        TYPE_E_CIRCULARTYPE = unchecked((int)0x80029C84),
'    '-------------------------------^--- GenCode(token): unexpected token type
'    Friend Const TRUST_E_PROVIDER_UNKNOWN As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TYPE_E_CIRCULARTYPE = unchecked((int)0x80029C84),
'    '        TRUST_E_PROVIDER_UNKNOWN = unchecked((int)0x800B0001),
'    '------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const TRUST_E_ACTION_UNKNOWN As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TRUST_E_PROVIDER_UNKNOWN = unchecked((int)0x800B0001),
'    '        TRUST_E_ACTION_UNKNOWN = unchecked((int)0x800B0002),
'    '----------------------------------^--- GenCode(token): unexpected token type
'    Friend Const TRUST_E_SUBJECT_FORM_UNKNOWN As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TRUST_E_ACTION_UNKNOWN = unchecked((int)0x800B0002),
'    '        TRUST_E_SUBJECT_FORM_UNKNOWN = unchecked((int)0x800B0003),
'    '----------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const TRUST_E_SUBJECT_NOT_TRUSTED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TRUST_E_SUBJECT_FORM_UNKNOWN = unchecked((int)0x800B0003),
'    '        TRUST_E_SUBJECT_NOT_TRUSTED = unchecked((int)0x800B0004),
'    '---------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const TRUST_E_NOSIGNATURE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TRUST_E_SUBJECT_NOT_TRUSTED = unchecked((int)0x800B0004),
'    '        TRUST_E_NOSIGNATURE = unchecked((int)0x800B0100),
'    '-------------------------------^--- GenCode(token): unexpected token type
'    Friend Const TA_NOUPDATECP As Integer = 0
'    Friend Const TA_UPDATECP As Integer = 1
'    Friend Const TA_LEFT As Integer = 0
'    Friend Const TA_RIGHT As Integer = 2
'    Friend Const TA_CENTER As Integer = 6
'    Friend Const TA_TOP As Integer = 0
'    Friend Const TA_BOTTOM As Integer = 8
'    Friend Const TA_BASELINE As Integer = 24
'    Friend Const TA_RTLREADING As Integer = 256
'    Friend Const TA_MASK As Integer = 24 + 6 + 1 + 256
'    ' TA_MASK = (24+6+1);
'    Friend Const TRANSFORM_CTM As Integer = 4107
'    Friend Const TCI_SRCCHARSET As Integer = 1
'    Friend Const TCI_SRCCODEPAGE As Integer = 2
'    Friend Const TCI_SRCFONTSIG As Integer = 3
'    Friend Const TMPF_FIXED_PITCH As Integer = &H1
'    Friend Const TMPF_VECTOR As Integer = &H2
'    Friend Const TMPF_DEVICE As Integer = &H8
'    Friend Const TMPF_TRUETYPE As Integer = &H4
'    Friend Const TURKISH_CHARSET As Integer = 162
'    Friend Const THAI_CHARSET As Integer = 222
'    Friend Const TRUETYPE_FONTTYPE As Integer = &H4
'    Friend Const TRANSPARENT As Integer = 1
'    Friend Const TECHNOLOGY As Integer = 2
'    Friend Const TEXTCAPS As Integer = 34
'    Friend Const TC_OP_CHARACTER As Integer = &H1
'    Friend Const TC_OP_STROKE As Integer = &H2
'    Friend Const TC_CP_STROKE As Integer = &H4
'    Friend Const TC_CR_90 As Integer = &H8
'    Friend Const TC_CR_ANY As Integer = &H10
'    Friend Const TC_SF_X_YINDEP As Integer = &H20
'    Friend Const TC_SA_DOUBLE As Integer = &H40
'    Friend Const TC_SA_INTEGER As Integer = &H80
'    Friend Const TC_SA_CONTIN As Integer = &H100
'    Friend Const TC_EA_DOUBLE As Integer = &H200
'    Friend Const TC_IA_ABLE As Integer = &H400
'    Friend Const TC_UA_ABLE As Integer = &H800
'    Friend Const TC_SO_ABLE As Integer = &H1000
'    Friend Const TC_RA_ABLE As Integer = &H2000
'    Friend Const TC_VA_ABLE As Integer = &H4000
'    Friend Const TC_RESERVED As Integer = &H8000
'    Friend Const TC_SCROLLBLT As Integer = &H10000
'    Friend Const TT_POLYGON_TYPE As Integer = 24
'    Friend Const TT_PRIM_LINE As Integer = 1
'    Friend Const TT_PRIM_QSPLINE As Integer = 2
'    Friend Const TT_AVAILABLE As Integer = &H1
'    Friend Const TT_ENABLED As Integer = &H2
'    Friend Const TIME_NOMINUTESORSECONDS As Integer = &H1
'    Friend Const TIME_NOSECONDS As Integer = &H2
'    Friend Const TIME_NOTIMEMARKER As Integer = &H4
'    Friend Const TIME_FORCE24HOURFORMAT As Integer = &H8
'    Friend Const THREAD_TERMINATE As Integer = &H1
'    Friend Const THREAD_SUSPEND_RESUME As Integer = &H2
'    Friend Const THREAD_GET_CONTEXT As Integer = &H8
'    Friend Const THREAD_SET_CONTEXT As Integer = &H10
'    Friend Const THREAD_SET_INFORMATION As Integer = &H20
'    Friend Const THREAD_QUERY_INFORMATION As Integer = &H40
'    Friend Const THREAD_SET_THREAD_TOKEN As Integer = &H80
'    Friend Const THREAD_IMPERSONATE As Integer = &H100
'    Friend Const THREAD_DIRECT_IMPERSONATION As Integer = &H200
'    Friend Const TLS_MINIMUM_AVAILABLE As Integer = 64
'    Friend Const THREAD_BASE_PRIORITY_LOWRT As Integer = 15
'    Friend Const THREAD_BASE_PRIORITY_MAX As Integer = 2
'    Friend Const THREAD_BASE_PRIORITY_MIN As Integer = - 2
'    Friend Const THREAD_BASE_PRIORITY_IDLE As Integer = - 15
'    Friend Const TIME_ZONE_ID_UNKNOWN As Integer = 0
'    Friend Const TIME_ZONE_ID_STANDARD As Integer = 1
'    Friend Const TIME_ZONE_ID_DAYLIGHT As Integer = 2
'    Friend Const TOKEN_ASSIGN_PRIMARY As Integer = &H1
'    Friend Const TOKEN_DUPLICATE As Integer = &H2
'    Friend Const TOKEN_IMPERSONATE As Integer = &H4
'    Friend Const TOKEN_QUERY As Integer = &H8
'    Friend Const TOKEN_QUERY_SOURCE As Integer = &H10
'    Friend Const TOKEN_ADJUST_PRIVILEGES As Integer = &H20
'    Friend Const TOKEN_ADJUST_GROUPS As Integer = &H40
'    Friend Const TOKEN_ADJUST_DEFAULT As Integer = &H80
'    Friend Const TOKEN_EXECUTE As Integer = &H20000
'    Friend Const TOKEN_SOURCE_LENGTH As Integer = 8
'    Friend Const TAPE_ERASE_SHORT As Integer = 0
'    Friend Const TAPE_ERASE_LONG As Integer = 1
'    Friend Const TAPE_LOAD As Integer = 0
'    Friend Const TAPE_UNLOAD As Integer = 1
'    Friend Const TAPE_TENSION As Integer = 2
'    Friend Const TAPE_LOCK As Integer = 3
'    Friend Const TAPE_UNLOCK As Integer = 4
'    Friend Const TAPE_FORMAT As Integer = 5
'    Friend Const TAPE_SETMARKS As Integer = 0
'    Friend Const TAPE_FILEMARKS As Integer = 1
'    Friend Const TAPE_SHORT_FILEMARKS As Integer = 2
'    Friend Const TAPE_LONG_FILEMARKS As Integer = 3
'    Friend Const TAPE_ABSOLUTE_POSITION As Integer = 0
'    Friend Const TAPE_LOGICAL_POSITION As Integer = 1
'    Friend Const TAPE_PSEUDO_LOGICAL_POSITION As Integer = 2
'    Friend Const TAPE_REWIND As Integer = 0
'    Friend Const TAPE_ABSOLUTE_BLOCK As Integer = 1
'    Friend Const TAPE_LOGICAL_BLOCK As Integer = 2
'    Friend Const TAPE_PSEUDO_LOGICAL_BLOCK As Integer = 3
'    Friend Const TAPE_SPACE_END_OF_DATA As Integer = 4
'    Friend Const TAPE_SPACE_RELATIVE_BLOCKS As Integer = 5
'    Friend Const TAPE_SPACE_FILEMARKS As Integer = 6
'    Friend Const TAPE_SPACE_SEQUENTIAL_FMKS As Integer = 7
'    Friend Const TAPE_SPACE_SETMARKS As Integer = 8
'    Friend Const TAPE_SPACE_SEQUENTIAL_SMKS As Integer = 9
'    Friend Const TAPE_DRIVE_FIXED As Integer = &H1
'    Friend Const TAPE_DRIVE_SELECT As Integer = &H2
'    Friend Const TAPE_DRIVE_INITIATOR As Integer = &H4
'    Friend Const TAPE_DRIVE_ERASE_SHORT As Integer = &H10
'    Friend Const TAPE_DRIVE_ERASE_LONG As Integer = &H20
'    Friend Const TAPE_DRIVE_ERASE_BOP_ONLY As Integer = &H40
'    Friend Const TAPE_DRIVE_ERASE_IMMEDIATE As Integer = &H80
'    Friend Const TAPE_DRIVE_TAPE_CAPACITY As Integer = &H100
'    Friend Const TAPE_DRIVE_TAPE_REMAINING As Integer = &H200
'    Friend Const TAPE_DRIVE_FIXED_BLOCK As Integer = &H400
'    Friend Const TAPE_DRIVE_VARIABLE_BLOCK As Integer = &H800
'    Friend Const TAPE_DRIVE_WRITE_PROTECT As Integer = &H1000
'    Friend Const TAPE_DRIVE_EOT_WZ_SIZE As Integer = &H2000
'    Friend Const TAPE_DRIVE_ECC As Integer = &H10000
'    Friend Const TAPE_DRIVE_COMPRESSION As Integer = &H20000
'    Friend Const TAPE_DRIVE_PADDING As Integer = &H40000
'    Friend Const TAPE_DRIVE_REPORT_SMKS As Integer = &H80000
'    Friend Const TAPE_DRIVE_GET_ABSOLUTE_BLK As Integer = &H100000
'    Friend Const TAPE_DRIVE_GET_LOGICAL_BLK As Integer = &H200000
'    Friend Const TAPE_DRIVE_SET_EOT_WZ_SIZE As Integer = &H400000
'    Friend Const TAPE_DRIVE_EJECT_MEDIA As Integer = &H1000000
'    Friend Const TAPE_DRIVE_RESERVED_BIT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TAPE_DRIVE_EJECT_MEDIA = 0x01000000,
'    '        TAPE_DRIVE_RESERVED_BIT = unchecked((int)0x80000000),
'    '-----------------------------------^--- GenCode(token): unexpected token type
'    Friend Const TAPE_DRIVE_LOAD_UNLOAD As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TAPE_DRIVE_RESERVED_BIT = unchecked((int)0x80000000),
'    '        TAPE_DRIVE_LOAD_UNLOAD = unchecked((int)0x80000001),
'    '----------------------------------^--- GenCode(token): unexpected token type
'    Friend Const TAPE_DRIVE_TENSION As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TAPE_DRIVE_LOAD_UNLOAD = unchecked((int)0x80000001),
'    '        TAPE_DRIVE_TENSION = unchecked((int)0x80000002),
'    '------------------------------^--- GenCode(token): unexpected token type
'    Friend Const TAPE_DRIVE_LOCK_UNLOCK As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TAPE_DRIVE_TENSION = unchecked((int)0x80000002),
'    '        TAPE_DRIVE_LOCK_UNLOCK = unchecked((int)0x80000004),
'    '----------------------------------^--- GenCode(token): unexpected token type
'    Friend Const TAPE_DRIVE_REWIND_IMMEDIATE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TAPE_DRIVE_LOCK_UNLOCK = unchecked((int)0x80000004),
'    '        TAPE_DRIVE_REWIND_IMMEDIATE = unchecked((int)0x80000008),
'    '---------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const TAPE_DRIVE_SET_BLOCK_SIZE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TAPE_DRIVE_REWIND_IMMEDIATE = unchecked((int)0x80000008),
'    '        TAPE_DRIVE_SET_BLOCK_SIZE = unchecked((int)0x80000010),
'    '-------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const TAPE_DRIVE_LOAD_UNLD_IMMED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TAPE_DRIVE_SET_BLOCK_SIZE = unchecked((int)0x80000010),
'    '        TAPE_DRIVE_LOAD_UNLD_IMMED = unchecked((int)0x80000020),
'    '--------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const TAPE_DRIVE_TENSION_IMMED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TAPE_DRIVE_LOAD_UNLD_IMMED = unchecked((int)0x80000020),
'    '        TAPE_DRIVE_TENSION_IMMED = unchecked((int)0x80000040),
'    '------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const TAPE_DRIVE_LOCK_UNLK_IMMED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TAPE_DRIVE_TENSION_IMMED = unchecked((int)0x80000040),
'    '        TAPE_DRIVE_LOCK_UNLK_IMMED = unchecked((int)0x80000080),
'    '--------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const TAPE_DRIVE_SET_ECC As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TAPE_DRIVE_LOCK_UNLK_IMMED = unchecked((int)0x80000080),
'    '        TAPE_DRIVE_SET_ECC = unchecked((int)0x80000100),
'    '------------------------------^--- GenCode(token): unexpected token type
'    Friend Const TAPE_DRIVE_SET_COMPRESSION As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TAPE_DRIVE_SET_ECC = unchecked((int)0x80000100),
'    '        TAPE_DRIVE_SET_COMPRESSION = unchecked((int)0x80000200),
'    '--------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const TAPE_DRIVE_SET_PADDING As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TAPE_DRIVE_SET_COMPRESSION = unchecked((int)0x80000200),
'    '        TAPE_DRIVE_SET_PADDING = unchecked((int)0x80000400),
'    '----------------------------------^--- GenCode(token): unexpected token type
'    Friend Const TAPE_DRIVE_SET_REPORT_SMKS As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TAPE_DRIVE_SET_PADDING = unchecked((int)0x80000400),
'    '        TAPE_DRIVE_SET_REPORT_SMKS = unchecked((int)0x80000800),
'    '--------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const TAPE_DRIVE_ABSOLUTE_BLK As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TAPE_DRIVE_SET_REPORT_SMKS = unchecked((int)0x80000800),
'    '        TAPE_DRIVE_ABSOLUTE_BLK = unchecked((int)0x80001000),
'    '-----------------------------------^--- GenCode(token): unexpected token type
'    Friend Const TAPE_DRIVE_ABS_BLK_IMMED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TAPE_DRIVE_ABSOLUTE_BLK = unchecked((int)0x80001000),
'    '        TAPE_DRIVE_ABS_BLK_IMMED = unchecked((int)0x80002000),
'    '------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const TAPE_DRIVE_LOGICAL_BLK As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TAPE_DRIVE_ABS_BLK_IMMED = unchecked((int)0x80002000),
'    '        TAPE_DRIVE_LOGICAL_BLK = unchecked((int)0x80004000),
'    '----------------------------------^--- GenCode(token): unexpected token type
'    Friend Const TAPE_DRIVE_LOG_BLK_IMMED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TAPE_DRIVE_LOGICAL_BLK = unchecked((int)0x80004000),
'    '        TAPE_DRIVE_LOG_BLK_IMMED = unchecked((int)0x80008000),
'    '------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const TAPE_DRIVE_END_OF_DATA As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TAPE_DRIVE_LOG_BLK_IMMED = unchecked((int)0x80008000),
'    '        TAPE_DRIVE_END_OF_DATA = unchecked((int)0x80010000),
'    '----------------------------------^--- GenCode(token): unexpected token type
'    Friend Const TAPE_DRIVE_RELATIVE_BLKS As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TAPE_DRIVE_END_OF_DATA = unchecked((int)0x80010000),
'    '        TAPE_DRIVE_RELATIVE_BLKS = unchecked((int)0x80020000),
'    '------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const TAPE_DRIVE_FILEMARKS As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TAPE_DRIVE_RELATIVE_BLKS = unchecked((int)0x80020000),
'    '        TAPE_DRIVE_FILEMARKS = unchecked((int)0x80040000),
'    '--------------------------------^--- GenCode(token): unexpected token type
'    Friend Const TAPE_DRIVE_SEQUENTIAL_FMKS As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TAPE_DRIVE_FILEMARKS = unchecked((int)0x80040000),
'    '        TAPE_DRIVE_SEQUENTIAL_FMKS = unchecked((int)0x80080000),
'    '--------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const TAPE_DRIVE_SETMARKS As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TAPE_DRIVE_SEQUENTIAL_FMKS = unchecked((int)0x80080000),
'    '        TAPE_DRIVE_SETMARKS = unchecked((int)0x80100000),
'    '-------------------------------^--- GenCode(token): unexpected token type
'    Friend Const TAPE_DRIVE_SEQUENTIAL_SMKS As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TAPE_DRIVE_SETMARKS = unchecked((int)0x80100000),
'    '        TAPE_DRIVE_SEQUENTIAL_SMKS = unchecked((int)0x80200000),
'    '--------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const TAPE_DRIVE_REVERSE_POSITION As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TAPE_DRIVE_SEQUENTIAL_SMKS = unchecked((int)0x80200000),
'    '        TAPE_DRIVE_REVERSE_POSITION = unchecked((int)0x80400000),
'    '---------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const TAPE_DRIVE_SPACE_IMMEDIATE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TAPE_DRIVE_REVERSE_POSITION = unchecked((int)0x80400000),
'    '        TAPE_DRIVE_SPACE_IMMEDIATE = unchecked((int)0x80800000),
'    '--------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const TAPE_DRIVE_WRITE_SETMARKS As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TAPE_DRIVE_SPACE_IMMEDIATE = unchecked((int)0x80800000),
'    '        TAPE_DRIVE_WRITE_SETMARKS = unchecked((int)0x81000000),
'    '-------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const TAPE_DRIVE_WRITE_FILEMARKS As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TAPE_DRIVE_WRITE_SETMARKS = unchecked((int)0x81000000),
'    '        TAPE_DRIVE_WRITE_FILEMARKS = unchecked((int)0x82000000),
'    '--------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const TAPE_DRIVE_WRITE_SHORT_FMKS As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TAPE_DRIVE_WRITE_FILEMARKS = unchecked((int)0x82000000),
'    '        TAPE_DRIVE_WRITE_SHORT_FMKS = unchecked((int)0x84000000),
'    '---------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const TAPE_DRIVE_WRITE_LONG_FMKS As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TAPE_DRIVE_WRITE_SHORT_FMKS = unchecked((int)0x84000000),
'    '        TAPE_DRIVE_WRITE_LONG_FMKS = unchecked((int)0x88000000),
'    '--------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const TAPE_DRIVE_WRITE_MARK_IMMED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TAPE_DRIVE_WRITE_LONG_FMKS = unchecked((int)0x88000000),
'    '        TAPE_DRIVE_WRITE_MARK_IMMED = unchecked((int)0x90000000),
'    '---------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const TAPE_DRIVE_FORMAT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TAPE_DRIVE_WRITE_MARK_IMMED = unchecked((int)0x90000000),
'    '        TAPE_DRIVE_FORMAT = unchecked((int)0xA0000000),
'    '-----------------------------^--- GenCode(token): unexpected token type
'    Friend Const TAPE_DRIVE_FORMAT_IMMEDIATE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TAPE_DRIVE_FORMAT = unchecked((int)0xA0000000),
'    '        TAPE_DRIVE_FORMAT_IMMEDIATE = unchecked((int)0xC0000000),
'    '---------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const TAPE_DRIVE_HIGH_FEATURES As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TAPE_DRIVE_FORMAT_IMMEDIATE = unchecked((int)0xC0000000),
'    '        TAPE_DRIVE_HIGH_FEATURES = unchecked((int)0x80000000),
'    '------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const TAPE_FIXED_PARTITIONS As Integer = 0
'    Friend Const TAPE_SELECT_PARTITIONS As Integer = 1
'    Friend Const TAPE_INITIATOR_PARTITIONS As Integer = 2
'    Friend Const TME_HOVER As Integer = &H1
'    Friend Const TME_LEAVE As Integer = &H2
'    Friend Const TME_QUERY As Integer = &H40000000
'    Friend Const TME_CANCEL As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TME_QUERY = 0x40000000,
'    '        TME_CANCEL = unchecked((int)0x80000000),
'    '----------------------^--- GenCode(token): unexpected token type
'    Friend Const TPM_LEFTBUTTON As Integer = &H0
'    Friend Const TPM_RIGHTBUTTON As Integer = &H2
'    Friend Const TPM_LEFTALIGN As Integer = &H0
'    Friend Const TPM_CENTERALIGN As Integer = &H4
'    Friend Const TPM_RIGHTALIGN As Integer = &H8
'    Friend Const TPM_TOPALIGN As Integer = &H0
'    Friend Const TPM_VCENTERALIGN As Integer = &H10
'    Friend Const TPM_BOTTOMALIGN As Integer = &H20
'    Friend Const TPM_HORIZONTAL As Integer = &H0
'    Friend Const TPM_VERTICAL As Integer = &H40
'    Friend Const TPM_NONOTIFY As Integer = &H80
'    Friend Const TPM_RETURNCMD As Integer = &H100
'    Friend Const TKF_TOGGLEKEYSON As Integer = &H1
'    Friend Const TKF_AVAILABLE As Integer = &H2
'    Friend Const TKF_HOTKEYACTIVE As Integer = &H4
'    Friend Const TKF_CONFIRMHOTKEY As Integer = &H8
'    Friend Const TKF_HOTKEYSOUND As Integer = &H10
'    Friend Const TKF_INDICATOR As Integer = &H20
'    Friend Const TV_FIRST As Integer = &H1100
'    Friend Const TVN_FIRST As Integer = 0 - 400
'    Friend Const TVN_LAST As Integer = 0 - 499
'    Friend Const TTN_FIRST As Integer = 0 - 520
'    Friend Const TTN_LAST As Integer = 0 - 549
'    Friend Const TCN_FIRST As Integer = 0 - 550
'    Friend Const TCN_LAST As Integer = 0 - 580
'    Friend Const TBN_FIRST As Integer = 0 - 700
'    Friend Const TBN_LAST As Integer = 0 - 720
'    Friend Const TBSTATE_CHECKED As Integer = &H1
'    Friend Const TBSTATE_PRESSED As Integer = &H2
'    Friend Const TBSTATE_ENABLED As Integer = &H4
'    Friend Const TBSTATE_HIDDEN As Integer = &H8
'    Friend Const TBSTATE_INDETERMINATE As Integer = &H10
'    Friend Const TBSTATE_WRAP As Integer = &H20
'    Friend Const TBSTATE_ELLIPSES As Integer = &H40
'    Friend Const TBSTATE_MARKED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TBSTATE_ELLIPSES = 0x40,
'    '        TBSTATE_MARKED = unchecked((int)0x80),
'    '--------------------------^--- GenCode(token): unexpected token type
'    Friend Const TBSTYLE_BUTTON As Integer = &H0
'    Friend Const TBSTYLE_SEP As Integer = &H1
'    Friend Const TBSTYLE_CHECK As Integer = &H2
'    Friend Const TBSTYLE_GROUP As Integer = &H4
'    Friend Const TBSTYLE_CHECKGROUP As Integer = &H4 Or &H2
'    Friend Const TBSTYLE_DROPDOWN As Integer = &H8
'    Friend Const TBSTYLE_AUTOSIZE As Integer = &H10
'    Friend Const TBSTYLE_NOPREFIX As Integer = &H20
'    Friend Const TBSTYLE_TOOLTIPS As Integer = &H100
'    Friend Const TBSTYLE_WRAPABLE As Integer = &H200
'    Friend Const TBSTYLE_ALTDRAG As Integer = &H400
'    Friend Const TBSTYLE_FLAT As Integer = &H800
'    Friend Const TBSTYLE_LIST As Integer = &H1000
'    Friend Const TBSTYLE_CUSTOMERASE As Integer = &H2000
'    Friend Const TBSTYLE_REGISTERDROP As Integer = &H4000
'    Friend Const TBSTYLE_TRANSPARENT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TBSTYLE_REGISTERDROP = 0x4000,
'    '        TBSTYLE_TRANSPARENT = unchecked((int)0x8000),
'    '-------------------------------^--- GenCode(token): unexpected token type
'    Friend Const TBSTYLE_EX_DRAWDDARROWS As Integer = &H1
'    Friend Const TB_ENABLEBUTTON As Integer = &H400 + 1
'    Friend Const TB_CHECKBUTTON As Integer = &H400 + 2
'    Friend Const TB_PRESSBUTTON As Integer = &H400 + 3
'    Friend Const TB_HIDEBUTTON As Integer = &H400 + 4
'    Friend Const TB_INDETERMINATE As Integer = &H400 + 5
'    Friend Const TB_MARKBUTTON As Integer = &H400 + 6
'    Friend Const TB_ISBUTTONENABLED As Integer = &H400 + 9
'    Friend Const TB_ISBUTTONCHECKED As Integer = &H400 + 10
'    Friend Const TB_ISBUTTONPRESSED As Integer = &H400 + 11
'    Friend Const TB_ISBUTTONHIDDEN As Integer = &H400 + 12
'    Friend Const TB_ISBUTTONINDETERMINATE As Integer = &H400 + 13
'    Friend Const TB_ISBUTTONHIGHLIGHTED As Integer = &H400 + 14
'    Friend Const TB_SETSTATE As Integer = &H400 + 17
'    Friend Const TB_GETSTATE As Integer = &H400 + 18
'    Friend Const TB_ADDBITMAP As Integer = &H400 + 19
'    Friend Const TB_ADDBUTTONSA As Integer = &H400 + 20
'    Friend Const TB_ADDBUTTONSW As Integer = &H400 + 68
'    Friend Const TB_INSERTBUTTONA As Integer = &H400 + 21
'    Friend Const TB_INSERTBUTTONW As Integer = &H400 + 67
'    Friend Const TB_DELETEBUTTON As Integer = &H400 + 22
'    Friend Const TB_GETBUTTON As Integer = &H400 + 23
'    Friend Const TB_BUTTONCOUNT As Integer = &H400 + 24
'    Friend Const TB_COMMANDTOINDEX As Integer = &H400 + 25
'    Friend Const TB_SAVERESTOREA As Integer = &H400 + 26
'    Friend Const TB_SAVERESTOREW As Integer = &H400 + 76
'    Friend Const TB_CUSTOMIZE As Integer = &H400 + 27
'    Friend Const TB_ADDSTRINGA As Integer = &H400 + 28
'    Friend Const TB_ADDSTRINGW As Integer = &H400 + 77
'    Friend Const TB_GETITEMRECT As Integer = &H400 + 29
'    Friend Const TB_BUTTONSTRUCTSIZE As Integer = &H400 + 30
'    Friend Const TB_SETBUTTONSIZE As Integer = &H400 + 31
'    Friend Const TB_SETBITMAPSIZE As Integer = &H400 + 32
'    Friend Const TB_AUTOSIZE As Integer = &H400 + 33
'    Friend Const TB_GETTOOLTIPS As Integer = &H400 + 35
'    Friend Const TB_SETTOOLTIPS As Integer = &H400 + 36
'    Friend Const TB_SETPARENT As Integer = &H400 + 37
'    Friend Const TB_SETROWS As Integer = &H400 + 39
'    Friend Const TB_GETROWS As Integer = &H400 + 40
'    Friend Const TB_GETBITMAPFLAGS As Integer = &H400 + 41
'    Friend Const TB_SETCMDID As Integer = &H400 + 42
'    Friend Const TB_CHANGEBITMAP As Integer = &H400 + 43
'    Friend Const TB_GETBITMAP As Integer = &H400 + 44
'    Friend Const TB_GETBUTTONTEXTA As Integer = &H400 + 45
'    Friend Const TB_GETBUTTONTEXTW As Integer = &H400 + 75
'    Friend Const TB_REPLACEBITMAP As Integer = &H400 + 46
'    Friend Const TB_SETINDENT As Integer = &H400 + 47
'    Friend Const TB_SETIMAGELIST As Integer = &H400 + 48
'    Friend Const TB_GETIMAGELIST As Integer = &H400 + 49
'    Friend Const TB_LOADIMAGES As Integer = &H400 + 50
'    Friend Const TB_GETRECT As Integer = &H400 + 51
'    Friend Const TB_SETHOTIMAGELIST As Integer = &H400 + 52
'    Friend Const TB_GETHOTIMAGELIST As Integer = &H400 + 53
'    Friend Const TB_SETDISABLEDIMAGELIST As Integer = &H400 + 54
'    Friend Const TB_GETDISABLEDIMAGELIST As Integer = &H400 + 55
'    Friend Const TB_SETSTYLE As Integer = &H400 + 56
'    Friend Const TB_GETSTYLE As Integer = &H400 + 57
'    Friend Const TB_GETBUTTONSIZE As Integer = &H400 + 58
'    Friend Const TB_SETBUTTONWIDTH As Integer = &H400 + 59
'    Friend Const TB_SETMAXTEXTROWS As Integer = &H400 + 60
'    Friend Const TB_GETTEXTROWS As Integer = &H400 + 61
'    Friend Const TB_GETOBJECT As Integer = &H400 + 62
'    Friend Const TB_GETBUTTONINFOW As Integer = &H400 + 63
'    Friend Const TB_SETBUTTONINFOW As Integer = &H400 + 64
'    Friend Const TB_GETBUTTONINFOA As Integer = &H400 + 65
'    Friend Const TB_SETBUTTONINFOA As Integer = &H400 + 66
'    Friend Const TB_HITTEST As Integer = &H400 + 69
'    Friend Const TB_GETHOTITEM As Integer = &H400 + 71
'    Friend Const TB_SETHOTITEM As Integer = &H400 + 72
'    Friend Const TB_SETANCHORHIGHLIGHT As Integer = &H400 + 73
'    Friend Const TB_GETANCHORHIGHLIGHT As Integer = &H400 + 74
'    Friend Const TB_MAPACCELERATORA As Integer = &H400 + 78
'    Friend Const TB_GETINSERTMARK As Integer = &H400 + 79
'    Friend Const TB_SETINSERTMARK As Integer = &H400 + 80
'    Friend Const TB_INSERTMARKHITTEST As Integer = &H400 + 81
'    Friend Const TB_MOVEBUTTON As Integer = &H400 + 82
'    Friend Const TB_GETMAXSIZE As Integer = &H400 + 83
'    Friend Const TB_SETEXTENDEDSTYLE As Integer = &H400 + 84
'    Friend Const TB_GETEXTENDEDSTYLE As Integer = &H400 + 85
'    Friend Const TB_GETPADDING As Integer = &H400 + 86
'    Friend Const TB_SETPADDING As Integer = &H400 + 87
'    Friend Const TB_SETINSERTMARKCOLOR As Integer = &H400 + 88
'    Friend Const TB_GETINSERTMARKCOLOR As Integer = &H400 + 89
'    Friend Const TB_MAPACCELERATORW As Integer = &H400 + 90
'    Friend Const TB_SETCOLORSCHEME As Integer = win.CCM_SETCOLORSCHEME
'    Friend Const TB_GETCOLORSCHEME As Integer = win.CCM_GETCOLORSCHEME
'    Friend Const TB_SETUNICODEFORMAT As Integer = win.CCM_SETUNICODEFORMAT
'    Friend Const TB_GETUNICODEFORMAT As Integer = win.CCM_GETUNICODEFORMAT
'    Friend Const TBIMHT_AFTER As Integer = &H1
'    Friend Const TBIMHT_BACKGROUND As Integer = &H2
'    Friend Const TBIF_IMAGE As Integer = &H1
'    Friend Const TBIF_TEXT As Integer = &H2
'    Friend Const TBIF_STATE As Integer = &H4
'    Friend Const TBIF_STYLE As Integer = &H8
'    Friend Const TBIF_LPARAM As Integer = &H10
'    Friend Const TBIF_COMMAND As Integer = &H20
'    Friend Const TBIF_SIZE As Integer = &H40
'    Friend Const TBBF_LARGE As Integer = &H1
'    Friend Const TBN_GETBUTTONINFOA As Integer = 0 - 700 - 0
'    Friend Const TBN_GETBUTTONINFOW As Integer = 0 - 700 - 20
'    Friend Const TBN_BEGINDRAG As Integer = 0 - 700 - 1
'    Friend Const TBN_ENDDRAG As Integer = 0 - 700 - 2
'    Friend Const TBN_BEGINADJUST As Integer = 0 - 700 - 3
'    Friend Const TBN_ENDADJUST As Integer = 0 - 700 - 4
'    Friend Const TBN_RESET As Integer = 0 - 700 - 5
'    Friend Const TBN_QUERYINSERT As Integer = 0 - 700 - 6
'    Friend Const TBN_QUERYDELETE As Integer = 0 - 700 - 7
'    Friend Const TBN_TOOLBARCHANGE As Integer = 0 - 700 - 8
'    Friend Const TBN_CUSTHELP As Integer = 0 - 700 - 9
'    Friend Const TBN_DROPDOWN As Integer = 0 - 700 - 10
'    Friend Const TBN_CLOSEUP As Integer = 0 - 700 - 11
'    Friend Const TBN_GETOBJECT As Integer = 0 - 700 - 12
'    Friend Const TBN_HOTITEMCHANGE As Integer = 0 - 700 - 13
'    Friend Const TBN_DRAGOUT As Integer = 0 - 700 - 14
'    Friend Const TBN_DELETINGBUTTON As Integer = 0 - 700 - 15
'    Friend Const TBN_GETDISPINFOA As Integer = 0 - 700 - 16
'    Friend Const TBN_GETDISPINFOW As Integer = 0 - 700 - 17
'    Friend Const TBN_GETINFOTIPA As Integer = 0 - 700 - 18
'    Friend Const TBN_GETINFOTIPW As Integer = 0 - 700 - 19
'    Friend Const TTS_ALWAYSTIP As Integer = &H1
'    Friend Const TTS_NOPREFIX As Integer = &H2
'    Friend Const TTF_IDISHWND As Integer = &H1
'    Friend Const TTF_CENTERTIP As Integer = &H2
'    Friend Const TTF_RTLREADING As Integer = &H4
'    Friend Const TTF_SUBCLASS As Integer = &H10
'    Friend Const TTF_TRACK As Integer = &H20
'    Friend Const TTF_ABSOLUTE As Integer = &H80
'    Friend Const TTF_TRANSPARENT As Integer = &H100
'    Friend Const TTF_DI_SETITEM As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TTF_TRANSPARENT = 0x0100,
'    '        TTF_DI_SETITEM = unchecked((int)0x8000),
'    '--------------------------^--- GenCode(token): unexpected token type
'    Friend Const TTDT_AUTOMATIC As Integer = 0
'    Friend Const TTDT_RESHOW As Integer = 1
'    Friend Const TTDT_AUTOPOP As Integer = 2
'    Friend Const TTDT_INITIAL As Integer = 3
'    Friend Const TTM_ACTIVATE As Integer = &H400 + 1
'    Friend Const TTM_ADJUSTRECT As Integer = &H400 + 31
'    Friend Const TTM_SETDELAYTIME As Integer = &H400 + 3
'    Friend Const TTM_ADDTOOLA As Integer = &H400 + 4
'    Friend Const TTM_ADDTOOLW As Integer = &H400 + 50
'    Friend Const TTM_DELTOOLA As Integer = &H400 + 5
'    Friend Const TTM_DELTOOLW As Integer = &H400 + 51
'    Friend Const TTM_NEWTOOLRECTA As Integer = &H400 + 6
'    Friend Const TTM_NEWTOOLRECTW As Integer = &H400 + 52
'    Friend Const TTM_RELAYEVENT As Integer = &H400 + 7
'    Friend Const TTM_GETTOOLINFOA As Integer = &H400 + 8
'    Friend Const TTM_GETTOOLINFOW As Integer = &H400 + 53
'    Friend Const TTM_SETTOOLINFOA As Integer = &H400 + 9
'    Friend Const TTM_SETTOOLINFOW As Integer = &H400 + 54
'    Friend Const TTM_HITTESTA As Integer = &H400 + 10
'    Friend Const TTM_HITTESTW As Integer = &H400 + 55
'    Friend Const TTM_GETTEXTA As Integer = &H400 + 11
'    Friend Const TTM_GETTEXTW As Integer = &H400 + 56
'    Friend Const TTM_UPDATE As Integer = &H400 + 29
'    Friend Const TTM_UPDATETIPTEXTA As Integer = &H400 + 12
'    Friend Const TTM_UPDATETIPTEXTW As Integer = &H400 + 57
'    Friend Const TTM_GETTOOLCOUNT As Integer = &H400 + 13
'    Friend Const TTM_ENUMTOOLSA As Integer = &H400 + 14
'    Friend Const TTM_ENUMTOOLSW As Integer = &H400 + 58
'    Friend Const TTM_GETCURRENTTOOLA As Integer = &H400 + 15
'    Friend Const TTM_GETCURRENTTOOLW As Integer = &H400 + 59
'    Friend Const TTM_WINDOWFROMPOINT As Integer = &H400 + 16
'    Friend Const TTM_TRACKACTIVATE As Integer = &H400 + 17
'    Friend Const TTM_TRACKPOSITION As Integer = &H400 + 18
'    Friend Const TTM_SETTIPBKCOLOR As Integer = &H400 + 19
'    Friend Const TTM_SETTIPTEXTCOLOR As Integer = &H400 + 20
'    Friend Const TTM_GETDELAYTIME As Integer = &H400 + 21
'    Friend Const TTM_GETTIPBKCOLOR As Integer = &H400 + 22
'    Friend Const TTM_GETTIPTEXTCOLOR As Integer = &H400 + 23
'    Friend Const TTM_SETMAXTIPWIDTH As Integer = &H400 + 24
'    Friend Const TTM_GETMAXTIPWIDTH As Integer = &H400 + 25
'    Friend Const TTM_SETMARGIN As Integer = &H400 + 26
'    Friend Const TTM_GETMARGIN As Integer = &H400 + 27
'    Friend Const TTM_POP As Integer = &H400 + 28
'    Friend Const TTN_GETDISPINFOA As Integer = 0 - 520 - 0
'    Friend Const TTN_GETDISPINFOW As Integer = 0 - 520 - 10
'    Friend Const TTN_SHOW As Integer = 0 - 520 - 1
'    Friend Const TTN_POP As Integer = 0 - 520 - 2
'    Friend Const TTN_NEEDTEXTA As Integer = 0 - 520 - 0
'    Friend Const TTN_NEEDTEXTW As Integer = 0 - 520 - 10
'    Friend Const TBS_AUTOTICKS As Integer = &H1
'    Friend Const TBS_VERT As Integer = &H2
'    Friend Const TBS_HORZ As Integer = &H0
'    Friend Const TBS_TOP As Integer = &H4
'    Friend Const TBS_BOTTOM As Integer = &H0
'    Friend Const TBS_LEFT As Integer = &H4
'    Friend Const TBS_RIGHT As Integer = &H0
'    Friend Const TBS_BOTH As Integer = &H8
'    Friend Const TBS_NOTICKS As Integer = &H10
'    Friend Const TBS_ENABLESELRANGE As Integer = &H20
'    Friend Const TBS_FIXEDLENGTH As Integer = &H40
'    Friend Const TBS_NOTHUMB As Integer = &H80
'    Friend Const TBS_TOOLTIPS As Integer = &H100
'    Friend Const TBM_GETPOS As Integer = &H400
'    Friend Const TBM_GETRANGEMIN As Integer = &H400 + 1
'    Friend Const TBM_GETRANGEMAX As Integer = &H400 + 2
'    Friend Const TBM_GETTIC As Integer = &H400 + 3
'    Friend Const TBM_SETTIC As Integer = &H400 + 4
'    Friend Const TBM_SETPOS As Integer = &H400 + 5
'    Friend Const TBM_SETRANGE As Integer = &H400 + 6
'    Friend Const TBM_SETRANGEMIN As Integer = &H400 + 7
'    Friend Const TBM_SETRANGEMAX As Integer = &H400 + 8
'    Friend Const TBM_CLEARTICS As Integer = &H400 + 9
'    Friend Const TBM_SETSEL As Integer = &H400 + 10
'    Friend Const TBM_SETSELSTART As Integer = &H400 + 11
'    Friend Const TBM_SETSELEND As Integer = &H400 + 12
'    Friend Const TBM_GETPTICS As Integer = &H400 + 14
'    Friend Const TBM_GETTICPOS As Integer = &H400 + 15
'    Friend Const TBM_GETNUMTICS As Integer = &H400 + 16
'    Friend Const TBM_GETSELSTART As Integer = &H400 + 17
'    Friend Const TBM_GETSELEND As Integer = &H400 + 18
'    Friend Const TBM_CLEARSEL As Integer = &H400 + 19
'    Friend Const TBM_SETTICFREQ As Integer = &H400 + 20
'    Friend Const TBM_SETPAGESIZE As Integer = &H400 + 21
'    Friend Const TBM_GETPAGESIZE As Integer = &H400 + 22
'    Friend Const TBM_SETLINESIZE As Integer = &H400 + 23
'    Friend Const TBM_GETLINESIZE As Integer = &H400 + 24
'    Friend Const TBM_GETTHUMBRECT As Integer = &H400 + 25
'    Friend Const TBM_GETCHANNELRECT As Integer = &H400 + 26
'    Friend Const TBM_SETTHUMBLENGTH As Integer = &H400 + 27
'    Friend Const TBM_GETTHUMBLENGTH As Integer = &H400 + 28
'    Friend Const TBM_SETTOOLTIPS As Integer = &H400 + 29
'    Friend Const TBM_GETTOOLTIPS As Integer = &H400 + 30
'    Friend Const TBM_SETTIPSIDE As Integer = &H400 + 31
'    Friend Const TBTS_TOP As Integer = 0
'    Friend Const TBTS_LEFT As Integer = 1
'    Friend Const TBTS_BOTTOM As Integer = 2
'    Friend Const TBTS_RIGHT As Integer = 3
'    Friend Const TBM_SETBUDDY As Integer = &H400 + 32
'    Friend Const TBM_GETBUDDY As Integer = &H400 + 33
'    Friend Const TB_LINEUP As Integer = 0
'    Friend Const TB_LINEDOWN As Integer = 1
'    Friend Const TB_PAGEUP As Integer = 2
'    Friend Const TB_PAGEDOWN As Integer = 3
'    Friend Const TB_THUMBPOSITION As Integer = 4
'    Friend Const TB_THUMBTRACK As Integer = 5
'    Friend Const TB_TOP As Integer = 6
'    Friend Const TB_BOTTOM As Integer = 7
'    Friend Const TB_ENDTRACK As Integer = 8
'    Friend Const TBCD_TICS As Integer = &H1
'    Friend Const TBCD_THUMB As Integer = &H2
'    Friend Const TBCD_CHANNEL As Integer = &H3
'    Friend Const TBCDRF_NOEDGES As Integer = &H10000
'    Friend Const TBCDRF_HILITEHOTTRACK As Integer = &H20000
'    Friend Const TBCDRF_NOOFFSET As Integer = &H40000
'    Friend Const TBCDRF_NOMARK As Integer = &H80000
'    Friend Const TBCDRF_NOETCHEDEFFECT As Integer = &H100000
'    Friend Const TVS_HASBUTTONS As Integer = &H1
'    Friend Const TVS_HASLINES As Integer = &H2
'    Friend Const TVS_LINESATROOT As Integer = &H4
'    Friend Const TVS_EDITLABELS As Integer = &H8
'    Friend Const TVS_DISABLEDRAGDROP As Integer = &H10
'    Friend Const TVS_SHOWSELALWAYS As Integer = &H20
'    Friend Const TVS_RTLREADING As Integer = &H40
'    Friend Const TVS_NOTOOLTIPS As Integer = &H80
'    Friend Const TVS_CHECKBOXES As Integer = &H100
'    Friend Const TVS_TRACKSELECT As Integer = &H200
'    Friend Const TVS_SHAREDIMAGELISTS As Integer = &H0
'    Friend Const TVS_PRIVATEIMAGELISTS As Integer = &H400
'    Friend Const TVS_FULLROWSELECT As Integer = &H1000
'    Friend Const TVIF_TEXT As Integer = &H1
'    Friend Const TVIF_IMAGE As Integer = &H2
'    Friend Const TVIF_PARAM As Integer = &H4
        Friend Const TVIF_STATE As Integer = &H8
'    Friend Const TVIF_HANDLE As Integer = &H10
'    Friend Const TVIF_SELECTEDIMAGE As Integer = &H20
'    Friend Const TVIF_CHILDREN As Integer = &H40
'    Friend Const TVIS_SELECTED As Integer = &H2
'    Friend Const TVIS_CUT As Integer = &H4
'    Friend Const TVIS_DROPHILITED As Integer = &H8
'    Friend Const TVIS_BOLD As Integer = &H10
'    Friend Const TVIS_EXPANDED As Integer = &H20
'    Friend Const TVIS_EXPANDEDONCE As Integer = &H40
'    Friend Const TVIS_EXPANDPARTIAL As Integer = &H80
'    Friend Const TVIS_OVERLAYMASK As Integer = &HF00
        Friend Const TVIS_STATEIMAGEMASK As Integer = &HF000
'    Friend Const TVIS_USERMASK As Integer = &HF000
'    Friend Const TVI_ROOT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TVIS_USERMASK = 0xF000,
'    '        TVI_ROOT = (unchecked((int)0xFFFF0000)),
'    '---------------------^--- GenCode(token): unexpected token type
'    Friend Const TVI_FIRST As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TVI_ROOT = (unchecked((int)0xFFFF0000)),
'    '                   TVI_FIRST = (unchecked((int)0xFFFF0001)),
'    '---------------------------------^--- GenCode(token): unexpected token type
'    Friend Const TVI_LAST As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                   TVI_FIRST = (unchecked((int)0xFFFF0001)),
'    '                               TVI_LAST = (unchecked((int)0xFFFF0002)),
'    '--------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const TVI_SORT As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                               TVI_LAST = (unchecked((int)0xFFFF0002)),
'    '                                          TVI_SORT = (unchecked((int)0xFFFF0003)),
'    '-------------------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const TVM_INSERTITEMA As Integer = &H1100 + 0
'    Friend Const TVM_INSERTITEMW As Integer = &H1100 + 50
'    Friend Const TVM_DELETEITEM As Integer = &H1100 + 1
'    Friend Const TVM_EXPAND As Integer = &H1100 + 2
'    Friend Const TVE_COLLAPSE As Integer = &H1
'    Friend Const TVE_EXPAND As Integer = &H2
'    Friend Const TVE_TOGGLE As Integer = &H3
'    Friend Const TVE_EXPANDPARTIAL As Integer = &H4000
'    Friend Const TVE_COLLAPSERESET As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TVE_EXPANDPARTIAL = 0x4000,
'    '        TVE_COLLAPSERESET = unchecked((int)0x8000),
'    '-----------------------------^--- GenCode(token): unexpected token type
'    Friend Const TVM_GETITEMRECT As Integer = &H1100 + 4
'    Friend Const TVM_GETCOUNT As Integer = &H1100 + 5
'    Friend Const TVM_GETINDENT As Integer = &H1100 + 6
'    Friend Const TVM_SETINDENT As Integer = &H1100 + 7
'    Friend Const TVM_GETIMAGELIST As Integer = &H1100 + 8
'    Friend Const TVSIL_NORMAL As Integer = 0
'    Friend Const TVSIL_STATE As Integer = 2
'    Friend Const TVM_SETIMAGELIST As Integer = &H1100 + 9
'    Friend Const TVM_GETNEXTITEM As Integer = &H1100 + 10
'    Friend Const TVGN_ROOT As Integer = &H0
'    Friend Const TVGN_NEXT As Integer = &H1
'    Friend Const TVGN_PREVIOUS As Integer = &H2
'    Friend Const TVGN_PARENT As Integer = &H3
'    Friend Const TVGN_CHILD As Integer = &H4
'    Friend Const TVGN_FIRSTVISIBLE As Integer = &H5
'    Friend Const TVGN_NEXTVISIBLE As Integer = &H6
'    Friend Const TVGN_PREVIOUSVISIBLE As Integer = &H7
'    Friend Const TVGN_DROPHILITE As Integer = &H8
'    Friend Const TVGN_CARET As Integer = &H9
'    Friend Const TVM_SELECTITEM As Integer = &H1100 + 11
'    Friend Const TVM_GETITEMA As Integer = &H1100 + 12
'    Friend Const TVM_GETITEMW As Integer = &H1100 + 62
        Friend Const TVM_SETITEMA As Integer = &H1100 + 13
'    Friend Const TVM_SETITEMW As Integer = &H1100 + 63
'    Friend Const TVM_EDITLABELA As Integer = &H1100 + 14
'    Friend Const TVM_EDITLABELW As Integer = &H1100 + 65
'    Friend Const TVM_GETEDITCONTROL As Integer = &H1100 + 15
'    Friend Const TVM_GETVISIBLECOUNT As Integer = &H1100 + 16
'    Friend Const TVM_HITTEST As Integer = &H1100 + 17
'    Friend Const TVHT_NOWHERE As Integer = &H1
'    Friend Const TVHT_ONITEMICON As Integer = &H2
'    Friend Const TVHT_ONITEMLABEL As Integer = &H4
'    Friend Const TVHT_ONITEMINDENT As Integer = &H8
'    Friend Const TVHT_ONITEMBUTTON As Integer = &H10
'    Friend Const TVHT_ONITEMRIGHT As Integer = &H20
'    Friend Const TVHT_ONITEMSTATEICON As Integer = &H40
'    Friend Const TVHT_ABOVE As Integer = &H100
'    Friend Const TVHT_BELOW As Integer = &H200
'    Friend Const TVHT_TORIGHT As Integer = &H400
'    Friend Const TVHT_TOLEFT As Integer = &H800
'    Friend Const TVM_CREATEDRAGIMAGE As Integer = &H1100 + 18
'    Friend Const TVM_SORTCHILDREN As Integer = &H1100 + 19
'    Friend Const TVM_ENSUREVISIBLE As Integer = &H1100 + 20
'    Friend Const TVM_SORTCHILDRENCB As Integer = &H1100 + 21
'    Friend Const TVM_ENDEDITLABELNOW As Integer = &H1100 + 22
'    Friend Const TVM_GETISEARCHSTRINGA As Integer = &H1100 + 23
'    Friend Const TVM_GETISEARCHSTRINGW As Integer = &H1100 + 64
'    Friend Const TVM_SETTOOLTIPS As Integer = &H1100 + 24
'    Friend Const TVM_GETTOOLTIPS As Integer = &H1100 + 25
'    Friend Const TVM_SETITEMHEIGHT As Integer = &H1100 + 27
'    Friend Const TVM_GETITEMHEIGHT As Integer = &H1100 + 28
'    Friend Const TVN_SELCHANGINGA As Integer = 0 - 400 - 1
'    Friend Const TVN_SELCHANGINGW As Integer = 0 - 400 - 50
'    Friend Const TVN_SELCHANGEDA As Integer = 0 - 400 - 2
'    Friend Const TVN_SELCHANGEDW As Integer = 0 - 400 - 51
'    Friend Const TVC_UNKNOWN As Integer = &H0
'    Friend Const TVC_BYMOUSE As Integer = &H1
'    Friend Const TVC_BYKEYBOARD As Integer = &H2
'    Friend Const TVN_GETDISPINFOA As Integer = 0 - 400 - 3
'    Friend Const TVN_GETDISPINFOW As Integer = 0 - 400 - 52
'    Friend Const TVN_SETDISPINFOA As Integer = 0 - 400 - 4
'    Friend Const TVN_SETDISPINFOW As Integer = 0 - 400 - 53
'    Friend Const TVIF_DI_SETITEM As Integer = &H1000
'    Friend Const TVN_ITEMEXPANDINGA As Integer = 0 - 400 - 5
'    Friend Const TVN_ITEMEXPANDINGW As Integer = 0 - 400 - 54
'    Friend Const TVN_ITEMEXPANDEDA As Integer = 0 - 400 - 6
'    Friend Const TVN_ITEMEXPANDEDW As Integer = 0 - 400 - 55
'    Friend Const TVN_BEGINDRAGA As Integer = 0 - 400 - 7
'    Friend Const TVN_BEGINDRAGW As Integer = 0 - 400 - 56
'    Friend Const TVN_BEGINRDRAGA As Integer = 0 - 400 - 8
'    Friend Const TVN_BEGINRDRAGW As Integer = 0 - 400 - 57
'    Friend Const TVN_DELETEITEMA As Integer = 0 - 400 - 9
'    Friend Const TVN_DELETEITEMW As Integer = 0 - 400 - 58
'    Friend Const TVN_BEGINLABELEDITA As Integer = 0 - 400 - 10
'    Friend Const TVN_BEGINLABELEDITW As Integer = 0 - 400 - 59
'    Friend Const TVN_ENDLABELEDITA As Integer = 0 - 400 - 11
'    Friend Const TVN_ENDLABELEDITW As Integer = 0 - 400 - 60
'    Friend Const TVN_KEYDOWN As Integer = 0 - 400 - 12
'    Friend Const TCS_SCROLLOPPOSITE As Integer = &H1
'    Friend Const TCS_BOTTOM As Integer = &H2
'    Friend Const TCS_RIGHT As Integer = &H2
'    Friend Const TCS_MULTISELECT As Integer = &H4
'    Friend Const TCS_FLATBUTTONS As Integer = &H8
'    Friend Const TCS_FORCEICONLEFT As Integer = &H10
'    Friend Const TCS_FORCELABELLEFT As Integer = &H20
'    Friend Const TCS_HOTTRACK As Integer = &H40
'    Friend Const TCS_VERTICAL As Integer = &H80
'    Friend Const TCS_TABS As Integer = &H0
'    Friend Const TCS_BUTTONS As Integer = &H100
'    Friend Const TCS_SINGLELINE As Integer = &H0
'    Friend Const TCS_MULTILINE As Integer = &H200
'    Friend Const TCS_RIGHTJUSTIFY As Integer = &H0
'    Friend Const TCS_FIXEDWIDTH As Integer = &H400
'    Friend Const TCS_RAGGEDRIGHT As Integer = &H800
'    Friend Const TCS_FOCUSONBUTTONDOWN As Integer = &H1000
'    Friend Const TCS_OWNERDRAWFIXED As Integer = &H2000
'    Friend Const TCS_TOOLTIPS As Integer = &H4000
'    Friend Const TCS_FOCUSNEVER As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        TCS_TOOLTIPS = 0x4000,
'    '        TCS_FOCUSNEVER = unchecked((int)0x8000),
'    '--------------------------^--- GenCode(token): unexpected token type
'    Friend Const TCM_FIRST As Integer = &H1300
'    Friend Const TCM_GETIMAGELIST As Integer = &H1300 + 2
'    Friend Const TCM_SETIMAGELIST As Integer = &H1300 + 3
'    Friend Const TCM_GETITEMCOUNT As Integer = &H1300 + 4
'    Friend Const TCIF_TEXT As Integer = &H1
'    Friend Const TCIF_IMAGE As Integer = &H2
'    Friend Const TCIF_RTLREADING As Integer = &H4
'    Friend Const TCIF_PARAM As Integer = &H8
'    Friend Const TCIF_STATE As Integer = &H10
'    Friend Const TCIS_BUTTONPRESSED As Integer = &H1
'    Friend Const TCM_GETITEMA As Integer = &H1300 + 5
'    Friend Const TCM_GETITEMW As Integer = &H1300 + 60
'    Friend Const TCM_SETITEMA As Integer = &H1300 + 6
'    Friend Const TCM_SETITEMW As Integer = &H1300 + 61
'    Friend Const TCM_INSERTITEMA As Integer = &H1300 + 7
'    Friend Const TCM_INSERTITEMW As Integer = &H1300 + 62
'    Friend Const TCM_DELETEITEM As Integer = &H1300 + 8
'    Friend Const TCM_DELETEALLITEMS As Integer = &H1300 + 9
'    Friend Const TCM_GETITEMRECT As Integer = &H1300 + 10
'    Friend Const TCM_GETCURSEL As Integer = &H1300 + 11
'    Friend Const TCM_SETCURSEL As Integer = &H1300 + 12
'    Friend Const TCHT_NOWHERE As Integer = &H1
'    Friend Const TCHT_ONITEMICON As Integer = &H2
'    Friend Const TCHT_ONITEMLABEL As Integer = &H4
'    Friend Const TCHT_ONITEM As Integer = &H2 Or &H4
'    Friend Const TCM_HITTEST As Integer = &H1300 + 13
'    Friend Const TCM_SETITEMEXTRA As Integer = &H1300 + 14
'    Friend Const TCM_ADJUSTRECT As Integer = &H1300 + 40
'    Friend Const TCM_SETITEMSIZE As Integer = &H1300 + 41
'    Friend Const TCM_REMOVEIMAGE As Integer = &H1300 + 42
'    Friend Const TCM_SETPADDING As Integer = &H1300 + 43
'    Friend Const TCM_GETROWCOUNT As Integer = &H1300 + 44
'    Friend Const TCM_GETTOOLTIPS As Integer = &H1300 + 45
'    Friend Const TCM_SETTOOLTIPS As Integer = &H1300 + 46
'    Friend Const TCM_GETCURFOCUS As Integer = &H1300 + 47
'    Friend Const TCM_SETCURFOCUS As Integer = &H1300 + 48
'    Friend Const TCM_SETMINTABWIDTH As Integer = &H1300 + 49
'    Friend Const TCM_DESELECTALL As Integer = &H1300 + 50
'    Friend Const TCN_KEYDOWN As Integer = 0 - 550 - 0
'    Friend Const TCN_SELCHANGE As Integer = 0 - 550 - 1
'    Friend Const TCN_SELCHANGING As Integer = 0 - 550 - 2
'    Friend Const THREAD_PRIORITY_LOWEST As Integer = - 2
'    Friend Const THREAD_PRIORITY_BELOW_NORMAL As Integer = - 2 + 1
'    Friend Const THREAD_PRIORITY_HIGHEST As Integer = 2
'    Friend Const THREAD_PRIORITY_ABOVE_NORMAL As Integer = 2 - 1
'    Friend Const THREAD_PRIORITY_ERROR_RETURN As Integer = &H7FFFFFFF
'    Friend Const THREAD_PRIORITY_TIME_CRITICAL As Integer = 15
'    Friend Const THREAD_PRIORITY_IDLE As Integer = - 15
'    Friend Const TVHT_ONITEM As Integer = &H2 Or &H4 Or &H40
'    Friend Const TBDDRET_DEFAULT As Integer = 0
'    Friend Const TBDDRET_NODEFAULT As Integer = 1
'    Friend Const TBDDRET_TREATPRESSED As Integer = 2
'    Friend Const TBNF_IMAGE As Integer = &H1
'    Friend Const TBNF_TEXT As Integer = &H2
'    Friend Const TBNF_DI_SETITEM As Integer = &H10000000
    
'    Friend Const TYMED_HGLOBAL As Integer = 1
'    Friend Const TYMED_FILE As Integer = 2
'    Friend Const TYMED_ISTREAM As Integer = 4
'    Friend Const TYMED_ISTORAGE As Integer = 8
'    Friend Const TYMED_GDI As Integer = 16
'    Friend Const TYMED_MFPICT As Integer = 32
'    Friend Const TYMED_ENHMF As Integer = 64
'    Friend Const TYMED_NULL As Integer = 0
    
'    Friend Const UI_CAP_2700 As Integer = &H1
'    Friend Const UI_CAP_ROT90 As Integer = &H2
'    Friend Const UI_CAP_ROTANY As Integer = &H4
'    Friend Const UIS_SET As Integer = 1
'    Friend Const UIS_CLEAR As Integer = 2
'    Friend Const UIS_INITIALIZE As Integer = 3
'    Friend Const UISF_HIDEFOCUS As Integer = &H1
'    Friend Const UISF_HIDEACCEL As Integer = &H2
'    Friend Const UNIQUE_NAME As Integer = &H0
'    Friend Const UPDFCACHE_NODATACACHE As Integer = &H1
'    Friend Const UPDFCACHE_ONSAVECACHE As Integer = &H2
'    Friend Const UPDFCACHE_ONSTOPCACHE As Integer = &H4
'    Friend Const UPDFCACHE_NORMALCACHE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                        UPDFCACHE_ONSTOPCACHE = (0x4),
'    '                                                                                UPDFCACHE_NORMALCACHE = (unchecked((int)0x8)),
'    '----------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const UPDFCACHE_IFBLANK As Integer = &H10
'    Friend Const UPDFCACHE_ONLYIFBLANK As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                        UPDFCACHE_IFBLANK = (0x10),
'    '                                                                                                                            UPDFCACHE_ONLYIFBLANK = (unchecked((int)0x80000000)),
'    '------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const UPDFCACHE_IFBLANKORONSAVECACHE As Integer = &H10 Or &H2
'    Friend Const UPDFCACHE_ALL As Integer = Not
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                    UPDFCACHE_IFBLANKORONSAVECACHE = ((0x10)|(0x2)),
'    '                                                                                                                                                                                     UPDFCACHE_ALL = (~(unchecked((int)0x80000000))),
'    '---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const UPDFCACHE_ALLBUTNODATACACHE As Integer = Not And Not &H1
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                                                     UPDFCACHE_ALL = (~(unchecked((int)0x80000000))),
'    '                                                                                                                                                                                                     UPDFCACHE_ALLBUTNODATACACHE = ((~(unchecked((int)0x80000000)))&~(0x1)),
'    '----------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const USERCLASSTYPE_FULL As Integer = 1
'    Friend Const USERCLASSTYPE_SHORT As Integer = 2
'    Friend Const USERCLASSTYPE_APPNAME As Integer = 3
'    Friend Const USER_MARSHAL_FC_BYTE As Integer = 1
'    Friend Const USER_MARSHAL_FC_CHAR As Integer = 2
'    Friend Const USER_MARSHAL_FC_SMALL As Integer = 3
'    Friend Const USER_MARSHAL_FC_USMALL As Integer = 4
'    Friend Const USER_MARSHAL_FC_WCHAR As Integer = 5
'    Friend Const USER_MARSHAL_FC_SHORT As Integer = 6
'    Friend Const USER_MARSHAL_FC_USHORT As Integer = 7
'    Friend Const USER_MARSHAL_FC_LONG As Integer = 8
'    Friend Const USER_MARSHAL_FC_ULONG As Integer = 9
'    Friend Const USER_MARSHAL_FC_FLOAT As Integer = 10
'    Friend Const USER_MARSHAL_FC_HYPER As Integer = 11
'    Friend Const USER_MARSHAL_FC_DOUBLE As Integer = 12
'    Friend Const UNLOAD_DLL_DEBUG_EVENT As Integer = 7
'    Friend Const UNIVERSAL_NAME_INFO_LEVEL As Integer = &H1
'    Friend Const UOI_FLAGS As Integer = 1
'    Friend Const UOI_NAME As Integer = 2
'    Friend Const UOI_TYPE As Integer = 3
'    Friend Const UOI_USER_SID As Integer = 4
'    Friend Const UDN_FIRST As Integer = 0 - 721
'    Friend Const UDN_LAST As Integer = 0 - 740
'    Friend Const UD_MAXVAL As Integer = &H7FFF
'    Friend Const UD_MINVAL As Integer = - &H7FFF
'    Friend Const UDS_WRAP As Integer = &H1
'    Friend Const UDS_SETBUDDYINT As Integer = &H2
'    Friend Const UDS_ALIGNRIGHT As Integer = &H4
'    Friend Const UDS_ALIGNLEFT As Integer = &H8
'    Friend Const UDS_AUTOBUDDY As Integer = &H10
'    Friend Const UDS_ARROWKEYS As Integer = &H20
'    Friend Const UDS_HORZ As Integer = &H40
'    Friend Const UDS_NOTHOUSANDS As Integer = &H80
'    Friend Const UDS_HOTTRACK As Integer = &H100
'    Friend Const UDM_SETRANGE As Integer = &H400 + 101
'    Friend Const UDM_GETRANGE As Integer = &H400 + 102
'    Friend Const UDM_SETPOS As Integer = &H400 + 103
'    Friend Const UDM_GETPOS As Integer = &H400 + 104
'    Friend Const UDM_SETBUDDY As Integer = &H400 + 105
'    Friend Const UDM_GETBUDDY As Integer = &H400 + 106
'    Friend Const UDM_SETACCEL As Integer = &H400 + 107
'    Friend Const UDM_GETACCEL As Integer = &H400 + 108
'    Friend Const UDM_SETBASE As Integer = &H400 + 109
'    Friend Const UDM_GETBASE As Integer = &H400 + 110
'    Friend Const UDM_SETRANGE32 As Integer = &H400 + 111
'    Friend Const UDM_GETRANGE32 As Integer = &H400 + 112
'    Friend Const ULW_COLORKEY As Integer = &H1
'    Friend Const ULW_ALPHA As Integer = &H2
'    Friend Const ULW_OPAQUE As Integer = &H4
'    Friend Const UDN_DELTAPOS As Integer = 0 - 721 - 1
'    ' NT5 begin 
'    ' NT5 end 
    
    
    
    
    
    
    
    
    
    
'    Friend Const VARIANT_NOVALUEPROP As Integer = &H1
'    Friend Const VARIANT_ALPHABOOL As Integer = &H2
'    Friend Const VARIANT_NOUSEROVERRIDE As Integer = &H4
'    Friend Const VAR_TIMEVALUEONLY As Integer = &H1
'    Friend Const VAR_DATEVALUEONLY As Integer = &H2
'    Friend Const VAR_VALIDDATE As Integer = &H4
'    Friend Const VAR_CALENDAR_HIJRI As Integer = &H8
'    Friend Const VARIANT_CALENDAR_HIJRI As Integer = &H8
'    Friend Const VER_PLATFORM_WIN32s As Integer = 0
'    Friend Const VER_PLATFORM_WIN32_WINDOWS As Integer = 1
'    Friend Const VER_PLATFORM_WIN32_NT As Integer = 2
'    Friend Const VIEW_E_FIRST As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        VER_PLATFORM_WIN32_NT = 2,
'    '        VIEW_E_FIRST = unchecked((int)0x80040140),
'    '------------------------^--- GenCode(token): unexpected token type
'    Friend Const VIEW_E_LAST As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        VIEW_E_FIRST = unchecked((int)0x80040140),
'    '        VIEW_E_LAST = unchecked((int)0x8004014F),
'    '-----------------------^--- GenCode(token): unexpected token type
'    Friend Const VIEW_S_FIRST As Integer = &H40140
'    Friend Const VIEW_S_LAST As Integer = &H4014F
'    Friend Const VIEW_E_DRAW As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        VIEW_S_LAST = 0x0004014F,
'    '        VIEW_E_DRAW = unchecked((int)0x80040140),
'    '-----------------------^--- GenCode(token): unexpected token type
'    Friend Const VIEW_S_ALREADY_FROZEN As Integer = &H40140
'    Friend Const VTA_BASELINE As Integer = 24
'    Friend Const VTA_LEFT As Integer = 8
'    Friend Const VTA_RIGHT As Integer = 0
'    Friend Const VTA_CENTER As Integer = 6
'    Friend Const VTA_BOTTOM As Integer = 2
'    Friend Const VTA_TOP As Integer = 0
'    Friend Const VARIABLE_PITCH As Integer = 2
'    Friend Const VIETNAMESE_CHARSET As Integer = 163
'    Friend Const VERTSIZE As Integer = 6
'    Friend Const VERTRES As Integer = 10
'    Friend Const VREFRESH As Integer = 116
'    Friend Const VALID_INHERIT_FLAGS As Integer = &HF
'    Friend Const VK_LBUTTON As Integer = &H1
'    Friend Const VK_RBUTTON As Integer = &H2
'    Friend Const VK_CANCEL As Integer = &H3
'    Friend Const VK_MBUTTON As Integer = &H4
'    Friend Const VK_BACK As Integer = &H8
'    Friend Const VK_TAB As Integer = &H9
'    Friend Const VK_CLEAR As Integer = &HC
'    Friend Const VK_RETURN As Integer = &HD
'    Friend Const VK_SHIFT As Integer = &H10
'    Friend Const VK_CONTROL As Integer = &H11
'    Friend Const VK_MENU As Integer = &H12
'    Friend Const VK_PAUSE As Integer = &H13
'    Friend Const VK_CAPITAL As Integer = &H14
'    Friend Const VK_ESCAPE As Integer = &H1B
'    Friend Const VK_SPACE As Integer = &H20
'    Friend Const VK_PRIOR As Integer = &H21
'    Friend Const VK_NEXT As Integer = &H22
'    Friend Const VK_END As Integer = &H23
'    Friend Const VK_HOME As Integer = &H24
'    Friend Const VK_LEFT As Integer = &H25
'    Friend Const VK_UP As Integer = &H26
'    Friend Const VK_RIGHT As Integer = &H27
'    Friend Const VK_DOWN As Integer = &H28
'    Friend Const VK_SELECT As Integer = &H29
'    Friend Const VK_PRINT As Integer = &H2A
'    Friend Const VK_EXECUTE As Integer = &H2B
'    Friend Const VK_SNAPSHOT As Integer = &H2C
'    Friend Const VK_INSERT As Integer = &H2D
'    Friend Const VK_DELETE As Integer = &H2E
'    Friend Const VK_HELP As Integer = &H2F
'    Friend Const VK_LWIN As Integer = &H5B
'    Friend Const VK_RWIN As Integer = &H5C
'    Friend Const VK_APPS As Integer = &H5D
'    Friend Const VK_NUMPAD0 As Integer = &H60
'    Friend Const VK_NUMPAD1 As Integer = &H61
'    Friend Const VK_NUMPAD2 As Integer = &H62
'    Friend Const VK_NUMPAD3 As Integer = &H63
'    Friend Const VK_NUMPAD4 As Integer = &H64
'    Friend Const VK_NUMPAD5 As Integer = &H65
'    Friend Const VK_NUMPAD6 As Integer = &H66
'    Friend Const VK_NUMPAD7 As Integer = &H67
'    Friend Const VK_NUMPAD8 As Integer = &H68
'    Friend Const VK_NUMPAD9 As Integer = &H69
'    Friend Const VK_MULTIPLY As Integer = &H6A
'    Friend Const VK_ADD As Integer = &H6B
'    Friend Const VK_SEPARATOR As Integer = &H6C
'    Friend Const VK_SUBTRACT As Integer = &H6D
'    Friend Const VK_DECIMAL As Integer = &H6E
'    Friend Const VK_DIVIDE As Integer = &H6F
'    Friend Const VK_F1 As Integer = &H70
'    Friend Const VK_F2 As Integer = &H71
'    Friend Const VK_F3 As Integer = &H72
'    Friend Const VK_F4 As Integer = &H73
'    Friend Const VK_F5 As Integer = &H74
'    Friend Const VK_F6 As Integer = &H75
'    Friend Const VK_F7 As Integer = &H76
'    Friend Const VK_F8 As Integer = &H77
'    Friend Const VK_F9 As Integer = &H78
'    Friend Const VK_F10 As Integer = &H79
'    Friend Const VK_F11 As Integer = &H7A
'    Friend Const VK_F12 As Integer = &H7B
'    Friend Const VK_F13 As Integer = &H7C
'    Friend Const VK_F14 As Integer = &H7D
'    Friend Const VK_F15 As Integer = &H7E
'    Friend Const VK_F16 As Integer = &H7F
'    Friend Const VK_F17 As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        VK_F16 = 0x7F,
'    '        VK_F17 = unchecked((int)0x80),
'    '------------------^--- GenCode(token): unexpected token type
'    Friend Const VK_F18 As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        VK_F17 = unchecked((int)0x80),
'    '        VK_F18 = unchecked((int)0x81),
'    '------------------^--- GenCode(token): unexpected token type
'    Friend Const VK_F19 As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        VK_F18 = unchecked((int)0x81),
'    '        VK_F19 = unchecked((int)0x82),
'    '------------------^--- GenCode(token): unexpected token type
'    Friend Const VK_F20 As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        VK_F19 = unchecked((int)0x82),
'    '        VK_F20 = unchecked((int)0x83),
'    '------------------^--- GenCode(token): unexpected token type
'    Friend Const VK_F21 As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        VK_F20 = unchecked((int)0x83),
'    '        VK_F21 = unchecked((int)0x84),
'    '------------------^--- GenCode(token): unexpected token type
'    Friend Const VK_F22 As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        VK_F21 = unchecked((int)0x84),
'    '        VK_F22 = unchecked((int)0x85),
'    '------------------^--- GenCode(token): unexpected token type
'    Friend Const VK_F23 As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        VK_F22 = unchecked((int)0x85),
'    '        VK_F23 = unchecked((int)0x86),
'    '------------------^--- GenCode(token): unexpected token type
'    Friend Const VK_F24 As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        VK_F23 = unchecked((int)0x86),
'    '        VK_F24 = unchecked((int)0x87),
'    '------------------^--- GenCode(token): unexpected token type
'    Friend Const VK_NUMLOCK As Integer = &H90
'    Friend Const VK_SCROLL As Integer = &H91
'    Friend Const VK_LSHIFT As Integer = &HA0
'    Friend Const VK_RSHIFT As Integer = &HA1
'    Friend Const VK_LCONTROL As Integer = &HA2
'    Friend Const VK_RCONTROL As Integer = &HA3
'    Friend Const VK_LMENU As Integer = &HA4
'    Friend Const VK_RMENU As Integer = &HA5
'    Friend Const VK_PROCESSKEY As Integer = &HE5
'    Friend Const VK_ATTN As Integer = &HF6
'    Friend Const VK_CRSEL As Integer = &HF7
'    Friend Const VK_EXSEL As Integer = &HF8
'    Friend Const VK_EREOF As Integer = &HF9
'    Friend Const VK_PLAY As Integer = &HFA
'    Friend Const VK_ZOOM As Integer = &HFB
'    Friend Const VK_NONAME As Integer = &HFC
'    Friend Const VK_PA1 As Integer = &HFD
'    Friend Const VK_OEM_CLEAR As Integer = &HFE
'    Friend Const VS_FILE_INFO As Integer = 16
'    Friend Const VS_VERSION_INFO As Integer = 1
'    Friend Const VS_USER_DEFINED As Integer = 100
'    Friend Const VS_FFI_SIGNATURE As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        VS_USER_DEFINED = 100,
'    '        VS_FFI_SIGNATURE = unchecked((int)0xFEEF04BD),
'    '----------------------------^--- GenCode(token): unexpected token type
'    Friend Const VS_FFI_STRUCVERSION As Integer = &H10000
'    Friend Const VS_FFI_FILEFLAGSMASK As Integer = &H3F
'    Friend Const VS_FF_DEBUG As Integer = &H1
'    Friend Const VS_FF_PRERELEASE As Integer = &H2
'    Friend Const VS_FF_PATCHED As Integer = &H4
'    Friend Const VS_FF_PRIVATEBUILD As Integer = &H8
'    Friend Const VS_FF_INFOINFERRED As Integer = &H10
'    Friend Const VS_FF_SPECIALBUILD As Integer = &H20
'    Friend Const VOS_UNKNOWN As Integer = &H0
'    Friend Const VOS_DOS As Integer = &H10000
'    Friend Const VOS_OS216 As Integer = &H20000
'    Friend Const VOS_OS232 As Integer = &H30000
'    Friend Const VOS_NT As Integer = &H40000
'    Friend Const VOS__BASE As Integer = &H0
'    Friend Const VOS__WINDOWS16 As Integer = &H1
'    Friend Const VOS__PM16 As Integer = &H2
'    Friend Const VOS__PM32 As Integer = &H3
'    Friend Const VOS__WINDOWS32 As Integer = &H4
'    Friend Const VOS_DOS_WINDOWS16 As Integer = &H10001
'    Friend Const VOS_DOS_WINDOWS32 As Integer = &H10004
'    Friend Const VOS_OS216_PM16 As Integer = &H20002
'    Friend Const VOS_OS232_PM32 As Integer = &H30003
'    Friend Const VOS_NT_WINDOWS32 As Integer = &H40004
'    Friend Const VFT_UNKNOWN As Integer = &H0
'    Friend Const VFT_APP As Integer = &H1
'    Friend Const VFT_DLL As Integer = &H2
'    Friend Const VFT_DRV As Integer = &H3
'    Friend Const VFT_FONT As Integer = &H4
'    Friend Const VFT_VXD As Integer = &H5
'    Friend Const VFT_STATIC_LIB As Integer = &H7
'    Friend Const VFT2_UNKNOWN As Integer = &H0
'    Friend Const VFT2_DRV_PRINTER As Integer = &H1
'    Friend Const VFT2_DRV_KEYBOARD As Integer = &H2
'    Friend Const VFT2_DRV_LANGUAGE As Integer = &H3
'    Friend Const VFT2_DRV_DISPLAY As Integer = &H4
'    Friend Const VFT2_DRV_MOUSE As Integer = &H5
'    Friend Const VFT2_DRV_NETWORK As Integer = &H6
'    Friend Const VFT2_DRV_SYSTEM As Integer = &H7
'    Friend Const VFT2_DRV_INSTALLABLE As Integer = &H8
'    Friend Const VFT2_DRV_SOUND As Integer = &H9
'    Friend Const VFT2_DRV_COMM As Integer = &HA
'    Friend Const VFT2_DRV_INPUTMETHOD As Integer = &HB
'    Friend Const VFT2_FONT_RASTER As Integer = &H1
'    Friend Const VFT2_FONT_VECTOR As Integer = &H2
'    Friend Const VFT2_FONT_TRUETYPE As Integer = &H3
'    Friend Const VFFF_ISSHAREDFILE As Integer = &H1
'    Friend Const VFF_CURNEDEST As Integer = &H1
'    Friend Const VFF_FILEINUSE As Integer = &H2
'    Friend Const VFF_BUFFTOOSMALL As Integer = &H4
'    Friend Const VIFF_FORCEINSTALL As Integer = &H1
'    Friend Const VIFF_DONTDELETEOLD As Integer = &H2
'    Friend Const VIF_TEMPFILE As Integer = &H1
'    Friend Const VIF_MISMATCH As Integer = &H2
'    Friend Const VIF_SRCOLD As Integer = &H4
'    Friend Const VIF_DIFFLANG As Integer = &H8
'    Friend Const VIF_DIFFCODEPG As Integer = &H10
'    Friend Const VIF_DIFFTYPE As Integer = &H20
'    Friend Const VIF_WRITEPROT As Integer = &H40
'    Friend Const VIF_FILEINUSE As Integer = &H80
'    Friend Const VIF_OUTOFSPACE As Integer = &H100
'    Friend Const VIF_ACCESSVIOLATION As Integer = &H200
'    Friend Const VIF_SHARINGVIOLATION As Integer = &H400
'    Friend Const VIF_CANNOTCREATE As Integer = &H800
'    Friend Const VIF_CANNOTDELETE As Integer = &H1000
'    Friend Const VIF_CANNOTRENAME As Integer = &H2000
'    Friend Const VIF_CANNOTDELETECUR As Integer = &H4000
'    Friend Const VIF_OUTOFMEMORY As Integer = &H8000
'    Friend Const VIF_CANNOTREADSRC As Integer = &H10000
'    Friend Const VIF_CANNOTREADDST As Integer = &H20000
'    Friend Const VIF_BUFFTOOSMALL As Integer = &H40000
'    Friend Const VIEW_LARGEICONS As Integer = 0
'    Friend Const VIEW_SMALLICONS As Integer = 1
'    Friend Const VIEW_LIST As Integer = 2
'    Friend Const VIEW_DETAILS As Integer = 3
'    Friend Const VIEW_SORTNAME As Integer = 4
'    Friend Const VIEW_SORTSIZE As Integer = 5
'    Friend Const VIEW_SORTDATE As Integer = 6
'    Friend Const VIEW_SORTTYPE As Integer = 7
'    Friend Const VIEW_PARENTFOLDER As Integer = 8
'    Friend Const VIEW_NETCONNECT As Integer = 9
'    Friend Const VIEW_NETDISCONNECT As Integer = 10
'    Friend Const VIEW_NEWFOLDER As Integer = 11
'    Friend Const VIEW_VIEWMENU As Integer = 12
    
'    Friend Const WM_DDE_FIRST As Integer = &H3E0
'    Friend Const WM_DDE_INITIATE As Integer = &H3E0
'    Friend Const WM_DDE_TERMINATE As Integer = &H3E0 + 1
'    Friend Const WM_DDE_ADVISE As Integer = &H3E0 + 2
'    Friend Const WM_DDE_UNADVISE As Integer = &H3E0 + 3
'    Friend Const WM_DDE_ACK As Integer = &H3E0 + 4
'    Friend Const WM_DDE_DATA As Integer = &H3E0 + 5
'    Friend Const WM_DDE_REQUEST As Integer = &H3E0 + 6
'    Friend Const WM_DDE_POKE As Integer = &H3E0 + 7
'    Friend Const WM_DDE_EXECUTE As Integer = &H3E0 + 8
'    Friend Const WM_DDE_LAST As Integer = &H3E0 + 8
'    Friend Const WAVERR_BASE As Integer = 32
'    Friend Const WAVERR_BADFORMAT As Integer = 32 + 0
'    Friend Const WAVERR_STILLPLAYING As Integer = 32 + 1
'    Friend Const WAVERR_UNPREPARED As Integer = 32 + 2
'    Friend Const WAVERR_SYNC As Integer = 32 + 3
'    Friend Const WAVERR_LASTERROR As Integer = 32 + 3
'    Friend Const WOM_OPEN As Integer = &H3BB
'    Friend Const WOM_CLOSE As Integer = &H3BC
'    Friend Const WOM_DONE As Integer = &H3BD
'    Friend Const WIM_OPEN As Integer = &H3BE
'    Friend Const WIM_CLOSE As Integer = &H3BF
'    Friend Const WIM_DATA As Integer = &H3C0
'    Friend Const WAVE_FORMAT_QUERY As Integer = &H1
'    Friend Const WAVE_ALLOWSYNC As Integer = &H2
'    Friend Const WAVE_MAPPED As Integer = &H4
'    Friend Const WAVE_FORMAT_DIRECT As Integer = &H8
'    Friend Const WAVE_FORMAT_DIRECT_QUERY As Integer = &H1 Or &H8
'    Friend Const WHDR_DONE As Integer = &H1
'    Friend Const WHDR_PREPARED As Integer = &H2
'    Friend Const WHDR_BEGINLOOP As Integer = &H4
'    Friend Const WHDR_ENDLOOP As Integer = &H8
'    Friend Const WHDR_INQUEUE As Integer = &H10
'    Friend Const WAVECAPS_PITCH As Integer = &H1
'    Friend Const WAVECAPS_PLAYBACKRATE As Integer = &H2
'    Friend Const WAVECAPS_VOLUME As Integer = &H4
'    Friend Const WAVECAPS_LRVOLUME As Integer = &H8
'    Friend Const WAVECAPS_SYNC As Integer = &H10
'    Friend Const WAVECAPS_SAMPLEACCURATE As Integer = &H20
'    Friend Const WAVECAPS_DIRECTSOUND As Integer = &H40
'    Friend Const WAVE_INVALIDFORMAT As Integer = &H0
'    Friend Const WAVE_FORMAT_1M08 As Integer = &H1
'    Friend Const WAVE_FORMAT_1S08 As Integer = &H2
'    Friend Const WAVE_FORMAT_1M16 As Integer = &H4
'    Friend Const WAVE_FORMAT_1S16 As Integer = &H8
'    Friend Const WAVE_FORMAT_2M08 As Integer = &H10
'    Friend Const WAVE_FORMAT_2S08 As Integer = &H20
'    Friend Const WAVE_FORMAT_2M16 As Integer = &H40
'    Friend Const WAVE_FORMAT_2S16 As Integer = &H80
'    Friend Const WAVE_FORMAT_4M08 As Integer = &H100
'    Friend Const WAVE_FORMAT_4S08 As Integer = &H200
'    Friend Const WAVE_FORMAT_4M16 As Integer = &H400
'    Friend Const WAVE_FORMAT_4S16 As Integer = &H800
        Friend Const WAVE_FORMAT_PCM As Integer = &H1
        Friend Const WAVE_FORMAT_ADPCM As Integer = &H2
        Friend Const WAVE_FORMAT_IEEE_FLOAT As Integer = &H3

'    Friend Const WIN32 As Integer = 100
'    Friend Const WIZ_CXDLG As Integer = 276
'    Friend Const WIZ_CYDLG As Integer = 140
'    Friend Const WIZ_CXBMP As Integer = 80
'    Friend Const WIZ_BODYX As Integer = 92
'    Friend Const WIZ_BODYCX As Integer = 184
'    Friend Const WIN_CERT_REVISION_1_0 As Integer = &H100
'    Friend Const WIN_CERT_TYPE_X509 As Integer = &H1
'    Friend Const WIN_CERT_TYPE_PKCS_SIGNED_DATA As Integer = &H2
'    Friend Const WIN_CERT_TYPE_RESERVED_1 As Integer = &H3
'    Friend Const WINDOW_BUFFER_SIZE_EVENT As Integer = &H4
'    Friend Const WINVER As Integer = &H400
'    Friend Const WHITEONBLACK As Integer = 2
'    Friend Const WINDING As Integer = 2
'    Friend Const WHITE_BRUSH As Integer = 0
'    Friend Const WHITE_PEN As Integer = 6
'    Friend Const WGL_FONT_LINES As Integer = 0
'    Friend Const WGL_FONT_POLYGONS As Integer = 1
'    Friend Const WGL_SWAP_MAIN_PLANE As Integer = &H1
'    Friend Const WGL_SWAP_OVERLAY1 As Integer = &H2
'    Friend Const WGL_SWAP_OVERLAY2 As Integer = &H4
'    Friend Const WGL_SWAP_OVERLAY3 As Integer = &H8
'    Friend Const WGL_SWAP_OVERLAY4 As Integer = &H10
'    Friend Const WGL_SWAP_OVERLAY5 As Integer = &H20
'    Friend Const WGL_SWAP_OVERLAY6 As Integer = &H40
'    Friend Const WGL_SWAP_OVERLAY7 As Integer = &H80
'    Friend Const WGL_SWAP_OVERLAY8 As Integer = &H100
'    Friend Const WGL_SWAP_OVERLAY9 As Integer = &H200
'    Friend Const WGL_SWAP_OVERLAY10 As Integer = &H400
'    Friend Const WGL_SWAP_OVERLAY11 As Integer = &H800
'    Friend Const WGL_SWAP_OVERLAY12 As Integer = &H1000
'    Friend Const WGL_SWAP_OVERLAY13 As Integer = &H2000
'    Friend Const WGL_SWAP_OVERLAY14 As Integer = &H4000
'    Friend Const WGL_SWAP_OVERLAY15 As Integer = &H8000
'    Friend Const WGL_SWAP_UNDERLAY1 As Integer = &H10000
'    Friend Const WGL_SWAP_UNDERLAY2 As Integer = &H20000
'    Friend Const WGL_SWAP_UNDERLAY3 As Integer = &H40000
'    Friend Const WGL_SWAP_UNDERLAY4 As Integer = &H80000
'    Friend Const WGL_SWAP_UNDERLAY5 As Integer = &H100000
'    Friend Const WGL_SWAP_UNDERLAY6 As Integer = &H200000
'    Friend Const WGL_SWAP_UNDERLAY7 As Integer = &H400000
'    Friend Const WGL_SWAP_UNDERLAY8 As Integer = &H800000
'    Friend Const WGL_SWAP_UNDERLAY9 As Integer = &H1000000
'    Friend Const WGL_SWAP_UNDERLAY10 As Integer = &H2000000
'    Friend Const WGL_SWAP_UNDERLAY11 As Integer = &H4000000
'    Friend Const WGL_SWAP_UNDERLAY12 As Integer = &H8000000
'    Friend Const WGL_SWAP_UNDERLAY13 As Integer = &H10000000
'    Friend Const WGL_SWAP_UNDERLAY14 As Integer = &H20000000
'    Friend Const WGL_SWAP_UNDERLAY15 As Integer = &H40000000
'    Friend Const WNNC_NET_MSNET As Integer = &H10000
'    Friend Const WNNC_NET_LANMAN As Integer = &H20000
'    Friend Const WNNC_NET_NETWARE As Integer = &H30000
'    Friend Const WNNC_NET_VINES As Integer = &H40000
'    Friend Const WNNC_NET_10NET As Integer = &H50000
'    Friend Const WNNC_NET_LOCUS As Integer = &H60000
'    Friend Const WNNC_NET_SUN_PC_NFS As Integer = &H70000
'    Friend Const WNNC_NET_LANSTEP As Integer = &H80000
'    Friend Const WNNC_NET_9TILES As Integer = &H90000
'    Friend Const WNNC_NET_LANTASTIC As Integer = &HA0000
'    Friend Const WNNC_NET_AS400 As Integer = &HB0000
'    Friend Const WNNC_NET_FTP_NFS As Integer = &HC0000
'    Friend Const WNNC_NET_PATHWORKS As Integer = &HD0000
'    Friend Const WNNC_NET_LIFENET As Integer = &HE0000
'    Friend Const WNNC_NET_POWERLAN As Integer = &HF0000
'    Friend Const WNNC_NET_BWNFS As Integer = &H100000
'    Friend Const WNNC_NET_COGENT As Integer = &H110000
'    Friend Const WNNC_NET_FARALLON As Integer = &H120000
'    Friend Const WNNC_NET_APPLETALK As Integer = &H130000
'    Friend Const WNNC_NET_INTERGRAPH As Integer = &H140000
'    Friend Const WNNC_NET_SYMFONET As Integer = &H150000
'    Friend Const WNNC_NET_CLEARCASE As Integer = &H160000
'    Friend Const WNFMT_MULTILINE As Integer = &H1
'    Friend Const WNFMT_ABBREVIATED As Integer = &H2
'    Friend Const WNFMT_INENUM As Integer = &H10
'    Friend Const WNFMT_CONNECTION As Integer = &H20
'    Friend Const WN_SUCCESS As Integer = 0
'    Friend Const WN_NO_ERROR As Integer = 0
'    Friend Const WN_NOT_SUPPORTED As Integer = 50
'    Friend Const WN_CANCEL As Integer = 1223
'    Friend Const WN_RETRY As Integer = 1237
'    Friend Const WN_NET_ERROR As Integer = 59
'    Friend Const WN_MORE_DATA As Integer = 234
'    Friend Const WN_BAD_POINTER As Integer = 487
'    Friend Const WN_BAD_VALUE As Integer = 87
'    Friend Const WN_BAD_USER As Integer = 2202
'    Friend Const WN_BAD_PASSWORD As Integer = 86
'    Friend Const WN_ACCESS_DENIED As Integer = 5
'    Friend Const WN_FUNCTION_BUSY As Integer = 170
'    Friend Const WN_WINDOWS_ERROR As Integer = 59
'    Friend Const WN_OUT_OF_MEMORY As Integer = 8
'    Friend Const WN_NO_NETWORK As Integer = 1222
'    Friend Const WN_EXTENDED_ERROR As Integer = 1208
'    Friend Const WN_BAD_LEVEL As Integer = 124
'    Friend Const WN_BAD_HANDLE As Integer = 6
'    Friend Const WN_NOT_INITIALIZING As Integer = 1247
'    Friend Const WN_NO_MORE_DEVICES As Integer = 1248
'    Friend Const WN_NOT_CONNECTED As Integer = 2250
'    Friend Const WN_OPEN_FILES As Integer = 2401
'    Friend Const WN_DEVICE_IN_USE As Integer = 2404
'    Friend Const WN_BAD_NETNAME As Integer = 67
'    Friend Const WN_BAD_LOCALNAME As Integer = 1200
'    Friend Const WN_ALREADY_CONNECTED As Integer = 85
'    Friend Const WN_DEVICE_ERROR As Integer = 31
'    Friend Const WN_CONNECTION_CLOSED As Integer = 1201
'    Friend Const WN_NO_NET_OR_BAD_PATH As Integer = 1203
'    Friend Const WN_BAD_PROVIDER As Integer = 1204
'    Friend Const WN_CANNOT_OPEN_PROFILE As Integer = 1205
'    Friend Const WN_BAD_PROFILE As Integer = 1206
'    Friend Const WN_BAD_DEV_TYPE As Integer = 66
'    Friend Const WN_DEVICE_ALREADY_REMEMBERED As Integer = 1202
'    Friend Const WN_NO_MORE_ENTRIES As Integer = 259
'    Friend Const WN_NOT_CONTAINER As Integer = 1207
'    Friend Const WN_NOT_AUTHENTICATED As Integer = 1244
'    Friend Const WN_NOT_LOGGED_ON As Integer = 1245
'    Friend Const WN_NOT_VALIDATED As Integer = 1311
'    Friend Const WNCON_FORNETCARD As Integer = &H1
'    Friend Const WNCON_NOTROUTED As Integer = &H2
'    Friend Const WNCON_SLOWLINK As Integer = &H4
'    Friend Const WNCON_DYNAMIC As Integer = &H8
'    Friend Const WC_DEFAULTCHECK As Integer = &H100
'    Friend Const WC_COMPOSITECHECK As Integer = &H200
'    Friend Const WC_DISCARDNS As Integer = &H10
'    Friend Const WC_SEPCHARS As Integer = &H20
'    Friend Const WC_DEFAULTCHAR As Integer = &H40
'    Friend Const WRITE_DAC As Integer = &H40000
'    Friend Const WRITE_OWNER As Integer = &H80000
'    Friend Const WIN31_CLASS As Integer = 0
'    Friend Const WH_MIN As Integer = - 1
'    Friend Const WH_MSGFILTER As Integer = - 1
'    Friend Const WH_JOURNALRECORD As Integer = 0
'    Friend Const WH_JOURNALPLAYBACK As Integer = 1
'    Friend Const WH_KEYBOARD As Integer = 2
'    Friend Const WH_GETMESSAGE As Integer = 3
'    Friend Const WH_CALLWNDPROC As Integer = 4
'    Friend Const WH_CBT As Integer = 5
'    Friend Const WH_SYSMSGFILTER As Integer = 6
'    Friend Const WH_MOUSE As Integer = 7
'    Friend Const WH_HARDWARE As Integer = 8
'    Friend Const WH_DEBUG As Integer = 9
'    Friend Const WH_SHELL As Integer = 10
'    Friend Const WH_FOREGROUNDIDLE As Integer = 11
'    Friend Const WH_CALLWNDPROCRET As Integer = 12
'    Friend Const WH_MAX As Integer = 12
'    ' WH_MAX = 11;
'    Friend Const WH_MINHOOK As Integer = - 1
'    Friend Const WH_MAXHOOK As Integer = 12
'    Friend Const WINSTA_ENUMDESKTOPS As Integer = &H1
'    Friend Const WINSTA_READATTRIBUTES As Integer = &H2
'    Friend Const WINSTA_ACCESSCLIPBOARD As Integer = &H4
'    Friend Const WINSTA_CREATEDESKTOP As Integer = &H8
'    Friend Const WINSTA_WRITEATTRIBUTES As Integer = &H10
'    Friend Const WINSTA_ACCESSGLOBALATOMS As Integer = &H20
'    Friend Const WINSTA_EXITWINDOWS As Integer = &H40
'    Friend Const WINSTA_ENUMERATE As Integer = &H100
'    Friend Const WINSTA_READSCREEN As Integer = &H200
'    Friend Const WSF_VISIBLE As Integer = &H1
'    Friend Const WM_NULL As Integer = &H0
'    Friend Const WM_CREATE As Integer = &H1
'    Friend Const WM_DESTROY As Integer = &H2
'    Friend Const WM_MOVE As Integer = &H3
'    Friend Const WM_SIZE As Integer = &H5
'    Friend Const WM_ACTIVATE As Integer = &H6
'    Friend Const WA_INACTIVE As Integer = 0
'    Friend Const WA_ACTIVE As Integer = 1
'    Friend Const WA_CLICKACTIVE As Integer = 2
        Friend Const WM_SETFOCUS As Integer = &H7
'    Friend Const WM_KILLFOCUS As Integer = &H8
'    Friend Const WM_ENABLE As Integer = &HA
'    Friend Const WM_SETREDRAW As Integer = &HB
'    Friend Const WM_SETTEXT As Integer = &HC
'    Friend Const WM_GETTEXT As Integer = &HD
'    Friend Const WM_GETTEXTLENGTH As Integer = &HE
'    Friend Const WM_PAINT As Integer = &HF
'    Friend Const WM_CLOSE As Integer = &H10
'    Friend Const WM_QUERYENDSESSION As Integer = &H11
'    Friend Const WM_QUIT As Integer = &H12
'    Friend Const WM_QUERYOPEN As Integer = &H13
'    Friend Const WM_ERASEBKGND As Integer = &H14
        Friend Const WM_SYSCOLORCHANGE As Integer = &H15
'    Friend Const WM_ENDSESSION As Integer = &H16
'    Friend Const WM_SHOWWINDOW As Integer = &H18
'    Friend Const WM_WININICHANGE As Integer = &H1A
        Friend Const WM_SETTINGCHANGE As Integer = &H1A
'    Friend Const WM_DEVMODECHANGE As Integer = &H1B
'    Friend Const WM_ACTIVATEAPP As Integer = &H1C
'    Friend Const WM_FONTCHANGE As Integer = &H1D
'    Friend Const WM_TIMECHANGE As Integer = &H1E
'    Friend Const WM_CANCELMODE As Integer = &H1F
'    Friend Const WM_SETCURSOR As Integer = &H20
'    Friend Const WM_MOUSEACTIVATE As Integer = &H21
'    Friend Const WM_CHILDACTIVATE As Integer = &H22
'    Friend Const WM_QUEUESYNC As Integer = &H23
'    Friend Const WM_GETMINMAXINFO As Integer = &H24
'    Friend Const WM_PAINTICON As Integer = &H26
'    Friend Const WM_ICONERASEBKGND As Integer = &H27
'    Friend Const WM_NEXTDLGCTL As Integer = &H28
'    Friend Const WM_SPOOLERSTATUS As Integer = &H2A
'    Friend Const WM_DRAWITEM As Integer = &H2B
'    Friend Const WM_MEASUREITEM As Integer = &H2C
'    Friend Const WM_DELETEITEM As Integer = &H2D
'    Friend Const WM_VKEYTOITEM As Integer = &H2E
'    Friend Const WM_CHARTOITEM As Integer = &H2F
'    Friend Const WM_SETFONT As Integer = &H30
'    Friend Const WM_GETFONT As Integer = &H31
'    Friend Const WM_SETHOTKEY As Integer = &H32
'    Friend Const WM_GETHOTKEY As Integer = &H33
'    Friend Const WM_QUERYDRAGICON As Integer = &H37
'    Friend Const WM_COMPAREITEM As Integer = &H39
'    Friend Const WM_GETOBJECT As Integer = &H3D
'    Friend Const WM_COMPACTING As Integer = &H41
'    Friend Const WM_COMMNOTIFY As Integer = &H44
'    Friend Const WM_WINDOWPOSCHANGING As Integer = &H46
'    Friend Const WM_WINDOWPOSCHANGED As Integer = &H47
'    Friend Const WM_POWER As Integer = &H48
'    Friend Const WM_COPYDATA As Integer = &H4A
'    Friend Const WM_CANCELJOURNAL As Integer = &H4B
'    Friend Const WM_NOTIFY As Integer = &H4E
'    Friend Const WM_INPUTLANGCHANGEREQUEST As Integer = &H50
'    Friend Const WM_INPUTLANGCHANGE As Integer = &H51
'    Friend Const WM_TCARD As Integer = &H52
        Friend Const WM_HELP As Integer = &H53
'    Friend Const WM_USERCHANGED As Integer = &H54
'    Friend Const WM_NOTIFYFORMAT As Integer = &H55
        Friend Const WM_CONTEXTMENU As Integer = &H7B
'    Friend Const WM_STYLECHANGING As Integer = &H7C
'    Friend Const WM_STYLECHANGED As Integer = &H7D
'    Friend Const WM_DISPLAYCHANGE As Integer = &H7E
'    Friend Const WM_GETICON As Integer = &H7F
'    Friend Const WM_SETICON As Integer = &H80
'    Friend Const WM_NCCREATE As Integer = &H81
'    Friend Const WM_NCDESTROY As Integer = &H82
'    Friend Const WM_NCCALCSIZE As Integer = &H83
'    Friend Const WM_NCHITTEST As Integer = &H84
'    Friend Const WM_NCPAINT As Integer = &H85
'    Friend Const WM_NCACTIVATE As Integer = &H86
        Friend Const WM_GETDLGCODE As Integer = &H87
'    Friend Const WM_NCMOUSEMOVE As Integer = &HA0
'    Friend Const WM_NCLBUTTONDOWN As Integer = &HA1
'    Friend Const WM_NCLBUTTONUP As Integer = &HA2
'    Friend Const WM_NCLBUTTONDBLCLK As Integer = &HA3
'    Friend Const WM_NCRBUTTONDOWN As Integer = &HA4
'    Friend Const WM_NCRBUTTONUP As Integer = &HA5
'    Friend Const WM_NCRBUTTONDBLCLK As Integer = &HA6
'    Friend Const WM_NCMBUTTONDOWN As Integer = &HA7
'    Friend Const WM_NCMBUTTONUP As Integer = &HA8
'    Friend Const WM_NCMBUTTONDBLCLK As Integer = &HA9
'    Friend Const WM_KEYFIRST As Integer = &H100
        Friend Const WM_KEYDOWN As Integer = &H100
        Friend Const WM_KEYUP As Integer = &H101
        Friend Const WM_CHAR As Integer = &H102
'    Friend Const WM_DEADCHAR As Integer = &H103
'    Friend Const WM_SYSKEYDOWN As Integer = &H104
'    Friend Const WM_SYSKEYUP As Integer = &H105
        Friend Const WM_SYSCHAR As Integer = &H106
'    Friend Const WM_SYSDEADCHAR As Integer = &H107
'    Friend Const WM_KEYLAST As Integer = &H108
'    Friend Const WM_IME_STARTCOMPOSITION As Integer = &H10D
'    Friend Const WM_IME_ENDCOMPOSITION As Integer = &H10E
'    Friend Const WM_IME_COMPOSITION As Integer = &H10F
'    Friend Const WM_IME_KEYLAST As Integer = &H10F
'    Friend Const WM_INITDIALOG As Integer = &H110
'    Friend Const WM_COMMAND As Integer = &H111
        Friend Const WM_SYSCOMMAND As Integer = &H112
'    Friend Const WM_TIMER As Integer = &H113
'    Friend Const WM_HSCROLL As Integer = &H114
'    Friend Const WM_VSCROLL As Integer = &H115
'    Friend Const WM_INITMENU As Integer = &H116
'    Friend Const WM_INITMENUPOPUP As Integer = &H117
'    Friend Const WM_MENUSELECT As Integer = &H11F
'    Friend Const WM_MENUCHAR As Integer = &H120
'    Friend Const WM_ENTERIDLE As Integer = &H121
'    Friend Const WM_CHANGEUISTATE As Integer = &H127
'    Friend Const WM_UPDATEUISTATE As Integer = &H128
'    Friend Const WM_QUERYUISTATE As Integer = &H129
'    Friend Const WM_CTLCOLORMSGBOX As Integer = &H132
'    Friend Const WM_CTLCOLOREDIT As Integer = &H133
'    Friend Const WM_CTLCOLORLISTBOX As Integer = &H134
'    Friend Const WM_CTLCOLORBTN As Integer = &H135
'    Friend Const WM_CTLCOLORDLG As Integer = &H136
'    Friend Const WM_CTLCOLORSCROLLBAR As Integer = &H137
'    Friend Const WM_CTLCOLORSTATIC As Integer = &H138
'    Friend Const WM_MOUSEFIRST As Integer = &H200
'    Friend Const WM_MOUSEMOVE As Integer = &H200
'    Friend Const WM_LBUTTONDOWN As Integer = &H201
'    Friend Const WM_LBUTTONUP As Integer = &H202
'    Friend Const WM_LBUTTONDBLCLK As Integer = &H203
        Friend Const WM_RBUTTONDOWN As Integer = &H204
        Friend Const WM_RBUTTONUP As Integer = &H205
'    Friend Const WM_RBUTTONDBLCLK As Integer = &H206
'    Friend Const WM_MBUTTONDOWN As Integer = &H207
'    Friend Const WM_MBUTTONUP As Integer = &H208
'    Friend Const WM_MBUTTONDBLCLK As Integer = &H209
'    Friend Const WM_NCMOUSEHOVER As Integer = &H2A0
'    Friend Const WM_NCMOUSELEAVE As Integer = &H2A2
'    Friend Const WM_MOUSEWHEEL As Integer = &H20A
'    Friend Const WM_MOUSELAST As Integer = &H20A
'    Friend Const WHEEL_DELTA As Integer = 120
'    Friend Const WM_PARENTNOTIFY As Integer = &H210
'    Friend Const WM_ENTERMENULOOP As Integer = &H211
'    Friend Const WM_EXITMENULOOP As Integer = &H212
'    Friend Const WM_NEXTMENU As Integer = &H213
'    Friend Const WM_SIZING As Integer = &H214
'    Friend Const WM_CAPTURECHANGED As Integer = &H215
'    Friend Const WM_MOVING As Integer = &H216
'    Friend Const WM_POWERBROADCAST As Integer = &H218
'    Friend Const WM_DEVICECHANGE As Integer = &H219
'    Friend Const WM_IME_SETCONTEXT As Integer = &H281
'    Friend Const WM_IME_NOTIFY As Integer = &H282
'    Friend Const WM_IME_CONTROL As Integer = &H283
'    Friend Const WM_IME_COMPOSITIONFULL As Integer = &H284
'    Friend Const WM_IME_SELECT As Integer = &H285
'    Friend Const WM_IME_CHAR As Integer = &H286
'    Friend Const WM_IME_KEYDOWN As Integer = &H290
'    Friend Const WM_IME_KEYUP As Integer = &H291
'    Friend Const WM_MDICREATE As Integer = &H220
'    Friend Const WM_MDIDESTROY As Integer = &H221
'    Friend Const WM_MDIACTIVATE As Integer = &H222
'    Friend Const WM_MDIRESTORE As Integer = &H223
'    Friend Const WM_MDINEXT As Integer = &H224
'    Friend Const WM_MDIMAXIMIZE As Integer = &H225
'    Friend Const WM_MDITILE As Integer = &H226
'    Friend Const WM_MDICASCADE As Integer = &H227
'    Friend Const WM_MDIICONARRANGE As Integer = &H228
'    Friend Const WM_MDIGETACTIVE As Integer = &H229
'    Friend Const WM_MDISETMENU As Integer = &H230
'    Friend Const WM_ENTERSIZEMOVE As Integer = &H231
'    Friend Const WM_EXITSIZEMOVE As Integer = &H232
'    Friend Const WM_DROPFILES As Integer = &H233
'    Friend Const WM_MDIREFRESHMENU As Integer = &H234
'    Friend Const WM_MOUSEHOVER As Integer = &H2A1
'    Friend Const WM_MOUSELEAVE As Integer = &H2A3
'    Friend Const WM_CUT As Integer = &H300
'    Friend Const WM_COPY As Integer = &H301
     Friend Const WM_PASTE As Integer = &H302
'    Friend Const WM_CLEAR As Integer = &H303
'    Friend Const WM_UNDO As Integer = &H304
'    Friend Const WM_RENDERFORMAT As Integer = &H305
'    Friend Const WM_RENDERALLFORMATS As Integer = &H306
'    Friend Const WM_DESTROYCLIPBOARD As Integer = &H307
'    Friend Const WM_DRAWCLIPBOARD As Integer = &H308
'    Friend Const WM_PAINTCLIPBOARD As Integer = &H309
'    Friend Const WM_VSCROLLCLIPBOARD As Integer = &H30A
'    Friend Const WM_SIZECLIPBOARD As Integer = &H30B
'    Friend Const WM_ASKCBFORMATNAME As Integer = &H30C
'    Friend Const WM_CHANGECBCHAIN As Integer = &H30D
'    Friend Const WM_HSCROLLCLIPBOARD As Integer = &H30E
'    Friend Const WM_QUERYNEWPALETTE As Integer = &H30F
'    Friend Const WM_PALETTEISCHANGING As Integer = &H310
        Friend Const WM_PALETTECHANGED As Integer = &H311
'    Friend Const WM_HOTKEY As Integer = &H312
'    Friend Const WM_PRINT As Integer = &H317
'    Friend Const WM_PRINTCLIENT As Integer = &H318
'    Friend Const WM_HANDHELDFIRST As Integer = &H358
'    Friend Const WM_HANDHELDLAST As Integer = &H35F
'    Friend Const WM_AFXFIRST As Integer = &H360
'    Friend Const WM_AFXLAST As Integer = &H37F
'    Friend Const WM_PENWINFIRST As Integer = &H380
'    Friend Const WM_PENWINLAST As Integer = &H38F
'    Friend Const WM_APP As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        WM_PENWINLAST = 0x038F,
'    '        WM_APP = unchecked((int)0x8000),
'    '------------------^--- GenCode(token): unexpected token type
     Friend Const WM_USER As Integer = &H400
'    Friend Const WM_REFLECT As Integer = WM_USER + &H1C00
'    Friend Const WMSZ_LEFT As Integer = 1
'    Friend Const WMSZ_RIGHT As Integer = 2
'    Friend Const WMSZ_TOP As Integer = 3
'    Friend Const WMSZ_TOPLEFT As Integer = 4
'    Friend Const WMSZ_TOPRIGHT As Integer = 5
'    Friend Const WMSZ_BOTTOM As Integer = 6
'    Friend Const WMSZ_BOTTOMLEFT As Integer = 7
'    Friend Const WMSZ_BOTTOMRIGHT As Integer = 8
'    Friend Const WVR_ALIGNTOP As Integer = &H10
'    Friend Const WVR_ALIGNLEFT As Integer = &H20
'    Friend Const WVR_ALIGNBOTTOM As Integer = &H40
'    Friend Const WVR_ALIGNRIGHT As Integer = &H80
'    Friend Const WVR_HREDRAW As Integer = &H100
'    Friend Const WVR_VREDRAW As Integer = &H200
'    Friend Const WVR_VALIDRECTS As Integer = &H400
'    Friend Const WS_OVERLAPPED As Integer = &H0
'    Friend Const WS_POPUP As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        WS_OVERLAPPED = 0x00000000,
'    '        WS_POPUP = unchecked((int)0x80000000),
'    '--------------------^--- GenCode(token): unexpected token type
'    Friend Const WS_CHILD As Integer = &H40000000
'    Friend Const WS_MINIMIZE As Integer = &H20000000
'    Friend Const WS_VISIBLE As Integer = &H10000000
'    Friend Const WS_DISABLED As Integer = &H8000000
'    Friend Const WS_CLIPSIBLINGS As Integer = &H4000000
'    Friend Const WS_CLIPCHILDREN As Integer = &H2000000
'    Friend Const WS_MAXIMIZE As Integer = &H1000000
'    Friend Const WS_CAPTION As Integer = &HC00000
'    Friend Const WS_BORDER As Integer = &H800000
'    Friend Const WS_DLGFRAME As Integer = &H400000
'    Friend Const WS_VSCROLL As Integer = &H200000
'    Friend Const WS_HSCROLL As Integer = &H100000
'    Friend Const WS_SYSMENU As Integer = &H80000
'    Friend Const WS_THICKFRAME As Integer = &H40000
'    Friend Const WS_GROUP As Integer = &H20000
'    Friend Const WS_TABSTOP As Integer = &H10000
'    Friend Const WS_MINIMIZEBOX As Integer = &H20000
'    Friend Const WS_MAXIMIZEBOX As Integer = &H10000
'    Friend Const WS_TILED As Integer = &H0
'    Friend Const WS_ICONIC As Integer = &H20000000
'    Friend Const WS_SIZEBOX As Integer = &H40000
'    Friend Const WS_OVERLAPPEDWINDOW As Integer = &H0 Or &HC00000 Or &H80000 Or &H40000 Or &H20000 Or &H10000
'    Friend Const WS_POPUPWINDOW As Integer = Or &H800000 Or &H80000
'    '
'    'Note:  Error processing original source shown below
'    '        WS_OVERLAPPEDWINDOW = (0x00000000|0x00C00000|0x00080000|0x00040000|0x00020000|0x00010000),
'    '                              WS_POPUPWINDOW = (unchecked((int)0x80000000)|0x00800000|0x00080000),
'    '-------------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const WS_CHILDWINDOW As Integer = &H40000000
'    Friend Const WS_EX_DLGMODALFRAME As Integer = &H1
'    Friend Const WS_EX_NOPARENTNOTIFY As Integer = &H4
'    Friend Const WS_EX_TOPMOST As Integer = &H8
'    Friend Const WS_EX_ACCEPTFILES As Integer = &H10
'    Friend Const WS_EX_TRANSPARENT As Integer = &H20
'    Friend Const WS_EX_MDICHILD As Integer = &H40
'    Friend Const WS_EX_TOOLWINDOW As Integer = &H80
'    Friend Const WS_EX_WINDOWEDGE As Integer = &H100
'    Friend Const WS_EX_CLIENTEDGE As Integer = &H200
'    Friend Const WS_EX_CONTEXTHELP As Integer = &H400
'    Friend Const WS_EX_RIGHT As Integer = &H1000
'    Friend Const WS_EX_LEFT As Integer = &H0
'    Friend Const WS_EX_RTLREADING As Integer = &H2000
'    Friend Const WS_EX_LTRREADING As Integer = &H0
'    Friend Const WS_EX_LEFTSCROLLBAR As Integer = &H4000
'    Friend Const WS_EX_RIGHTSCROLLBAR As Integer = &H0
'    Friend Const WS_EX_CONTROLPARENT As Integer = &H10000
'    Friend Const WS_EX_STATICEDGE As Integer = &H20000
'    Friend Const WS_EX_APPWINDOW As Integer = &H40000
'    Friend Const WS_EX_OVERLAPPEDWINDOW As Integer = &H100 Or &H200
'    Friend Const WS_EX_PALETTEWINDOW As Integer = &H100 Or &H80 Or &H8
'    Friend Const WS_EX_LAYERED As Integer = &H80000
'    Friend Const WS_EX_NOINHERITLAYOUT As Integer = &H100000
'    Friend Const WS_EX_LAYOUTRTL As Integer = &H400000
'    Friend Const WS_EX_NOACTIVATE As Integer = &H8000000
'    Friend Const WPF_SETMINPOSITION As Integer = &H1
'    Friend Const WPF_RESTORETOMAXIMIZED As Integer = &H2
'    Friend Const WB_LEFT As Integer = 0
'    Friend Const WB_RIGHT As Integer = 1
'    Friend Const WB_ISDELIMITER As Integer = 2
'    Friend Const WDT_INPROC_CALL As Integer = &H48746457
'    Friend Const WDT_REMOTE_CALL As Integer = &H52746457
'    Friend Const WM_CHOOSEFONT_GETLOGFONT As Integer = &H400 + 1
'    Friend Const WM_PSD_PAGESETUPDLG As Integer = &H400
'    Friend Const WM_PSD_FULLPAGERECT As Integer = &H400 + 1
'    Friend Const WM_PSD_MINMARGINRECT As Integer = &H400 + 2
'    Friend Const WM_PSD_MARGINRECT As Integer = &H400 + 3
'    Friend Const WM_PSD_GREEKTEXTRECT As Integer = &H400 + 4
'    Friend Const WM_PSD_ENVSTAMPRECT As Integer = &H400 + 5
'    Friend Const WM_PSD_YAFULLPAGERECT As Integer = &H400 + 6
'    Friend Const WAIT_IO_COMPLETION As Integer = &HC0
'    Friend Const WS_TILEDWINDOW As Integer = &H0 Or &HC00000 Or &H80000 Or &H40000 Or &H20000 Or &H10000
    
'    ' NT5 Begin 
'    ' Disable inheritence of mirroring by children
'    ' Right to left mirroring
'    ' NT5 End 
    
    
'    Friend Const WAIT_OBJECT_0 As Integer = &H0
'    Friend Const WAIT_FAILED As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        public const int WAIT_OBJECT_0 = 0x00000000,
'    '        WAIT_FAILED = unchecked((int)0xFFFFFFFF),
'    '-----------------------^--- GenCode(token): unexpected token type
'    Friend Const WAIT_TIMEOUT As Integer = &H102
'    Friend Const WAIT_ABANDONED As Integer = &H80
'    Friend Const WAIT_ABANDONED_0 As Integer = WAIT_ABANDONED
'    Friend Const WHITENESS As Integer = &HFF0062
    
    
'    Friend Const XST_NULL As Integer = 0
'    Friend Const XST_INCOMPLETE As Integer = 1
'    Friend Const XST_CONNECTED As Integer = 2
'    Friend Const XST_INIT1 As Integer = 3
'    Friend Const XST_INIT2 As Integer = 4
'    Friend Const XST_REQSENT As Integer = 5
'    Friend Const XST_DATARCVD As Integer = 6
'    Friend Const XST_POKESENT As Integer = 7
'    Friend Const XST_POKEACKRCVD As Integer = 8
'    Friend Const XST_EXECSENT As Integer = 9
'    Friend Const XST_EXECACKRCVD As Integer = 10
'    Friend Const XST_ADVSENT As Integer = 11
'    Friend Const XST_UNADVSENT As Integer = 12
'    Friend Const XST_ADVACKRCVD As Integer = 13
'    Friend Const XST_UNADVACKRCVD As Integer = 14
'    Friend Const XST_ADVDATASENT As Integer = 15
'    Friend Const XST_ADVDATAACKRCVD As Integer = 16
'    Friend Const XTYPF_NOBLOCK As Integer = &H2
'    Friend Const XTYPF_NODATA As Integer = &H4
'    Friend Const XTYPF_ACKREQ As Integer = &H8
'    Friend Const XCLASS_MASK As Integer = &HFC00
'    Friend Const XCLASS_BOOL As Integer = &H1000
'    Friend Const XCLASS_DATA As Integer = &H2000
'    Friend Const XCLASS_FLAGS As Integer = &H4000
'    Friend Const XCLASS_NOTIFICATION As Integer = 
'    '
'    'Note:  Error processing original source shown below
'    '        XCLASS_FLAGS = 0x4000,
'    '        XCLASS_NOTIFICATION = unchecked((int)0x8000),
'    '-------------------------------^--- GenCode(token): unexpected token type
'    Friend Const XTYP_ERROR As Integer = &H0 Or Or &H2
'    '
'    'Note:  Error processing original source shown below
'    '        XCLASS_NOTIFICATION = unchecked((int)0x8000),
'    '        XTYP_ERROR = (0x0000|unchecked((int)0x8000)|0x0002),
'    '------------------------------^--- GenCode(token): unexpected token type
'    Friend Const XTYP_ADVDATA As Integer = &H10 Or &H4000
'    Friend Const XTYP_ADVREQ As Integer = &H20 Or &H2000 Or &H2
'    Friend Const XTYP_ADVSTART As Integer = &H30 Or &H1000
'    Friend Const XTYP_ADVSTOP As Integer = &H40 Or
'    '
'    'Note:  Error processing original source shown below
'    '                                                  XTYP_ADVSTART = (0x0030|0x1000),
'    '                                                                  XTYP_ADVSTOP = (0x0040|unchecked((int)0x8000)),
'    '------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const XTYP_EXECUTE As Integer = &H50 Or &H4000
'    Friend Const XTYP_CONNECT As Integer = &H60 Or &H1000 Or &H2
'    Friend Const XTYP_CONNECT_CONFIRM As Integer = &H70 Or Or &H2
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                XTYP_CONNECT = (0x0060|0x1000|0x0002),
'    '                                                                                                               XTYP_CONNECT_CONFIRM = (0x0070|unchecked((int)0x8000)|0x0002),
'    '-----------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const XTYP_XACT_COMPLETE As Integer = &H80 Or
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                               XTYP_CONNECT_CONFIRM = (0x0070|unchecked((int)0x8000)|0x0002),
'    '                                                                                                                                      XTYP_XACT_COMPLETE = (0x0080|unchecked((int)0x8000)),
'    '--------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const XTYP_POKE As Integer = &H90 Or &H4000
'    Friend Const XTYP_REGISTER As Integer = &HA0 Or Or &H2
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                           XTYP_POKE = (0x0090|0x4000),
'    '                                                                                                                                                                       XTYP_REGISTER = (0x00A0|unchecked((int)0x8000)|0x0002),
'    '------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const XTYP_REQUEST As Integer = &HB0 Or &H2000
'    Friend Const XTYP_DISCONNECT As Integer = &HC0 Or Or &H2
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                                                       XTYP_REQUEST = (0x00B0|0x2000),
'    '                                                                                                                                                                                                      XTYP_DISCONNECT = (0x00C0|unchecked((int)0x8000)|0x0002),
'    '---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const XTYP_UNREGISTER As Integer = &HD0 Or Or &H2
'    '
'    'Note:  Error processing original source shown below
'    '                                                                                                                                                                                                      XTYP_DISCONNECT = (0x00C0|unchecked((int)0x8000)|0x0002),
'    '                                                                                                                                                                                                                        XTYP_UNREGISTER = (0x00D0|unchecked((int)0x8000)|0x0002),
'    '---------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------------^--- GenCode(token): unexpected token type
'    Friend Const XTYP_WILDCONNECT As Integer = &HE0 Or &H2000 Or &H2
'    Friend Const XTYP_MASK As Integer = &HF0
'    Friend Const XTYP_SHIFT As Integer = 4
'    Friend Const XTYP_MONITOR As Integer = &HF0 Or Or &H2
'    '
'    'Note:  Error processing original source shown below
'    '        XTYP_SHIFT = 4,
'    '        XTYP_MONITOR = (0x00F0|unchecked((int)0x8000)|0x0002);
'    '--------------------------------^--- GenCode(token): unexpected token type
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
    
'    Friend Shared CBEM_GETITEM As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.CBEM_GETITEMA, win.CBEM_GETITEMW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared CBEM_SETITEM As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.CBEM_SETITEMA, win.CBEM_SETITEMW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared CBEN_ENDEDIT As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.CBEN_ENDEDITA, win.CBEN_ENDEDITW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared CBEM_INSERTITEM As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.CBEM_INSERTITEMA, win.CBEM_INSERTITEMW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared LVM_GETITEMTEXT As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.LVM_GETITEMTEXTA, win.LVM_GETITEMTEXTW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared LVM_SETITEMTEXT As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.LVM_SETITEMTEXTA, win.LVM_SETITEMTEXTW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared ACM_OPEN As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.ACM_OPENA, win.ACM_OPENW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared DTM_SETFORMAT As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.DTM_SETFORMATA, win.DTM_SETFORMATW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared DTN_USERSTRING As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.DTN_USERSTRINGA, win.DTN_USERSTRINGW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared DTN_WMKEYDOWN As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.DTN_WMKEYDOWNA, win.DTN_WMKEYDOWNW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared DTN_FORMAT As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.DTN_FORMATA, win.DTN_FORMATW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared DTN_FORMATQUERY As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.DTN_FORMATQUERYA, win.DTN_FORMATQUERYW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared EMR_EXTTEXTOUT As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.EMR_EXTTEXTOUTA, win.EMR_EXTTEXTOUTW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared EMR_POLYTEXTOUT As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.EMR_POLYTEXTOUTA, win.EMR_POLYTEXTOUTW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared HDM_INSERTITEM As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.HDM_INSERTITEMA, win.HDM_INSERTITEMW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared HDM_GETITEM As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.HDM_GETITEMA, win.HDM_GETITEMW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared HDM_SETITEM As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.HDM_SETITEMA, win.HDM_SETITEMW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared HDN_ITEMCHANGING As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.HDN_ITEMCHANGINGA, win.HDN_ITEMCHANGINGW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared HDN_ITEMCHANGED As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.HDN_ITEMCHANGEDA, win.HDN_ITEMCHANGEDW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared HDN_ITEMCLICK As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.HDN_ITEMCLICKA, win.HDN_ITEMCLICKW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared HDN_ITEMDBLCLICK As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.HDN_ITEMDBLCLICKA, win.HDN_ITEMDBLCLICKW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared HDN_DIVIDERDBLCLICK As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.HDN_DIVIDERDBLCLICKA, win.HDN_DIVIDERDBLCLICKW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared HDN_BEGINTRACK As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.HDN_BEGINTRACKA, win.HDN_BEGINTRACKW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared HDN_ENDTRACK As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.HDN_ENDTRACKA, win.HDN_ENDTRACKW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared HDN_TRACK As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.HDN_TRACKA, win.HDN_TRACKW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared HDN_GETDISPINFO As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.HDN_GETDISPINFOA, win.HDN_GETDISPINFOW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared LVM_GETITEM As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.LVM_GETITEMA, win.LVM_GETITEMW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared LVM_SETITEM As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.LVM_SETITEMA, win.LVM_SETITEMW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared LVM_INSERTITEM As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.LVM_INSERTITEMA, win.LVM_INSERTITEMW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared LVM_FINDITEM As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.LVM_FINDITEMA, win.LVM_FINDITEMW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared LVM_GETSTRINGWIDTH As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.LVM_GETSTRINGWIDTHA, win.LVM_GETSTRINGWIDTHW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared LVM_EDITLABEL As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.LVM_EDITLABELA, win.LVM_EDITLABELW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared LVM_GETCOLUMN As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.LVM_GETCOLUMNA, win.LVM_GETCOLUMNW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared LVM_SETCOLUMN As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.LVM_SETCOLUMNA, win.LVM_SETCOLUMNW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared LVM_GETISEARCHSTRING As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.LVM_GETISEARCHSTRINGA, win.LVM_GETISEARCHSTRINGW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared LVM_INSERTCOLUMN As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.LVM_INSERTCOLUMNA, win.LVM_INSERTCOLUMNW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared LVN_BEGINLABELEDIT As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.LVN_BEGINLABELEDITA, win.LVN_BEGINLABELEDITW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared LVN_ENDLABELEDIT As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.LVN_ENDLABELEDITA, win.LVN_ENDLABELEDITW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared LVN_ODFINDITEM As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.LVN_ODFINDITEMA, win.LVN_ODFINDITEMW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared LVN_GETDISPINFO As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.LVN_GETDISPINFOA, win.LVN_GETDISPINFOW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared LVN_SETDISPINFO As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.LVN_SETDISPINFOA, win.LVN_SETDISPINFOW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared PSM_SETTITLE As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.PSM_SETTITLEA, win.PSM_SETTITLEW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared PSM_SETFINISHTEXT As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.PSM_SETFINISHTEXTA, win.PSM_SETFINISHTEXTW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared RB_INSERTBAND As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.RB_INSERTBANDA, win.RB_INSERTBANDW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared RB_SETBANDINFO As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.RB_SETBANDINFOA, win.RB_SETBANDINFOW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared SB_SETTEXT As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.SB_SETTEXTA, win.SB_SETTEXTW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared SB_GETTEXT As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.SB_GETTEXTA, win.SB_GETTEXTW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared SB_GETTEXTLENGTH As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.SB_GETTEXTLENGTHA, win.SB_GETTEXTLENGTHW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared SB_SETTIPTEXT As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.SB_SETTIPTEXTA, win.SB_SETTIPTEXTW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared SB_GETTIPTEXT As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.SB_GETTIPTEXTA, win.SB_GETTIPTEXTW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared TB_SAVERESTORE As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TB_SAVERESTOREA, win.TB_SAVERESTOREW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared TB_ADDSTRING As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TB_ADDSTRINGA, win.TB_ADDSTRINGW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared TB_GETBUTTONTEXT As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TB_GETBUTTONTEXTA, win.TB_GETBUTTONTEXTW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared TB_MAPACCELERATOR As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TB_MAPACCELERATORA, win.TB_MAPACCELERATORW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared TB_GETBUTTONINFO As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TB_GETBUTTONINFOA, win.TB_GETBUTTONINFOW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared TB_SETBUTTONINFO As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TB_SETBUTTONINFOA, win.TB_SETBUTTONINFOW) 'Note:  For performance reasons this should be changed to nested IF statements
'    '
'    'Note:  Error processing original source shown below
'    '        public static readonly int TB_SETBUTTONINFO = Marshal.SystemDefaultCharSize == 1 ? win.TB_SETBUTTONINFOA : win.TB_SETBUTTONINFOW;
'    '#if cpr
'    '---^--- Pre-processor directives not translated
'    Friend Const RB_GETBANDINFO As Integer = IIf(Win32Lib.systemCommCtrlVersion >= Win32Lib.WIN32_IE400, IIf(Marshal.SystemDefaultCharSize = 1, win.RB_GETBANDINFOA, win.RB_GETBANDINFOW), win.RB_GETBANDINFO_OLD)
'    'Note:  For performance reasons this should be changed to nested IF statements
'    'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Const TB_INSERTBUTTON As Integer = IIf(Marshal.SystemDefaultCharSize = 1 OrElse Win32Lib.systemCommCtrlVersion < Win32Lib.WIN32_IE400, win.TB_INSERTBUTTONA, win.TB_INSERTBUTTONW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Const TB_ADDBUTTONS As Integer = IIf(Marshal.SystemDefaultCharSize = 1 OrElse Win32Lib.systemCommCtrlVersion < Win32Lib.WIN32_IE400, win.TB_ADDBUTTONSA, win.TB_ADDBUTTONSW) 'Note:  For performance reasons this should be changed to nested IF statements
'    '
'    'Note:  Error processing original source shown below
'    '                                         win.TB_ADDBUTTONSA : win.TB_ADDBUTTONSW;
'    '#else
'    '---^--- Pre-processor directives not translated
'    Friend Shared RB_GETBANDINFO As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.RB_GETBANDINFOA, win.RB_GETBANDINFOW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared TB_INSERTBUTTON As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TB_INSERTBUTTONA, win.TB_INSERTBUTTONW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared TB_ADDBUTTONS As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TB_ADDBUTTONSA, win.TB_ADDBUTTONSW) 'Note:  For performance reasons this should be changed to nested IF statements
'    '
'    'Note:  Error processing original source shown below
'    '        public static readonly int TB_ADDBUTTONS = (Marshal.SystemDefaultCharSize == 1) ? win.TB_ADDBUTTONSA : win.TB_ADDBUTTONSW;
'    '#endif
'    '---^--- Pre-processor directives not translated
'    Friend Shared TBN_GETBUTTONINFO As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TBN_GETBUTTONINFOA, win.TBN_GETBUTTONINFOW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared TBN_GETINFOTIP As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TBN_GETINFOTIPA, win.TBN_GETINFOTIPW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared TBN_GETDISPINFO As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TBN_GETDISPINFOA, win.TBN_GETDISPINFOW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared TTM_ADDTOOL As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TTM_ADDTOOLA, win.TTM_ADDTOOLW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared TTM_DELTOOL As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TTM_DELTOOLA, win.TTM_DELTOOLW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared TTM_NEWTOOLRECT As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TTM_NEWTOOLRECTA, win.TTM_NEWTOOLRECTW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared TTM_GETTOOLINFO As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TTM_GETTOOLINFOA, win.TTM_GETTOOLINFOW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared TTM_SETTOOLINFO As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TTM_SETTOOLINFOA, win.TTM_SETTOOLINFOW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared TTM_HITTEST As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TTM_HITTESTA, win.TTM_HITTESTW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared TTM_GETTEXT As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TTM_GETTEXTA, win.TTM_GETTEXTW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared TTM_UPDATETIPTEXT As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TTM_UPDATETIPTEXTA, win.TTM_UPDATETIPTEXTW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared TTM_ENUMTOOLS As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TTM_ENUMTOOLSA, win.TTM_ENUMTOOLSW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared TTM_GETCURRENTTOOL As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TTM_GETCURRENTTOOLA, win.TTM_GETCURRENTTOOLW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared TTN_GETDISPINFO As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TTN_GETDISPINFOA, win.TTN_GETDISPINFOW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared TTN_NEEDTEXT As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TTN_NEEDTEXTA, win.TTN_NEEDTEXTW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared TVM_INSERTITEM As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TVM_INSERTITEMA, win.TVM_INSERTITEMW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared TVM_GETITEM As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TVM_GETITEMA, win.TVM_GETITEMW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared TVM_SETITEM As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TVM_SETITEMA, win.TVM_SETITEMW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared TVM_EDITLABEL As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TVM_EDITLABELA, win.TVM_EDITLABELW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared TVM_GETISEARCHSTRING As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TVM_GETISEARCHSTRINGA, win.TVM_GETISEARCHSTRINGW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared TVN_SELCHANGING As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TVN_SELCHANGINGA, win.TVN_SELCHANGINGW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared TVN_SELCHANGED As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TVN_SELCHANGEDA, win.TVN_SELCHANGEDW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared TVN_GETDISPINFO As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TVN_GETDISPINFOA, win.TVN_GETDISPINFOW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared TVN_SETDISPINFO As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TVN_SETDISPINFOA, win.TVN_SETDISPINFOW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared TVN_ITEMEXPANDING As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TVN_ITEMEXPANDINGA, win.TVN_ITEMEXPANDINGW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared TVN_ITEMEXPANDED As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TVN_ITEMEXPANDEDA, win.TVN_ITEMEXPANDEDW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared TVN_BEGINDRAG As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TVN_BEGINDRAGA, win.TVN_BEGINDRAGW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared TVN_BEGINRDRAG As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TVN_BEGINRDRAGA, win.TVN_BEGINRDRAGW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared TVN_DELETEITEM As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TVN_DELETEITEMA, win.TVN_DELETEITEMW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared TVN_BEGINLABELEDIT As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TVN_BEGINLABELEDITA, win.TVN_BEGINLABELEDITW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared TVN_ENDLABELEDIT As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TVN_ENDLABELEDITA, win.TVN_ENDLABELEDITW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared TCM_GETITEM As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TCM_GETITEMA, win.TCM_GETITEMW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared TCM_SETITEM As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TCM_SETITEMA, win.TCM_SETITEMW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared TCM_INSERTITEM As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.TCM_INSERTITEMA, win.TCM_INSERTITEMW) 'Note:  For performance reasons this should be changed to nested IF statements
'    Friend Shared CP_WINNEUTRAL As Integer = IIf(Marshal.SystemDefaultCharSize = 1, win.CP_WINANSI, win.CP_WINUNICODE) 'Note:  For performance reasons this should be changed to nested IF statements
    
    
'    Friend Shared LBSELCHSTRINGA As String = "commdlg_LBSelChangedNotify"
'    Friend Shared SHAREVISTRINGA As String = "commdlg_ShareViolation"
'    Friend Shared FILEOKSTRINGA As String = "commdlg_FileNameOK"
'    Friend Shared COLOROKSTRINGA As String = "commdlg_ColorOK"
'    Friend Shared SETRGBSTRINGA As String = "commdlg_SetRGBColor"
'    Friend Shared HELPMSGSTRINGA As String = "commdlg_help"
'    Friend Shared FINDMSGSTRINGA As String = "commdlg_FindReplace"
'    Friend Shared LBSELCHSTRINGW As String = "commdlg_LBSelChangedNotify"
'    Friend Shared SHAREVISTRINGW As String = "commdlg_ShareViolation"
'    Friend Shared FILEOKSTRINGW As String = "commdlg_FileNameOK"
'    Friend Shared COLOROKSTRINGW As String = "commdlg_ColorOK"
'    Friend Shared SETRGBSTRINGW As String = "commdlg_SetRGBColor"
'    Friend Shared HELPMSGSTRINGW As String = "commdlg_help"
'    Friend Shared FINDMSGSTRINGW As String = "commdlg_FindReplace"
'    Friend Shared SZDDESYS_TOPIC As String = "System"
'    Friend Shared SZDDESYS_ITEM_TOPICS As String = "Topics"
'    Friend Shared SZDDESYS_ITEM_SYSITEMS As String = "SysItems"
'    Friend Shared SZDDESYS_ITEM_RTNMSG As String = "ReturnMessage"
'    Friend Shared SZDDESYS_ITEM_STATUS As String = "Status"
'    Friend Shared SZDDESYS_ITEM_FORMATS As String = "Formats"
'    Friend Shared SZDDESYS_ITEM_HELP As String = "Help"
'    Friend Shared SZDDE_ITEM_ITEMLIST As String = "TopicItemList"
'    Friend Shared ALL_TRANSPORTS As String = "M???"
'    Friend Shared MS_NBF As String = "MNBF"
'    Friend Shared MS_DEF_PROV_A As String = "Microsoft Base Cryptographic Provider v1.0"
'    Friend Shared MS_DEF_PROV_W As String = "Microsoft Base Cryptographic Provider v1.0"
'    Friend Shared SE_CREATE_TOKEN_NAME As String = "SeCreateTokenPrivilege"
'    Friend Shared SE_ASSIGNPRIMARYTOKEN_NAME As String = "SeAssignPrimaryTokenPrivilege"
'    Friend Shared SE_LOCK_MEMORY_NAME As String = "SeLockMemoryPrivilege"
'    Friend Shared SE_INCREASE_QUOTA_NAME As String = "SeIncreaseQuotaPrivilege"
'    Friend Shared SE_UNSOLICITED_INPUT_NAME As String = "SeUnsolicitedInputPrivilege"
'    Friend Shared SE_MACHINE_ACCOUNT_NAME As String = "SeMachineAccountPrivilege"
'    Friend Shared SE_TCB_NAME As String = "SeTcbPrivilege"
'    Friend Shared SE_SECURITY_NAME As String = "SeSecurityPrivilege"
'    Friend Shared SE_TAKE_OWNERSHIP_NAME As String = "SeTakeOwnershipPrivilege"
'    Friend Shared SE_LOAD_DRIVER_NAME As String = "SeLoadDriverPrivilege"
'    Friend Shared SE_SYSTEM_PROFILE_NAME As String = "SeSystemProfilePrivilege"
'    Friend Shared SE_SYSTEMTIME_NAME As String = "SeSystemtimePrivilege"
'    Friend Shared SE_PROF_SINGLE_PROCESS_NAME As String = "SeProfileSingleProcessPrivilege"
'    Friend Shared SE_INC_BASE_PRIORITY_NAME As String = "SeIncreaseBasePriorityPrivilege"
'    Friend Shared SE_CREATE_PAGEFILE_NAME As String = "SeCreatePagefilePrivilege"
'    Friend Shared SE_CREATE_PERMANENT_NAME As String = "SeCreatePermanentPrivilege"
'    Friend Shared SE_BACKUP_NAME As String = "SeBackupPrivilege"
'    Friend Shared SE_RESTORE_NAME As String = "SeRestorePrivilege"
'    Friend Shared SE_SHUTDOWN_NAME As String = "SeShutdownPrivilege"
'    Friend Shared SE_DEBUG_NAME As String = "SeDebugPrivilege"
'    Friend Shared SE_AUDIT_NAME As String = "SeAuditPrivilege"
'    Friend Shared SE_SYSTEM_ENVIRONMENT_NAME As String = "SeSystemEnvironmentPrivilege"
'    Friend Shared SE_CHANGE_NOTIFY_NAME As String = "SeChangeNotifyPrivilege"
'    Friend Shared SE_REMOTE_SHUTDOWN_NAME As String = "SeRemoteShutdownPrivilege"
'    Friend Shared SPLREG_DEFAULT_SPOOL_DIRECTORY As String = "DefaultSpoolDirectory"
'    Friend Shared SPLREG_PORT_THREAD_PRIORITY_DEFAULT As String = "PortThreadPriorityDefault"
'    Friend Shared SPLREG_PORT_THREAD_PRIORITY As String = "PortThreadPriority"
'    Friend Shared SPLREG_SCHEDULER_THREAD_PRIORITY_DEFAULT As String = "SchedulerThreadPriorityDefault"
'    Friend Shared SPLREG_SCHEDULER_THREAD_PRIORITY As String = "SchedulerThreadPriority"
'    Friend Shared SPLREG_BEEP_ENABLED As String = "BeepEnabled"
'    Friend Shared SPLREG_NET_POPUP As String = "NetPopup"
'    Friend Shared SPLREG_EVENT_LOG As String = "EventLog"
'    Friend Shared SPLREG_MAJOR_VERSION As String = "MajorVersion"
'    Friend Shared SPLREG_MINOR_VERSION As String = "MinorVersion"
'    Friend Shared SPLREG_ARCHITECTURE As String = "Architecture"
'    Friend Shared SERVICES_ACTIVE_DATABASEW As String = "ServicesActive"
'    Friend Shared SERVICES_FAILED_DATABASEW As String = "ServicesFailed"
'    Friend Shared SERVICES_ACTIVE_DATABASEA As String = "ServicesActive"
'    Friend Shared SERVICES_FAILED_DATABASEA As String = "ServicesFailed"
'    Friend Shared WC_HEADERA As String = "SysHeader32"
'    Friend Shared WC_HEADERW As String = "SysHeader32"
'    Friend Shared WC_HEADER As String = "SysHeader"
'    Friend Shared TOOLBARCLASSNAMEW As String = "WFCToolbarWindow32"
'    Friend Shared TOOLBARCLASSNAMEA As String = "WFCToolbarWindow32"
'    Friend Shared TOOLBARCLASSNAME As String = "WFCToolbarWindow32"
'    Friend Shared TOOLBARCLASSNAMEW As String = "ToolbarWindow32"
'    Friend Shared TOOLBARCLASSNAMEA As String = "ToolbarWindow32"
'    Friend Shared TOOLBARCLASSNAME As String = "ToolbarWindow32"
'    Friend Shared REBARCLASSNAMEW As String = "ReBarWindow32"
'    Friend Shared REBARCLASSNAMEA As String = "ReBarWindow32"
'    Friend Shared REBARCLASSNAME As String = "ReBarWindow32"
'    Friend Shared TOOLTIPS_CLASSW As String = "WFCTooltips32"
'    Friend Shared TOOLTIPS_CLASSA As String = "WFCTooltips32"
'    Friend Shared TOOLTIPS_CLASS As String = "WFCTooltips32"
'    Friend Shared TOOLTIPS_CLASSW As String = "tooltips_class32"
'    Friend Shared TOOLTIPS_CLASSA As String = "tooltips_class32"
'    Friend Shared TOOLTIPS_CLASS As String = "tooltips_class32"
'    Friend Shared DRAGLISTMSGSTRING As String = "commctrl_DragListMsg"
'    Friend Shared HOTKEY_CLASSA As String = "msctls_hotkey32"
'    Friend Shared HOTKEY_CLASSW As String = "msctls_hotkey32"
'    Friend Shared HOTKEY_CLASS As String = "msctls_hotkey32"
'    Friend Shared WC_BUTTON As String = "BUTTON"
'    Friend Shared WC_COMBOBOX As String = "COMBOBOX"
'    Friend Shared WC_EDIT As String = "EDIT"
'    Friend Shared WC_LISTBOX As String = "LISTBOX"
'    Friend Shared WC_MDICLIENT As String = "MDICLIENT"
'    Friend Shared WC_SCROLLBAR As String = "SCROLLBAR"
'    Friend Shared WC_RICHEDITA As String = "RichEdit32"
'    Friend Shared WC_RICHEDITW As String = "RichEdit32"
'    Friend Shared WC_RICHEDIT As String = "RichEdit32"
'    Friend Shared WC_DATETIMEPICKA As String = "WFCDateTimePick32"
'    Friend Shared WC_DATETIMEPICKW As String = "WFCDateTimePick32"
'    Friend Shared WC_DATETIMEPICK As String = "WFCDateTimePick32"
'    Friend Shared WC_LISTVIEWA As String = "WFCListView32"
'    Friend Shared WC_LISTVIEWW As String = "WFCListView32"
'    Friend Shared WC_LISTVIEW As String = "WFCListView32"
'    Friend Shared WC_MONTHCALA As String = "WFCMonthCal32"
'    Friend Shared WC_MONTHCALW As String = "WFCMonthCal32"
'    Friend Shared WC_MONTHCAL As String = "WFCMonthCal32"
'    Friend Shared WC_PROGRESSA As String = "WFCProgress32"
'    Friend Shared WC_PROGRESSW As String = "WFCProgress32"
'    Friend Shared WC_PROGRESS As String = "WFCProgress32"
'    Friend Shared WC_STATUSBARA As String = "WFCStatusBar32"
'    Friend Shared WC_STATUSBARW As String = "WFCStatusBar32"
'    Friend Shared WC_STATUSBAR As String = "WFCStatusBar32"
'    Friend Shared WC_TOOLBAR As String = "WFCToolbarWindow32"
'    Friend Shared WC_TRACKBARA As String = "WFCTrackbar32"
'    Friend Shared WC_TRACKBARW As String = "WFCTrackbar32"
'    Friend Shared WC_TRACKBAR As String = "WFCTrackbar32"
'    Friend Shared WC_TREEVIEWA As String = "WFCTreeView32"
'    Friend Shared WC_TREEVIEWW As String = "WFCTreeView32"
'    Friend Shared WC_TREEVIEW As String = "WFCTreeView32"
'    Friend Shared WC_DATETIMEPICKA As String = "SysDateTimePick32"
'    Friend Shared WC_DATETIMEPICKW As String = "SysDateTimePick32"
'    Friend Shared WC_DATETIMEPICK As String = "SysDateTimePick32"
'    Friend Shared WC_LISTVIEWA As String = "SysListView32"
'    Friend Shared WC_LISTVIEWW As String = "SysListView32"
'    Friend Shared WC_LISTVIEW As String = "SysListView32"
'    Friend Shared WC_MONTHCALA As String = "SysMonthCal32"
'    Friend Shared WC_MONTHCALW As String = "SysMonthCal32"
'    Friend Shared WC_MONTHCAL As String = "SysMonthCal32"
'    Friend Shared WC_PROGRESSA As String = "msctls_progress32"
'    Friend Shared WC_PROGRESSW As String = "msctls_progress32"
'    Friend Shared WC_PROGRESS As String = "msctls_progress32"
'    Friend Shared WC_STATUSBARA As String = "msctls_statusbar32"
'    Friend Shared WC_STATUSBARW As String = "msctls_statusbar32"
'    Friend Shared WC_STATUSBAR As String = "msctls_statusbar32"
'    Friend Shared WC_TOOLBAR As String = "ToolbarWindow32"
'    Friend Shared WC_TRACKBARA As String = "msctls_trackbar32"
'    Friend Shared WC_TRACKBARW As String = "msctls_trackbar32"
'    Friend Shared WC_TRACKBAR As String = "msctls_trackbar32"
'    Friend Shared WC_TREEVIEWA As String = "SysTreeView32"
'    Friend Shared WC_TREEVIEWW As String = "SysTreeView32"
'    Friend Shared WC_TREEVIEW As String = "SysTreeView32"
'    Friend Shared WC_COMBOBOXEXW As String = "ComboBoxEx32"
'    Friend Shared WC_COMBOBOXEXA As String = "ComboBoxEx32"
'    Friend Shared WC_COMBOBOXEX As String = "ComboBoxEx32"
'    Friend Shared WC_STATIC As String = "STATIC"
'    Friend Shared WC_TABCONTROLA As String = "WFCTabControl32"
'    Friend Shared WC_TABCONTROLW As String = "WFCTabControl32"
'    Friend Shared WC_TABCONTROL As String = "WFCTabControl32"
'    Friend Shared WC_TABCONTROLA As String = "SysTabControl32"
'    Friend Shared WC_TABCONTROLW As String = "SysTabControl32"
'    Friend Shared WC_TABCONTROL As String = "SysTabControl32"
'    Friend Shared LBSELCHSTRING As String = "commdlg_LBSelChangedNotify"
'    Friend Shared FINDMSGSTRING As String = "commdlg_FindReplace"
'    Friend Shared SHAREVISTRING As String = "commdlg_ShareViolation"
'    Friend Shared SERVICES_FAILED_DATABASE As String = "ServicesFailed"
'    Friend Shared MS_DEF_PROV_ As String = "Microsoft Base Cryptographic Provider v1.0"
'    Friend Shared HELPMSGSTRING As String = "commdlg_help"
'    Friend Shared FILEOKSTRING As String = "commdlg_FileNameOK"
'    Friend Shared SERVICES_ACTIVE_DATABASE As String = "ServicesActive"
'    Friend Shared COLOROKSTRING As String = "commdlg_ColorOK"
'    Friend Shared SETRGBSTRING As String = "commdlg_SetRGBColor"
'    Friend Shared MSH_MOUSEWHEEL As String = "MSWHEEL_ROLLMSG"
'    Friend Shared MSH_SCROLL_LINES As String = "MSH_SCROLL_LINES_MSG"
'    Friend Shared MSH_WHEELSUPPORT As String = "MSH_WHEELSUPPORT_MSG"
'    Friend Shared MOUSEZ_CLASSNAME As String = "MouseZ"
'    Friend Shared MOUSEZ_TITLE As String = "Magellan MSWHEEL"
    
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
    
    
'    Friend Shared HKEY_CLASSES_ROOT As RegistryHive = RegistryHive.ClassesRoot
'    Friend Shared HKEY_CURRENT_USER As RegistryHive = RegistryHive.CurrentUser
'    Friend Shared HKEY_LOCAL_MACHINE As RegistryHive = RegistryHive.LocalMachine
'    Friend Shared HKEY_USERS As RegistryHive = RegistryHive.Users
'    Friend Shared HKEY_PERFORMANCE_DATA As RegistryHive = RegistryHive.PerformanceData
'    Friend Shared HKEY_CURRENT_CONFIG As RegistryHive = RegistryHive.CurrentConfig
'    Friend Shared HKEY_DYN_DATA As RegistryHive = RegistryHive.DynData
End Class 'win

End Namespace
