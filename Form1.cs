using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using QRCoder;  // QRCoder���C�u�������g�p


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

            // �t�H�[�������[�h���ꂽ�Ƃ���IPv4�A�h���X���擾���ĕ\��
            string ipv4Address = GetLocalIPv4();
            if (!string.IsNullOrEmpty(ipv4Address))
            {
                GenerateQRCode(ipv4Address);
            }
            else
            {
                MessageBox.Show("IPv4�A�h���X��������܂���ł����B");
            }
        }

        // HTTP�T�[�o�[�̊J�n
        private void StartHttpServer()
        {
            _httpListener = new HttpListener();
            _httpListener.Prefixes.Add("http://localhost:8080/");
            _httpListener.Start();
            _listenerThread = new Thread(HandleRequests);
            _listenerThread.Start();
            Console.WriteLine("HTTP�T�[�o�[���N�����܂����B");
        }

        // ���N�G�X�g������
        private void HandleRequests()
        {
            while (_httpListener.IsListening)
            {
                try
                {
                    // �N���C�A���g����̃��N�G�X�g��ҋ@
                    HttpListenerContext context = _httpListener.GetContext();


                    // ���N�G�X�g���̎擾
                    HttpListenerRequest request = context.Request;
                    string requestData = new System.IO.StreamReader(request.InputStream).ReadToEnd();
                    Console.WriteLine("���N�G�X�g����M: " + requestData);
                    MessageBox.Show(requestData);


                    // ���X�|���X��Ԃ�
                    HttpListenerResponse response = context.Response;
                    string responseString = "QR�R�[�h����������܂���!";
                    byte[] buffer = System.Text.Encoding.UTF8.GetBytes(responseString);
                    response.ContentLength64 = buffer.Length;
                    response.OutputStream.Write(buffer, 0, buffer.Length);
                    response.OutputStream.Close();
                }
                catch (HttpListenerException)
                {
                    break; // ���X�i�[����~�����Ƃ��̗�O����������
                }
                catch (Exception ex)
                {
                    Console.WriteLine("�G���[: " + ex.Message);
                }
            }
        }

        // �t�H�[���������鎞�ɃT�[�o�[���~
        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            StopHttpServer();
        }

        // HTTP�T�[�o�[�̒�~
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
                            localIP = ip.Address.ToString();
                            return localIP;
                        }
                    }
                }
            }

            return localIP;
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

                // QR�R�[�h���r�b�g�}�b�v�`���Ő���
                Bitmap qrBitmap = qrCode.GetGraphic(7);

                // PictureBox��QR�R�[�h��\��
                pictureBox1.Image = qrBitmap;
            }
            catch (Exception ex)
            {
                MessageBox.Show("QR�R�[�h�������ɃG���[���������܂���: " + ex.Message);
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            // QR�R�[�h�𐶐�����e�L�X�g�iURL�Ȃǁj
            string qrText = "https://example.com";  // �����ɔC�ӂ̃e�L�X�g�����܂�

            // QRCoder���C�u�������g����QR�R�[�h�𐶐�
            QRCodeGenerator qrGenerator = new QRCodeGenerator();
            QRCodeData qrCodeData = qrGenerator.CreateQrCode(qrText, QRCodeGenerator.ECCLevel.Q);
            QRCode qrCode = new QRCode(qrCodeData);

            // QR�R�[�h���r�b�g�}�b�v�`���Ő���
            Bitmap qrBitmap = qrCode.GetGraphic(20);

            // PictureBox��QR�R�[�h��\��
            pictureBox1.Image = qrBitmap;
        }
    }
}
