using Microsoft.Kinect;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace KinectServerConsole
{
    class KinectServerConsole
    {
        // 2 classes to deserialize JSON commands from Flash client
        public class CommandList
        {
            public bool colorImage { get; set; }
            public bool bodyIndexImage { get; set; }
            public bool bodyData { get; set; }
        }

        public class KinectDataRequest
        {
            public string command { get; set; }
            public CommandList dataReceive { get; set; }
            public string[] gestureDatabase { get; set; }
        }

        private class StateObject
        {
            public Socket socket;
            public byte[] buffer;
            public StringBuilder stringBuilder = new StringBuilder();
        }

        #region Constants

        const int DEFAULT_PORT = 7001;
        const string DEBUG_SYMBOL = "[DEBUG]";
        const string ERROR_SYMBOL = "[ERROR]";

        #endregion

        #region Variable

        private bool hasDetector;
        private bool isReceiving = false;

        private bool isSendColor = false;
        private bool isSendBodyIndex = false;
        private bool isSendBodyData = false;
        private string[] gestureDatabase = { };

        private KinectSensor _sensor;
        private MultiSourceFrameReader _reader;
        private IList<Body> _bodies;

        private Skeleton[] _skeletonList;
        private SkeletonsDataCollection _skeletonCollection;

        private object serverLock = new object();

        private int serverPort;
        private bool debugFlag = false;

        private Socket serverSocket;
        private Socket handlerSocket;

        private List<GestureDetector> gestureDetectorList = null;
        private List<StateObject> connections = new List<StateObject>();

        #endregion

        public ManualResetEvent allDone = new ManualResetEvent(false);

        public KinectServerConsole(int serverPort, bool debugFlag)
        {
            // Some boilerplate to react to close window event
            _handler += new EventHandler(Handler);
            SetConsoleCtrlHandler(_handler, true);

            this.serverPort = serverPort;
            this.debugFlag = debugFlag;

            StartServer(this.serverPort);
            StartKinect();

            while (true)
            {
                allDone.Reset();
                ContinueListen(serverSocket);
                allDone.WaitOne();
            }
        }

        private void SetupServerSocket(int inputPort)
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

        public void StartServer(int inputPort)
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
                Console.WriteLine(ERROR_SYMBOL + "Fail. Can not listen at " + serverPort);
                Console.WriteLine(e);
            }
            Console.WriteLine("Done. Listening for flash at port " + serverPort);

            if (debugFlag)
            {
                Console.WriteLine("Debug enabled.");
            }
        }

        private void ContinueListen(Socket socket)
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

        private void AcceptCallback(IAsyncResult result)
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

        private void ReceiveCallback(IAsyncResult result)
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

        private void CloseConnection(StateObject ci)
        {
            if (ci.socket != null)
            {
                ci.socket.Close();
            }
            lock (connections) connections.Remove(ci);
        }

        public void StartKinect()
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

        void Reader_MultiSourceFrameArrived(object sender, MultiSourceFrameArrivedEventArgs e)
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
                        lock (serverLock)
                        {
                            if (jsonBS.jsonSkeletons.Bodies.Count > 0)
                            {
                                for (int i = 0; i < bodyCount; i++)
                                {
                                    foreach (var gesture in gestureDetectorList[i].GestureResults)
                                    {
                                        if (gesture.Detected)
                                        {
                                            ulong trackingId = gestureDetectorList[i].TrackingId;
                                            hasGesture = true;
                                            JSONBody jsBD = jsonBS.jsonSkeletons.Bodies.FirstOrDefault(n => n.trackingID == trackingId.ToString());
                                            jsBD.Gestures.Add(new JSONGesture
                                            {
                                                Name = gesture.Name,
                                                Confidence = gesture.Confidence
                                            });
                                        }
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
                            Console.WriteLine("BodyIndex = " + i + " | BodyTrackingId = " + trackingId + "\n GestureTrackingId = " + gestureDetectorList[i].TrackingId + gestureDetectorList[i]);
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

                    foreach (var gestureDetector in gestureDetectorList)
                    {
                        if (gestureDetector.TrackingId == 0)
                        {
                            foreach (var gesture in gestureDetector.GestureResults)
                            {
                                gesture.UpdateGestureResult(gesture.Name, false, false, 0.0f);
                            }
                        }
                    }
                }
            }
        }

        string ConvertStringArrayToStringJoin(string[] array)
        {
            //
            // Use string Join to concatenate the string elements.
            //
            string result = string.Join(".", array);
            return result;
        }

        private void SendBodyDataCallback(IAsyncResult ar)
        {
            WriteDebug("Send Body data completed. [isSendBodyData: " + isSendBodyData + "]");
            //Console.WriteLine(DEBUG_SYMBOL + "Send Body data completed. [isSendBodyData: " + isSendBodyData + "]");
            try
            {
                handlerSocket.EndSend(ar);
            }
            catch (SocketException se)
            {
                Console.WriteLine(se.ToString() + "\n" + ERROR_SYMBOL + "Error code: " + se.SocketErrorCode);
            }
            catch (ObjectDisposedException ode)
            {
                Console.WriteLine(ode.ToString());
            }
        }

        private void WriteDebug(string debugString)
        {
            if (debugFlag)
            {
                Console.WriteLine(DEBUG_SYMBOL + debugString);
            }
        }

        public void ShutdownServer()
        {
            Console.WriteLine("Shutting down server... ");
            if (connections != null && connections.Count != 0)
            {
                lock (connections)
                {
                    for (int i = connections.Count - 1; i >= 0; i--)
                    {
                        CloseConnection(connections[i]);
                    }
                }
            }

            if (gestureDetectorList != null && gestureDetectorList.Count != 0)
            {
                lock (gestureDetectorList)
                {
                    foreach (var gestureDetector in gestureDetectorList)
                    {
                        gestureDetector.Dispose();
                    }
                }
            }

            if (serverSocket != null)
            {
                serverSocket.Close();
            }

            if (handlerSocket != null)
            {
                handlerSocket.Close();
            }

            if (_reader != null)
            {
                _reader.Dispose();
            }

            if (_sensor != null)
            {
                _sensor.Close();
            }

            Console.WriteLine("Done!");

            Environment.Exit(0);
        }

        private bool Handler(CtrlType sig)
        {
            ShutdownServer();
            return false;
        }

        [DllImport("Kernel32")]
        private static extern bool SetConsoleCtrlHandler(EventHandler handler, bool add);

        private delegate bool EventHandler(CtrlType sig);
        EventHandler _handler;
    }
}