using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Windows.Forms;
using System.Linq;
using System.Runtime.InteropServices;
using agora.rtc;
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
        public DeviceInfo SelectedSpeaker { get; private set; }

        private IFormHostHolder workForm;
        private static IAgoraRtcAudioPlaybackDeviceManager SpeakersManager;
        static List<DeviceInfo> Speakers;

        private static int oldVolumeOut;
        private static DeviceInfo oldSpeaker;

        public Devices()
        {
            InitializeComponent();

            SpeakersManager = AgoraObject.Rtc.GetAgoraRtcAudioPlaybackDeviceManager();

            foreach (var dev in SpeakersManager.EnumeratePlaybackDevices())
            {
                devicesOutName.Add(dev.deviceName);
                devicesOutInd.Add(dev.deviceId);
                OutputCmb.Items.Add(dev.deviceName);
            }
            OutputCmb.SelectedIndex = 0;
            UpdateRelayLangs();
            workForm = AgoraObject.GetWorkForm;
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
            oldVolumeOut = Volume;
            trackBarSoundOut.Value = oldVolumeOut;
            
            Speakers = getListAudioOutDevices();
            UpdateComboBoxSpeaker();
            getComputerDescription();
        }
        private void UpdateComboBoxSpeaker()
        {
            Speakers = getListAudioOutDevices();
            bool hasoldSpeaker = Speakers.Any((s) => s.deviceId == oldSpeaker.deviceId);

            int index = (oldSpeaker.deviceId != null) ?
                Speakers.FindLastIndex((s) => s.deviceId == oldSpeaker.deviceId) :
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
            bool found = false;
            int id = 0;
            var device = SpeakersManager.GetPlaybackDevice();

            foreach (var dev in SpeakersManager.EnumeratePlaybackDevices())
            {
                if (device == dev.deviceId) { found = true; break; };
                id += 1;
            }

            if (!found)
                id = -1;

            return id;
        }

        #region getDevicesList

        private static List<DeviceInfo> getListAudioOutDevices()
        {
            List<DeviceInfo> devicesOut = new();
            devicesOut.AddRange(SpeakersManager.EnumeratePlaybackDevices());

            return devicesOut;
        }

        #endregion

        #region ComboBoxEventHandlers
        private void comboBoxAudioOutput_SelectedIndexChanged(object sender, EventArgs e)
        {
            DeviceInfo dev;
            int ind = ((ComboBox)sender).SelectedIndex;
            var RecorderList = SpeakersManager.EnumeratePlaybackDevices();

            if (RecorderList.Length > ind)
                dev = RecorderList[ind];

            SelectedSpeaker = dev;
            SpeakersManager.SetPlaybackDevice(dev.deviceId);
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

            if (oldSpeaker.deviceId != null)
                SpeakersManager.SetPlaybackDevice(oldSpeaker.deviceId);

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
