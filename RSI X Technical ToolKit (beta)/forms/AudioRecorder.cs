﻿using System;
using System.Collections.Generic;
using agorartc;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using System.Runtime.InteropServices;
using static System.Environment;

namespace RSI_X_Desktop.forms
{
    public partial class Devices : Form
    {
        private const string AppOut = "appOut.exe";
        private const string StopWord = "Stop";
        private const string StartWord = "Start";
        private string HostName = string.Empty;
        bool Order = false;
        public class ChanBtn
        {
            internal ReaLTaiizor.Controls.SkyButton btn;
            //Переменная для хранения количества языков 
            int indexLang;
            //Логика активности языков
            public bool langNotActive { get; private set; } = true;
            //Индекс количества подключенного оборудования
            public ChanBtn(ReaLTaiizor.Controls.SkyButton btn, int indexLang)
            {
                this.btn = btn;
                this.indexLang = indexLang;
            }
            public int GetIndexLang() { return indexLang; }
            public void ButtonRelay_Click(object sender, EventArgs e)
            {
                UpdateColors(sender as ReaLTaiizor.Controls.SkyButton, !langNotActive);
            }
            public void UpdateColors(ReaLTaiizor.Controls.SkyButton btn, bool? active = null)
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
            }
        }

        public bool GetOutCode { get; private set; } = false;
        public bool IsPublishing { get; private set; } = false;

        AgoraAudioPlaybackDeviceManager audioOutDeviceManager;

        static readonly Color ButtonPushColor = Color.BurlyWood;

        List<string> devicesOutInd = new();
        List<string> devicesOutName = new();
        List<ChanBtn> BtnCmbPairs = new();
        List<ChanBtn> BtnCmbPairs2 = new();
        List<Process> XAgora = new();
        List<langHolder> Langs = AgoraObject.GetComplexToken().GetTranslLangs;
        int Output_ind = 0;


        //Обновляем список языков и оборудования для работы
        private void UpdateRelayLangs()
        {
            langHolder host = new();
            host.langFull = AgoraObject.GetHostName();
            host.langShort = "Floor";
            host.token = AgoraObject.GetHostToken();
            Langs.Insert(0, host);
            //Переменная для языков
            var langs = AgoraObject.GetComplexToken();
            var controls = panelRelayButtons.Controls;
            int defHeight = 35;
            int offset_l = 1;
            int offset_r = 1;

            ReaLTaiizor.Controls.SkyButton btn;
            ChanBtn pair;

            for (int i = 0; i < Langs.Count; i++)
            {
                var lang = Langs[i];
                //if (lang.langShort == "HOST") continue;

                btn = CreateButton(lang.langShort, defHeight, 80);
                btn.Name = "REL" + i.ToString();
                btn.Height = 35;
                btn.Font = new Font("Segoe UI Semibold", 12);
                btn.Cursor = Cursors.Hand;
                if (!Order)
                {
                    btn.Location = new Point(64, defHeight * offset_l);
                    offset_l++;
                }
                else
                {
                    btn.Location = new Point(284, defHeight * offset_r);
                    offset_r++;
                }
                btn.Show();
                Order = !Order;

                controls.Add(btn);


                pair = new(btn, i);

                btn.Click += pair.ButtonRelay_Click;

                BtnCmbPairs.Add(pair);
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
                fsd.ShowDialog(this);
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
                    string ind = devicesOutInd[Output_ind];

                    //pair.UpdateColors(pair.btn, pair.langNotActive);
                    //pair.btn.Refresh();
                    langHolder lh;
                        lh = AgoraObject.room.GetTargetRoomsAt(pair.GetIndexLang());
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

        private void OutputCmb_SelectedIndexChanged(object sender, EventArgs e)
        {
            Output_ind = OutputCmb.SelectedIndex;
        }

        private void materialShowTabControl1_SelectedIndexChanged(object sender, EventArgs e)
        {
            //
        }
    }
}
