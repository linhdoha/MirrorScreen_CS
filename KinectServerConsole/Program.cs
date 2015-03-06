﻿using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using Microsoft.Kinect;
using Newtonsoft.Json;
using System.IO;

namespace KinectServerConsole
{
    // 2 classes to deserialize JSON commands from Flash client
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

    public class Program
    {
        static bool isSendColor = false;
        static bool isSendBodyIndex = false;
        static bool isSendBodyData = false;

        static KinectSensor _sensor;
        static MultiSourceFrameReader _reader;
        static IList<Body> _bodies;

        static Skeleton[] _skeletonList;
        static SkeletonsDataCollection _skeletonCollection;

        private static object serverLock = new object();

        private static int port = 7001;
        private static Socket serverSocket;
        private static Socket handlerSocket;

        private class StateObject
        {
            public Socket socket;
            public byte[] buffer;
            public StringBuilder stringBuilder = new StringBuilder();
        }

        private static List<StateObject> connections = new List<StateObject>();


        private static void SetupServerSocket()
        {
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), port);

            // Create the socket, bind it, and start listening
            serverSocket = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);
            serverSocket.Blocking = false;

            serverSocket.Bind(localEndPoint);
            serverSocket.Listen(1);
        }

        public static void Start()
        {
            Console.Write("Starting server... ");
            try
            {
                SetupServerSocket();
                for (int i = 0; i < 10; i++)
                {
                    ContinueListen(serverSocket);
                }
            }
            catch (Exception e)
            {
                Console.WriteLine("Fail.");
                Console.WriteLine(e);
            }
            Console.WriteLine("Done. Listening for flash at port " + port);
        }

        private static void ContinueListen(Socket socket)
        {
            socket.BeginAccept(new AsyncCallback(AcceptCallback), socket);
        }

        private static void AcceptCallback(IAsyncResult result)
        {
            allDone.Set();

            Console.WriteLine("Accept");
            StateObject connection = new StateObject();
            try
            {
                // Finish Accept
                Socket s = (Socket)result.AsyncState;
                connection.socket = s.EndAccept(result);
                connection.socket.Blocking = false;
                connection.buffer = new byte[1024];
                lock (connections) connections.Add(connection);

                Console.WriteLine("New connection from " + s);

                // Start Receive
                connection.socket.BeginReceive(connection.buffer, 0, connection.buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), connection);
                // Start new Accept
                serverSocket.BeginAccept(new AsyncCallback(AcceptCallback), result.AsyncState);
            }
            catch (SocketException exc)
            {
                CloseConnection(connection);
                Console.WriteLine("Socket exception: " + exc.SocketErrorCode);
                Console.WriteLine(exc);
            }
            catch (Exception exc)
            {
                CloseConnection(connection);
                Console.WriteLine("Exception: " + exc);
            }
        }

        private static void ReceiveCallback(IAsyncResult result)
        {
            isSendColor = false;
            isSendBodyIndex = false;
            isSendBodyData = false;

            DataReceive listCommand;
            KinectDataRequest request;

            StateObject connection = (StateObject)result.AsyncState;
            try
            {
                //connection.socket.BeginReceive(connection.buffer, 0, connection.buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), connection);
                int bytesRead = connection.socket.EndReceive(result);
                if (0 != bytesRead)
                {
                    lock (connections)
                    {
                        connection.stringBuilder.Clear();
                        connection.stringBuilder.Append(Encoding.UTF8.GetString(connection.buffer, 0, bytesRead));
                        string jsonCommand = connection.stringBuilder.ToString();

                        request = JsonConvert.DeserializeObject<KinectDataRequest>(jsonCommand);
                        listCommand = request.dataReceive;

                        isSendColor = listCommand.colorImage;
                        isSendBodyIndex = listCommand.bodyIndexImage;
                        isSendBodyData = listCommand.bodyData;
                    }
                    Console.WriteLine("======> request received");
                    Console.WriteLine("isSendBodyData: " + isSendBodyData);
                    connection.stringBuilder.Clear();
                    //lock (connections)
                    //{
                    //    foreach (StateObject conn in connections)
                    //    {
                    //        if (connection != conn)
                    //        {
                    //            conn.socket.Send(connection.buffer, bytesRead,
                    //                SocketFlags.None);
                    //        }
                    //    }
                    //}
                    connection.socket.BeginReceive(connection.buffer, 0,
                        connection.buffer.Length, SocketFlags.None,
                        new AsyncCallback(ReceiveCallback), connection);
                }
                else CloseConnection(connection);
            }
            catch (SocketException)
            {
                CloseConnection(connection);
            }
            catch (Exception)
            {
                CloseConnection(connection);
            }
        }

        private static void CloseConnection(StateObject ci)
        {
            ci.socket.Close();
            lock (connections) connections.Remove(ci);
        }

        public static void StartKinect()
        {
            _sensor = KinectSensor.GetDefault();

            if (_sensor != null)
            {
                _sensor.Open();

                _reader = _sensor.OpenMultiSourceFrameReader(FrameSourceTypes.Color | FrameSourceTypes.Depth | FrameSourceTypes.Infrared | FrameSourceTypes.Body | FrameSourceTypes.BodyIndex);
                _reader.MultiSourceFrameArrived += Reader_MultiSourceFrameArrived;
            }
        }
        static void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            //outputEvent = e;            
            var reference = e.FrameReference.AcquireFrame();

            // Color
            //using (var frame = reference.ColorFrameReference.AcquireFrame())
            //{
            //    if (frame != null)
            //    {
            //        if (_mode == Mode.Color)
            //        {
            //            camera.Source = frame.ToBitmap();
            //        }

            //        if (sock.Connected)
            //        {
            //            if (isSendColor)
            //            {
            //                SendColor(frame);
            //            }
            //        }
            //    }
            //}

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
            using (var frame = reference.BodyFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    //canvas.Children.Clear();

                    int bodyCount = frame.BodyFrameSource.BodyCount;
                    _skeletonCollection = new SkeletonsDataCollection();
                    _skeletonList = new Skeleton[bodyCount];

                    _bodies = new Body[frame.BodyFrameSource.BodyCount];

                    frame.GetAndRefreshBodyData(_bodies);

                    //List<Body> _bodyList = _bodies.ToList<Body>();

                    //String _bodiesJSON = _bodyList.Serialize(_sensor.CoordinateMapper, Mode.Color);

                    //Console.WriteLine(_bodiesJSON);

                    for (int i = 0; i < _bodies.Count; i++)
                    {
                        if (_bodies[i] != null)
                        {
                            if (_bodies[i].IsTracked)
                            {
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

                        byte[] bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(_skeletonCollection));
                        //byte[] bytes = Encoding.UTF8.GetBytes(_bodiesJSON);
                        byte[] nullBytes = new byte[] { 0, 0, 0, 0 };
                        byte[] bodyDataDirectiveBytes;

                        bodyDataDirectiveBytes = Encoding.UTF8.GetBytes("+BDD");
                        //bytes = ms.ToArray();                        

                        byte[] sentData = new byte[bodyDataDirectiveBytes.Length + bytes.Length + nullBytes.Length];
                        //bytesSize.CopyTo(sentData, 0);
                        bodyDataDirectiveBytes.CopyTo(sentData, 0);
                        bytes.CopyTo(sentData, 4);
                        nullBytes.CopyTo(sentData, sentData.Length - 4);

                        //File.WriteAllBytes("body_byte.txt", sentData);

                        //byte[] sentData = new byte[bodyDataDirectiveBytes.Length + bytes.Length + nullBytes.Length];
                        //List<byte> sentData = new List<byte>();
                        //sentData.AddRange(bodyDataDirectiveBytes);
                        //sentData.AddRange(bytes);
                        //sentData.AddRange(nullBytes);

                        //Console.WriteLine(bytes.Length);
                        //Console.WriteLine(sentData.ToArray().Length);

                        handlerSocket = connections[0].socket;

                        if (handlerSocket.Connected)
                        {
                            try
                            {
                                lock (serverLock)
                                {
                                    //Console.WriteLine("Begin send body data");
                                    handlerSocket.BeginSend(sentData, 0, sentData.Length, 0, new AsyncCallback(SendBodyDataCallback), handlerSocket);
                                }
                            }

                            catch (SocketException se)
                            {
                                Console.WriteLine("Socket exception: " + se.SocketErrorCode);
                                Console.WriteLine(se);
                            }
                            finally
                            {
                                isSendBodyData = false;
                            }
                        }
                        else
                        {
                            Console.WriteLine("not connected");
                        }
                    }
                }
            }
        }

        private static void SendBodyDataCallback(IAsyncResult ar)
        {
            //Console.WriteLine("Send Body data completed");
            handlerSocket.EndSend(ar);
        }

        public static ManualResetEvent allDone = new ManualResetEvent(false);

        static void Main(string[] args)
        {
            StartKinect();
            Start();
            string line = "";
            while (line != "exit")
            {
                allDone.Reset();
                ContinueListen(serverSocket);
                allDone.WaitOne();
                //line = Console.ReadLine();
            }
            Console.Write("Shutting down server... ");
            lock (connections)
            {
                for (int i = connections.Count - 1; i >= 0; i--)
                {
                    CloseConnection(connections[i]);
                }
            }

            if (_reader != null)
            {
                _reader.Dispose();
            }

            if (_sensor != null)
            {
                _sensor.Close();
            }

            Thread.Sleep(500);
        }

        public enum Mode
        {
            Color,
            Depth,
            Infrared
        }
    }
}
