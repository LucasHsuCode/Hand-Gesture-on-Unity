using UnityEngine;

/** \brief Hand provides a set of APIs for hand tracking. */
namespace Coretronic.Reality.Hand
{
    /** \brief Gesture provides APIs to convert the hand joints into gesture. */
    public class Gesture
    {
        /** \brief An empty gesture. */
        public readonly static Gesture unknown = new Gesture();

        /** \brief A strict type of this gesture. */
        public StrictTypes strictType { get; private set; } = StrictTypes.Unknown;
        
        private int condVal = (int) Conditions.Unknown;
        
        /** \brief A condition set of this gesture. */
        public Conditions condition => (Conditions) condVal;

        private Gesture() { }

        public Gesture(StrictTypes s)
        {
            condVal = (int) s;
            strictType = s;
        }

        private static bool filter(int cond, int match)
        {
            return (cond & match) == match;
        }
        private static bool filter(int cond, StrictTypes match)
        {
            return filter(cond, (int) match);
        }
        private static bool filter(int cond, Types match)
        {
            return filter(cond, (int) match);
        }
        private static bool filter(int cond, Conditions match)
        {
            return filter(cond, (int) match);
        }

        /**
         * \brief Compared this gesture with selected gesture.
         * @param type Selected gesture.
         * @return A boolean value represents the comparison results.
         */
        public bool IsType(Types type)
        {
            switch(type)
            {
                case Types.Fist:
                    if (filter(this.condVal, StrictTypes.Fist)) return true;
                    if (filter(this.condVal, StrictTypes.Pinch)) return true;
                    return false;
                case Types.Pinch:
                    if (filter(this.condVal, StrictTypes.Fist)) return true;
                    if (filter(this.condVal, StrictTypes.Pinch)) return true;
                    if (filter(this.condVal, StrictTypes.OK1)) return true;
                    if (filter(this.condVal, StrictTypes.OK2)) return true;
                    return false;
                case Types.OK:
                    if (filter(this.condVal, StrictTypes.OK1)) return true;
                    if (filter(this.condVal, StrictTypes.OK2)) return true;
                    return false;
            }
            return filter(this.condVal, type);
        }

        /**
         * \brief Compared this gesture with selected strict gesture.
         * @param type Selected strict gesture.
         * @return A boolean value represents the comparison results.
         */
        public bool IsType(StrictTypes type)
        {
            return filter(this.condVal, type);
        }

        /**
         * \brief Compared this gesture with a selected condition set.
         * @param match Selected strict gesture.
         * @return A boolean value represents the comparison results.
         */
        public bool MatchConditions(Conditions match)
        {
            return filter(this.condVal, match);
        }

        /**
         * \brief Convert the hand joints into gesture.
         * @param handType   Hand type.
         * @param handJoints Hand joints.
         * @return Gesture instance for this hand joints.
         */
        public static Gesture GetInstance(Hand.Types handType, Joints handJoints)
        {
            Vector3[] joints = handJoints.ToArray();
            return GetInstance(handType, joints);
        }

