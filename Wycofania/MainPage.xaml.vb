Imports vb14 = VBlib.pkarlibmodule14
Imports pkar.Uwp.Ext

' 2022.02.06
' ikonka dla Toast (ta sama co na liście)
' 

' 2022.02.03
' * VBlib, do którego trafiają wszystkie poza Rasff i UOKiK (bo te mają RSS)
' * wysyłanie Toastu - z Builder (niestety, w VBLib nie ma .GetXml więc nie da się przenieść do VBLib)
' * MinVers na 16299, co pozwala na VBlib .Net Std 2.0, a to daje RSS
' * klasy Source* udało się w całości przenieść do VBlib, po przenosinach Toast do App, zaś związane z UI - do xaml.vb, głównie Settings
' * byla wiec konieczna migracja z ServiceModel.Syndication.SyndicationFeed do ServiceModel.Syndication.SyndicationFeed

' 2021.12.10
' tak jakby UOKIK_Reg, GIS, GIF i jeszcze jedno miało w Timer problem ze ściąganiem danych (get/post, "text associated with this error code cannot be found")

' 2021.11.30
' * uaktualnienie linku do RASFF

' 2021.06.18
' * UOKIK-R nie reagował na wyłączenie
' * trochę dodatkowego DebugOut
' * 

' STORE: 2021.06.16
' opóźnienie, bo niby działało, ale ciągle nie było prezentowania informacji z JSON z RAPEX
' początek: 2021.04.07

Public NotInheritable Class MainPage
    Inherits Page


    Private Sub uiSetup_Click(sender As Object, e As RoutedEventArgs)
        Me.Frame.Navigate(GetType(Settings))
    End Sub

    Private Async Sub Page_Loaded(sender As Object, e As RoutedEventArgs)
        '  CrashMessageInit() zbędne w aktualnej wersji biblioteki

        ProgRingInit(True, True)

        If Not IsTriggersRegistered("Wycofania") Then
            If Not Await CanRegisterTriggersAsync() Then
                vb14.DialogBoxRes("errNoBackgroud")
            Else
                RegisterTimerTrigger("Wycofania_Timer", 90)
            End If
        End If

        ProgRingShow(True)
        If VBlib.App.glItems.Count < 1 Then App.WczytajCache()
        DodajLinkiSzukania()

        ProgRingShow(False)

        uiList.ItemsSource = From c In VBlib.App.glItems Order By c.sData Descending

    End Sub

    Public Sub GoDetailsToastId(sIcon As String, sId As String)

        For Each oItem As JednoPowiadomienie In VBlib.App.glItems
            If oItem.sIcon = sIcon AndAlso oItem.sId = sId Then
                Me.Frame.Navigate(GetType(Detailsy), oItem.sLink)
            End If
        Next
    End Sub


    Private Sub DodajLinkiSzukania()
        For Each oZrodlo As VBlib.Source_Base In VBlib.App.gaSrc
            Dim oMFI As MenuFlyoutItem = New MenuFlyoutItem
            oMFI.Text = oZrodlo.GetSettingName
            oMFI.Name = "uiSearchMFI_" & oZrodlo.GetSettingName
            oMFI.DataContext = oZrodlo.GetSearchLink
            AddHandler oMFI.Click, AddressOf uiMFIsearch_Click
            uiSearch.Items.Add(oMFI)
        Next
    End Sub

    Private Sub uiMFIsearch_Click(sender As Object, e As RoutedEventArgs)
        Dim oMFI As MenuFlyoutItem = TryCast(sender, MenuFlyoutItem)
        If oMFI Is Nothing Then Return
        Dim sLink As String = CType(oMFI.DataContext, String)
        Dim oUri As New Uri(sLink)
        oUri.OpenBrowser
    End Sub

    Private Async Sub uiRefresh_Click(sender As Object, e As RoutedEventArgs)

        Dim oLista As Collection(Of JednoPowiadomienie) = New Collection(Of JednoPowiadomienie)

        If VBlib.App.gaSrc.Count < 1 Then Return

        ProgRingShow(True, False, 0, VBlib.App.gaSrc.Count)
        Await App.SciagnijDane(Me, True)
        ProgRingShow(False)

        uiList.ItemsSource = From c In VBlib.App.glItems Order By c.sData Descending

    End Sub

    Private Sub uiOpenDetails_Click(sender As Object, e As RoutedEventArgs)
        Dim oItem As JednoPowiadomienie = Nothing
        Dim oMFI As FrameworkElement = TryCast(sender, FrameworkElement)
        If oMFI Is Nothing Then Return
        oItem = TryCast(oMFI.DataContext, JednoPowiadomienie)

        ' wiemy już co
        Me.Frame.Navigate(GetType(Detailsy), oItem.sLink)

    End Sub


    Private Sub uiOpenWeb_Click(sender As Object, e As RoutedEventArgs)
        Dim oMFI As MenuFlyoutItem = TryCast(sender, MenuFlyoutItem)
        If oMFI Is Nothing Then Return
        Dim oItem As JednoPowiadomienie = TryCast(oMFI.DataContext, JednoPowiadomienie)
        If oItem Is Nothing Then Return

        Dim oUri As New Uri(oItem.sLink)
        oUri.OpenBrowser()
    End Sub

    Private Sub uiCopyLink_Click(sender As Object, e As RoutedEventArgs)
        Dim oMFI As MenuFlyoutItem = TryCast(sender, MenuFlyoutItem)
        If oMFI Is Nothing Then Return
        Dim oItem As JednoPowiadomienie = TryCast(oMFI.DataContext, JednoPowiadomienie)
        If oItem Is Nothing Then Return

        vb14.ClipPut(oItem.sLink)
    End Sub

