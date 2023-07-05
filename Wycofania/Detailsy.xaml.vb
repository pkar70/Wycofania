Imports Windows.ApplicationModel.DataTransfer
Imports vb14 = VBlib.pkarlibmodule14

Public NotInheritable Class Detailsy
    Inherits Page

    Dim msLink As String = ""
    Dim moItem As JednoPowiadomienie = Nothing
    Protected Overrides Sub onNavigatedTo(e As NavigationEventArgs)
        msLink = e.Parameter.ToString.ToLower

        If msLink.Length < 5 Then Return

        For Each oItem As JednoPowiadomienie In VBlib.App.glItems
            If oItem.sLink.ToLower = msLink Then
                moItem = oItem
            End If
        Next

    End Sub

    Private Sub Page_Loaded(sender As Object, e As RoutedEventArgs)

        If moItem IsNot Nothing Then
            If moItem.sHtmlInfo.Length > 10 Then
                uiWebView.NavigateToString(moItem.sHtmlInfo)
                uiCopyLabel.IsEnabled = True
                uiShareLabel.IsEnabled = True
            Else
                uiCopyLabel.IsEnabled = False
                uiShareLabel.IsEnabled = False
            End If
        End If

    End Sub

    Private Sub uiOk_Click(sender As Object, e As RoutedEventArgs)
        Me.Frame.GoBack()
    End Sub

    Private Sub uiCopyHtml_Click(sender As Object, e As RoutedEventArgs)
        If moItem IsNot Nothing Then
            vb14.ClipPutHtml(moItem.sHtmlInfo)
        End If
    End Sub

    Private Sub uiCopyLink_Click(sender As Object, e As RoutedEventArgs)
        If moItem IsNot Nothing Then
            vb14.ClipPut(moItem.sLink)
        End If
    End Sub

    Private Sub uiOpenLink_Click(sender As Object, e As RoutedEventArgs)
        If moItem IsNot Nothing Then
            Dim oUri As New Uri(moItem.sLink)
            oUri.OpenBrowser
        End If
    End Sub

    ' https://inthehand.com/2015/08/20/add-sharing-to-your-uwp-app/
    Private Sub uiShare_Click(sender As Object, e As RoutedEventArgs)
        If moItem IsNot Nothing AndAlso moItem.sHtmlInfo.Length > 10 Then
            AddHandler DataTransfer.DataTransferManager.GetForCurrentView.DataRequested, AddressOf SzarnijDane
            DataTransfer.DataTransferManager.ShowShareUI()
        End If

    End Sub

    Private Sub SzarnijDane(sender As DataTransferManager, args As DataRequestedEventArgs)
        ' dwa przypadki ktore nie mają prawa się zdarzyć, bo guzik jest zablokowany
        If moItem Is Nothing Then
            args.Request.FailWithDisplayText("Nothing to share")
            Return
        End If

        If moItem.sHtmlInfo.Length < 10 Then
            args.Request.FailWithDisplayText("Nothing to share")
            Return
        End If

        args.Request.Data.SetHtmlFormat(moItem.sHtmlInfo)
        args.Request.Data.Properties.Title = Windows.ApplicationModel.Package.Current.DisplayName

    End Sub
End Class
