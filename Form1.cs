using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using QRCoder;  // QRCoderライブラリを使用


namespace LeftDeviceWindows
{
    public partial class Form1 : Form
    {
        private HttpListener? _httpListener;
        private Thread? _listenerThread;

        public Form1()
        {
            InitializeComponent();
        }

        private void Form1_Load(object sender, EventArgs e)
        {
            StartHttpServer();

            // フォームがロードされたときにIPv4アドレスを取得して表示
            string ipv4Address = GetLocalIPv4();
            if (!string.IsNullOrEmpty(ipv4Address))
            {
                GenerateQRCode(ipv4Address);
            }
            else
            {
                MessageBox.Show("IPv4アドレスが見つかりませんでした。");
            }
        }

        // HTTPサーバーの開始
        private void StartHttpServer()
        {
            _httpListener = new HttpListener();
            _httpListener.Prefixes.Add("http://localhost:8080/");
            _httpListener.Start();
            _listenerThread = new Thread(HandleRequests);
            _listenerThread.Start();
            Console.WriteLine("HTTPサーバーを起動しました。");
        }

        // リクエストを処理
        private void HandleRequests()
        {
            while (_httpListener.IsListening)
            {
                try
                {
                    // クライアントからのリクエストを待機
                    HttpListenerContext context = _httpListener.GetContext();


                    // リクエスト情報の取得
                    HttpListenerRequest request = context.Request;
                    string requestData = new System.IO.StreamReader(request.InputStream).ReadToEnd();
                    Console.WriteLine("リクエストを受信: " + requestData);
                    MessageBox.Show(requestData);


                    // レスポンスを返す
                    HttpListenerResponse response = context.Response;
                    string responseString = "QRコードが生成されました!";
                    byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                    response.ContentLength64 = buffer.Length;
                    response.OutputStream.Write(buffer, 0, buffer.Length);
                    response.OutputStream.Close();
                }
                catch (HttpListenerException)
                {
                    break; // リスナーが停止されるとこの例外が発生する
                }
                catch (Exception ex)
                {
                    Console.WriteLine("エラー: " + ex.Message);
                }
            }
        }

        // フォームが閉じられる時にサーバーを停止
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            StopHttpServer();
        }

        // HTTPサーバーの停止
        private void StopHttpServer()
        {
            if (_httpListener != null && _httpListener.IsListening)
            {
                _httpListener.Stop();
                _httpListener.Close();
            }

            if (_listenerThread != null && _listenerThread.IsAlive)
            {
                _listenerThread.Abort();
            }
        }

        private string GetLocalIPv4()
        {
            string localIP = string.Empty;

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
                            localIP = ip.Address.ToString();
                            return localIP;
                        }
                    }
                }
            }

            return localIP;
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

                // QRコードをビットマップ形式で生成
                Bitmap qrBitmap = qrCode.GetGraphic(7);

                // PictureBoxにQRコードを表示
                pictureBox1.Image = qrBitmap;
            }
            catch (Exception ex)
            {
                MessageBox.Show("QRコード生成中にエラーが発生しました: " + ex.Message);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // QRコードを生成するテキスト（URLなど）
            string qrText = "https://example.com";  // ここに任意のテキストを入れます

            // QRCoderライブラリを使ってQRコードを生成
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(qrText, QRCodeGenerator.ECCLevel.Q);
            QRCode qrCode = new QRCode(qrCodeData);

            // QRコードをビットマップ形式で生成
            Bitmap qrBitmap = qrCode.GetGraphic(20);

            // PictureBoxにQRコードを表示
            pictureBox1.Image = qrBitmap;
        }
    }
}
