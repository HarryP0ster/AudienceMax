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
    internal enum SettingPages
    {
        General = 0,
        Audio = 1,
        Recorder = 2
    }
    public partial class Devices : Form
    {
        [DllImport("winmm.dll")]
        public  static extern int waveOutSetVolume(IntPtr hwo, uint dwVolume); //Контроль громкости
        private static int volume = 100;
        public int Volume { get => volume; }
        
        private IFormHostHolder workForm = AgoraObject.GetWorkForm;
        private static AgoraAudioPlaybackDeviceManager SpeakersManager;
        static List<string> Speakers;

        private static int oldVolumeOut;
        private static string oldSpeaker = null;

        public Devices()
        {
            InitializeComponent();

            audioOutDeviceManager = AgoraObject.Rtc.CreateAudioPlaybackDeviceManager();

            for (int i = 0; i < audioOutDeviceManager.GetDeviceCount(); i++)
            {
                var ret = audioOutDeviceManager.GetDeviceInfoByIndex(i, out string device, out string id);

                devicesOutName.Add(device);
                devicesOutInd.Add(id);
                OutputCmb.Items.Add(device);
            }
            OutputCmb.SelectedIndex = 0;
            UpdateRelayLangs();
        }
        public static void SetVolume(int value) 
        {
            volume = value;
            int NewVolume = ((ushort.MaxValue / 100) * value);
            uint NewVolumeAllChannels = (((uint)NewVolume & 0x0000ffff) | ((uint)NewVolume << 16));

            waveOutSetVolume(IntPtr.Zero, NewVolumeAllChannels);
        }

        private void NewDevices_Load(object sender, EventArgs e)
        {
            SpeakersManager = AgoraObject.Rtc.CreateAudioPlaybackDeviceManager();

            oldVolumeOut = Volume;
            trackBarSoundOut.Value = oldVolumeOut;
            
            Speakers = getListAudioOutDevices();
            UpdateComboBoxSpeaker();
            getComputerDescription();
        }
        private void UpdateComboBoxSpeaker()
        {
            Speakers = getListAudioOutDevices();
            bool hasoldSpeaker = Speakers.Any((s) => s == oldSpeaker);

            int index = (oldSpeaker != null) ?
                Speakers.FindLastIndex((s) => s == oldSpeaker) :
                getActiveAudioOutputDevice();

            if (Speakers.Count == 0)
            {
                comboBoxAudioOutput.DataSource = new List<string> { "Playback Devices Error" };
                return;
            }

            if (index < 0)
                index = 0;

            oldSpeaker = Speakers[index];
            comboBoxAudioOutput.DataSource = Speakers;
            comboBoxAudioOutput.SelectedIndex = index;
        }

        private void getComputerDescription()
        {
            dungeonLabel1.Text = "Версия ОС - " + OSVersion.VersionString;

            if (Is64BitOperatingSystem == true)
            {
                dungeonLabel2.Text = "64 Bit операционная система";
            }
            else
            {
                dungeonLabel2.Text = "32 Bit операционная система";
            }

            dungeonLabel3.Text = "Пользователь - " + UserName;

        }

        private int getActiveAudioOutputDevice()
        {
            int id = -1;

            SpeakersManager.GetCurrentDeviceInfo(out string idAcvite, out string nameAcitve);

            for (int i = 0; i < SpeakersManager.GetDeviceCount(); i++)
            {
                var ret = SpeakersManager.GetDeviceInfoByIndex(i, out string name, out string deviceid);
                if (idAcvite == deviceid)
                {
                    id = i;
                    break;
                }
            }
            return id;
        }

        #region getDevicesList

        private List<string> getListAudioOutDevices()
        {
            List<string> devicesOut = new();

            for (int i = 0; i < SpeakersManager.GetDeviceCount(); i++)
            {
                string device, id;

                var ret = SpeakersManager.GetDeviceInfoByIndex(i, out device, out id);

                if (ret == ERROR_CODE.ERR_OK)
                    devicesOut.Add(device);
            }

            return devicesOut;
        }
        #endregion

        #region ComboBoxEventHandlers

        private void comboBoxAudioOutput_SelectedIndexChanged(object sender, EventArgs e)
        {
            int ind = ((ComboBox)sender).SelectedIndex;
            string name, id;

            SpeakersManager.GetDeviceInfoByIndex(ind, out name, out id);
            //audioOutDeviceManager.SetCurrentDevice(id);
        }

        #endregion

        private void NewDevices_FormClosed(object sender, FormClosedEventArgs e)
        {
            if (AgoraObject.CurrentForm != CurForm.FormAudience)
            {
                Dispose();
            }
        }

        private void AcceptButton_Click(object sender, EventArgs e)
        {
            var aout = comboBoxAudioOutput.SelectedIndex;

            if (Speakers.Count() < aout) oldSpeaker = Speakers[aout];
         
            oldVolumeOut = trackBarSoundOut.Value;

            CloseButton_Click(sender, e);
        }

        internal void CloseButton_Click(object sender, EventArgs e)
        {
            trackBarSoundOut.Value = oldVolumeOut;
            trackBarSoundOut_ValueChanged();

            if (oldSpeaker != null)
                SpeakersManager.SetCurrentDevice(oldSpeaker);

            AgoraObject.GetWorkForm?.DevicesClosed(this);
            //Close();
        }

        private void trackBarSoundOut_ValueChanged()
        {
            SetVolume(trackBarSoundOut.Value);
            if (workForm != null)
                workForm.SetTrackBarVolume(trackBarSoundOut.Value);
        }
        public void UpdateSoundTrackBar() 
        {
            trackBarSoundOut.Value = volume;
        }
        public void typeOfAlligment(bool sign)
        {
            if (sign == true)
                materialShowTabControl1.Alignment = TabAlignment.Left;
            else
                materialShowTabControl1.Alignment = TabAlignment.Right;
        }

        internal void ChangeSelectedTab(int newTabIndex)
        {
            materialShowTabControl1.SelectedIndex = newTabIndex;
        }
    }
}
