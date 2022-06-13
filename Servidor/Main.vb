Imports System.Net.Sockets
Imports System.Threading
Imports System.Net
Imports System.Runtime.Serialization.Formatters.Binary
Imports System.IO
Imports Microsoft.Win32
Public Class Main
    Dim YO As TcpListener
    Dim REMOTO As TcpClient
    Dim RECIBE As Thread
    Dim NS As NetworkStream
    Dim ENVIA As Thread
    Public ENVIO As Byte()
    Dim DirArray As New ArrayList
    Dim ServerIP As String = "localhost"
    Dim ServerPort As Integer = 15243

    Dim rmtUsername As String
    Sub LoadMemory()
        Try
            Dim llaveReg As String = "SOFTWARE\\Zhenboro\\RMTFS"
            Dim registerKey As RegistryKey = Registry.CurrentUser.OpenSubKey(llaveReg, True)
            If registerKey Is Nothing Then
                SaveMemory()
            Else
                ServerIP = registerKey.GetValue("ServerIP")
                ServerPort = registerKey.GetValue("ServerPort")
                TextBox1.Text = ServerIP & ":" & ServerPort
            End If
        Catch ex As Exception
            Console.WriteLine("LoadMemory Error: " & ex.Message)
        End Try
    End Sub
    Sub SaveMemory()
        Try
            Dim llaveReg As String = "SOFTWARE\\Zhenboro\\RMTFS"
            Dim registerKey As RegistryKey = Registry.CurrentUser.OpenSubKey(llaveReg, True)
            If registerKey Is Nothing Then
                Registry.CurrentUser.CreateSubKey(llaveReg, True)
                registerKey = Registry.CurrentUser.OpenSubKey(llaveReg, True)
            End If
            registerKey.SetValue("ServerIP", ServerIP)
            registerKey.SetValue("ServerPort", ServerPort)
        Catch ex As Exception
            Console.WriteLine("SaveMemory Error: " & ex.Message)
        End Try
    End Sub
    Private Sub Server_Load(sender As Object, e As EventArgs) Handles MyBase.Load
        CheckForIllegalCrossThreadCalls = False
        LoadMemory()
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
            End
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
            End
        End Try
    End Sub
    Sub Procesar(ByVal contenido As String)
        Try
            Dim paquete As String = contenido.Remove(0, contenido.LastIndexOf(">") + 1)
            If contenido = "[END]>" Then
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
            Dim i = TextBox2.Text.LastIndexOf("\")
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

    Private Sub TextBox2_KeyDown(sender As Object, e As KeyEventArgs) Handles TextBox2.KeyDown
        If e.KeyCode = Keys.Enter Then
            ENVIO = System.Text.Encoding.UTF7.GetBytes(TextBox2.Text)
        End If
    End Sub

    Private Sub ObtenerArchivoToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ObtenerArchivoToolStripMenuItem.Click
        Try
            ENVIO = System.Text.Encoding.UTF7.GetBytes("[GET_FILE]>" & DirArray(ListBox1.SelectedIndex))
            RecibirFichero(DirArray(ListBox1.SelectedIndex))
        Catch ex As Exception
            Console.WriteLine("ObtenerArchivoToolStripMenuItem_Click Error: " & ex.Message)
        End Try
    End Sub
    Private Sub ObtenerCarpetaToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles ObtenerCarpetaToolStripMenuItem.Click
        Try
            ENVIO = System.Text.Encoding.UTF7.GetBytes("[GET_FOLDER]>" & DirArray(ListBox1.SelectedIndex))
            RecibirFichero(DirArray(ListBox1.SelectedIndex) & ".zip")
        Catch ex As Exception
            Console.WriteLine("ObtenerCarpetaToolStripMenuItem_Click Error: " & ex.Message)
        End Try
    End Sub
    Private Sub RecargarToolStripMenuItem_Click(sender As Object, e As EventArgs) Handles RecargarToolStripMenuItem.Click
        Procesar(TextBox2.Text)
    End Sub

    Sub RecibirFichero(ByVal filePath As String)
        Try
            Dim CLIENTE_TCP As TcpClient
            Dim TAMAÑOBUFFER As Integer = 1024
            Dim ARCHIVORECIBIDO As Byte() = New Byte(TAMAÑOBUFFER - 1) {}
            Dim BYTESRECIBIDOS As Integer
            Dim FIN As Integer = 0
            Dim SaveFile As New SaveFileDialog
            SaveFile.Title = "Guardar archivo entrante..."
            SaveFile.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.Desktop)
            SaveFile.FileName = IO.Path.GetFileName(filePath)
            SaveFile.Filter = "Todos los archivos|*.*"
            If SaveFile.ShowDialog() = DialogResult.OK Then
                Dim SERVIDOR_TCP As New TcpListener(IPAddress.Any, Val(Val(TextBox1.Text.Split(":")(1)) + 1))
                SERVIDOR_TCP.Start()
                While FIN = 0
                    Dim NS As NetworkStream = Nothing
                    If SERVIDOR_TCP.Pending Then
                        CLIENTE_TCP = SERVIDOR_TCP.AcceptTcpClient
                        NS = CLIENTE_TCP.GetStream
                        Dim FICHERORECIBIDO As String = SaveFile.FileName
                        If FICHERORECIBIDO <> String.Empty Then
                            Dim TOTALBYTESRECIBIDOS As Integer = 0
                            Dim FS As New FileStream(FICHERORECIBIDO, FileMode.OpenOrCreate, FileAccess.Write)
                            While (AYUDAENLINEA(BYTESRECIBIDOS, NS.Read(ARCHIVORECIBIDO, 0, ARCHIVORECIBIDO.Length))) > 0
                                FS.Write(ARCHIVORECIBIDO, 0, BYTESRECIBIDOS)
                                TOTALBYTESRECIBIDOS = TOTALBYTESRECIBIDOS + BYTESRECIBIDOS
                            End While
                            FS.Close()
                        End If
                        NS.Close()
                        CLIENTE_TCP.Close()
                        FIN = 1
                    End If
                End While
                SERVIDOR_TCP.Stop()
            End If
        Catch ex As Exception
            Console.WriteLine("RecibirFichero Error: " & ex.Message)
        End Try
    End Sub
    Function AYUDAENLINEA(Of T)(ByRef OBJETIVO As T, VALOR As T)
        OBJETIVO = VALOR
        Return VALOR
    End Function
End Class