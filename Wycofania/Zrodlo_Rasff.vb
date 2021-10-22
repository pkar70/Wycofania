' https://webgate.ec.europa.eu/rasff-window/consumers/?event=rss&country=PL - zdrowotny; może być wybór kraju
Public Class Source_RASFF
    Inherits Source_Base
    Protected Overrides Property SRC_SOURCE_FULL_NAME As String = "Rapid Alert System for Food and Feed (EU)"
    Protected Overrides Property SRC_SETTING_NAME As String = "RASFF"
    Protected Overrides Property SRC_ABOUTUS_LINK As String = "https://ec.europa.eu/food/safety/rasff/"
    Protected Overrides Property SRC_SEARCH_LINK As String = "https://webgate.ec.europa.eu/rasff-window/portal/?event=SearchForm&cleanSearch=1"

    Protected Overrides Async Function ReadDataMain(bMsg As Boolean) As Task
        DebugOut("ReadData for " & SRC_SETTING_NAME)
        If Not GetSettingsBool(SRC_SETTING_NAME, SRC_DEFAULT_ENABLE) Then Return

        Dim sLimitDate As String = GetLimitDate()
        DebugOut("  Limits: sLimitDate=" & sLimitDate)

        ' Return ' ***********************************************************************

        Dim iLimit As Integer = 20

        Dim oRssClnt As Windows.Web.Syndication.SyndicationClient = New Windows.Web.Syndication.SyndicationClient
        Dim oRssFeed As Windows.Web.Syndication.SyndicationFeed = Nothing
        Try
            oRssFeed = Await oRssClnt.RetrieveFeedAsync(New Uri("https://webgate.ec.europa.eu/rasff-window/consumers/?event=rss&country=PL"))
        Catch ex As Exception
            oRssFeed = Nothing
        End Try
        If oRssFeed Is Nothing Then
            If bMsg Then DialogBox("Error reading RSS from " & SRC_SETTING_NAME & " (null from RetrieveFeedAsync)")
            Return
        End If

        If oRssFeed.Items.Count < 1 Then
            If bMsg Then DialogBox("Error reading RSS from " & SRC_SETTING_NAME)
            Return
        End If

        For Each oRssItem As Windows.Web.Syndication.SyndicationItem In oRssFeed.Items
            Dim oNew As JednoPowiadomienie = NewPowiadomienie()

            '<item>
            '  <title>2021.0690 - withdrawal from the market of breadsticks from Italy containing sesame seeds potentially contaminated with ethylene oxide</title>
            '  <link>https://webgate.ec.europa.eu/rasff-window/consumers/?event=notificationDetail&NOTIF_REFERENCE=2021.0690</link>
            '  <description>Notified by Romania on 10/02/2021</description>
            '</item>

            Dim bMam As Boolean = False
            oNew.sLink = oRssItem.Links.Item(0).Uri.AbsoluteUri.ToString
            For Each oItem As JednoPowiadomienie In App.glItems
                If oItem.sLink = oNew.sLink Then
                    bMam = True
                    Exit For
                    ' DebugOut("iteraing END because of ID")
                    ' tu jest w odwrotnej kolejnosci, od najstarszego
                End If
            Next
            If bMam Then Continue For

            oNew.sTitle = oRssItem.Title.Text
            Dim iInd As Integer = oNew.sTitle.IndexOf(" - ")
            If iInd > 8 AndAlso iInd < 20 Then
                oNew.sTitle = oNew.sTitle.Substring(iInd + 3).Trim
            End If

            oNew.sData = oRssItem.Summary.Text   ' w Atom summary, w Rss description; feed nie ma daty jako daty :)
            iInd = oNew.sData.LastIndexOf(" on ")
            If iInd > 1 Then
                oNew.sData = oNew.sData.Substring(iInd + 4).Trim
                If oNew.sData.Length = 10 Then
                    ' Notified by Germany on 15/03/2021 , a wiec obracamy datę
                    oNew.sData = oNew.sData.Substring(6, 4) & oNew.sData.Substring(2, 4) & oNew.sData.Substring(0, 2)
                    oNew.sData = oNew.sData.Replace("/", ".")
                    If oNew.sData < sLimitDate Then
                        Continue For
                        ' DebugOut("iteraing END because of DATE: " & oNew.sData & " < " & sLimitDate)
                        ' tu jest w odwrotnej kolejnosci, od najstarszego
                    End If

                Else
                    oNew.sData = ""
                End If
            End If

            ' <link>https://webgate.ec.europa.eu/rasff-window/consumers/?event=notificationDetail&NOTIF_REFERENCE=2021.1150</link>
            iInd = oNew.sLink.IndexOf("REFERENCE")
            If iInd > 0 Then
                iInd = oNew.sLink.IndexOf("=", iInd)
                oNew.sId = oNew.sLink.Substring(iInd + 1, 8)
            End If

            Dim sPage As String = Await HttpPageAsync(oNew.sLink, "", False)
            iInd = sPage.IndexOf("<h3")
            If iInd > 0 Then
                sPage = sPage.Substring(iInd)
                iInd = sPage.IndexOf("<script")
                If iInd > 0 Then sPage = sPage.Substring(0, iInd)
            End If
            oNew.sHtmlInfo = sPage

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
