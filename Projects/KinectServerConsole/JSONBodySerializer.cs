﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Kinect;
using System.Runtime.Serialization.Json;
using System.Runtime.Serialization;
using System.IO;
using System.Windows;
using Newtonsoft.Json;

namespace KinectServerConsole
{
    public static class JSONBodySerializer
    {
        [DataContract]
        class JSONBodyCollection
        {
            [DataMember(Name = "command")]
            public string command { get; set; }
            [DataMember(Name = "bodies")]
            public List<JSONBody> Bodies { get; set; }
        }
        [DataContract]
        class JSONBody
        {
            [DataMember(Name = "trackingID")]
            public string trackingID { get; set; }
            [DataMember(Name = "handLeftState")]
            public HandState HandLeftState { get; set; }
            [DataMember(Name = "handRightState")]
            public HandState HandRightState { get; set; }
            [DataMember(Name = "joints")]
            public List<JSONJoint> Joints { get; set; }
            [DataMember(Name = "gesture")]
            public List<JSONGesture> Gestures { get; set; }
        }
        [DataContract]
        class JSONJoint
        {
            [DataMember(Name = "name")]
            public string Name { get; set; }
            [DataMember(Name = "X")]
            public double X { get; set; }
            [DataMember(Name = "Y")]
            public double Y { get; set; }
            [DataMember(Name = "mappedX")]
            public double mappedX { get; set; }
            [DataMember(Name = "mappedY")]
            public double mappedY { get; set; }
            [DataMember(Name = "z")]
            public double Z { get; set; }
        }

        [DataContract]
        class JSONGesture
        {
            [DataMember(Name = "name")]
            public string Name { get; set; }
            [DataMember(Name = "progress")]
            public float Confidence { get; set; }
        }

        public static string Serialize(this List<Body> bodies, KinectSensor sensor, CoordinateMapper mapper, Mode mode)
        {
            List<GestureDetector> gestureDetectorList = new List<GestureDetector>();

            // create gesture detector for each body
            int bodyCount = bodies.Count;

            //for (int i = 0; i < bodyCount; ++i)
            //{
            //    GestureResult result = new GestureResult(i, false, false, 0.0f);             
            //    String appCurrentPath = Path.GetDirectoryName(new Uri(System.Reflection.Assembly.GetExecutingAssembly().CodeBase).LocalPath);
            //    String database = Path.Combine(appCurrentPath,"Database\\WaveHand.gbd");
            //    //Console.WriteLine(database);
            //    GestureDetector detector = new GestureDetector(sensor, result, database, "Wave_Left");
            //    gestureDetectorList.Add(detector);
            //}

            JSONBodyCollection jsonSkeletons = new JSONBodyCollection { Bodies = new List<JSONBody>() };
            jsonSkeletons.command = "bodyData";

            for (int i = 0; i < bodyCount; ++i)
            {
                JSONBody jsonSkeleton = new JSONBody();
                if (bodies[i].IsTracked)
                {
                    ulong trackingId = bodies[i].TrackingId;

                    //if (trackingId != gestureDetectorList[i].TrackingId)
                    //{
                    //    gestureDetectorList[i].TrackingId = trackingId;
                    //    gestureDetectorList[i].IsPaused = (trackingId == 0);
                    //}

                    jsonSkeleton.trackingID = bodies[i].TrackingId.ToString();
                    jsonSkeleton.Joints = new List<JSONJoint>();
                    jsonSkeleton.HandLeftState = bodies[i].HandLeftState;
                    jsonSkeleton.HandRightState = bodies[i].HandRightState;
                    //jsonSkeleton.Gestures = new List<JSONGesture>();

                    //if (gestureDetectorList[i].GestureResult.Detected)
                    //{
                    //    jsonSkeleton.Gestures.Add(new JSONGesture
                    //    {
                    //        Name = gestureDetectorList[i].gestureName,
                    //        Confidence = gestureDetectorList[i].GestureResult.Confidence
                    //    });
                    //}

                    foreach (var joint in bodies[i].Joints)
                    {
                        Point point = new Point();
                        switch (mode)
                        {
                            case Mode.Color:
                                ColorSpacePoint colorPoint = mapper.MapCameraPointToColorSpace(joint.Value.Position);
                                point.X = colorPoint.X;
                                point.Y = colorPoint.Y;
                                break;
                            case Mode.Depth:
                                DepthSpacePoint depthPoint = mapper.MapCameraPointToDepthSpace(joint.Value.Position);
                                point.X = depthPoint.X;
                                point.Y = depthPoint.Y;
                                break;
                            default:
                                break;
                        }

                        jsonSkeleton.Joints.Add(new JSONJoint
                        {
                            Name = joint.Key.ToString().ToLower(),
                            X = joint.Value.Position.X,
                            Y = joint.Value.Position.Y,
                            mappedX = Double.IsInfinity(point.X) ? -1 : point.X,
                            mappedY = Double.IsInfinity(point.X) ? -1 : point.Y,
                            Z = joint.Value.Position.Z
                        });
                    }
                    jsonSkeletons.Bodies.Add(jsonSkeleton);
                }              
            }
            return JsonConvert.SerializeObject(jsonSkeletons);
        }       
    }
}
