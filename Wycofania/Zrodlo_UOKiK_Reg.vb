' * UOKiK - powiadomienie o produktach niebezpiecznych (źródło: UOKiK: https://www.uokik.gov.pl/rss/5.xml)
' == https://www.uokik.gov.pl/powiadomienia.php
Public Class Source_UOKIK_Reg
    Inherits Source_Base

    Protected Overrides Property SRC_SOURCE_FULL_NAME As String = "Urząd Ochrony Konkurencji i Konsumentów (rejestry)"
    Protected Overrides Property SRC_SETTING_NAME As String = "UOKiKr"
    Protected Overrides Property SRC_ABOUTUS_LINK As String = "http://publikacje.uokik.gov.pl/hermes3_pub/Rejestr.ashx"
    Protected Overrides Property SRC_SEARCH_LINK As String = "http://publikacje.uokik.gov.pl/hermes3_pub/Rejestr.ashx"

    Protected Overrides Async Function ReadDataMain(bMsg As Boolean) As Task
        DebugOut("ReadData for " & SRC_SETTING_NAME)
        If Not GetSettingsBool(SRC_SETTING_NAME, SRC_DEFAULT_ENABLE) Then Return

        Dim sLimitDate As String = GetLimitDate()
        DebugOut("  Limits: sLimitDate=" & sLimitDate)

        Dim iLimit As Integer = 20

        ' Return ' ***********************************************************************

        ' http://publikacje.uokik.gov.pl/hermes3_pub/Rejestr.ashx?Typ=ProduktNiebezpieczny&DataWpisuOd=&DataWpisuDo=&NumerIdentyfikacyjny=&NazwaProduktu=&KodWyrobu=&Sort=DataDokonaniaWpisu_DESC
        Dim sPage As String = Await pkar.HttpPageAsync("http://publikacje.uokik.gov.pl/hermes3_pub/Rejestr.ashx", "Error loading UOKiK registers data", bMsg)
        If sPage = "" Then Return

        Dim iInd As Integer = sPage.IndexOf("rejestrTable")
        If iInd < 10 Then
            If bMsg Then DialogBox("ERROR parsing data (rejestrTable)")
            Return
        End If

        iInd = sPage.LastIndexOf("<table", iInd)
        sPage = sPage.Substring(iInd)
        iInd = sPage.IndexOf("</table>")
        sPage = sPage.Substring(0, iInd + 8)

        Dim oXml As System.Xml.XmlDocument = New Xml.XmlDocument
        oXml.LoadXml(sPage)
        DebugOut("  XML loaded")

        ' mogłoby być sprawdzenie kolumn (w THEAD, sprawdzanie tekstów, i zapisanie index kolumny)
        ' z tej strony moze byc: data, nazwa produktu -> tytul, link. Nie ma fulldata, ktora jest brana dopiero z linku. Ale za to łatwiej wyciągnąc dane :)
        Dim oElems As Xml.XmlNodeList = oXml.GetElementsByTagName("tbody")
        If oElems.Count <> 1 Then
            If bMsg Then DialogBox("ERROR parsing data (tbody)")
            Return
        End If

        DebugOut("iterating ROWSy")
        ' kolejne decyzje - <tr
        For Each oRow As Xml.XmlNode In oElems.Item(0).ChildNodes
            Dim oNew As JednoPowiadomienie = NewPowiadomienie()

            Dim oTD2 As Xml.XmlNode = oRow.ChildNodes.Item(1)  ' <tr..><td><img></td><td>......
            Dim oH2 As Xml.XmlNode = oTD2.ChildNodes.Item(0) ' <td><h2><a ...>title...</a>
            oNew.sTitle = oH2.InnerText

            Dim oAelem As Xml.XmlNode = oH2.ChildNodes.Item(0)
            oNew.sLink = "http://publikacje.uokik.gov.pl/hermes3_pub/" & oAelem.Attributes.GetNamedItem("href").InnerText
            Dim bWas As Boolean = False
            For Each oItem As JednoPowiadomienie In App.glItems
                If oItem.sLink = oNew.sLink Then
                    DebugOut("iteraing END because of ID=" & oNew.sLink)
                    bWas = True
                    Exit For
                End If
            Next
            If bWas Then Continue For

            oNew.sId = oTD2.ChildNodes.Item(3).InnerText

            oNew.sData = oTD2.ChildNodes.Item(4).InnerText
            iInd = oNew.sData.LastIndexOf(" ")
            oNew.sData = oNew.sData.Substring(iInd + 1).Replace("-", ".")    ' <p>Data wpisu: 2021-02-02
            If oNew.sData < sLimitDate Then
                DebugOut("iteraing END because of DATE: " & oNew.sData & " < " & sLimitDate)
                Exit For
            End If

            ' wczytanie danych
            sPage = Await pkar.HttpPageAsync(oNew.sLink, "Error loading UOKiK product info", bMsg)
            iInd = sPage.IndexOf("<table id=""wpis")
            If iInd > 0 Then
                sPage = sPage.Substring(iInd)
                iInd = sPage.IndexOf("</table") ' to bedzie koniec "subtabelki" (obrazków)
                iInd = sPage.IndexOf("</table", iInd + 5)
                sPage = sPage.Substring(0, iInd) & "</table>"
                oNew.sHtmlInfo = sPage
            End If

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
