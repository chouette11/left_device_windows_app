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

            this.FormBorderStyle = FormBorderStyle.FixedSingle; // ウィンドウの境界スタイルを固定にする
            this.MaximizeBox = false;
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            // フォームがロードされたときにIPv4アドレスを取得して表示
            string ipv4Address = GetLocalIPv4();
            if (!string.IsNullOrEmpty(ipv4Address))
            {
                StartTcpServer(ipv4Address);
                GenerateQRCode(ipv4Address);
            }
            else
            {
                MessageBox.Show("IPv4アドレスが見つかりませんでした。");
            }
        }

        // TCPサーバーの開始
        private void StartTcpServer(string ipAddress)
        {
            IPAddress localAddr = IPAddress.Parse(ipAddress);
            _tcpListener = new TcpListener(localAddr, 8080);
            _tcpListener.Start();

            _cancellationTokenSource = new CancellationTokenSource();
            _listenerThread = new Thread(() => ListenForClients(_cancellationTokenSource.Token));
            _listenerThread.Start();
            Console.WriteLine("TCPサーバーを起動しました。");
        }

        // クライアントの接続を待機して処理
        private async void ListenForClients(CancellationToken cancellationToken)
        {
            try
            {
                while (_tcpListener != null && !cancellationToken.IsCancellationRequested)
                {
                    Console.WriteLine("クライアントの接続を待機中...");
                    TcpClient client = await _tcpListener.AcceptTcpClientAsync();

                    // クライアントごとにタスクを生成して処理
                    _ = Task.Run(() => HandleClient(client), cancellationToken);
                }
            }
            catch (ObjectDisposedException)
            {
                // サーバーが停止した場合の例外を無視
            }
            catch (Exception ex)
            {
                Console.WriteLine("サーバーエラー: " + ex.ToString());
            }
        }

        // クライアントからのデータを処理
        private async Task HandleClient(TcpClient client)
        {
            try
            {
                using NetworkStream stream = client.GetStream();
                using StreamReader reader = new StreamReader(stream, Encoding.UTF8);
                using StreamWriter writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

                // リクエストラインを読み込む
                string? requestLine = await reader.ReadLineAsync();
                Console.WriteLine("リクエストライン: " + requestLine);

                if (string.IsNullOrEmpty(requestLine))
                {
                    await SendBadRequestResponse(writer);
                    return;
                }

                // メソッド、パス、プロトコルを分割
                string[] requestParts = requestLine.Split(' ');
                if (requestParts.Length < 3)
                {
                    // 不正なリクエスト
                    await SendBadRequestResponse(writer);
                    return;
                }

                string method = requestParts[0];
                string path = requestParts[1];
                string protocol = requestParts[2];

                // ヘッダーを解析
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
                        Console.WriteLine($"ヘッダー: {headerName} = {headerValue}");
                    }
                }

                // リクエストボディの読み取り
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
                        Console.WriteLine("受信したデータ: " + requestBody);

                        // リクエストの処理
                        ProcessRequest(path, requestBody);

                        // レスポンスを送信
                        string responseString = "HTTP/1.1 200 OK\r\nContent-Type: text/plain\r\n\r\nデータを受け取りました";
                        await writer.WriteAsync(responseString);
                    }
                    else
                    {
                        await SendBadRequestResponse(writer);
                    }
                }
                else
                {
                    // サポートされていないメソッドまたはヘッダーが不足
                    await SendBadRequestResponse(writer);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("クライアント処理中のエラー: " + ex.ToString());
            }
            finally
            {
                client.Close();
            }
        }

        // 不正なリクエストに対するレスポンス
        private async Task SendBadRequestResponse(StreamWriter writer)
        {
            string responseString = "HTTP/1.1 400 Bad Request\r\nContent-Type: text/plain\r\n\r\n不正なリクエストです";
            await writer.WriteAsync(responseString);
        }

        // リクエストデータの処理
        private void ProcessRequest(string path, string requestData)
        {
            if (path == "/data")
            {
                try
                {
                    using var jsonDoc = JsonDocument.Parse(requestData);
                    JsonElement root = jsonDoc.RootElement;

                    // dataの値を取得
                    if (root.TryGetProperty("data", out JsonElement dataElement))
                    {
                        string? dataValue = dataElement.GetString();
                        if (!string.IsNullOrEmpty(dataValue))
                        {
                            // dataValueを_+_で分割
                            string[] values = dataValue.Split("_+_");
                            // valuesの数が2つの場合
                            if (values.Length == 2)
                            {
                                // 2つの値を取得
                                string type = values[0];
                                string value = values[1];
                                // typeがhotkeyの場合
                                if (type == "hotkey")
                                {
                                    // シミュレート
                                    SimulateHotkey(value);
                                }
                            }
                            else
                            {
                                Console.WriteLine("dataの値が2つではありません。");
                            }
                        }
                        else
                        {
                            Console.WriteLine("dataが見つかりませんでした。");
                        }
                    }
                }
                catch (JsonException ex)
                {
                    Console.WriteLine("JSONパースエラー: " + ex.Message);
                }
            }
            else
            {
                Console.WriteLine("未対応のパス: " + path);
            }
        }

        // フォームが閉じられる時にサーバーを停止
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            StopTcpServer();
        }

        // TCPサーバーの停止
        private void StopTcpServer()
        {
            if (_tcpListener != null)
            {
                _tcpListener.Stop();
                _tcpListener = null;
            }

            // サーバースレッドを安全に停止
            if (_listenerThread != null && _listenerThread.IsAlive)
            {
                _cancellationTokenSource?.Cancel(); // スレッドを終了させるフラグをセット
                _listenerThread.Join(); // スレッドの終了を待つ
            }
        }

        // IPv4アドレスを取得
        private string GetLocalIPv4()
        {
            foreach (NetworkInterface networkInterface in NetworkInterface.GetAllNetworkInterfaces())
            {
                // ネットワークインターフェースが動作していて、イーサネットやWiFiである場合
                if (networkInterface.OperationalStatus == OperationalStatus.Up &&
                    (networkInterface.NetworkInterfaceType == NetworkInterfaceType.Ethernet ||
                     networkInterface.NetworkInterfaceType == NetworkInterfaceType.Wireless80211))
                {
                    foreach (UnicastIPAddressInformation ip in networkInterface.GetIPProperties().UnicastAddresses)
                    {
                        // IPv4アドレスを探す
                        if (ip.Address.AddressFamily == AddressFamily.InterNetwork)
                        {
                            return ip.Address.ToString();
                        }
                    }
                }
            }

            return "127.0.0.1"; // デフォルトのローカルIPアドレス
        }

        // QRコード生成
        private void GenerateQRCode(string qrText)
        {
            try
            {
                // QRCoderライブラリを使ってQRコードを生成
                QRCodeGenerator qrGenerator = new QRCodeGenerator();
                QRCodeData qrCodeData = qrGenerator.CreateQrCode(qrText, QRCodeGenerator.ECCLevel.Q);
                QRCode qrCode = new QRCode(qrCodeData);

                // PictureBoxのサイズに合わせてQRコードをビットマップ形式で生成
                int qrCodeSize = Math.Min(pictureBox1.Width, pictureBox1.Height); // PictureBoxの幅と高さの小さい方に合わせる
                Bitmap qrBitmap = qrCode.GetGraphic(10); // ピクセルサイズを調整

                // 生成したビットマップをPictureBoxのサイズにリサイズ
                Bitmap resizedQrBitmap = new Bitmap(qrBitmap, new Size(qrCodeSize, qrCodeSize));

                // PictureBoxにQRコードを表示
                pictureBox1.Image = resizedQrBitmap;
            }
            catch (Exception ex)
            {
                MessageBox.Show("QRコード生成中にエラーが発生しました: " + ex.Message);
            }
        }

        // ボタンクリックでキー操作をシミュレート
        private void button1_Click(object sender, EventArgs e)
        {
            Console.WriteLine("Button Clicked");
            SimulateHotkey("win");
        }

        public static void SimulateHotkey(string value)
        {
            // valueをスペースで分割
            string[] keys = value.Trim().Split(' ');
            Console.WriteLine("Hotkey: " + string.Join(", ", keys));

            // 修飾キーとキーコードを保持するリスト
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

            // キー押下イベントのシミュレーション
            if (modifiers.Count > 0 || codes.Count > 0)
            {
                // 修飾キーがある場合は同時押しをシミュレート
                if (modifiers.Count > 0)
                {
                    sim.Keyboard.ModifiedKeyStroke(modifiers, codes);
                }
                else if (codes.Count > 0)
                {
                    // 通常のキー押下
                    foreach (var code in codes)
                    {
                        sim.Keyboard.KeyPress(code);
                    }
                }
            }
        }

        public static VirtualKeyCode? ModifierFlagForKey(string key)
        {
            // 修飾キーに対応するVirtualKeyCodeを返す
            switch (key.ToLower())
            {
                case "control":
                case "ctrl":
                    return VirtualKeyCode.CONTROL;  // Ctrlキー
                case "shift":
                    return VirtualKeyCode.SHIFT;    // Shiftキー
                case "alt":
                    return VirtualKeyCode.MENU;     // Altキー
                case "capslock":
                    return VirtualKeyCode.CAPITAL;  // CapsLockキー
                case "numlock":
                    return VirtualKeyCode.NUMLOCK;  // NumLockキー
                case "scrolllock":
                    return VirtualKeyCode.SCROLL;   // ScrollLockキー
                case "win":
                case "lwin":
                    return VirtualKeyCode.LWIN;     // 左Windowsキー
                case "rwin":
                    return VirtualKeyCode.RWIN;     // 右Windowsキー
                case "apps":
                    return VirtualKeyCode.APPS;     // メニューキー (アプリケーションキー)
                default:
                    return null;
            }
        }

        public static VirtualKeyCode? KeyCodeForKey(string key)
        {
            // InputSimulatorで使えるキーコードを返す
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
