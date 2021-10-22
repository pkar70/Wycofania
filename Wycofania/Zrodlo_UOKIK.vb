' * UOKiK - powiadomienie o produktach niebezpiecznych (źródło: UOKiK: https://www.uokik.gov.pl/rss/5.xml)
' == https://www.uokik.gov.pl/powiadomienia.php
Public Class Source_UOKIK
    Inherits Source_Base

    Protected Overrides Property SRC_SOURCE_FULL_NAME As String = "Urząd Ochrony Konkurencji i Konsumentów"
    Protected Overrides Property SRC_SETTING_NAME As String = "UOKiK"
    Protected Overrides Property SRC_ABOUTUS_LINK As String = "https://www.uokik.gov.pl/powiadomienia.php"
    Protected Overrides Property SRC_SEARCH_LINK As String = "https://www.uokik.gov.pl/powiadomienia.php"

    Protected Overrides Async Function ReadDataMain(bMsg As Boolean) As Task
        DebugOut("ReadData for " & SRC_SETTING_NAME)
        If Not GetSettingsBool(SRC_SETTING_NAME, SRC_DEFAULT_ENABLE) Then Return

        Dim sLimitDate As String = GetLimitDate()
        DebugOut("  Limits: sLimitDate=" & sLimitDate)

        Dim iLimit As Integer = 20

        ' Return ' ***********************************************************************


        Dim oRssClnt As Windows.Web.Syndication.SyndicationClient = New Windows.Web.Syndication.SyndicationClient
        oRssClnt.BypassCacheOnRetrieve = True
        Dim oRssFeed As Windows.Web.Syndication.SyndicationFeed
        oRssFeed = Await oRssClnt.RetrieveFeedAsync(New Uri("https://www.uokik.gov.pl/rss/5.xml"))

        If oRssFeed.Items.Count < 1 Then
            If bMsg Then DialogBox("Error reading RSS from UOKIK")
            Return
        End If
        DebugOut(" Wczytalem " & oRssFeed.Items.Count & " itemów, data: " & oRssFeed.LastUpdatedTime.ToString)

        For Each oRssItem As Windows.Web.Syndication.SyndicationItem In oRssFeed.Items
            Dim oNew As JednoPowiadomienie = NewPowiadomienie()

            oNew.sTitle = oRssItem.Title.Text

            Dim bWas As Boolean = False
            oNew.sLink = oRssItem.Links.Item(0).Uri.AbsoluteUri.ToString
            For Each oItem As JednoPowiadomienie In App.glItems
                If oItem.sLink = oNew.sLink Then
                    DebugOut("iteraing END because of ID=" & oNew.sLink)
                    bWas = True
                    Exit For
                End If
            Next
            If bWas Then Continue For

            Dim iInd As Integer = oNew.sLink.LastIndexOf("=")
            oNew.sId = oNew.sLink.Substring(iInd + 1)

            oNew.sData = oRssItem.PublishedDate.ToString("yyyy.MM.dd HH:mm:ss")
            If oNew.sData < sLimitDate Then
                DebugOut("iteraing END because of DATE: " & oNew.sData & " < " & sLimitDate)
                bWas = True
                Exit For
            End If
            If bWas Then Continue For

            oNew.sHtmlInfo = oRssItem.Summary.Text   ' w Atom summary, w Rss description

            App.glItems.Add(oNew)
            MakeToast(oNew)

            iLimit -= 1
            If iLimit < 0 Then
                DebugOut("iteraing END because of INT limit")
                Return
            End If

        Next


    End Function


End Class
