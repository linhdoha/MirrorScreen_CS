using Microsoft.Kinect;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Net;
using System.Net.Sockets;
using System.IO;
using System.Threading;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.Serialization.Json;
using Newtonsoft.Json;

namespace KinectStreams
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {

        public class DataReceive
        {
            public bool colorImage { get; set; }
            public bool bodyIndexImage { get; set; }
            public bool bodyData { get; set; }
        }

        public class KinectDataRequest
        {
            public string command { get; set; }
            public DataReceive dataReceive { get; set; }
        }

        public class StateObject
        {
            public Socket socket;
            public byte[] buffer = new byte[1024];
            public StringBuilder sb = new StringBuilder();
        }

        bool isSendColor = false;
        bool isSendBodyIndex = false;
        bool isSendBodyData = false;

        #region Members

        Socket sock;

        Mode _mode = Mode.Color;

        KinectSensor _sensor;
        MultiSourceFrameReader _reader;
        IList<Body> _bodies;
        //
        BodyIndexFrameReader _bodyIndexReader;

        bool _displayBody = false;

        Stopwatch sw2;

        Skeleton[] _skeletonList;
        SkeletonsDataCollection _skeletonCollection;
        
        #endregion

        #region Constructor

        public MainWindow()
        {
            InitializeComponent();
        }

        #endregion

        #region Event handlers

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            sw2 = new Stopwatch();
            _sensor = KinectSensor.GetDefault();

            if (_sensor != null)
            {
                _sensor.Open();

                _reader = _sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.Depth | FrameSourceTypes.Infrared | FrameSourceTypes.Body | FrameSourceTypes.BodyIndex);
                _bodyIndexReader = _sensor.BodyIndexFrameSource.OpenReader();
                _reader.MultiSourceFrameArrived += Reader_MultiSourceFrameArrived;
            }

            try
            {
                StateObject so = new StateObject();
                IPEndPoint ipEnd = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 7001);
                so.socket = new Socket(ipEnd.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                //so.socket = new Socket(ipEnd.AddressFamily, SocketType.Dgram, ProtocolType.Udp);

                so.socket.Bind(ipEnd);
                so.socket.Listen(1000);

                so.socket.BeginAccept(new AsyncCallback(AcceptCallback), so);
                sock = so.socket;
                sock.SendBufferSize = 524288;
            }
            catch (SocketException se)
            {
                Console.WriteLine(se.Message);
                sock.Close();

                IPEndPoint ipEnd = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 7001);
                sock.Bind(ipEnd);
                sock.Listen(1000);
            }
            //sock.SendBufferSize = 10000;
        }

        private void AcceptCallback(IAsyncResult ar)
        {
            try
            {
                StateObject so = (StateObject)ar.AsyncState;

                so.socket = so.socket.EndAccept(ar);
                sock = so.socket;
                so.socket.BeginReceive(so.buffer, 0, so.buffer.Length, 0, new AsyncCallback(ReceivedOrder), so);
                Console.WriteLine(so.socket.RemoteEndPoint.AddressFamily + " connected");
            }
            catch (ObjectDisposedException ode)
            {
                Console.WriteLine(ode.ToString());
            }
        }

        private void ReceivedOrder(IAsyncResult ar)
        {
            isSendColor = false;
            StateObject so = (StateObject)ar.AsyncState;

            try
            {
                int read = so.socket.EndReceive(ar);
                so.socket.BeginReceive(so.buffer, 0, so.buffer.Length, 0, new AsyncCallback(ReceivedOrder), so);                
                if (read == 0)
                {
                    so.socket.Shutdown(SocketShutdown.Both);                    
                }
                else
                {                   
                    Console.WriteLine(so.socket.RemoteEndPoint.AddressFamily + " sending data " + read);

                    so.sb.Clear();
                    so.sb.Append(Encoding.UTF8.GetString(so.buffer, 0, read));
                    string jsonCommand = so.sb.ToString();
                    Console.WriteLine(jsonCommand);
                    
                    //String fakeJSON = "{\"command\":\"requestData\",\"dataReceive\":{\"colorImage\":true,\"bodyIndexImage\":false,\"bodyData\":true}}";

                    KinectDataRequest request = JsonConvert.DeserializeObject<KinectDataRequest>(jsonCommand);
                    DataReceive listCommand = request.dataReceive;

                    StringBuilder newsb = so.sb;

                    lock (this)
                    {
                        isSendColor = listCommand.colorImage;
                        isSendBodyIndex = listCommand.bodyIndexImage;
                        isSendBodyData = listCommand.bodyData;
                    }

                    Console.WriteLine("isSendColor:" + isSendColor + ", isSendBodyIndex:" + isSendBodyIndex + ", isSendBodyData:" + isSendBodyData);

                    so.sb.Clear();
                }
            }
            catch (SocketException se)
            {
                Console.WriteLine(se.ToString());
                IPEndPoint ipEnd = new IPEndPoint(IPAddress.Parse("127.0.0.1"), 7001);
                
                so.socket = new Socket(ipEnd.AddressFamily, SocketType.Stream, ProtocolType.Tcp);
                so.socket.Bind(ipEnd);
                so.socket.Listen(1000);

                so.socket.BeginAccept(new AsyncCallback(AcceptCallback), so);
                sock = so.socket;
            }
            catch (ObjectDisposedException ode)
            {
                Console.WriteLine(ode.StackTrace);
            }
        }

        private void Window_Closed(object sender, EventArgs e)
        {
            if (_reader != null)
            {
                _reader.Dispose();
            }

            if (_sensor != null)
            {
                _sensor.Close();
            }

            if (sock != null)
            {
                sock.Close();
            }
        }

        void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            //outputEvent = e;            
            var reference = e.FrameReference.AcquireFrame();                        

            // Color
            using (var frame = reference.ColorFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    if (_mode == Mode.Color)
                    {
                        camera.Source = frame.ToBitmap();
                    }

                    if (sock.Connected)
                    {
                        SendColor(frame);
                    }
                }
            }

            // Depth
            //using (var frame = reference.DepthFrameReference.AcquireFrame())
            //{
            //    if (frame != null)
            //    {
            //        if (_mode == Mode.Depth)
            //        {
            //            camera.Source = frame.ToBitmap();
            //        }
            //    }
            //}

            // Infrared
            //using (var frame = reference.InfraredFrameReference.AcquireFrame())
            //{
            //    if (frame != null)
            //    {
            //        if (_mode == Mode.Infrared)
            //        {
            //            camera.Source = frame.ToBitmap();
            //        }
            //    }
            //}

            // Body
            DataContractJsonSerializer js = new DataContractJsonSerializer(typeof(Skeleton));            

            using (var frame = reference.BodyFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    canvas.Children.Clear();

                    int bodyCount = frame.BodyFrameSource.BodyCount;
                    _skeletonCollection = new SkeletonsDataCollection();
                    _skeletonList = new Skeleton[bodyCount];

                    _bodies = new Body[frame.BodyFrameSource.BodyCount];

                    frame.GetAndRefreshBodyData(_bodies);

                    //JSONBodySerializer.Serialize(_bodies, _sensor.CoordinateMapper, Mode.Color);

                    for (int i = 0; i< _bodies.Count; i++)
                    {
                        if (_bodies[i] != null)
                        {
                            if (_bodies[i].IsTracked)
                            {
                                //Draw skeleton.
                                if (_displayBody)
                                {
                                    canvas.DrawSkeleton(_bodies[i]);
                                }

                               _skeletonList[i] = new Skeleton();
                               _skeletonList[i].HandLeftState = _bodies[i].HandLeftState;
                               _skeletonList[i].HandRightState = _bodies[i].HandRightState;
                               
                               _skeletonList[i].LeftHandPos = new CameraSpacePoint
                               {
                                   X = (float)_bodies[i].Joints[JointType.HandLeft].Position.ToPoint(_sensor.CoordinateMapper).X,
                                   Y = (float)_bodies[i].Joints[JointType.HandLeft].Position.ToPoint(_sensor.CoordinateMapper).Y,
                                   Z = _bodies[i].Joints[JointType.HandLeft].Position.Z
                               };
                               _skeletonList[i].RightHandPos = new CameraSpacePoint
                               {
                                   X = (float)_bodies[i].Joints[JointType.HandRight].Position.ToPoint(_sensor.CoordinateMapper).X,
                                   Y = (float)_bodies[i].Joints[JointType.HandRight].Position.ToPoint(_sensor.CoordinateMapper).Y,
                                   Z = _bodies[i].Joints[JointType.HandLeft].Position.Z
                               };                               
                               _skeletonList[i].TrackingId = _bodies[i].TrackingId;
                            }
                        }                            
                    }
                    

                    if (isSendBodyData)
                    {
                        _skeletonCollection.BodyCount = bodyCount;
                        _skeletonCollection.SkeletonList = _skeletonList;

                        //MemoryStream ms = new MemoryStream();
                        //js.WriteObject(ms, _skeletonCollection);
                        //ms.Position = 0;

                        //sr = new StreamReader(ms);
                        byte[] bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(_skeletonCollection));
                        byte[] nullBytes = new byte[] { 0, 0, 0, 0 };
                        byte[] bodyDataDirectiveBytes;

                        bodyDataDirectiveBytes = Encoding.UTF8.GetBytes("+BDD");
                        //bytes = ms.ToArray();                        

                        //byte[] sentData = new byte[bodyDataDirectiveBytes.Length + bytes.Length + nullBytes.Length];
                        List<byte> sentData = new List<byte>();
                        sentData.AddRange(bodyDataDirectiveBytes);
                        sentData.AddRange(bytes);
                        sentData.AddRange(nullBytes);

                        Console.WriteLine(bytes.Length);
                        Console.WriteLine(sentData.ToArray().Length);
                        
                        try
                        {                                                     
                            sock.BeginSend(sentData.ToArray(), 0, sentData.Count, 0, new AsyncCallback(SendBodyDataCallback), this);
                        }

                        catch (SocketException se)
                        {
                            Console.WriteLine(se.ToString());
                        }
                        finally
                        {
                            isSendBodyData = false;
                        }
                    }
                }                    
            }
        }

        private void SendColor(ColorFrame frame) {
            if (isSendColor)
            {
                using (MemoryStream ms = new MemoryStream())
                {
                    Stopwatch sw = new Stopwatch();
                    sw.Start();
                    //convert color frame to bitmap
                    Bitmap bitmap = BitmapFromSource((BitmapSource)frame.ToBitmap(1280, 720));
                    Bitmap b = new Bitmap(bitmap);
                    bitmap.Dispose();

                    System.Drawing.Imaging.Encoder encoder = System.Drawing.Imaging.Encoder.Quality;
                    ImageCodecInfo imgCodec = GetEncoder(ImageFormat.Jpeg);
                    EncoderParameters encoderParams = new EncoderParameters(1);
                    EncoderParameter encoderParam = new EncoderParameter(encoder, 50L); //image quality
                    encoderParams.Param[0] = encoderParam;

                    b.Save(ms, imgCodec, encoderParams);
                    sw.Stop();

                    Console.WriteLine("encode time: " + sw.ElapsedMilliseconds + " ms");

                    sw2.Start();

                    //prepare for sending color frame
                    //byte[] bytesSize;
                    byte[] bytes;
                    byte[] nullBytes = new byte[4];
                    byte[] colorDirectiveBytes;

                    bytes = ms.ToArray();
                    //bytesSize = BitConverter.GetBytes(bytes.Length);
                    colorDirectiveBytes = Encoding.UTF8.GetBytes("+CLR");

                    byte[] sentData = new byte[colorDirectiveBytes.Length + bytes.Length + nullBytes.Length];
                    //bytesSize.CopyTo(sentData, 0);
                    colorDirectiveBytes.CopyTo(sentData, 0);
                    bytes.CopyTo(sentData, 4);
                    nullBytes.CopyTo(sentData, sentData.Length - 4);

                    Console.WriteLine(bytes.Length);
                    Console.WriteLine(sentData.Length);

                    lock (this)
                    {
                        isSendColor = false;
                        sock.BeginSend(sentData, 0, sentData.Length, 0, new AsyncCallback(SendImgCallback), this);
                    }

                }
            }
        }

        public static string EscapeStringValue(string value)
        {
            const char BACK_SLASH = '\\';
            const char SLASH = '/';
            const char DBL_QUOTE = '"';

            var output = new StringBuilder(value.Length);
            foreach (char c in value)
            {
                switch (c)
                {
                    case SLASH:
                        output.AppendFormat("{0}{1}", BACK_SLASH, SLASH);
                        break;

                    case BACK_SLASH:
                        output.AppendFormat("{0}{0}", BACK_SLASH);
                        break;

                    case DBL_QUOTE:
                        output.AppendFormat("{0}{1}",BACK_SLASH,DBL_QUOTE);
                        break;

                    default:
                        output.Append(c);
                        break;
                }
            }

            return output.ToString();
        }

        private System.Drawing.Bitmap BitmapFromSource(BitmapSource bitmapsource)
        {
            System.Drawing.Bitmap bitmap;
            using (MemoryStream outStream = new MemoryStream())
            {
                BitmapEncoder enc = new BmpBitmapEncoder();
                enc.Frames.Add(BitmapFrame.Create(bitmapsource));
                enc.Save(outStream);
                bitmap = new System.Drawing.Bitmap(outStream);
            }
            return bitmap;
        }

        private void SendImgCallback(IAsyncResult ar)
        {
            Console.WriteLine("Send Color completed");

            sw2.Stop();
            Console.WriteLine("send time: " + sw2.ElapsedMilliseconds + " ms");
            sw2.Reset();
        }

        private void SendBodyDataCallback(IAsyncResult ar)
        {
            Console.WriteLine("Send Body data completed");
        }

        private void Color_Click(object sender, RoutedEventArgs e)
        {
            _mode = Mode.Color;
        }

        private void Depth_Click(object sender, RoutedEventArgs e)
        {
            _mode = Mode.Depth;
        }

        private void Infrared_Click(object sender, RoutedEventArgs e)
        {
            _mode = Mode.Infrared;
        }

        private void Body_Click(object sender, RoutedEventArgs e)
        {
            _displayBody = !_displayBody;
        }

        #endregion

        public byte[] getJPGFromImageControl(BitmapSource imageC)
        {
            MemoryStream memStream = new MemoryStream();
            JpegBitmapEncoder encoder = new JpegBitmapEncoder();
            encoder.Frames.Add(BitmapFrame.Create(imageC));
            encoder.Save(memStream);
            return memStream.GetBuffer();
        }

        private ImageCodecInfo GetEncoder(ImageFormat format)
        {

            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();

            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }
    }

    public enum Mode
    {
        Color,
        Depth,
        Infrared
    }
}
