Imports System.Net.Sockets
Imports System.Threading
Imports System.Net
Imports System.Runtime.Serialization.Formatters.Binary
Public Class Server
    Dim YO As TcpListener
    Dim REMOTO As TcpClient
    Dim RECIBE As Thread
    Dim NS As NetworkStream
    Dim ENVIA As Thread
    Public ENVIO As Byte()
    Dim DirArray As New ArrayList

    Private Sub Server_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        CheckForIllegalCrossThreadCalls = False
    End Sub
    Private Sub Server_FormClosing(sender As Object, e As FormClosingEventArgs) Handles Me.FormClosing
        Try
            NS.Dispose()
            YO.Stop()
            RECIBE.Abort()
        Catch
        End Try
    End Sub

    Private Sub Button1_Click(sender As Object, e As EventArgs) Handles Button1.Click
        Iniciar()
    End Sub

    Sub Iniciar()
        Try
            Button1.Enabled = False
            TextBox1.Enabled = False
            Dim args As String() = TextBox1.Text.Split(":")
            YO = New TcpListener(IPAddress.Any, args(1))
            YO.Start()
            RECIBE = New Thread(AddressOf RECIBIR)
            ENVIA = New Thread(AddressOf ENVIAR)
            RECIBE.Start()
            ENVIA.Start()
        Catch ex As Exception
            Console.WriteLine("Iniciar Error: " & ex.Message)
        End Try
    End Sub
    Sub RECIBIR()
        Dim BF As New BinaryFormatter
        Try
            While True
                REMOTO = YO.AcceptTcpClient()
                NS = REMOTO.GetStream
                While REMOTO.Connected = True
                    Procesar(System.Text.Encoding.UTF7.GetString(BF.Deserialize(NS)))
                End While
            End While
        Catch ex As Exception
            Console.WriteLine("RECIBIR Error: " & ex.Message)
            ENVIO = System.Text.Encoding.UTF7.GetBytes("END")
        End Try
    End Sub
    Sub ENVIAR()
        Try
            While True
                If ENVIO IsNot Nothing Then
                    Dim BF As New BinaryFormatter
                    NS = REMOTO.GetStream
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
                DirArray.Clear()
                ListBox1.Items.Clear()
                DirArray.Add("..")
                ListBox1.Items.Add("..")
                For Each item As String In contenido.Split("|")
                    DirArray.Add(item)
                    ListBox1.Items.Add(IO.Path.GetFileName(item))
                Next
                ListBox1.Enabled = True
            End If
            Console.WriteLine("[Procesar] " & contenido)
        Catch ex As Exception
            Console.WriteLine("Procesar Error: " & ex.Message)
        End Try
    End Sub

    Private Sub ListBox1_MouseDoubleClick(sender As Object, e As MouseEventArgs) Handles ListBox1.MouseDoubleClick
        If ListBox1.SelectedItem = ".." Then
            'C:\Users\Zhenboro\source\repos\RMTFS\Servidor\bin\Debug
            Dim i = TextBox2.Text.LastIndexOf("\") 'Find the index to cut
            Dim rutaMenor = TextBox2.Text.Substring(0, i)
            TextBox2.Text = rutaMenor
            ENVIO = System.Text.Encoding.UTF7.GetBytes(rutaMenor)
        Else
            ENVIO = System.Text.Encoding.UTF7.GetBytes(DirArray(ListBox1.SelectedIndex))
            TextBox2.Text = DirArray(ListBox1.SelectedIndex)
        End If
        ListBox1.Enabled = False
    End Sub

    Private Sub TextBox2_TextChanged(sender As Object, e As EventArgs) Handles TextBox2.TextChanged
        If TextBox2.Text = "C:" Then
            TextBox2.Text = "C:\"
        End If
    End Sub
End Class