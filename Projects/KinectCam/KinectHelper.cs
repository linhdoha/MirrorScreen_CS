using System;
using System.Diagnostics;
using Microsoft.Kinect;
using System.Threading;
using System.Windows.Forms;
using System.Drawing;
using System.IO;

namespace KinectCam
{
    public static class KinectHelper
    {
        class KinectCamApplicationContext : ApplicationContext
        {
            private NotifyIcon TrayIcon;
            private ContextMenuStrip TrayIconContextMenu;
            private ToolStripMenuItem MirroredMenuItem;
            private ToolStripMenuItem DesktopMenuItem;

            public KinectCamApplicationContext()
            {
                Application.ApplicationExit += new EventHandler(this.OnApplicationExit);
                InitializeComponent();
                TrayIcon.Visible = true;
                //TrayIcon.ShowBalloonTip(30000);
            }

            private void InitializeComponent()
            {
                TrayIcon = new NotifyIcon();

                TrayIcon.BalloonTipIcon = ToolTipIcon.Info;
                TrayIcon.BalloonTipText =
                  "For options use this tray icon.";
                TrayIcon.BalloonTipTitle = "KinectCamV2 (BI Version)";
                TrayIcon.Text = "KinectCam (BI Version)";

                TrayIcon.Icon = IconExtractor.Extract(117, false);

                TrayIcon.DoubleClick += TrayIcon_DoubleClick;

                TrayIconContextMenu = new ContextMenuStrip();
                MirroredMenuItem = new ToolStripMenuItem();
                DesktopMenuItem = new ToolStripMenuItem();
                TrayIconContextMenu.SuspendLayout();

                // 
                // TrayIconContextMenu
                // 
                this.TrayIconContextMenu.Items.AddRange(new ToolStripItem[] {
                this.MirroredMenuItem,
                this.DesktopMenuItem
                });
                this.TrayIconContextMenu.Name = "TrayIconContextMenu";
                this.TrayIconContextMenu.Size = new Size(153, 70);
                // 
                // MirroredMenuItem
                // 
                this.MirroredMenuItem.Name = "Mirrored";
                this.MirroredMenuItem.Size = new Size(152, 22);
                this.MirroredMenuItem.Text = "Mirrored";
                this.MirroredMenuItem.Click += new EventHandler(this.MirroredMenuItem_Click);

                // 
                // DesktopMenuItem
                // 
                this.DesktopMenuItem.Name = "Desktop";
                this.DesktopMenuItem.Size = new Size(152, 22);
                this.DesktopMenuItem.Text = "Desktop";
                this.DesktopMenuItem.Click += new EventHandler(this.DesktopMenuItem_Click);

                TrayIconContextMenu.ResumeLayout(false);
                TrayIcon.ContextMenuStrip = TrayIconContextMenu;
            }

            private void OnApplicationExit(object sender, EventArgs e)
            {
                TrayIcon.Visible = false;
                TrayIcon.Icon = null;
                TrayIcon = null;
                TrayIcon.Dispose();
            }

            private void TrayIcon_DoubleClick(object sender, EventArgs e)
            {
                //TrayIcon.ShowBalloonTip(30000);
            }

            private void MirroredMenuItem_Click(object sender, EventArgs e)
            {
                KinectCamSettings.Default.Mirrored = !KinectCamSettings.Default.Mirrored;
            }

            private void DesktopMenuItem_Click(object sender, EventArgs e)
            {
                KinectCamSettings.Default.Desktop = !KinectCamSettings.Default.Desktop;
            }

            public void Exit()
            {
                TrayIcon.Visible = false;
                TrayIcon.Icon = null;
                TrayIcon = null;
                TrayIcon.Dispose();
            }
        }

        static KinectCamApplicationContext context;
        static Thread contexThread;
        static Thread refreshThread;
        static KinectSensor Sensor;

