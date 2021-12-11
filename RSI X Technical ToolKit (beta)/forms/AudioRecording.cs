using agorartc;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;

namespace RSI_X_Desktop.forms
{
    public partial class AudioRecording : Form
    {
        private const string AppOut = "appOut.exe";
        private const string StopWord = "Stop";
        private const string StartWord = "Start";
        private string HostName = string.Empty;
        //Публичный класс, который описывает пары языковых кнопок и списки подключенного оборудования
        public class BtnCmbPair
        {
            internal ReaLTaiizor.Controls.SkyButton btn;
            ComboBox cmb;
            //Переменная для хранения количества языков 
            int indexLang;
            //Логика активности языков
            public bool langNotActive { get; private set; } = true;
            //Индекс количества подключенного оборудования
            public BtnCmbPair(ReaLTaiizor.Controls.SkyButton btn, ComboBox cmb, int indexLang)
            {
                this.btn = btn;
                this.cmb = cmb;
                this.indexLang = indexLang;
            }
            public int GetIndexID()
            {
                if (cmb == null)
                    return -1;
                return cmb.SelectedIndex;
            }
            public int GetIndexLang() { return indexLang; }
            public void ButtonRelay_Click(object sender, EventArgs e)
            {
                UpdateColors(sender as ReaLTaiizor.Controls.SkyButton, !langNotActive);
            }
            public void UpdateColors(ReaLTaiizor.Controls.SkyButton btn, bool? active=null)
            {
                if (active.HasValue)
                    langNotActive = active.Value;
                
                AgoraObject.room.SetActiveInterpRoomsAt(indexLang, !langNotActive);

                if (langNotActive)
                {
                    SkyBtnUpdate(btn, Color.FromArgb(80, 80, 80), Color.FromArgb(64, 64, 64), Color.FromArgb(64, 64, 64),
                        Color.FromArgb(50, 50, 50), Color.White, Color.LightGray, Color.FromArgb(45, 45, 45), Color.FromArgb(45, 45, 45),
                            Color.FromArgb(53, 53, 53));
                }
                else
                {
                    SkyBtnUpdate(btn, Color.FromArgb(80, 80, 80), Color.FromArgb(64, 64, 64), Color.FromArgb(64, 64, 64),
                        Color.FromArgb(50, 50, 50), Color.White, Color.LightGray, Color.FromArgb(45, 45, 45), Color.Red,
                        Color.DarkRed);
                }
            }
            internal void Enable(bool enable)
            {
                btn.Enabled = enable;
                cmb.Enabled = enable;
            }
        }

        public bool GetOutCode { get; private set; } = false;
        public bool IsPublishing { get; private set; } = false;

        AgoraAudioPlaybackDeviceManager audioOutDeviceManager;

        static readonly Color ButtonPushColor = Color.BurlyWood;

        List<string> devicesOutInd = new();
        List<string> devicesOutName = new();
        List<BtnCmbPair> BtnCmbPairs = new();
        List<BtnCmbPair> BtnCmbPairs2 = new();
        List<Process> XAgora = new();

        public AudioRecording()
        {
            InitializeComponent();
            audioOutDeviceManager = AgoraObject.Rtc.CreateAudioPlaybackDeviceManager();

            for (int i = 0; i < audioOutDeviceManager.GetDeviceCount(); i++)
            {
                var ret = audioOutDeviceManager.GetDeviceInfoByIndex(i, out string device, out string id);

                devicesOutName.Add(device);
                devicesOutInd.Add(id);
            }

            UpdateRelayLangs();
        }

