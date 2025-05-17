Imports pkar
Imports pkar.BaseStruct

Public Class App

    Public Shared gaSrc As Source_Base() = {
        New Source_Gif,
        New Source_GIS,
        New Source_UOKIK,
        New Source_UOKIK_Reg,
        New Source_RASFF,
        New Source_Rapex
    }

    Public Shared glItems As BaseList(Of JednoPowiadomienie)

    Public Shared Function WczytajCache(sCacheFolder As String) As Boolean
        If glItems Is Nothing Then
            glItems = New BaseList(Of JednoPowiadomienie)(sCacheFolder)
        End If

        Return glItems.Load

        'Dim sFilename As String = IO.Path.Combine(sCacheFolder, "items.json")
        'Dim sTxt As String = ""
        'If IO.File.Exists(sFilename) Then
        '    sTxt = IO.File.ReadAllText(sFilename)
        'End If

        'If sTxt Is Nothing OrElse sTxt.Length < 5 Then
        '    glItems.Clear()
        '    Return False
        'End If

        'glItems = Newtonsoft.Json.JsonConvert.DeserializeObject(sTxt, GetType(ObjectModel.Collection(Of JednoPowiadomienie)))
        'Return True
    End Function

    'Public Shared Function ZapiszCache(sCacheFolder As String) As Boolean
    '    If glItems Is Nothing Then
    '        DebugOut("ZapiszCache - glItems null")
    '        Return False
    '    End If
    '    If glItems.Count < 1 Then
    '        DebugOut("ZapiszCache - glItems.count<1")
    '        Return False
    '    End If

    '    RemoveOldCache()    ' usun zbyt stare entries
    '    If glItems.Count < 1 Then
    '        DebugOut("ZapiszCache - glItems.count<1 (po usunieciu starych)")
    '        Return False
    '    End If

    '    DebugOut("ZapiszCache - do zapisania jest " & glItems.Count & " entries")

    '    Dim sTxt As String = Newtonsoft.Json.JsonConvert.SerializeObject(glItems, Newtonsoft.Json.Formatting.Indented)
    '    Dim sFilename As String = IO.Path.Combine(sCacheFolder, "items.json")
    '    IO.File.WriteAllText(sFilename, sTxt)

    '    Return True
    'End Function

    'Private Shared Function RemoveOldCache() As Integer
    '    Dim iRet As Integer = 0

    '    For Each oSource As Source_Base In gaSrc
    '        iRet += oSource.RemoveOldCache
    '    Next

    '    DebugOut("RemoveOldCache - removed " & iRet & " old items")
    '    Return iRet

    'End Function




End Class
