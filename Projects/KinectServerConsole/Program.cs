﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net.Sockets;
using System.Net;
using System.Threading;
using Microsoft.Kinect;
using Newtonsoft.Json;
using System.IO;
using System.Windows.Forms;
using System.Drawing;
using System.Runtime.InteropServices;
using Microsoft.CSharp.RuntimeBinder;

namespace KinectServerConsole
{
    //// 2 classes to deserialize JSON commands from Flash client
    //public class CommandList
    //{
    //    public bool colorImage { get; set; }
    //    public bool bodyIndexImage { get; set; }
    //    public bool bodyData { get; set; }
    //}

    //public class KinectDataRequest
    //{
    //    public string command { get; set; }
    //    public CommandList dataReceive { get; set; }
    //    public string[] gestureDatabase { get; set; }
    //}

    public class Program
    {
        #region Constants

        const int DEFAULT_PORT = 7001;
        const string DEBUG_SYMBOL = "[DEBUG]";
        const string ERROR_SYMBOL = "[ERROR]";

        #endregion

        //#region Variable

        //static bool hasDetector;
        //static bool isReceiving = false;

        //static bool isSendColor = false;
        //static bool isSendBodyIndex = false;
        //static bool isSendBodyData = false;
        //static string[] gestureDatabase = {};

        //static KinectSensor _sensor;
        //static MultiSourceFrameReader _reader;
        //static IList<Body> _bodies;

        //static Skeleton[] _skeletonList;
        //static SkeletonsDataCollection _skeletonCollection;

        //private static object serverLock = new object();

        private static int serverPort;
        private static bool debugFlag = false;
        
        //private static Socket serverSocket;
        //private static Socket handlerSocket;

        //private static List<GestureDetector> gestureDetectorList = null;
        //private static List<StateObject> connections = new List<StateObject>();

