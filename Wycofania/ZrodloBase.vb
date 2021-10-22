

Public MustInherit Class Source_Base
    ' ułatwienie dodawania następnych
    Protected MustOverride Property SRC_SOURCE_FULL_NAME As String ' pełna nazwa (na stronie SettingsSources)
    Protected MustOverride Property SRC_SETTING_NAME As String  ' krótka nazwa, do SetSettings (a także jako ikonka)
    Protected Overridable Property SRC_DEFAULT_ENABLE As Boolean = True ' czy domyślnie jest włączone (tylko GIF ma tak)
    Protected Overridable Property SRC_MAXWEEKS As Integer = 8 ' jak stary moze byc
    Protected Overridable Property SRC_DEFAULT_ONETOAST As Boolean = False
    Protected MustOverride Property SRC_ABOUTUS_LINK As String
    Protected MustOverride Property SRC_SEARCH_LINK As String
    Protected MustOverride Async Function ReadDataMain(bMsg As Boolean) As Task

    Public Function GetName()
        Return SRC_SETTING_NAME
    End Function

    Public Overridable Sub ReadResStrings()
        ' jakby cos bylo do przekopiowania z Resources do App.Settings
    End Sub

    Public Function GetSearchMFI() As MenuFlyoutItem
        Dim oMFI As MenuFlyoutItem = New MenuFlyoutItem
        oMFI.Text = SRC_SETTING_NAME
        oMFI.Name = "uiSearchMFI_" & SRC_SETTING_NAME
        Return oMFI
    End Function

    Public Sub OpenBrowserSearch(oMFI As MenuFlyoutItem)
        If oMFI.Name <> "uiSearchMFI_" & SRC_SETTING_NAME Then Return
        OpenBrowser(SRC_SEARCH_LINK)
    End Sub
    Public Overridable Sub ConfigCreate(oStack As StackPanel)

        Dim oTB As TextBlock = New TextBlock
        oTB.Text = SRC_SOURCE_FULL_NAME
        oTB.FontWeight = Windows.UI.Text.FontWeights.Bold
        oTB.FontSize = 18
        oTB.Margin = New Thickness(0, 5, 0, 0)
        oStack.Children.Add(oTB)

        Dim oLnk As HyperlinkButton = New HyperlinkButton
        oLnk.Content = "O serwisie"
        oLnk.NavigateUri = New Uri(SRC_ABOUTUS_LINK)
        oStack.Children.Add(oLnk)

        Dim oTS As ToggleSwitch = New ToggleSwitch
        oTS.Name = "uiConfig_" & SRC_SETTING_NAME
        oTS.IsOn = GetSettingsBool(SRC_SETTING_NAME, SRC_DEFAULT_ENABLE)
        oStack.Children.Add(oTS)

        oTS = New ToggleSwitch
        oTS.Name = "uiConfig_" & SRC_SETTING_NAME & "_Toast"
        oTS.OffContent = "powiadomienia wyłączone" ' GetLangString("msgSettToastOff")
        oTS.OnContent = "pokazuj powiadomienia" ' GetLangString("msgSettToastOn")
        oTS.IsOn = GetSettingsBool(SRC_SETTING_NAME & "_Toast", False)
        oStack.Children.Add(oTS)

        Dim oBindExpr = oTS.GetBindingExpression(ToggleSwitch.IsOnProperty)
        Dim oBind As New Binding()
        oBind.ElementName = oTS.Name
        oBind.Path = New PropertyPath("IsOn")

        oTS = New ToggleSwitch
        oTS.Name = "uiConfig_" & SRC_SETTING_NAME & "_OneToast"
        oTS.OffContent = "osobne powiadomienia" ' GetLangString("msgSettToastOff")
        oTS.OnContent = "powiadomienie zbiorcze" ' GetLangString("msgSettToastOn")
        oTS.IsOn = GetSettingsBool(SRC_SETTING_NAME & "_OneToast", SRC_DEFAULT_ONETOAST)
        ' oTS.IsEnabled = oBind
        oTS.SetBinding(ToggleSwitch.IsEnabledProperty, oBind)
        oStack.Children.Add(oTS)

        Dim oSld As Slider = New Slider
        oSld.Name = "uiConfig_" & SRC_SETTING_NAME & "_Slider"
        oSld.Header = "Czas przechowywania (tygodnie)" ' GetLangString("msgStorageTime")
        oSld.Minimum = 1
        oSld.Maximum = 52
        oSld.Value = GetSettingsInt(SRC_SETTING_NAME & "_Slider", SRC_MAXWEEKS).MinMax(1, 52)
        oStack.Children.Add(oSld)

        Dim oKreska As Shapes.Rectangle = New Shapes.Rectangle()
        oKreska.Name = "uiConfig_" & SRC_SETTING_NAME & "_Kreska"
        oKreska.Height = 1
        oKreska.HorizontalAlignment = HorizontalAlignment.Stretch
        oKreska.Margin = New Thickness(30, 5, 30, 5)
        oKreska.Stroke = New SolidColorBrush(Windows.UI.Colors.Blue)
        oStack.Children.Add(oKreska)

    End Sub

    Public Overridable Function ConfigDataOk(oStack As StackPanel) As String
        ' jesli nie ma Key, to na pewno poprawne
        Return ""

    End Function

    Public Overridable Sub ConfigSave(oStack As StackPanel)
        For Each oItem As UIElement In oStack.Children
            Dim oTS As ToggleSwitch
            oTS = TryCast(oItem, ToggleSwitch)
            If oTS IsNot Nothing Then
                If oTS.Name = "uiConfig_" & SRC_SETTING_NAME Then SetSettingsBool(SRC_SETTING_NAME, oTS.IsOn)
                If oTS.Name = "uiConfig_" & SRC_SETTING_NAME & "_Toast" Then SetSettingsBool(SRC_SETTING_NAME & "_Toast", oTS.IsOn)
                If oTS.Name = "uiConfig_" & SRC_SETTING_NAME & "_OneToast" Then SetSettingsBool(SRC_SETTING_NAME & "_OneToast", oTS.IsOn)
            End If
        Next

        For Each oItem As UIElement In oStack.Children
            Dim oSld As Slider
            oSld = TryCast(oItem, Slider)
            If oSld IsNot Nothing Then
                If oSld.Name = "uiConfig_" & SRC_SETTING_NAME & "_Slider" Then
                    SetSettingsInt(SRC_SETTING_NAME & "_Slider", oSld.Value)
                    Exit For
                End If
            End If
        Next
    End Sub

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
        Dim iWeeks As Integer = GetSettingsInt(SRC_SETTING_NAME & "_Slider", SRC_MAXWEEKS)
        Dim oDate As Date = Date.Now.AddDays(-7 * iWeeks)
        Return oDate.ToString("yyyy.MM.dd")
    End Function

    Private mToastIcon As String = ""
    Private mToastLines As String = ""

    Protected Sub MakeToast(oItem As JednoPowiadomienie)

        If Not GetSettingsBool(SRC_SETTING_NAME & "_Toast") Then Return

        If GetSettingsBool(SRC_SETTING_NAME & "_OneToast", SRC_DEFAULT_ONETOAST) Then
            mToastIcon = oItem.sIcon
            If mToastLines <> "" Then mToastLines += vbCrLf
            mToastLines += oItem.sTitle
        Else
            MakeToastMain(oItem.sIcon, oItem.sTitle, oItem.sId)
            Return
        End If

    End Sub

    Public Async Function ReadData(bMsg As Boolean) As Task
        mToastLines = ""
        mToastIcon = ""

        If Not GetSettingsBool(SRC_SETTING_NAME, SRC_DEFAULT_ENABLE) Then
            DebugOut("ReadData for " & SRC_SETTING_NAME & " - but not enabled")
            Return
        End If

        Await ReadDataMain(bMsg)

        If Not GetSettingsBool(SRC_SETTING_NAME & "_OneToast", SRC_DEFAULT_ONETOAST) Then Return
        If mToastLines = "" Then Return

        ' jeden wspolny toast (wszystkie linie tego source razem)
        MakeToastMain(mToastIcon, mToastLines, "")

    End Function

    Private Sub MakeToastMain(sIcon As String, sTitle As String, sId As String)

        ' kopia z FilteredRSS
        Dim sHdr As String = ""
        Dim sAttrib As String = ""

        If WinVer() > 15062 Then
            ' jako header
            ' https://docs.microsoft.com/en-us/windows/uwp/design/shell/tiles-and-notifications/toast-headers
            sHdr = "<header id=""" & sIcon & """ title=""" & sIcon & """ arguments="""" />"
        ElseIf WinVer() > 14392 Then
            ' https://docs.microsoft.com/en-us/windows/uwp/design/shell/tiles-and-notifications/adaptive-interactive-toasts
            sAttrib = "<text placement=""attribution"">" & sIcon & "</text>"
        End If

        Dim sXml As String = "<text>" & XmlSafeStringQt(sTitle) & "</text>"

        Dim sTag As String = ""
        If sId <> "" Then sTag = sIcon & "-" & sId

        Dim sGlobalAction As String = " launch=""OPEN" & sTag & """ "

        Dim oXml As Windows.Data.Xml.Dom.XmlDocument = New Windows.Data.Xml.Dom.XmlDocument
        Dim bError As Boolean = False
        Try
            oXml.LoadXml("<toast" & sGlobalAction & ">" & sHdr &
                         "<visual><binding template='ToastGeneric'>" &
                         sAttrib & sXml & "</binding></visual></toast>")
        Catch ex As Exception
            bError = True
        End Try

        If bError Then
            Try
                oXml.LoadXml("<toast><visual><binding template='ToastGeneric'><text>Error creating Toast</text></binding></visual></toast>")
            Catch ex As Exception
                Exit Sub
            End Try
        End If

        Dim oToast As Windows.UI.Notifications.ToastNotification = New Windows.UI.Notifications.ToastNotification(oXml)
        bError = False
        Try
            oToast.Tag = sTag   ' żeby można było usunąć toast gdy sie usunie w aplikacji
        Catch ex As Exception
            bError = True
        End Try

        Windows.UI.Notifications.ToastNotificationManager.CreateToastNotifier().Show(oToast)
    End Sub

    Public Function RemoveOldCache() As Integer
        Dim iRet As Integer = 0
        Dim sLimitDate As String = GetLimitDate()

        Dim iGuard As Integer = 100

        Dim bBylo As Boolean = False
        Do
            For Each oItem As JednoPowiadomienie In App.glItems
                If oItem.sIcon = SRC_SETTING_NAME Then
                    If oItem.sData < sLimitDate Then
                        bBylo = True
                        iRet += 1
                        App.glItems.Remove(oItem)
                        Exit For
                    End If
                End If
            Next
            iGuard -= 1
        Loop While bBylo AndAlso iGuard > 0

        Return iRet

    End Function


End Class


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
