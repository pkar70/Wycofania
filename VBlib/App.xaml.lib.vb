Imports pkar

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

        If glItems.Count > 0 Then Return True

        ' bo Load nie kasuje starego cache, tylko go uzupełnia
        Return glItems.Load

    End Function

    Public Shared Function ZapiszCache() As Boolean
        If glItems Is Nothing Then
            DebugOut("ZapiszCache - glItems null")
            Return False
        End If
        If glItems.Count < 1 Then
            DebugOut("ZapiszCache - glItems.count<1")
            Return False
        End If

        RemoveOldCache()    ' usun zbyt stare entries
        If glItems.Count < 1 Then
            DebugOut("ZapiszCache - glItems.count<1 (po usunieciu starych)")
            Return False
        End If

        DebugOut("ZapiszCache - do zapisania jest " & glItems.Count & " entries")

        glItems.Save()

        Return True
    End Function

    Private Shared Function RemoveOldCache() As Integer
        Dim iRet As Integer = 0

        For Each oSource As Source_Base In gaSrc
            iRet += oSource.RemoveOldCache
        Next

        DebugOut("RemoveOldCache - removed " & iRet & " old items")
        Return iRet

    End Function




End Class
