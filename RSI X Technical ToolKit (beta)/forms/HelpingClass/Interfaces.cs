﻿using System;

namespace RSI_X_Desktop
{
    public interface IFormHostHolder
    {
        public IntPtr RemoteWnd { get; }
        public void UpdateRemoteWnd();
        public void DevicesClosed(System.Windows.Forms.Form Wnd);
        public void SetTrackBarVolume(int volume);
        public void NewBroadcaster(uint uid, agorartc.UserInfo info);
        public void BroadcasterUpdateInfo(uint uid, agorartc.UserInfo info);
        public void BroadcasterLeave(uint uid);
    }
    public interface IFormInterpreterHolder 
    { }
}
