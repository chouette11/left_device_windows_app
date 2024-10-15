using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

using System;
using System.Windows.Forms;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Text.Json;

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

//namespace LeftDeviceWindows
//{
//    public partial class ClientForm : Form
//    {
//        public ClientForm()
//        {
//            InitializeComponent();
//        }



//        private void buttonScreenShot_Click(object sender, EventArgs e)
//        {

//        }

//        private void buttonCopy_Click_1(object sender, EventArgs e)
//        {

//        }

//        private void buttonCut_Click_1(object sender, EventArgs e)
//        {

//        }
//    }
//}


namespace LeftDeviceWindows
{
    public enum ActionType
    {
        Hotkey,
        Typewrite,
        // 他のアクションタイプがあれば追加
    }

    public class ActionEntity
    {
        public ActionType Type { get; set; }
        public string Value { get; set; }
    }

    public partial class ClientForm : Form
    {
        private TextBox textBoxIpAddress;

        public ClientForm()
        {
            InitializeComponent();

            //// フォームの初期設定
            //this.Text = "クライアントウィンドウ";
            //this.FormBorderStyle = FormBorderStyle.FixedSingle;
            //this.MaximizeBox = false;
            //this.ClientSize = new System.Drawing.Size(1000, 600);

            // コントロールを追加
            InitializeControls();
        }

        private void InitializeControls()
        {
            // IPアドレス入力
            Label labelIpAddress = new Label() { Text = "IPア:", Left = 10, Top = 10, Width = 70 };
            textBoxIpAddress = new TextBox() { Left = 90, Top = 10, Width = 150 };

            //// アクションタイプ選択
            //Label labelActionType = new Label() { Text = "アクションタイプ:", Left = 10, Top = 40, Width = 70 };
            //comboBoxActionType = new ComboBox() { Left = 90, Top = 40, Width = 150 };
            //comboBoxActionType.Items.AddRange(Enum.GetNames(typeof(ActionType)));
            //comboBoxActionType.SelectedIndex = 0;

            //// 値入力
            //Label labelValue = new Label() { Text = "値:", Left = 10, Top = 70, Width = 70 };
            //textBoxValue = new TextBox() { Left = 90, Top = 70, Width = 150 };

            //// 送信ボタン
            //buttonSend = new Button() { Text = "送信", Left = 90, Top = 100, Width = 80 };
            //buttonSend.Click += ButtonSend_Click;

            // コントロールをフォームに追加
            this.Controls.Add(labelIpAddress);
            this.Controls.Add(textBoxIpAddress);
        }

        private async void ButtonSend_Click(String value, ActionType actionType)
        {
            string ipAddress = textBoxIpAddress.Text.Trim();

            if (string.IsNullOrEmpty(ipAddress))
            {
                MessageBox.Show("IPアドレスを入力してください");
                return;
            }

            
            ActionEntity action = new ActionEntity { Type = actionType, Value = value };
            await SendData(action, ipAddress);
        }

        private async Task SendData(ActionEntity action, string ipAddress)
        {
            using (var client = new HttpClient())
            {
                var url = $"http://{ipAddress}:8080/data";

                var data = new Dictionary<string, string>
                {
                    { "data", $"{action.Type.ToString().ToLower()}_+_{action.Value}" }
                };

                var json = JsonSerializer.Serialize(data);

                var content = new StringContent(json, Encoding.UTF8, "application/json");

                try
                {
                    var response = await client.PostAsync(url, content);

                    if (response.IsSuccessStatusCode)
                    {
                        string responseBody = await response.Content.ReadAsStringAsync();
                        MessageBox.Show("Success: " + responseBody);
                    }
                    else
                    {
                        MessageBox.Show("データの送信に失敗しました");
                    }
                }
                catch (Exception ex)
                {
                    
                }
            }
        }

        private void buttonScreenShot_Click(object sender, EventArgs e)
        {
            ButtonSend_Click("win shift s", ActionType.Hotkey);
        }

        private void buttonCopy_Click_1(object sender, EventArgs e)
        {
            ButtonSend_Click("ctrl c", ActionType.Hotkey);
        }

        private void buttonCut_Click_1(object sender, EventArgs e)
        {
            ButtonSend_Click("ctrl x", ActionType.Hotkey);
        }

        private void buttonPaste_Click(object sender, EventArgs e)
        {
            ButtonSend_Click("ctrl v", ActionType.Hotkey);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            ButtonSend_Click("1", ActionType.Typewrite);
        }

        private void button2_Click(object sender, EventArgs e)
        {
            ButtonSend_Click("2", ActionType.Typewrite);
        }

        private void button3_Click(object sender, EventArgs e)
        {
            ButtonSend_Click("3", ActionType.Typewrite);
        }

        private void button4_Click(object sender, EventArgs e)
        {
            ButtonSend_Click("4", ActionType.Typewrite);
        }

        private void button5_Click(object sender, EventArgs e)
        {
            ButtonSend_Click("5", ActionType.Typewrite);
        }

        private void button6_Click(object sender, EventArgs e)
        {
            ButtonSend_Click("6", ActionType.Typewrite);
        }

        private void button7_Click(object sender, EventArgs e)
        {
            ButtonSend_Click("7", ActionType.Typewrite);
        }

        private void button8_Click(object sender, EventArgs e)
        {
            ButtonSend_Click("8", ActionType.Typewrite);
        }

        private void button9_Click(object sender, EventArgs e)
        {
            ButtonSend_Click("9", ActionType.Typewrite);
        }

        private void button10_Click(object sender, EventArgs e)
        {
            ButtonSend_Click("0", ActionType.Typewrite);
        }
    }
}
