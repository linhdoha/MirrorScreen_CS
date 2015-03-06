using System;
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
        class JSONSkeletonCollection
        {
            [DataMember(Name = "bodies")]
            public List<JSONSkeleton> Skeletons { get; set; }
        }
        [DataContract]
        class JSONSkeleton
        {
            [DataMember(Name = "command")]
            public string command { get; set; }
            [DataMember(Name = "trackingID")]
            public string trackingID { get; set; }
            [DataMember(Name = "handLeftState")]
            public HandState HandLeftState { get; set; }
            [DataMember(Name = "handRightState")]
            public HandState HandRightState { get; set; }
            [DataMember(Name = "joints")]
            public List<JSONJoint> Joints { get; set; }
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

        public static string Serialize(this List<Body> skeletons, CoordinateMapper mapper, KinectServerConsole.Program.Mode mode)
        {
            JSONSkeletonCollection jsonSkeletons = new JSONSkeletonCollection { Skeletons = new List<JSONSkeleton>() };
            foreach (Body skeleton in skeletons)
            {
                JSONSkeleton jsonSkeleton = new JSONSkeleton();
                if (skeleton.IsTracked)
                {
                    jsonSkeleton.command = "bodyData";
                    jsonSkeleton.trackingID = skeleton.TrackingId.ToString();
                    jsonSkeleton.Joints = new List<JSONJoint>();
                    jsonSkeleton.HandLeftState = skeleton.HandLeftState;
                    jsonSkeleton.HandRightState = skeleton.HandRightState;

                    foreach (var joint in skeleton.Joints)
                    {
                        Point point = new Point();
                        switch (mode)
                        {
                            case KinectServerConsole.Program.Mode.Color:
                                ColorSpacePoint colorPoint = mapper.MapCameraPointToColorSpace(joint.Value.Position);
                                point.X = colorPoint.X;
                                point.Y = colorPoint.Y;
                                break;
                            case KinectServerConsole.Program.Mode.Depth:
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
                            mappedX = point.X,
                            mappedY = point.Y,
                            Z = joint.Value.Position.Z
                        });
                    }
                    jsonSkeletons.Skeletons.Add(jsonSkeleton);
                }
            }
            return JsonConvert.SerializeObject(jsonSkeletons);
        }       
    }
}