        static void InitializeSensor()
        {
            var sensor = Sensor;
            if (sensor != null) return;

            try
            {
                sensor = KinectSensor.GetDefault();                
                if (sensor == null) return;                

                //var reader = sensor.BodyIndexFrameSource.OpenReader();

                var reader = sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.BodyIndex);
                
                //reader.FrameArrived += reader_BodyIndexFrameArrived;

                reader.MultiSourceFrameArrived += reader_MultiSourceFrameArrived;

                sensor.Open();

                Sensor = sensor;

                if (context == null)
                {
                    contexThread = new Thread(() =>
                    {
                        context = new KinectCamApplicationContext();
                        Application.Run(context);
                    });
                    refreshThread = new Thread(() =>
                    {
                        while (true)
                        {
                            Thread.Sleep(250);
                            Application.DoEvents();
                        }
                    });
                    contexThread.IsBackground = true;
                    refreshThread.IsBackground = true;
                    contexThread.SetApartmentState(ApartmentState.STA);
                    refreshThread.SetApartmentState(ApartmentState.STA);
                    contexThread.Start();
                    refreshThread.Start();
                }
            }
            catch
            {
                Trace.WriteLine("Error of enable the Kinect sensor!");
            }
        }

        public delegate void InvokeDelegate();

        static byte[] _displayPixels = new byte[512 * 424 * 4];
        static BackgroundRemovalTool _bgRemove = new BackgroundRemovalTool();

        static void reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {        
            var reference = e.FrameReference.AcquireFrame();

            using (var bodyIndexFrame = reference.BodyIndexFrameReference.AcquireFrame())
            {
                //using (var depthFrame = reference.DepthFrameReference.AcquireFrame())
                //{
                //    using (var colorFrame = reference.ColorFrameReference.AcquireFrame())
                //    {
                //        if (bodyIndexFrame != null && depthFrame != null && colorFrame != null)
                //        {
                //            MultiFrameReady(colorFrame, depthFrame, bodyIndexFrame);
                //        }
                //    }
                //}
                if (bodyIndexFrame != null)
                {
                    BodyIndexFrameReady(bodyIndexFrame);
                }
            }
        }

        static unsafe void MultiFrameReady(ColorFrame colorFrame, DepthFrame depthFrame, BodyIndexFrame biFrame)
        {
            _displayPixels = _bgRemove.GreenScreen(colorFrame, depthFrame, biFrame);
        }

        static void reader_ColorFrameArrived(object sender, ColorFrameArrivedEventArgs e)
        {
            using (var colorFrame = e.FrameReference.AcquireFrame())
            {
                if (colorFrame != null)
                {
                    ColorFrameReady(colorFrame);
                }
            }            
        }

        static void reader_BodyIndexFrameArrived(object sender, BodyIndexFrameArrivedEventArgs e)
        {            
            using (var bodyIndexFrame = e.FrameReference.AcquireFrame())
            {
                if (bodyIndexFrame != null)
                {
                    BodyIndexFrameReady(bodyIndexFrame);
                }
            }
        }

        static unsafe void ColorFrameReady(ColorFrame frame)
        {
            if (frame.RawColorImageFormat == ColorImageFormat.Bgra)
            {
                frame.CopyRawFrameDataToArray(sensorColorFrameData);
            }
            else
            {
                frame.CopyConvertedFrameDataToArray(sensorColorFrameData, ColorImageFormat.Bgra);
            }
        }

        static unsafe void BodyIndexFrameReady(BodyIndexFrame frame)
        {            
            frame.CopyFrameDataToArray(sensorBodyIndexFrameData);
        }

        public static void DisposeSensor()
        {
            try
            {
                var sensor = Sensor;
                if (sensor != null && sensor.IsOpen)
                {
                    sensor.Close();
                    sensor = null;
                    Sensor = null;
                }

                if (context != null)
                {
                    context.Exit();
                    context.Dispose();
                    context = null;

                    contexThread.Abort();
                    refreshThread.Abort();
                }
            }
            catch
            {
                Trace.WriteLine("Error of disable the Kinect sensor!");
            }
        }

        public static int Width = 1920;
        public static int Height = 1080;       
        static readonly byte[] sensorColorFrameData = new byte[1920 * 1080 * 4];

        public static int BodyIndexWidth = 512;
        public static int BodyIndexHeight = 424;
        static readonly byte[] sensorBodyIndexFrameData = new byte[512*424];        

