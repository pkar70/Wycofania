


Public MustInherit Class Source_Base
    ' ułatwienie dodawania następnych
    Protected MustOverride Property SRC_SOURCE_FULL_NAME As String ' pełna nazwa (na stronie SettingsSources)
    Protected MustOverride Property SRC_SETTING_NAME As String  ' krótka nazwa, do SetSettings (a także jako ikonka)
    Protected Overridable Property SRC_DEFAULT_ENABLE As Boolean = True ' czy domyślnie jest włączone (tylko GIF ma tak)
    Protected Overridable Property SRC_MAXWEEKS As Integer = 8 ' jak stary moze byc
    Protected Overridable Property SRC_DEFAULT_ONETOAST As Boolean = False
    Protected MustOverride Property SRC_ABOUTUS_LINK As String
    Protected MustOverride Property SRC_SEARCH_LINK As String
    Public MustOverride Async Function ReadData(bMsg As Boolean) As Task(Of ObjectModel.Collection(Of JednoPowiadomienie))

    Public Function GetSettingName() As String
        Return SRC_SETTING_NAME
    End Function

    Public Function GetDefEnable() As Boolean
        Return SRC_DEFAULT_ENABLE
    End Function

    Public Function GetDefOneToast() As Boolean
        Return SRC_DEFAULT_ONETOAST
    End Function
    Public Function GetMaxWeeks() As Integer
        Return SRC_MAXWEEKS
    End Function

    Public Function GetFullName() As String
        Return SRC_SOURCE_FULL_NAME
    End Function

    Public Function GetAboutUs() As String
        Return SRC_ABOUTUS_LINK
    End Function

    Public Function GetSearchLink() As String
        Return SRC_SEARCH_LINK
    End Function

    Protected Function NewPowiadomienie() As JednoPowiadomienie
        Dim oNew As JednoPowiadomienie = New JednoPowiadomienie
        oNew.sSourceFullName = SRC_SOURCE_FULL_NAME
        oNew.sIcon = SRC_SETTING_NAME
        oNew.bNew = True
        Return oNew
    End Function

    ''' <summary>
    ''' String yyyy.MM.dd
    ''' </summary>
    Protected Function GetLimitDate() As String
        Dim iWeeks As Integer = VBlib.GetSettingsInt(SRC_SETTING_NAME & "_Slider", SRC_MAXWEEKS)
        Dim oDate As Date = Date.Now.AddDays(-7 * iWeeks)
        Return oDate.ToString("yyyy.MM.dd")
    End Function


    Public Function RemoveOldCache() As Integer
        Dim iRet As Integer = 0
        Dim sLimitDate As String = GetLimitDate()

        Dim iGuard As Integer = 100

        Dim bBylo As Boolean = False
        Do
            For Each oItem As VBlib.JednoPowiadomienie In VBlib.App.glItems
                If oItem.sIcon = SRC_SETTING_NAME Then
                    If oItem.sData < sLimitDate Then
                        bBylo = True
                        iRet += 1
                        VBlib.App.glItems.Remove(oItem)
                        Exit For
                    End If
                End If
            Next
            iGuard -= 1
        Loop While bBylo AndAlso iGuard > 0

        Return iRet

    End Function

End Class

'Namespace Global

Public Class JednoPowiadomienie
        Public Property sSourceFullName As String
        Public Property sTitle As String = ""
        Public Property sHtmlInfo As String = ""
        Public Property sLink As String ' more info
        Public Property sId As String   ' wywolywanie z Toast na przykład (pewnie częśc linku)
        Public Property sIcon As String
        Public Property bNew As Boolean = True
        Public Property sData As String = ""
    End Class

'End Namespace
