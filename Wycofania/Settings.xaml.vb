


Public NotInheritable Class Settings
    Inherits Page

    Private Sub CreateConfigItems(oStack As StackPanel, oZrodlo As VBlib.Source_Base)
        Dim oTB As TextBlock = New TextBlock
        oTB.Text = oZrodlo.GetFullName
        oTB.FontWeight = Windows.UI.Text.FontWeights.Bold
        oTB.FontSize = 18
        oTB.Margin = New Thickness(0, 5, 0, 0)
        oStack.Children.Add(oTB)

        Dim oLnk As HyperlinkButton = New HyperlinkButton
        oLnk.Content = "O serwisie"
        oLnk.NavigateUri = New Uri(oZrodlo.GetAboutUs)
        oStack.Children.Add(oLnk)

        Dim oTS As ToggleSwitch = New ToggleSwitch
        oTS.Name = "uiConfig_" & oZrodlo.GetSettingName
        oTS.IsOn = GetSettingsBool(oZrodlo.GetSettingName, oZrodlo.GetDefEnable)
        oStack.Children.Add(oTS)

        oTS = New ToggleSwitch
        oTS.Name = "uiConfig_" & oZrodlo.GetSettingName & "_Toast"
        oTS.OffContent = "powiadomienia wyłączone" ' GetLangString("msgSettToastOff")
        oTS.OnContent = "pokazuj powiadomienia" ' GetLangString("msgSettToastOn")
        oTS.IsOn = GetSettingsBool(oZrodlo.GetSettingName & "_Toast", False)
        oStack.Children.Add(oTS)

        Dim oBindExpr = oTS.GetBindingExpression(ToggleSwitch.IsOnProperty)
        Dim oBind As New Binding()
        oBind.ElementName = oTS.Name
        oBind.Path = New PropertyPath("IsOn")

        oTS = New ToggleSwitch
        oTS.Name = "uiConfig_" & oZrodlo.GetSettingName & "_OneToast"
        oTS.OffContent = "osobne powiadomienia" ' GetLangString("msgSettToastOff")
        oTS.OnContent = "powiadomienie zbiorcze" ' GetLangString("msgSettToastOn")
        oTS.IsOn = GetSettingsBool(oZrodlo.GetSettingName & "_OneToast", oZrodlo.GetDefOneToast)
        ' oTS.IsEnabled = oBind
        oTS.SetBinding(ToggleSwitch.IsEnabledProperty, oBind)
        oStack.Children.Add(oTS)

        Dim oSld As Slider = New Slider
        oSld.Name = "uiConfig_" & oZrodlo.GetSettingName & "_Slider"
        oSld.Header = "Czas przechowywania (tygodnie)" ' GetLangString("msgStorageTime")
        oSld.Minimum = 1
        oSld.Maximum = 52
        oSld.Value = GetSettingsInt(oZrodlo.GetSettingName & "_Slider", oZrodlo.GetMaxWeeks).MinMax(1, 52)
        oStack.Children.Add(oSld)

        Dim oKreska As Shapes.Rectangle = New Shapes.Rectangle()
        oKreska.Name = "uiConfig_" & oZrodlo.GetSettingName & "_Kreska"
        oKreska.Height = 1
        oKreska.HorizontalAlignment = HorizontalAlignment.Stretch
        oKreska.Margin = New Thickness(30, 5, 30, 5)
        oKreska.Stroke = New SolidColorBrush(Windows.UI.Colors.Blue)
        oStack.Children.Add(oKreska)


    End Sub

    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)

        ' GetAppVers(Nothing)
        Me.ShowAppVers

        For Each oZrodlo As VBlib.Source_Base In VBlib.App.gaSrc
            CreateConfigItems(uiStackConfig, oZrodlo)
            ' oZrodlo.ConfigCreate(uiStackConfig)
        Next

        Dim oTS As ToggleSwitch = New ToggleSwitch
        oTS.Name = "uiConfigGlobal_ShowIcons"
        oTS.Header = "Pokazuj źródło jako"
        oTS.OffContent = "tekst"
        oTS.OnContent = "ikonkę"
        oTS.IsOn = GetSettingsBool("uiConfig_ShowIcons", False)
        uiStackConfig.Children.Add(oTS)

        oTS = New ToggleSwitch
        oTS.Name = "uiConfigGlobal_AllowRemoteSystem"
        oTS.Header = "Dostęp debug"
        oTS.IsOn = Not GetSettingsBool("remoteSystemDisabled", False)
        uiStackConfig.Children.Add(oTS)

    End Sub

    'Private Function VerifyDataOK() As String
    '    Dim sMsg As String = ""

    '    For Each oZrodlo As Source_Base In App.gaSrc
    '        sMsg = oZrodlo.ConfigDataOk(uiStackConfig)
    '        If sMsg <> "" Then Return sMsg
    '    Next

    '    Return ""
    'End Function

    ' wersja not-used, ale można tak zrobić - i wtedy spada dostęp do UI z klas Source*
    ' ale wtedy nie da się zrobić jakiegoś 'customized' Setup
    Private Sub uiOk_Click(sender As Object, e As RoutedEventArgs)

        ' skipping VerifyDataOK(), bo nic takiego nie ma (i nie ma widoków na to by było)

        For Each oItem As FrameworkElement In uiStackConfig.Children
            Dim oTS As ToggleSwitch

            If oItem.Name.StartsWith("uiConfigGlobal_") Then
                oTS = TryCast(oItem, ToggleSwitch)
                If oTS IsNot Nothing Then
                    If oTS.Name = "uiConfigGlobal_ShowIcons" Then SetSettingsBool("uiConfig_ShowIcons", oTS.IsOn)
                    If oTS.Name = "uiConfigGlobal_AllowRemoteSystem" Then SetSettingsBool("remoteSystemDisabled", Not oTS.IsOn)
                End If

                Continue For
            End If

            If Not oItem.Name.StartsWith("uiConfig_") Then Continue For

            oTS = TryCast(oItem, ToggleSwitch)
            If oTS IsNot Nothing Then
                Dim sSettName As String = oTS.Name.Replace("uiConfig_", "")
                SetSettingsBool(sSettName, oTS.IsOn)
            End If

            Dim oSld As Slider
            oSld = TryCast(oItem, Slider)
            If oSld IsNot Nothing Then
                Dim sSettName As String = oSld.Name.Replace("uiConfig_", "")

                SetSettingsInt(sSettName, oSld.Value)
            End If

        Next

        Me.Frame.GoBack()

    End Sub

    Private Async Sub uiClear_Click(sender As Object, e As RoutedEventArgs)
        If Not Await DialogBoxYNAsync("Na pewno skasować cały cache?") Then Return

        VBlib.App.glItems.Clear()
        App.ZapiszCache()
    End Sub
End Class