        //#endregion
        /*
        private class StateObject
        {
            public Socket socket;
            public byte[] buffer;
            public StringBuilder stringBuilder = new StringBuilder();
        }

        private static void SetupServerSocket(int inputPort)
        {
            IPEndPoint localEndPoint = new IPEndPoint(IPAddress.Parse("127.0.0.1"), inputPort);

            // Create the socket, bind it, and start listening
            serverSocket = new Socket(AddressFamily.InterNetwork,
                SocketType.Stream, ProtocolType.Tcp);
            serverSocket.Blocking = false;

            try
            {
                serverSocket.Bind(localEndPoint);
                serverSocket.Listen(10);
            }
            catch (SocketException exc)
            {
                Console.WriteLine(ERROR_SYMBOL + "Socket exception: " + exc.SocketErrorCode);
                Console.WriteLine(exc);
            }
            catch (Exception exc)
            {
                Console.WriteLine(ERROR_SYMBOL + "Exception: " + exc);
            }

        }

        public static void StartServer(int inputPort)
        {
            hasDetector = false;

            Console.WriteLine("Starting server... ");            
            try
            {
                SetupServerSocket(inputPort);
                ContinueListen(serverSocket);
            }
            catch (Exception e)
            {
                Console.WriteLine(ERROR_SYMBOL + "Fail. Can not listen at " + port);
                Console.WriteLine(e);
            }
            Console.WriteLine("Done. Listening for flash at port " + port);

            if (debugFlag)
            {
                Console.WriteLine("Debug enabled.");
            }
        }

        private static void ContinueListen(Socket socket)
        {
            try
            {
                socket.BeginAccept(new AsyncCallback(AcceptCallback), socket);
            }
            catch (ObjectDisposedException ode)
            {
                Console.WriteLine(ode.StackTrace);
                Console.ReadLine();
            }
        }

        private static void AcceptCallback(IAsyncResult result)
        {                        
            StateObject connection = new StateObject();
            try
            {
                // Finish Accept
                Socket s = (Socket)result.AsyncState;
                connection.socket = s.EndAccept(result);
                connection.socket.Blocking = false;
                connection.buffer = new byte[1024];
                lock (connections) connections.Add(connection);

                WriteDebug("New connection from " + s);
                //Console.WriteLine(DEBUG_SYMBOL + "New connection from " + s);

                // Start Receive
                connection.socket.BeginReceive(connection.buffer, 0, connection.buffer.Length, SocketFlags.None, 
                    new AsyncCallback(ReceiveCallback), connection);
                allDone.Set();
                // Start new Accept
                serverSocket.BeginAccept(new AsyncCallback(AcceptCallback), result.AsyncState);
            }
            catch (SocketException exc)
            {
                CloseConnection(connection);
                Console.WriteLine(ERROR_SYMBOL + "Socket exception: " + exc.SocketErrorCode);
            }
            catch (Exception exc)
            {
                CloseConnection(connection);
                Console.WriteLine(ERROR_SYMBOL + "Exception: " + exc);
            }
        }

        private static void ReceiveCallback(IAsyncResult result)
        {
            isReceiving = true;
            //isSendColor = false;
            //isSendBodyIndex = false;
            isSendBodyData = false;

            CommandList commandList;
            KinectDataRequest request;

            string[] oldGestureDatabase = gestureDatabase;

            StateObject connection = (StateObject)result.AsyncState;

            try
            {
                //connection.socket.BeginReceive(connection.buffer, 0, connection.buffer.Length, SocketFlags.None, new AsyncCallback(ReceiveCallback), connection);
                int bytesRead = connection.socket.EndReceive(result);
                WriteDebug("Byte read = " + bytesRead);
                //Console.WriteLine(DEBUG_SYMBOL + "Byte read = " + bytesRead);

                if (0 != bytesRead)
                {
                    lock (connections)
                    {
                        connection.stringBuilder.Clear();
                        connection.stringBuilder.Append(Encoding.UTF8.GetString(connection.buffer, 0, bytesRead));
                        string jsonCommand = connection.stringBuilder.ToString();

                        request = JsonConvert.DeserializeObject<KinectDataRequest>(jsonCommand);

                        commandList = request.dataReceive;

                        gestureDatabase = request.gestureDatabase;

                        isSendColor = commandList.colorImage;
                        isSendBodyIndex = commandList.bodyIndexImage;
                        isSendBodyData = commandList.bodyData;
                        
                        WriteDebug("Request received [isSendBodyData: " + isSendBodyData + "]");
                        WriteDebug("Request received [gestureDatabase: " + ConvertStringArrayToStringJoin(gestureDatabase) + "]");
                        //Console.WriteLine(DEBUG_SYMBOL + "Request received [isSendBodyData: " + isSendBodyData+"]");               

                        connection.stringBuilder.Clear();

                        connection.socket.BeginReceive(connection.buffer, 0, connection.buffer.Length, SocketFlags.None,
                            new AsyncCallback(ReceiveCallback), connection);
                    }
                }
                else
                {
                    CloseConnection(connection);
                }
            }

            catch (SocketException)
            {                
                CloseConnection(connection);
            }
            catch (Exception)
            {                
                CloseConnection(connection);
            }

            int maxBodies = _sensor.BodyFrameSource.BodyCount;
            string[] gestureDatas = { "database\\HandGestures.gbd" };

            lock (gestureDetectorList)
            {
                if (gestureDetectorList.Count == 0)
                {
                    for (int i = 0; i < maxBodies; ++i)
                    {
                        List<GestureResult> results = new List<GestureResult>();

                        GestureDetector detector = new GestureDetector(i, _sensor, results, gestureDatabase);
                        gestureDetectorList.Add(detector);
                    }
                    hasDetector = true;
                }
                else
                {
                    if (!gestureDatabase.SequenceEqual(oldGestureDatabase))
                    {
                        foreach (var detector in gestureDetectorList)
                        {
                            detector.UpdateGestureDetector(gestureDatabase);
                        }
                    }
                }
            }
        }

        private static void CloseConnection(StateObject ci)
        {
            if (ci.socket != null)
            {
                ci.socket.Close();
            }            
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

            gestureDetectorList = new List<GestureDetector>();             
        }

        static void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
        {
            bool hasGesture = false;

            var reference = e.FrameReference.AcquireFrame();

            bool dataReceived = false;
            // Body       
            using (var frame = reference.BodyFrameReference.AcquireFrame())
            {
                if (frame != null)
                {
                    int bodyCount = frame.BodyFrameSource.BodyCount;
                    _skeletonCollection = new SkeletonsDataCollection();
                    _skeletonList = new Skeleton[bodyCount];

                    _bodies = new Body[frame.BodyFrameSource.BodyCount];

                    frame.GetAndRefreshBodyData(_bodies);
                    dataReceived = true;

                    List<Body> _bodyList = _bodies.ToList<Body>();

                    JSONBodySerialize jsonBS = new JSONBodySerialize();
                    
                    jsonBS.PopulateBodies(_bodyList, _sensor.CoordinateMapper, Mode.Color);

                    if (isReceiving)
                    {
                        if (jsonBS.jsonSkeletons.Bodies.Count > 0)
                        {
                            for (int i = 0; i < jsonBS.jsonSkeletons.Bodies.Count; i++)
                            {
                                foreach (var gesture in gestureDetectorList[i].GestureResults)
                                {
                                    if (gesture.Detected)
                                    {
                                        hasGesture = true;
                                        jsonBS.jsonSkeletons.Bodies[i].Gestures.Add(new JSONGesture
                                        {
                                            Name = gesture.Name,
                                            Confidence = gesture.Confidence
                                        });
                                    }
                                }
                            }
                        } 
                    }
                   
                    string _bodiesJSON = jsonBS.Serialize();
                    //string _bodiesJSON = _bodyList.Serialize(_sensor, _sensor.CoordinateMapper, Mode.Color);

                    if (hasGesture)
                    {
                        File.WriteAllBytes("body_byte.txt", Encoding.UTF8.GetBytes(_bodiesJSON));
                    }

                    if (isSendBodyData)
                    {
                        _skeletonCollection.BodyCount = bodyCount;
                        _skeletonCollection.SkeletonList = _skeletonList;

                        //byte[] bytes = Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(_skeletonCollection));
                        byte[] bytes = Encoding.UTF8.GetBytes(_bodiesJSON);
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

                        handlerSocket = connections[0].socket;

                        if (handlerSocket.Connected)
                        {
                            try
                            {
                                lock (serverLock)
                                {
                                    WriteDebug("Send Body data begins");
                                    //Console.WriteLine(DEBUG_SYMBOL + "Send Body data begins");
                                    handlerSocket.BeginSend(sentData, 0, sentData.Length, 0,
                                        new AsyncCallback(SendBodyDataCallback), handlerSocket);
                                }
                            }

                            catch (SocketException se)
                            {
                                Console.WriteLine(ERROR_SYMBOL + "Socket exception: " + se.SocketErrorCode);
                                Console.WriteLine(se);
                            }
                        }
                    }
                }
            }

            if (dataReceived)
            {
                if (hasDetector)
                {
                    // we may have lost/acquired bodies, so update the corresponding gesture detectors
                    if (_bodies != null)
                    {
                        // loop through all bodies to see if any of the gesture detectors need to be updated
                        int maxBodies = _sensor.BodyFrameSource.BodyCount;
                        for (int i = 0; i < maxBodies; ++i)
                        {
                            Body body = _bodies[i];
                            ulong trackingId = body.TrackingId;

                            //WriteDebug("BodyIndex = " + i + " | TrackingId = " + trackingId + gestureDetectorList[i]);
                            Console.WriteLine("BodyIndex = " + i + " | TrackingId = " + trackingId + gestureDetectorList[i]);

                            // if the current body TrackingId changed, update the corresponding gesture detector with the new value
                            if (trackingId != gestureDetectorList[i].TrackingId)
                            {
                                gestureDetectorList[i].TrackingId = trackingId;
                                // if the current body is tracked, unpause its detector to get VisualGestureBuilderFrameArrived events
                                // if the current body is not tracked, pause its detector so we don't waste resources trying to get invalid gesture results
                                gestureDetectorList[i].IsPaused = (trackingId == 0);
                            }
                        }
                    }
                }                
            }
        }

        static string ConvertStringArrayToStringJoin(string[] array)
        {
            //
            // Use string Join to concatenate the string elements.
            //
            string result = string.Join(".", array);
            return result;
        }

        private static void SendBodyDataCallback(IAsyncResult ar)
        {
            WriteDebug("Send Body data completed. [isSendBodyData: " + isSendBodyData + "]");
            //Console.WriteLine(DEBUG_SYMBOL + "Send Body data completed. [isSendBodyData: " + isSendBodyData + "]");
            try
            {
                handlerSocket.EndSend(ar);
            }
            catch (SocketException se)
            {
                Console.WriteLine(se.ToString() + "\n" + ERROR_SYMBOL + "Error code: "+se.SocketErrorCode);
            }
            catch (ObjectDisposedException ode)
            {
                Console.WriteLine(ode.ToString());
            }
        }

        public static ManualResetEvent allDone = new ManualResetEvent(false);

        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);

        private delegate bool EventHandler(CtrlType sig);
        static EventHandler _handler;

        static void WriteDebug(string debugString)
        {
            if (debugFlag)
            {
                Console.WriteLine(DEBUG_SYMBOL + debugString);
            }
        }
        */

