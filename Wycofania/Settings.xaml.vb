

Public NotInheritable Class Settings
    Inherits Page

    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)

        GetAppVers(Nothing)

        For Each oZrodlo As Source_Base In App.gaSrc
            oZrodlo.ConfigCreate(uiStackConfig)
        Next

        Dim oTS As ToggleSwitch = New ToggleSwitch
        oTS.Name = "uiConfig_ShowIcons"
        oTS.Header = "Pokazuj źródło jako"
        oTS.OffContent = "tekst"
        oTS.OnContent = "ikonkę"
        oTS.IsOn = GetSettingsBool("uiConfig_ShowIcons", False)
        uiStackConfig.Children.Add(oTS)

        oTS = New ToggleSwitch
        oTS.Name = "uiConfig_AllowRemoteSystem"
        oTS.Header = "Dostęp debug"
        oTS.IsOn = Not GetSettingsBool("remoteSystemDisabled", False)
        uiStackConfig.Children.Add(oTS)

    End Sub

    Private Function VerifyDataOK() As String
        Dim sMsg As String = ""

        For Each oZrodlo As Source_Base In App.gaSrc
            sMsg = oZrodlo.ConfigDataOk(uiStackConfig)
            If sMsg <> "" Then Return sMsg
        Next

        Return ""
    End Function
    Private Async Sub uiOk_Click(sender As Object, e As RoutedEventArgs)
        Dim sMsg As String = VerifyDataOK()
        If sMsg <> "" Then
            Await DialogBoxAsync(sMsg)
            Exit Sub
        End If

        For Each oZrodlo As Source_Base In App.gaSrc
            oZrodlo.ConfigSave(uiStackConfig)
        Next

        For Each oItem As UIElement In uiStackConfig.Children
            Dim oTS As ToggleSwitch
            oTS = TryCast(oItem, ToggleSwitch)
            If oTS IsNot Nothing Then
                If oTS.Name = "uiConfig_ShowIcons" Then SetSettingsBool("uiConfig_ShowIcons", oTS.IsOn)
                If oTS.Name = "uiConfig_AllowRemoteSystem" Then SetSettingsBool("remoteSystemDisabled", Not oTS.IsOn)
            End If
        Next

        Me.Frame.GoBack()

    End Sub

    Private Async Sub uiClear_Click(sender As Object, e As RoutedEventArgs)
        If Not Await DialogBoxYNAsync("Na pewno skasować cały cache?") Then Return

        App.glItems.Clear()
        Await App.ZapiszCache()
    End Sub
End Class
