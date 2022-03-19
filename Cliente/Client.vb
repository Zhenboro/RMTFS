Imports System.Net.Sockets
Imports System.Runtime.Serialization.Formatters.Binary
Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Threading
Public Class Client
    Dim YO As New TcpClient
    Dim NS As NetworkStream
    Dim ServerIP As String ' = "localhost"
    Dim ServerPort As Integer ' = 190322

    Dim RECIBE As Thread
    Dim ENVIA As Thread
    Public ENVIO As Byte()

    Private Sub Client_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        CheckForIllegalCrossThreadCalls = False
        LeerParametros()
        Iniciar()
    End Sub
    Private Sub Client_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        Try
            ENVIO = System.Text.Encoding.UTF7.GetBytes("END")
            NS.Dispose()
            YO.Close()
        Catch
        End Try
    End Sub

    Sub LeerParametros()
        Try
            If My.Application.CommandLineArgs.Count = 0 Then
                End
            Else
                For i As Integer = 0 To My.Application.CommandLineArgs.Count - 1
                    Dim parameter As String = My.Application.CommandLineArgs(i)
                    If parameter Like "*-ServerIP=*" Then
                        Dim args As String() = parameter.Split("=")
                        ServerIP = args(1)
                    ElseIf parameter Like "*-ServerPort=*" Then
                        Dim args As String() = parameter.Split("=")
                        ServerPort = Integer.Parse(args(1))
                    End If
                Next
            End If
        Catch ex As Exception
            Console.WriteLine("LeerParametros Error: " & ex.Message)
            End
        End Try
    End Sub

    Sub Iniciar()
        Try
            YO.Connect(ServerIP, ServerPort)
            RECIBE = New Thread(AddressOf RECIBIR)
            RECIBE.Start()
            ENVIA = New Thread(AddressOf ENVIAR)
            ENVIA.Start()
            Procesar("C:\")
        Catch ex As Exception
            Console.WriteLine("Iniciar Error: " & ex.Message)
            End
        End Try
    End Sub

    Sub RECIBIR()
        Try
            While True
                NS = YO.GetStream
            Dim BF As New BinaryFormatter
                If NS.DataAvailable Then
                    Procesar(System.Text.Encoding.UTF7.GetString(BF.Deserialize(NS)))
                End If
            End While
        Catch ex As Exception
            Console.WriteLine("RECIBIR Error: " & ex.Message)
        End Try
    End Sub
    Sub ENVIAR()
        Try
            While True
                If ENVIO IsNot Nothing Then
                    Dim BF As New BinaryFormatter
                    NS = YO.GetStream
                    BF.Serialize(NS, ENVIO)
                    ENVIO = Nothing
                End If
            End While
        Catch ex As Exception
            Console.WriteLine("ENVIAR Error: " & ex.Message)
        End Try
    End Sub
    Sub Procesar(ByVal contenido As String)
        Try
            If contenido = "END" Then
                End
            Else
                If contenido = "C:" Then
                    contenido = "C:\"
                End If
                Dim folders As String = Nothing
                Dim files As String = Nothing

                For Each folder As String In My.Computer.FileSystem.GetDirectories(contenido, FileIO.SearchOption.SearchTopLevelOnly)
                    folders &= folder & "|"
                Next

                For Each file As String In My.Computer.FileSystem.GetFiles(contenido, FileIO.SearchOption.SearchTopLevelOnly)
                    files &= file & "|"
                Next

                ENVIO = System.Text.Encoding.UTF7.GetBytes(folders & files)
            End If
            Console.WriteLine("[Procesar] " & contenido)
        Catch ex As Exception
            Console.WriteLine("Procesar Error: " & ex.Message)
        End Try
    End Sub
End Class