        static void WarningInvalidPort(string arg)
        {
            Console.WriteLine();
            Console.WriteLine("Invalid port: " + arg);
            Console.WriteLine();
            Console.WriteLine("     Usage: KinectServerConsole.exe [port_number] [-debug]");
            Console.WriteLine();
            Console.WriteLine("Port " + DEFAULT_PORT + " is used if no port is provided.");
        }

        static void WarningInvalidOption(string arg)
        {
            Console.WriteLine();
            Console.WriteLine("Invalid option: " + arg);
            Console.WriteLine();
            Console.WriteLine("     Usage: KinectServerConsole.exe [port_number] [-debug]");
            Console.WriteLine();
            Console.WriteLine("Port " + DEFAULT_PORT + " is used if no port is provided.");
        }        

        //private static bool Handler(CtrlType sig)
        //{
        //    return false;
        //}

        //[DllImport("Kernel32")]
        //private static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);

        //private delegate bool EventHandler(CtrlType sig);
        //static EventHandler _handler;

        static void Main(string[] args)
        {
           if (args == null || args.Length == 0)
           {
               serverPort = DEFAULT_PORT;
           }
           else if (args.Length == 1 && args[0].Equals("-debug"))
           {
               debugFlag = true;
               serverPort = DEFAULT_PORT;
           }
           else if (args.Length > 2)
           {               
               Console.WriteLine();
               Console.WriteLine("     Usage: KinectServerConsole.exe [port_number] [-debug]");
               return;
           }
           else
           {
               int portNum;
               bool isNumber = Int32.TryParse(args[0], out portNum);
               bool isDebug;

               if (isNumber)
               {
                   if (portNum >= 1024 && portNum <= 49151)
                   {
                       serverPort = portNum;
                   }
                   else
                   {
                       WarningInvalidPort(args[0]);
                       return;
                   }                        
               }
               else
               {
                   WarningInvalidPort(args[0]);
                   return;
               }

               if (args.Length == 2)
               {
                   isDebug = args[1].Equals("-debug");

                   if (isDebug)
                   {
                       debugFlag = true;
                   }
                   else
                   {
                       WarningInvalidOption(args[1]);
                       return;
                   }
               }               
           }

           KinectServerConsole server = new KinectServerConsole(serverPort, debugFlag);
           //StartKinect();
           //StartServer(port);

           // while (true)
           // {
           //     allDone.Reset();
           //     ContinueListen(serverSocket);
           //     allDone.WaitOne();
           // }
        }
    }    
}
