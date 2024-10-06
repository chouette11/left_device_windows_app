using System;
using System.Drawing;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Text;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using QRCoder;
using System.Text.Json;
using WindowsInput.Native;
using WindowsInput;
using System.Collections.Generic;

namespace LeftDeviceWindows
{
    public partial class Form1 : Form
    {
        private TcpListener? _tcpListener;
        private Thread? _listenerThread;
        private CancellationTokenSource? _cancellationTokenSource;
        private static InputSimulator sim = new InputSimulator();

        public Form1()
        {
            InitializeComponent();
            this.Text = "LeftDevice";

            this.FormBorderStyle = FormBorderStyle.FixedSingle; // �E�B���h�E�̋��E�X�^�C�����Œ�ɂ���
            this.MaximizeBox = false;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // �t�H�[�������[�h���ꂽ�Ƃ���IPv4�A�h���X���擾���ĕ\��
            string ipv4Address = GetLocalIPv4();
            if (!string.IsNullOrEmpty(ipv4Address))
            {
                StartTcpServer(ipv4Address);
                GenerateQRCode(ipv4Address);
            }
            else
            {
                MessageBox.Show("IPv4�A�h���X��������܂���ł����B");
            }
        }

        // TCP�T�[�o�[�̊J�n
        private void StartTcpServer(string ipAddress)
        {
            IPAddress localAddr = IPAddress.Parse(ipAddress);
            _tcpListener = new TcpListener(localAddr, 8080);
            _tcpListener.Start();

            _cancellationTokenSource = new CancellationTokenSource();
            _listenerThread = new Thread(() => ListenForClients(_cancellationTokenSource.Token));
            _listenerThread.Start();
            Console.WriteLine("TCP�T�[�o�[���N�����܂����B");
        }

        // �N���C�A���g�̐ڑ���ҋ@���ď���
        private async void ListenForClients(CancellationToken cancellationToken)
        {
            try
            {
                while (_tcpListener != null && !cancellationToken.IsCancellationRequested)
                {
                    Console.WriteLine("�N���C�A���g�̐ڑ���ҋ@��...");
                    TcpClient client = await _tcpListener.AcceptTcpClientAsync();

                    // �N���C�A���g���ƂɃ^�X�N�𐶐����ď���
                    _ = Task.Run(() => HandleClient(client), cancellationToken);
                }
            }
            catch (ObjectDisposedException)
            {
                // �T�[�o�[����~�����ꍇ�̗�O�𖳎�
            }
            catch (Exception ex)
            {
                Console.WriteLine("�T�[�o�[�G���[: " + ex.ToString());
            }
        }

