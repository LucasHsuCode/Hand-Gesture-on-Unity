using UnityEngine;

namespace Coretronic.Reality
{
    public struct TransformComponent
    {
        public Quaternion Rotation;
        public Vector3 Position;

        public TransformComponent(Transform transform, bool useLocal)
        {
            this.Position = useLocal ? transform.localPosition : transform.position;
            this.Rotation = useLocal ? transform.localRotation : transform.rotation;
        }
    }
}