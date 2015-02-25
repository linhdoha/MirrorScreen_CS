using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Runtime.Serialization.Json;
using System.Runtime.Serialization;

namespace KinectStreams
{
    public struct CameraSpacePoint
    {
        public float X;
        public float Y;
        public float Z;
    }

    enum HandState
    {
        Unknown = 0,
        NotTracked = 1,
        Open = 2,
        Closed = 3,
        Lasso = 4
    };

    [DataContract]
    class Skeleton
    {        
        [DataMember]
        public Microsoft.Kinect.HandState HandLeftState { get; set; }
        [DataMember]
        public Microsoft.Kinect.HandState HandRightState { get; set; }
        [DataMember]
        public Microsoft.Kinect.CameraSpacePoint LeftHandPos { get; set; }
        [DataMember]
        public Microsoft.Kinect.CameraSpacePoint RightHandPos { get; set; }
        [DataMember]
        public ulong TrackingId { get; set; }
        //[DataMember]
        //public int BodyCount { get; set; }
    }
}