        // �N���C�A���g����̃f�[�^������
        private async Task HandleClient(TcpClient client)
        {
            try
            {
                using NetworkStream stream = client.GetStream();
                using StreamReader reader = new StreamReader(stream, Encoding.UTF8);
                using StreamWriter writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

                // ���N�G�X�g���C����ǂݍ���
                string? requestLine = await reader.ReadLineAsync();
                Console.WriteLine("���N�G�X�g���C��: " + requestLine);

                if (string.IsNullOrEmpty(requestLine))
                {
                    await SendBadRequestResponse(writer);
                    return;
                }

                // ���\�b�h�A�p�X�A�v���g�R���𕪊�
                string[] requestParts = requestLine.Split(' ');
                if (requestParts.Length < 3)
                {
                    // �s���ȃ��N�G�X�g
                    await SendBadRequestResponse(writer);
                    return;
                }

                string method = requestParts[0];
                string path = requestParts[1];
                string protocol = requestParts[2];

                // �w�b�_�[�����
                Dictionary<string, string> headers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                string? line;
                while (!string.IsNullOrEmpty(line = await reader.ReadLineAsync()))
                {
                    int separatorIndex = line.IndexOf(':');
                    if (separatorIndex > 0)
                    {
                        string headerName = line.Substring(0, separatorIndex).Trim();
                        string headerValue = line.Substring(separatorIndex + 1).Trim();
                        headers[headerName] = headerValue;
                        Console.WriteLine($"�w�b�_�[: {headerName} = {headerValue}");
                    }
                }

                // ���N�G�X�g�{�f�B�̓ǂݎ��
                string requestBody = "";
                if (method.Equals("POST", StringComparison.OrdinalIgnoreCase) && headers.ContainsKey("Content-Length"))
                {
                    if (int.TryParse(headers["Content-Length"], out int contentLength))
                    {
                        char[] buffer = new char[contentLength];
                        int totalRead = 0;
                        while (totalRead < contentLength)
                        {
                            int read = await reader.ReadAsync(buffer, totalRead, contentLength - totalRead);
                            if (read == 0)
                            {
                                break;
                            }
                            totalRead += read;
                        }
                        requestBody = new string(buffer);
                        Console.WriteLine("��M�����f�[�^: " + requestBody);

                        // ���N�G�X�g�̏���
                        ProcessRequest(path, requestBody);

                        // ���X�|���X�𑗐M
                        string responseString = "HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\n\r\n�f�[�^���󂯎��܂���";
                        await writer.WriteAsync(responseString);
                    }
                    else
                    {
                        await SendBadRequestResponse(writer);
                    }
                }
                else
                {
                    // �T�|�[�g����Ă��Ȃ����\�b�h�܂��̓w�b�_�[���s��
                    await SendBadRequestResponse(writer);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("�N���C�A���g�������̃G���[: " + ex.ToString());
            }
            finally
            {
                client.Close();
            }
        }

        // �s���ȃ��N�G�X�g�ɑ΂��郌�X�|���X
        private async Task SendBadRequestResponse(StreamWriter writer)
        {
            string responseString = "HTTP/1.1 400 Bad Request\r\nContent-Type: text/plain\r\n\r\n�s���ȃ��N�G�X�g�ł�";
            await writer.WriteAsync(responseString);
        }

        // ���N�G�X�g�f�[�^�̏���
        private void ProcessRequest(string path, string requestData)
        {
            if (path == "/data")
            {
                try
                {
                    using var jsonDoc = JsonDocument.Parse(requestData);
                    JsonElement root = jsonDoc.RootElement;

                    // data�̒l���擾
                    if (root.TryGetProperty("data", out JsonElement dataElement))
                    {
                        string? dataValue = dataElement.GetString();
                        if (!string.IsNullOrEmpty(dataValue))
                        {
                            // dataValue��_+_�ŕ���
                            string[] values = dataValue.Split("_+_");
                            // values�̐���2�̏ꍇ
                            if (values.Length == 2)
                            {
                                // 2�̒l���擾
                                string type = values[0];
                                string value = values[1];
                                // type��hotkey�̏ꍇ
                                if (type == "hotkey")
                                {
                                    // �V�~�����[�g
                                    SimulateHotkey(value);
                                }
                            }
                            else
                            {
                                Console.WriteLine("data�̒l��2�ł͂���܂���B");
                            }
                        }
                        else
                        {
                            Console.WriteLine("data��������܂���ł����B");
                        }
                    }
                }
                catch (JsonException ex)
                {
                    Console.WriteLine("JSON�p�[�X�G���[: " + ex.Message);
                }
            }
            else
            {
                Console.WriteLine("���Ή��̃p�X: " + path);
            }
        }

        // �t�H�[���������鎞�ɃT�[�o�[���~
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            StopTcpServer();
        }

        // TCP�T�[�o�[�̒�~
        private void StopTcpServer()
        {
            if (_tcpListener != null)
            {
                _tcpListener.Stop();
                _tcpListener = null;
            }

            // �T�[�o�[�X���b�h�����S�ɒ�~
            if (_listenerThread != null && _listenerThread.IsAlive)
            {
                _cancellationTokenSource?.Cancel(); // �X���b�h���I��������t���O���Z�b�g
                _listenerThread.Join(); // �X���b�h�̏I����҂�
            }
        }

