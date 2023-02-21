using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using UnityEngine;

namespace Coretronic.Reality.Hand
{
    /** \brief Joints provides APIs to convert the raw buffer. */
    public class Joints
    {
        /** \brief Length of the array of the raw buffer. */
        public const int BufferLength = 63;
        
        // public const int Buffer2DLength = 42;
        
        /** \brief The number of hand joints.  */
        public const int Length = 21;

        private float[] buf;

        /**
         * \brief The default constructor for Joints.
         * @param buf Raw buffer for hand joints.
         */
        public Joints(float[] buf)
        {
            if (buf == null || buf.Length != BufferLength) throw new System.NotSupportedException();
            this.buf = buf;
        }

        /**
         * \brief Get the position of this joints by the joint name.
         * @param jn Name of this hand joint.
         * @return A Vector3 that represents the position of this hand joints.
         */
        public Vector3 this[JointName jn]
        {
            get
            {
                if (jn != JointName.None) 
                {
                    int idx = (int) jn * 3;
                    return new Vector3(buf[idx], buf[idx+1], buf[idx+2]);
                } 
                else
                    return Vector3.zero;
            }
        }

        /**
         * \brief Get the raw buffer of this joints.
         * @param[out] buffer The raw buffer of this hand joints.
         * @param[in]  offset The offset within the array of the first byte to be written.
         */
        public void GetBuffer(float[] buffer, int offset = 0)
        {
            if (buffer == null || (buffer.Length - offset) < BufferLength) 
                throw new System.NotSupportedException();
            Array.Copy(this.buf, 0, buffer, offset, BufferLength);
        }

        // public void GetBuffer2D(float[] buffer, int offset = 0)
        // {
        //     if (buffer == null || (buffer.Length - offset) < Buffer2DLength) 
        //         throw new System.NotSupportedException();
            
        //     for (int i = 0; i < this.buf.Length; ) 
        //     {
        //         buf[offset++] = this.buf[i++];
        //         buf[offset++] = this.buf[i++];
        //         i++;
        //     }
        // }

        /**
         * \brief An enum of hand joint names
         */
        public enum JointName : int
        {
            None = -1,  /**< None (Value is -1) */
            Wrist = 0,  /**< Wrist (Value is 0) */
            TMCP,  /**< MetaCarpoPhalangeal joints of thumb finger (Value is 1) */
            IMCP,  /**< MetaCarpoPhalangeal joints of index finger (Value is 2) */
            MMCP,  /**< MetaCarpoPhalangeal joints of middle finger (Value is 3) */
            RMCP,  /**< MetaCarpoPhalangeal joints of ring finger (Value is 4) */
            PMCP,  /**< MetaCarpoPhalangeal joints of pinky finger (Value is 5) */
            TPIP,  /**< Proximal InterPhalangeal joints of thumb finger (Value is 6) */
            TDIP,  /**< Distal InterPhalangeal joints of thumb finger (Value is 7) */
            TTIP,  /**< Tip joints of thumb finger (Value is 8) */
            IPIP,  /**< Proximal InterPhalangeal joints of index finger (Value is 9) */
            IDIP,  /**< Distal InterPhalangeal joints of index finger (Value is 10) */
            ITIP,  /**< Tip joints of index finger (Value is 11) */
            MPIP,  /**< Proximal InterPhalangeal joints of middle finger (Value is 12) */
            MDIP,  /**< Distal InterPhalangeal joints of middle finger (Value is 13) */
            MTIP,  /**< Tip joints of middle finger (Value is 14) */
            RPIP,  /**< Proximal InterPhalangeal joints of ring finger (Value is 15) */
            RDIP,  /**< Distal InterPhalangeal joints of ring finger (Value is 16) */
            RTIP,  /**< Tip joints of ring finger (Value is 17) */
            PPIP,  /**< Proximal InterPhalangeal joints of pinky finger (Value is 18) */
            PDIP,  /**< Distal InterPhalangeal joints of pinky finger (Value is 19) */
            PTIP   /**< Tip joints of pinky finger (Value is 20) */
        }

        private static readonly JointName[] joint_name_array = new JointName[] 
        {
            JointName.Wrist, 
            JointName.TMCP, JointName.IMCP, JointName.MMCP, JointName.RMCP, JointName.PMCP,
            JointName.TPIP, JointName.TDIP, JointName.TTIP,
            JointName.IPIP, JointName.IDIP, JointName.ITIP,
            JointName.MPIP, JointName.MDIP, JointName.MTIP,
            JointName.RPIP, JointName.RDIP, JointName.RTIP,
            JointName.PPIP, JointName.PDIP, JointName.PTIP
        };

        private static readonly JointName[] parent_joint_array = new JointName[] 
        {
            JointName.None, 
            JointName.Wrist, JointName.Wrist, JointName.Wrist, JointName.Wrist, JointName.Wrist,
            JointName.TMCP, JointName.TPIP, JointName.TDIP,
            JointName.IMCP, JointName.IPIP, JointName.IDIP,
            JointName.MMCP, JointName.MPIP, JointName.MDIP,
            JointName.RMCP, JointName.RPIP, JointName.RDIP,
            JointName.PMCP, JointName.PPIP, JointName.PDIP,
        };

        /**
         * \brief A read-only set of hand joints names.
         */
        public static readonly ReadOnlyCollection<JointName> JointNameArray = Array.AsReadOnly(joint_name_array);
        
        /**
         * \brief A read-only set of parent joints names of each hand joints.
         */
        public static readonly ReadOnlyCollection<JointName> ParentJointArray = Array.AsReadOnly(parent_joint_array);

        /**
         * \brief Get the positions of joints for this hand.
         * @return An array of Unity Vector3 that represents the positions of joints for this hands.
         */
        public Vector3[] ToArray()
        {
            Vector3[] jointsArray = new Vector3[Length];
            foreach(JointName jn in joint_name_array)
                jointsArray[(int) jn] = this[jn];
            return jointsArray;
        }
    }
}