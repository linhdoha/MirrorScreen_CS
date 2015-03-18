namespace KinectCam
{
    using System;
    using System.Collections.Generic;

    internal sealed class KinectCamSettings
    {

        private static KinectCamSettings defaultInstance = new KinectCamSettings();

        public static KinectCamSettings Default
        {
            get
            {
                return defaultInstance;
            }
        }

        public bool Mirrored
        {
            get;
            set;
        }

        public bool Desktop
        {
            get;
            set;
        }
    }
}