        // IPv4�A�h���X���擾
        private string GetLocalIPv4()
        {
            foreach (NetworkInterface networkInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                // �l�b�g���[�N�C���^�[�t�F�[�X�����삵�Ă��āA�C�[�T�l�b�g��WiFi�ł���ꍇ
                if (networkInterface.OperationalStatus == OperationalStatus.Up &&
                    (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
                     networkInterface.NetworkInterfaceType == NetworkInterfaceType.Wireless80211))
                {
                    foreach (UnicastIPAddressInformation ip in networkInterface.GetIPProperties().UnicastAddresses)
                    {
                        // IPv4�A�h���X��T��
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            return ip.Address.ToString();
                        }
                    }
                }
            }

            return "127.0.0.1"; // �f�t�H���g�̃��[�J��IP�A�h���X
        }

        // QR�R�[�h����
        private void GenerateQRCode(string qrText)
        {
            try
            {
                // QRCoder���C�u�������g����QR�R�[�h�𐶐�
                QRCodeGenerator qrGenerator = new QRCodeGenerator();
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(qrText, QRCodeGenerator.ECCLevel.Q);
                QRCode qrCode = new QRCode(qrCodeData);

                // PictureBox�̃T�C�Y�ɍ��킹��QR�R�[�h���r�b�g�}�b�v�`���Ő���
                int qrCodeSize = Math.Min(pictureBox1.Width, pictureBox1.Height); // PictureBox�̕��ƍ����̏��������ɍ��킹��
                Bitmap qrBitmap = qrCode.GetGraphic(10); // �s�N�Z���T�C�Y�𒲐�

                // ���������r�b�g�}�b�v��PictureBox�̃T�C�Y�Ƀ��T�C�Y
                Bitmap resizedQrBitmap = new Bitmap(qrBitmap, new Size(qrCodeSize, qrCodeSize));

                // PictureBox��QR�R�[�h��\��
                pictureBox1.Image = resizedQrBitmap;
            }
            catch (Exception ex)
            {
                MessageBox.Show("QR�R�[�h�������ɃG���[���������܂���: " + ex.Message);
            }
        }

        // �{�^���N���b�N�ŃL�[������V�~�����[�g
        private void button1_Click(object sender, EventArgs e)
        {
            Console.WriteLine("Button Clicked");
            SimulateHotkey("win");
        }

        public static void SimulateHotkey(string value)
        {
            // value���X�y�[�X�ŕ���
            string[] keys = value.Trim().Split(' ');
            Console.WriteLine("Hotkey: " + string.Join(", ", keys));

            // �C���L�[�ƃL�[�R�[�h��ێ����郊�X�g
            List<VirtualKeyCode> modifiers = new List<VirtualKeyCode>();
            List<VirtualKeyCode> codes = new List<VirtualKeyCode>();

            foreach (var key in keys)
            {
                var modifier = ModifierFlagForKey(key);
                var keyCode = KeyCodeForKey(key);

                if (modifier != null)
                {
                    modifiers.Add(modifier.Value);
                }
                else if (keyCode != null)
                {
                    codes.Add(keyCode.Value);
                }
            }

            // �L�[�����C�x���g�̃V�~�����[�V����
            if (modifiers.Count > 0 || codes.Count > 0)
            {
                // �C���L�[������ꍇ�͓����������V�~�����[�g
                if (modifiers.Count > 0)
                {
                    sim.Keyboard.ModifiedKeyStroke(modifiers, codes);
                }
                else if (codes.Count > 0)
                {
                    // �ʏ�̃L�[����
                    foreach (var code in codes)
                    {
                        sim.Keyboard.KeyPress(code);
                    }
                }
            }
        }