        public static int GetBIWidthAndHeight()
        {
            FrameDescription bifd = Sensor.BodyIndexFrameSource.FrameDescription;
            int res = bifd.Width * bifd.Height;
            return res;
        }

        public unsafe static void GenerateBodyIndexFrame(IntPtr _ptr, int length, bool mirrored)
        {
            byte[] bodyIndexFrame = sensorBodyIndexFrameData;
            //byte[] bodyIndexFrame = _displayPixels;
            void* bodyIndexData = _ptr.ToPointer();

            try
            {
                InitializeSensor();
                
                if (bodyIndexFrame != null)
                {
                    if (mirrored)
                    {
                        fixed (byte* sDataB = &bodyIndexFrame[0])
                        fixed (byte* sDataE = &bodyIndexFrame[bodyIndexFrame.Length - 1])
                        {
                            byte* pData = (byte*)bodyIndexData;
                            byte* sData = (byte*)sDataE;

                            for (; sData > sDataB; )
                            {
                                for (var i = 0; i < BodyIndexWidth; ++i)
                                {
                                    //var p = sData-3;
                                    //*pData++ = *p++;
                                    //*pData++ = *p++;
                                    //*pData++ = *p++;
                                    //p = (sData -= 4);
                                    var p = sData;
                                    *pData++ = *p++;
                                    *pData++ = *p++;
                                    *pData++ = *p++;
                                    p = (sData -= 1);
                                }
                            }
                        }
                    }
                    else
                    {
                        fixed (byte* sDataB = &bodyIndexFrame[0])
                        fixed (byte* sDataE = &bodyIndexFrame[bodyIndexFrame.Length - 1])
                        {
                            byte* pData = (byte*)bodyIndexData;
                            byte* sData = (byte*)sDataE;

                            var sDataBE = sData;
                            var p = sData;
                            var r = sData;

                            while (sData == (sDataBE = sData) &&
                                   sDataB <= (sData -= (BodyIndexWidth - 1)))
                            {
                                r = sData;
                                do
                                {
                                    p = sData;
                                    *pData++ = *p++;
                                    *pData++ = *p++;
                                    *pData++ = *p++;
                                }
                                while ((sData += 1) <= sDataBE);
                                sData = r - 1;
                            }
                        }
                    }
                }
            }

            catch
            {
                byte* pData = (byte*)bodyIndexData;
                for (int i = 0; i < length; ++i)
                    *pData++ = 0;
            }
        }

        public unsafe static void GenerateFrame(IntPtr _ptr, int length, bool mirrored)
        {            
            byte[] colorFrame = sensorColorFrameData;
            void* camData = _ptr.ToPointer();

            try
            {
                InitializeSensor();

                if (colorFrame != null)
                {
                    if (!mirrored)
                    {
                        fixed (byte* sDataB = &colorFrame[0])
                        fixed (byte* sDataE = &colorFrame[colorFrame.Length - 1])
                        {
                            byte* pData = (byte*)camData;
                            byte* sData = (byte*)sDataE;

                            for (;sData > sDataB;)
                            {
                                for (var i = 0; i < Width; ++i)
                                {
                                    var p = sData - 3;
                                    *pData++ = *p++;
                                    *pData++ = *p++;
                                    *pData++ = *p++;
                                    p = (sData -= 4);
                                }
                            }
                        }
                    }
                    else
                    {
                        fixed (byte* sDataB = &colorFrame[0])
                        fixed (byte* sDataE = &colorFrame[colorFrame.Length - 1])
                        {
                            byte* pData = (byte*)camData;
                            byte* sData = (byte*)sDataE;

                            var sDataBE = sData;
                            var p = sData;
                            var r = sData;

                            while (sData == (sDataBE = sData) &&
                                   sDataB <= (sData -= (Width * 4 - 1)))
                            {
                                r = sData;
                                do
                                {
                                    p = sData;
                                    *pData++ = *p++;
                                    *pData++ = *p++;
                                    *pData++ = *p++;
                                }
                                while ((sData += 4) <= sDataBE);
                                sData = r - 1;
                            }
                        }
                    }
                }
            }
            catch
            {
                byte* pData = (byte*)camData;
                for (int i = 0; i < length; ++i)
                    *pData++ = 0;
            }
        }
    }
}