        /**
         * \brief Convert the hand joints into gesture.
         * @param handType Hand type.
         * @param jArr     An array of Unity Vector3 that represents the positions of each hand joints.
         * @return Gesture instance for this hand joints.
         */
        public static Gesture GetInstance(Hand.Types handType, Vector3[] jArr)
        {
            float scale = (jArr[3] - jArr[0]).magnitude;
            Vector3[] joints = new Vector3[jArr.Length];
            // left to right hand
            for (int i = 0; i < joints.Length; i++)
                joints[i] = new Vector3(jArr[i].x, jArr[i].y, handType == Hand.Types.Left ? -jArr[i].z : jArr[i].z) / scale;
            // get palm rotation
            const float RAD_90 = 90 * Mathf.Deg2Rad;
            Vector3 rotationCenter = new Vector3(joints[3].x, joints[3].y, joints[3].z);
            var palmVec = joints[0] - joints[3];
            float rotateZ = -(Mathf.Atan2(palmVec.y, palmVec.x) + RAD_90) * Mathf.Rad2Deg;
            rotateZ = ((int) rotateZ / 5) * 5;
            Quaternion rotation = Quaternion.Euler(0, 0, rotateZ);
            rotateZ = rotation.eulerAngles.z;
            for (int i = 0; i < joints.Length; i++) joints[i] = rotation * (joints[i] - rotationCenter) * 1000;
            palmVec = joints[0] - joints[3];
            float rotateX = (Mathf.Atan2(palmVec.y, palmVec.z) + RAD_90) * Mathf.Rad2Deg;
            rotateX = ((int) rotateX / 5) * 5;
            rotation = Quaternion.Euler(rotateX, 0, 0);
            rotateX = rotation.eulerAngles.x;
            for (int i = 0; i < joints.Length; i++) joints[i] = rotation * joints[i];
            palmVec = joints[2] - joints[5];
            float rotateY = -(Mathf.Atan2(palmVec.x, palmVec.z) + RAD_90) * Mathf.Rad2Deg;
            rotateY = ((int) rotateY / 5) * 5;
            rotation = Quaternion.Euler(0, rotateY, 0);
            rotateY = rotation.eulerAngles.y;
            for (int i = 0; i < joints.Length; i++) joints[i] = rotation * joints[i];
            // recognition
            var result = new Gesture();
            bool palmFacingCamera = false;
            bool tIsOpen = false;
            // var itVec = joints[8] - joints[11];
            // var itxx = itVec.x * itVec.x;
            // var ityy = itVec.y * itVec.y;
            // var itzz = itVec.z * itVec.z;
            // var it_dist = Mathf.Min(Mathf.Sqrt(itxx+ityy), Mathf.Sqrt(itxx+itzz));
            // bool isThumbNearIndex = it_dist < 400f;
            bool isThumbNearIndex = (joints[8] - joints[11]).magnitude < 350f;

            if (handType != Hand.Types.Unknown)
            {
                palmFacingCamera = 90 < rotateY && rotateY < 270;
                tIsOpen = joints[8].x < joints[7].x;

                if (handType == Hand.Types.Right && palmFacingCamera || 
                    handType == Hand.Types.Left && !palmFacingCamera)
                    result.condVal |= (int) Conditions.PalmFacingCamera;
            }
            if (tIsOpen)
            {
                result.condVal |= (int) Conditions.ThumbIsOpen;
                if ((palmFacingCamera ? 350 <= rotateZ || rotateZ <= 50 : 150 <= rotateZ && rotateZ <= 210))
                    result.condVal |= (int) Conditions.ThumbIsRight;
                if ((palmFacingCamera ? 80 <= rotateZ && rotateZ <= 140 : 240 <= rotateZ && rotateZ <= 300))
                    result.condVal |= (int) Conditions.ThumbIsDown;
                if ((palmFacingCamera ? 170 <= rotateZ && rotateZ <= 230 : 330 <= rotateZ || rotateZ <= 30))
                    result.condVal |= (int) Conditions.ThumbIsLeft;
                if ((palmFacingCamera ? 260 <= rotateZ && rotateZ <= 320 : 60 <= rotateZ && rotateZ <= 120))
                    result.condVal |= (int) Conditions.ThumbIsUp;
            }
            else
                result.condVal |= (int) Conditions.ThumbIsClose;
            if (joints[9].y < joints[10].y && joints[10].y < joints[11].y)
                result.condVal |= (int) Conditions.IndexIsOpen;
            if (joints[12].y < joints[13].y && joints[13].y < joints[14].y)
                result.condVal |= (int) Conditions.MiddleIsOpen;
            if (joints[15].y < joints[16].y && joints[16].y < joints[17].y)
                result.condVal |= (int) Conditions.RingIsOpen;
            if (joints[18].y < joints[19].y && joints[19].y < joints[20].y)
                result.condVal |= (int) Conditions.PinkyIsOpen;
            if (joints[9].y > joints[10].y && joints[10].y > joints[11].y)
                result.condVal |= (int) Conditions.IndexIsClose;
            if (joints[12].y > joints[13].y && joints[13].y > joints[14].y)
                result.condVal |= (int) Conditions.MiddleIsClose;
            if (joints[15].y > joints[16].y && joints[16].y > joints[17].y)
                result.condVal |= (int) Conditions.RingIsClose;
            if (joints[18].y > joints[19].y && joints[19].y > joints[20].y)
                result.condVal |= (int) Conditions.PinkyIsClose;
            if (rotateZ <= 30 || (150 <= rotateZ && rotateZ <= 210) || 330 <= rotateZ)
                result.condVal |= (int) Conditions.PalmIsVertical;
            if ((60 <= rotateZ && rotateZ <= 120) || (240 <= rotateZ && rotateZ <= 300))
                result.condVal |= (int) Conditions.PalmIsHorizon;
            if (isThumbNearIndex)
                result.condVal |= (int) Conditions.ThumbNearIndex;
            
            if (filter(result.condVal, StrictTypes.Five)) 
                result.strictType = StrictTypes.Five;
            else if (filter(result.condVal, StrictTypes.Four))
                result.strictType = StrictTypes.Four;
            else if (filter(result.condVal, StrictTypes.Three))
                result.strictType = StrictTypes.Three;
            else if (filter(result.condVal, StrictTypes.Two))
                result.strictType = StrictTypes.Two;
            else if (filter(result.condVal, StrictTypes.One))
                result.strictType = StrictTypes.One;
            // else if (filter(result.condVal, StrictTypes.ThumbUp) && !isThumbNearIndex)
            //     result.strictType = StrictTypes.ThumbUp;
            // else if (filter(result.condVal, StrictTypes.ThumbDown) && !isThumbNearIndex)
            //     result.strictType = StrictTypes.ThumbDown;
            // else if (filter(result.condVal, StrictTypes.ThumbLeft) && !isThumbNearIndex)
            //     result.strictType = StrictTypes.ThumbLeft;
            // else if (filter(result.condVal, StrictTypes.ThumbRight) && !isThumbNearIndex)
            //     result.strictType = StrictTypes.ThumbRight;
            else if (filter(result.condVal, StrictTypes.Yeah))
                result.strictType = StrictTypes.Yeah;
            else if (filter(result.condVal, StrictTypes.SpiderMan))
                result.strictType = StrictTypes.SpiderMan;
            else if (filter(result.condVal, StrictTypes.Rock))
                result.strictType = StrictTypes.Rock;
            else if (filter(result.condVal, StrictTypes.OK1))
                result.strictType = StrictTypes.OK1;
            else if (filter(result.condVal, StrictTypes.OK2))
                result.strictType = StrictTypes.OK2;
            else if (isThumbNearIndex)
                result.strictType = StrictTypes.Pinch;
            else if (filter(result.condVal, StrictTypes.Fist)) 
                result.strictType = StrictTypes.Pinch;
            // else if (filter(result.condVal, StrictTypes.Fist)) 
            //     result.strictType = StrictTypes.Fist;
            
            return result;
        }