        public static VirtualKeyCode? ModifierFlagForKey(string key)
        {
            // �C���L�[�ɑΉ�����VirtualKeyCode��Ԃ�
            switch (key.ToLower())
            {
                case "control":
                case "ctrl":
                    return VirtualKeyCode.CONTROL;  // Ctrl�L�[
                case "shift":
                    return VirtualKeyCode.SHIFT;    // Shift�L�[
                case "alt":
                    return VirtualKeyCode.MENU;     // Alt�L�[
                case "capslock":
                    return VirtualKeyCode.CAPITAL;  // CapsLock�L�[
                case "numlock":
                    return VirtualKeyCode.NUMLOCK;  // NumLock�L�[
                case "scrolllock":
                    return VirtualKeyCode.SCROLL;   // ScrollLock�L�[
                case "win":
                case "lwin":
                    return VirtualKeyCode.LWIN;     // ��Windows�L�[
                case "rwin":
                    return VirtualKeyCode.RWIN;     // �EWindows�L�[
                case "apps":
                    return VirtualKeyCode.APPS;     // ���j���[�L�[ (�A�v���P�[�V�����L�[)
                default:
                    return null;
            }
        }

        public static VirtualKeyCode? KeyCodeForKey(string key)
        {
            // InputSimulator�Ŏg����L�[�R�[�h��Ԃ�
            Dictionary<string, VirtualKeyCode> keyCodeMap = new Dictionary<string, VirtualKeyCode>
            {
                { "a", VirtualKeyCode.VK_A },
                { "b", VirtualKeyCode.VK_B },
                { "c", VirtualKeyCode.VK_C },
                { "d", VirtualKeyCode.VK_D },
                { "e", VirtualKeyCode.VK_E },
                { "f", VirtualKeyCode.VK_F },
                { "g", VirtualKeyCode.VK_G },
                { "h", VirtualKeyCode.VK_H },
                { "i", VirtualKeyCode.VK_I },
                { "j", VirtualKeyCode.VK_J },
                { "k", VirtualKeyCode.VK_K },
                { "l", VirtualKeyCode.VK_L },
                { "m", VirtualKeyCode.VK_M },
                { "n", VirtualKeyCode.VK_N },
                { "o", VirtualKeyCode.VK_O },
                { "p", VirtualKeyCode.VK_P },
                { "q", VirtualKeyCode.VK_Q },
                { "r", VirtualKeyCode.VK_R },
                { "s", VirtualKeyCode.VK_S },
                { "t", VirtualKeyCode.VK_T },
                { "u", VirtualKeyCode.VK_U },
                { "v", VirtualKeyCode.VK_V },
                { "w", VirtualKeyCode.VK_W },
                { "x", VirtualKeyCode.VK_X },
                { "y", VirtualKeyCode.VK_Y },
                { "z", VirtualKeyCode.VK_Z },
                { "1", VirtualKeyCode.VK_1 },
                { "2", VirtualKeyCode.VK_2 },
                { "3", VirtualKeyCode.VK_3 },
                { "4", VirtualKeyCode.VK_4 },
                { "5", VirtualKeyCode.VK_5 },
                { "6", VirtualKeyCode.VK_6 },
                { "7", VirtualKeyCode.VK_7 },
                { "8", VirtualKeyCode.VK_8 },
                { "9", VirtualKeyCode.VK_9 },
                { "0", VirtualKeyCode.VK_0 },
                { "enter", VirtualKeyCode.RETURN },
                { "space", VirtualKeyCode.SPACE },
                { "esc", VirtualKeyCode.ESCAPE },
                { "tab", VirtualKeyCode.TAB },
                { "delete", VirtualKeyCode.DELETE },
                { "backspace", VirtualKeyCode.BACK },
                { "left_arrow", VirtualKeyCode.LEFT },
                { "right_arrow", VirtualKeyCode.RIGHT },
                { "up_arrow", VirtualKeyCode.UP },
                { "down_arrow", VirtualKeyCode.DOWN }
            };

            return keyCodeMap.TryGetValue(key.ToLower(), out var keyCode) ? keyCode : null;
        }
    }
}
