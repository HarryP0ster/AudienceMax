using System;
using agorartc;
using System.Threading;

namespace ConsoleAppOut
{
    class Program
    {
        static int parentID;
        static System.Diagnostics.Process proc;
        static void Main(string[] args)
        {

            //var retInput = XAgoraObject.SetupOutputDevices(args[2]);

            //Console.WriteLine(retInput);

            var retPubl = XAgoraObject.Publish(args[0], args[1], args[2]);
            Console.WriteLine(retPubl);

           // if (retInput != ERROR_CODE.ERR_OK ||
            //    retPubl != ERROR_CODE.ERR_OK)
            //    return;

            parentID = System.Convert.ToInt32(args[3]);
            proc = System.Diagnostics.Process.GetProcessById(parentID);
            proc.WaitForExit();

            //proc.Exited += ParentClose;

            //Console.WriteLine(args[1]);
            //Console.WriteLine(agoraObject.nameDevice);

            //while (true)
            //    System.Threading.Thread.Sleep(100);
        }

        private static void ParentClose(object sender, EventArgs e)
        {
            System.Environment.Exit(0);
        }
    }
    static class XAgoraObject
    {
        static AgoraRtcEngine Rtc;
        static AgoraAudioRecordingDeviceManager audioInDeviceManager;
        static AgoraAudioPlaybackDeviceManager audioPlaybackDeviceManager;
        static AgoraAudioPlaybackDeviceManager audioOutDeviceManager;

        const string AppID = "31f0e571a89542b09049087e3283417f";
        static string nameDevice;
        static XAgoraObject()
        {
            Rtc = AgoraRtcEngine.CreateRtcEngine();
            Rtc.MuteLocalVideoStream(true);
            Rtc.Initialize(new RtcEngineContext(AppID));
            audioInDeviceManager = Rtc.CreateAudioRecordingDeviceManager();
            audioOutDeviceManager = Rtc.CreateAudioPlaybackDeviceManager();
        }

        public static ERROR_CODE SetupOutputDevices(string ind)
        {
            return audioOutDeviceManager.SetCurrentDevice(ind);
        }

        public static ERROR_CODE Publish(string token, string name, string postfix)
        {
            ERROR_CODE res = Rtc.JoinChannel(token, name, "", 0);


            audioOutDeviceManager.GetCurrentDeviceInfo(out string idOUT, out string nameOUT);
            nameDevice = nameOUT;

            Console.WriteLine("\n\n\n\nHello World!");
            RecordAudio(true, postfix);
            return res;
        }

        public static void UnPublish()
        {
            RecordAudio(false, "");
            Rtc.LeaveChannel();
            //IsJoin = false;
        }
        internal static bool RecordAudio(bool state, string postfix)
        {
            string direct = Environment.GetFolderPath(Environment.SpecialFolder.MyMusic) + "\\RSI";
            string filename = $"AudioRecording-{DateTime.Now.ToString("dd-MM-yy-HH-mm-ss")}_" + postfix + ".wav";

            if (false == System.IO.Directory.Exists(direct))
                System.IO.Directory.CreateDirectory(direct);

            //SaveFileDialog fd = new()
            //{
            //    DefaultExt = "wav",
            //    Filter = "Audio files(*.wav)|*.wav|Audio files(*.acc)|*.acc",
            //    InitialDirectory = direct,
            //    FileName = filename,
            //};

            switch (state)
            {
                case true:
                    //fd.ShowDialog();

                    filename = "c:\\recordings\\" + filename;

                    if (System.IO.Path.GetDirectoryName(filename) == String.Empty)
                        return false;

                    AUDIO_RECORDING_QUALITY_TYPE quality = System.IO.Path.GetExtension(filename) == ".wav" ?
                        AUDIO_RECORDING_QUALITY_TYPE.AUDIO_RECORDING_QUALITY_MEDIUM :
                        AUDIO_RECORDING_QUALITY_TYPE.AUDIO_RECORDING_QUALITY_LOW;

                    Rtc.StartAudioRecording(filename, quality);
                    break;
                default:
                    Rtc.StopAudioRecording();
                    break;
            }
            //IsAudioRecordActive = state;
            return true;
        }
    }
}