        /**
         * \brief An enum of conditions to recognite the hand
         */
        public enum Conditions : int
        {
            Unknown          = 0,       /**< Unknown */
            PalmFacingCamera = 1 << 0,  /**< Palm facing the camera. */
            ThumbIsOpen      = 1 << 1,  /**< Thumb finger is open. */
            IndexIsOpen      = 1 << 2,  /**< Index finger is open. */
            MiddleIsOpen     = 1 << 3,  /**< Middle finger is open. */
            RingIsOpen       = 1 << 4,  /**< Ring finger is open. */
            PinkyIsOpen      = 1 << 5,  /**< Pinky finger is open. */
            ThumbIsClose     = 1 << 6,  /**< Thumb finger is close. */
            IndexIsClose     = 1 << 7,  /**< Index finger is close. */
            MiddleIsClose    = 1 << 8,  /**< Middle finger is close. */
            RingIsClose      = 1 << 9,  /**< Ring finger is close. */
            PinkyIsClose     = 1 << 10,  /**< Pinky finger is close. */
            PalmIsVertical   = 1 << 11,  /**< Palm is vertical to the xz plane. */
            PalmIsHorizon    = 1 << 12,  /**< Palm is horizontal to the xz plane. */
            ThumbIsRight     = 1 << 13,  /**< Thumb finger to the right. */
            ThumbIsDown      = 1 << 14,  /**< Thumb finger to the down. */
            ThumbIsLeft      = 1 << 15,  /**< Thumb finger to the left. */
            ThumbIsUp        = 1 << 16,  /**< Thumb finger to the up. */
            ThumbNearIndex   = 1 << 17,  /**< Thumb finger close to index finger. */
        }
        