        //Обновляем список языков и оборудования для работы
        private void UpdateRelayLangs()
        {
            //Переменная для языков
            var langs = AgoraObject.GetComplexToken();
            var controls = panelRelayButtons.Controls;
            int defHeight = 35;
            int offset = 1;

            ReaLTaiizor.Controls.SkyButton btn;
            ComboBox cmb;
            //Соответствие пар
            BtnCmbPair pair;

            for (int i = 0; i < langs.GetTranslLangs.Count; i++)
            {
                var lang = langs.GetTranslLangs[i];
                if (lang.langShort == "HOST") continue;

                btn = CreateButton(lang.langShort, defHeight, 80);
                btn.Name = "REL" + i.ToString();
                btn.Height = 35;
                btn.Font = new Font("Segoe UI Semibold", 12);
                btn.Cursor = Cursors.Hand;
                btn.Location = new Point(24, defHeight * offset);
                btn.Show();

                cmb = new ComboBox();
                cmb.Name = "CMB" + i.ToString();
                cmb.Height = defHeight;
                cmb.Width = 450;
                cmb.Font = new Font("Segoe UI Semibold", 12);
                cmb.Cursor = Cursors.Hand;
                cmb.Location = new Point(120, defHeight * offset);
                cmb.DropDownStyle = ComboBoxStyle.DropDownList;



                if (devicesOutName.Count > 0)
                {
                    foreach (var dev in devicesOutName)
                        cmb.Items.Add(dev);
                    cmb.SelectedIndex = 0;
                }
                cmb.Show();

                controls.Add(cmb);

                controls.Add(btn);


                pair = new(btn, cmb, i);

                btn.Click += pair.ButtonRelay_Click;

                BtnCmbPairs.Add(pair);
                offset++;
                ButtonRelay_Click(btn, new EventArgs());
                pair.UpdateColors(btn);
            }
        }
        internal void Publish()
        {
            string direct = String.Empty;
            XAgora = new List<Process>();

            KillRecProcess();

            using (var fsd = new FolderBrowserDialog() { RootFolder = Environment.SpecialFolder.MyMusic })
            {
                fsd.ShowDialog();
                direct = fsd.SelectedPath;
            }

            if (direct == "") return;
            direct += "\\RSI\\" + DateTime.Now.ToString("ddMMyyHHmmss") + "\\";

            if (false == System.IO.Directory.Exists(direct))
                System.IO.Directory.CreateDirectory(direct);

            int index = 1;
            foreach (var pair in BtnCmbPairs)
            {
                pair.Enable(false);
                
                if (pair.langNotActive == false)
                {
                    int indId = pair.GetIndexID();
                    string ind = devicesOutInd[indId];

                    //pair.UpdateColors(pair.btn, pair.langNotActive);
                    //pair.btn.Refresh();

                    langHolder lh = AgoraObject.room.GetTargetRoomsAt(pair.GetIndexLang() + 1);
                    int id = Process.GetCurrentProcess().Id;

                    List<string> args = new() { lh.token, lh.langFull, lh.langShort, id.ToString(), direct };

                    Process proc = new Process();
                    proc.StartInfo.CreateNoWindow = true;
                    proc = Process.Start(AppOut, args);
                    System.Threading.Thread.Sleep(60);

                    XAgora.Add(proc);
                    index++;
                }
            }

            IsPublishing = true;
            mButton_start.Text = StopWord;
        }
        internal void UnPublish()
        {
            KillRecProcess();

            foreach (var pair in BtnCmbPairs)
            {
                pair.Enable(true);
                if (pair.langNotActive == false)
                {
                    pair.UpdateColors(pair.btn);
                    pair.btn.Refresh();
                }
            }

            IsPublishing = false;
            mButton_start.Text = StartWord;
        }
        private void KillRecProcess()
        {
            foreach (var proc in XAgora)
            {
                proc.Kill();
            }
        }

        #region ButtonEvents
        private void ButtonRelay_Click(object sender, EventArgs e)
        {
            // buttons with name REL#
            string s_index = ((ReaLTaiizor.Controls.SkyButton)sender).Name[3..];
            int m_intRel = Convert.ToInt32(s_index);
            bool? langActiveT = AgoraObject.room.IsActiveInterpRoomsAt(m_intRel);

            if (langActiveT != null)
            {
                bool langActive = (bool)langActiveT;
                AgoraObject.room.SetActiveInterpRoomsAt(m_intRel, !langActive);

                (sender as ReaLTaiizor.Controls.SkyButton).BackColor = 
                    !langActive ? ButtonPushColor : DefaultBackColor;
            }
        }
        private void buttonOk_Click(object sender, EventArgs e)
        {
            if (IsPublishing)
                UnPublish();
            else
                Publish();
        }
        private void mButton_cancel_Click(object sender, EventArgs e)
        {
            Close();
        }
        #endregion

        internal ReaLTaiizor.Controls.SkyButton CreateButton(string text, int height, int width)
        {
            return new ReaLTaiizor.Controls.SkyButton()
            {
                Text = text,
                Height = height,
                Width = width,
                Font = new Font("Bahnschrift Condensed", 14, FontStyle.Bold),
            };
        }
        private static void SkyBtnUpdate(ReaLTaiizor.Controls.SkyButton btn, Color BorA, Color BorB, Color BorC, Color BorD, Color Fore, Color ForeShad,
                Color BackColor, Color NormA, Color NormB)
        {
            btn.NormalBorderColorA = BorA;
            btn.NormalBorderColorB = BorB;
            btn.NormalBorderColorC = BorC;
            btn.NormalBorderColorD = BorD;
            btn.NormalForeColor = Fore;
            btn.NormalShadowForeColor = ForeShad;
            btn.BackColor = BackColor;
            btn.NormalBGColorA = NormA;
            btn.NormalBGColorB = NormB;
        }

        private void Ingestor_FormClosed(object sender, FormClosedEventArgs e)
        {
            AgoraObject.LeaveHostChannel();
            AgoraObject.LeaveSrcChannel();
            //AgoraObject.LeaveTranslChannel();
            //AgoraObject.LeaveTargetChannel();
            KillRecProcess();
            Owner.Show();
            Owner.Refresh();
        }
    }
}