End Class

Public Class KonwersjaDaty
    Implements IValueConverter

    Public Function Convert(ByVal value As Object,
            ByVal targetType As Type, ByVal parameter As Object,
            ByVal language As System.String) As Object _
            Implements IValueConverter.Convert

        If value Is Nothing Then Return False

        Dim sTmp As String = CType(value, String)
        Return sTmp.Substring(2, 8)  ' yyyy.mm.dd
    End Function

    ' ConvertBack is not implemented for a OneWay binding.
    Public Function ConvertBack(ByVal value As Object,
            ByVal targetType As Type, ByVal parameter As Object,
            ByVal language As System.String) As Object _
            Implements IValueConverter.ConvertBack

        Throw New NotImplementedException

    End Function
End Class

Public Class KonwersjaIkonki
    Implements IValueConverter

    Public Function Convert(ByVal value As Object,
            ByVal targetType As Type, ByVal parameter As Object,
            ByVal language As System.String) As Object _
            Implements IValueConverter.Convert

        ' jeden rządek w ListView ma 32 piksele (znaczy tyle ma buttton)
        Dim sTmp As String
        Try
            sTmp = CType(value, String)
        Catch
            sTmp = "unkn"
        End Try

        Dim oUri As Uri = New Uri("ms-appx:///Assets/icon-" & sTmp.ToLower & ".png")
        Dim oBmpImg As BitmapImage = New BitmapImage(oUri)
        Return oBmpImg

        'ikonki:
        'RAPEX paczka z EU
        'RASFF pigulka/chleb z EU

    End Function

    ' ConvertBack is not implemented for a OneWay binding.
    Public Function ConvertBack(ByVal value As Object,
            ByVal targetType As Type, ByVal parameter As Object,
            ByVal language As System.String) As Object _
            Implements IValueConverter.ConvertBack

        Throw New NotImplementedException

    End Function
End Class

Public Class KonwersjaIkonkiVisibility
    Implements IValueConverter

    Public Function Convert(ByVal value As Object,
            ByVal targetType As Type, ByVal parameter As Object,
            ByVal language As System.String) As Object _
            Implements IValueConverter.Convert

        Dim bIcon As Boolean = vb14.GetSettingsBool("uiConfig_ShowIcons", False)
        Dim sUnit As String = parameter

        'DebugOut("KonwersjaIkonkiVisibility(,," & sUnit & "..), bIcon=" & bIcon)
        If sUnit = "icon" Then Return bIcon
        Return Not bIcon

    End Function

    ' ConvertBack is not implemented for a OneWay binding.
    Public Function ConvertBack(ByVal value As Object,
            ByVal targetType As Type, ByVal parameter As Object,
            ByVal language As System.String) As Object _
            Implements IValueConverter.ConvertBack

        Throw New NotImplementedException

    End Function
End Class