        /**
         * \brief An enum of strict gesture recognited by strict conditions.
         */
        public enum StrictTypes : int
        {
            Unknown    = /**< Unknown gesture */ Conditions.Unknown, 
            Fist       = /**< Fist gesture  */ Conditions.ThumbIsClose | Conditions.IndexIsClose | Conditions.MiddleIsClose | Conditions.RingIsClose | Conditions.PinkyIsClose, 
            One        = /**< One gesture   */ Conditions.ThumbIsClose | Conditions.IndexIsOpen  | Conditions.MiddleIsClose | Conditions.RingIsClose | Conditions.PinkyIsClose, 
            Two        = /**< Two gesture   */ Conditions.ThumbIsOpen  | Conditions.IndexIsOpen  | Conditions.MiddleIsClose | Conditions.RingIsClose | Conditions.PinkyIsClose, 
            Three      = /**< Three gesture */ Conditions.ThumbIsOpen  | Conditions.IndexIsOpen  | Conditions.MiddleIsOpen  | Conditions.RingIsClose | Conditions.PinkyIsClose, 
            Four       = /**< Four gesture  */ Conditions.ThumbIsClose | Conditions.IndexIsOpen  | Conditions.MiddleIsOpen  | Conditions.RingIsOpen  | Conditions.PinkyIsOpen, 
            Five       = /**< Five gesture  */ Conditions.ThumbIsOpen  | Conditions.IndexIsOpen  | Conditions.MiddleIsOpen  | Conditions.RingIsOpen  | Conditions.PinkyIsOpen, 
            Pinch      = /**< Pinch gesture */ Conditions.ThumbNearIndex, 
            OK1        = /**< The first OK gesture  */ Conditions.ThumbIsClose | Conditions.IndexIsClose | Conditions.MiddleIsOpen | Conditions.RingIsOpen | Conditions.PinkyIsOpen, 
            OK2        = /**< The second OK gesture */ Conditions.ThumbNearIndex | Conditions.MiddleIsOpen | Conditions.RingIsOpen | Conditions.PinkyIsOpen, 
            Yeah       = /**< Yeah gesture */ Conditions.ThumbIsClose | Conditions.IndexIsOpen  | Conditions.MiddleIsOpen  | Conditions.RingIsClose | Conditions.PinkyIsClose,
            Rock       = /**< Rock gesture */ Conditions.ThumbIsOpen  | Conditions.IndexIsOpen  | Conditions.MiddleIsClose | Conditions.RingIsClose | Conditions.PinkyIsOpen,
            SpiderMan  = /**< Spider man gesture  */ Conditions.ThumbIsClose | Conditions.IndexIsOpen  | Conditions.MiddleIsClose | Conditions.RingIsClose | Conditions.PinkyIsOpen,
            // ThumbUp    = /**< Thumb up gesture    */ Conditions.ThumbIsUp    | Conditions.IndexIsClose | Conditions.MiddleIsClose | Conditions.RingIsClose | Conditions.PinkyIsClose, 
            // ThumbDown  = /**< Thumb down gesture  */ Conditions.ThumbIsDown  | Conditions.IndexIsClose | Conditions.MiddleIsClose | Conditions.RingIsClose | Conditions.PinkyIsClose, 
            // ThumbLeft  = /**< Thumb left gesture  */ Conditions.ThumbIsLeft  | Conditions.IndexIsClose | Conditions.MiddleIsClose | Conditions.RingIsClose | Conditions.PinkyIsClose, 
            // ThumbRight = /**< Thumb right gesture */ Conditions.ThumbIsRight | Conditions.IndexIsClose | Conditions.MiddleIsClose | Conditions.RingIsClose | Conditions.PinkyIsClose, 
        }
        
        /**
         * \brief An enum of gesture recognited by conditions.
         */
        public enum Types : int
        {
            Unknown    = /**< Unknown gesture */ StrictTypes.Unknown, 
            Fist       = /**< Fist gesture  */ StrictTypes.Fist | StrictTypes.Pinch, 
            // One        = /**< One gesture   */ StrictTypes.One, 
            Two        = /**< Two gesture   */ StrictTypes.Two, 
            Three      = /**< Three gesture */ StrictTypes.Three, 
            Four       = /**< Four gesture  */ StrictTypes.Four, 
            Five       = /**< Five gesture  */ StrictTypes.Five, 
            Pinch      = /**< Pinch gesture */ StrictTypes.Fist | StrictTypes.Pinch | StrictTypes.OK1 | StrictTypes.OK2, 
            OK         = /**< OK gesture    */ StrictTypes.OK1 | StrictTypes.OK2, 
            Yeah       = /**< Yeah gesture  */ StrictTypes.Yeah, 
            Rock       = /**< Rock gesture  */ StrictTypes.Rock,  
            SpiderMan  = /**< Spider man gesture  */ StrictTypes.SpiderMan, 
            // ThumbUp    = /**< Thumb up gesture    */ StrictTypes.ThumbUp, 
            // ThumbDown  = /**< Thumb down gesture  */ StrictTypes.ThumbDown, 
            // ThumbLeft  = /**< Thumb left gesture  */ StrictTypes.ThumbLeft, 
            // ThumbRight = /**< Thumb right gesture */ StrictTypes.ThumbRight, 
        }
    }
}
