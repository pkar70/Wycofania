
' przemigrowane z Windows.Web.Syndication do System.ServiceModel.Syndication
' * UOKiK - powiadomienie o produktach niebezpiecznych (źródło: UOKiK: https://www.uokik.gov.pl/rss/5.xml)
' == https://www.uokik.gov.pl/powiadomienia.php
Public Class Source_UOKIK
    Inherits Source_Base

    Protected Overrides Property SRC_SOURCE_FULL_NAME As String = "Urząd Ochrony Konkurencji i Konsumentów"
    Protected Overrides Property SRC_SETTING_NAME As String = "UOKiK"
    Protected Overrides Property SRC_ABOUTUS_LINK As String = "https://www.uokik.gov.pl/powiadomienia.php"
    Protected Overrides Property SRC_SEARCH_LINK As String = "https://www.uokik.gov.pl/powiadomienia.php"


    Private Async Function GetRssPageAsync() As Task(Of String)
        Dim sPage As String = Await VBlib.HttpPageAsync(New Uri("https://www.uokik.gov.pl/rss/5.xml"))
        If sPage = "" Then Return ""

        ' <rss version="0.92">  ->  ﻿<rss version="2.0" >
        ' bo: Error in line 1 position 2. The Rss20Serializer does not support RSS version '0.92'.
        sPage = sPage.Replace("<rss version=""0.92", "<rss version=""2.0")

        Return sPage
    End Function

    Public Overrides Async Function ReadData(bMsg As Boolean) As Task(Of ObjectModel.Collection(Of JednoPowiadomienie))

        Dim iLimit As Integer = 20
        Dim sLimitDate As String = GetLimitDate()
        DebugOut("  Limits: sLimitDate=" & sLimitDate)

        Dim oRetList As New ObjectModel.Collection(Of JednoPowiadomienie)
        Dim oRssFeed As ServiceModel.Syndication.SyndicationFeed = Nothing
        Dim sPage As String = Await GetRssPageAsync
        Try
            Using oReader = Xml.XmlReader.Create(New IO.StringReader(sPage))
                oRssFeed = ServiceModel.Syndication.SyndicationFeed.Load(oReader)
            End Using
        Catch ex As Exception

        End Try

        ' <rss version="0.92">
        If oRssFeed.Items.Count < 1 Then
            If bMsg Then DialogBox("Error reading RSS from UOKIK")
            Return Nothing
        End If
        DebugOut(" Wczytalem " & oRssFeed.Items.Count & " itemów, data: " & oRssFeed.LastUpdatedTime.ToString)

        For Each oRssItem As ServiceModel.Syndication.SyndicationItem In oRssFeed.Items
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

            oNew.sData = oRssItem.PublishDate.ToString("yyyy.MM.dd HH:mm:ss")
            If oNew.sData < sLimitDate Then
                DebugOut("iteraing END because of DATE: " & oNew.sData & " < " & sLimitDate)
                bWas = True
                Exit For
            End If
            If bWas Then Continue For

            oNew.sHtmlInfo = oRssItem.Summary.Text   ' w Atom summary, w Rss description


            oRetList.Add(oNew)

            iLimit -= 1
            If iLimit < 0 Then
                DebugOut("iteraing END because of INT limit")
                Return oRetList
            End If

        Next

        Return oRetList


    End Function


End Class
