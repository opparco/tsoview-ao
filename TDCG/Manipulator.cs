using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.DirectX;
using Microsoft.DirectX.Direct3D;

namespace TDCG
{
    /// 操作に用いるデバイス
    public enum ManipulatorDeviceType
    {
        /// マウス
        Mouse,
        /// キーボード
        Keyboard
    };

    public class Manipulator
    {
        SimpleCamera camera;
        Figure fig;
        TMONode selected_node;

        bool grab_node = false;
        bool grab_camera = false;
        bool rotate_lamp = false;
        bool rotate_node = false;
        bool rotate_camera = false;
        ManipulatorDeviceType device_type;

        //ボーン移動操作の感度
        public float GrabNodeDelta { get; set; }
        //ボーン回転操作の感度
        public float RotateNodeDelta { get; set; }
        //カメラ移動操作の感度
        public float GrabCameraDelta { get; set; }
        //カメラ回転操作の感度
        public float RotateCameraDelta { get; set; }

        public Manipulator(SimpleCamera camera)
        {
            this.camera = camera;
            GrabNodeDelta = 0.0125f;
            RotateNodeDelta = 0.0125f;
            GrabCameraDelta = 0.125f;
            RotateCameraDelta = 0.01f;
        }

        public void BeginGrabNode(ManipulatorDeviceType device_type, TMONode selected_node)
        {
            this.device_type = device_type;
            this.selected_node = selected_node;

            grab_node = true;
        }

        public bool WhileGrabNode(int dx, int dy)
        {
            if (! grab_node)
                return false;

            if (dx == 0 && dy == 0)
                return false;

            Vector3 translation = new Vector3(dx * GrabNodeDelta, -dy * GrabNodeDelta, 0.0f);

            Quaternion world_rotation = Quaternion.Identity;
            TMONode parent_node = selected_node.parent;
            if (parent_node != null)
                world_rotation = parent_node.GetWorldRotation();

            Quaternion q = camera.RotationQuaternion * Quaternion.Conjugate(world_rotation);

            selected_node.Translation += Vector3.TransformCoordinate(translation, Matrix.RotationQuaternion(q));

            return true;
        }

        public bool WhileGrabNodeLocal(int dx, int dy, int dz)
        {
            if (! grab_node)
                return false;

            if (dx == 0 && dy == 0 & dz == 0)
                return false;

            Vector3 translation = new Vector3(dx * GrabNodeDelta, -dy * GrabNodeDelta, dz * GrabNodeDelta);

            selected_node.Translation += translation;

            return true;
        }

        public bool EndGrabNode(ManipulatorDeviceType device_type)
        {
            bool same_device = (device_type == this.device_type);
            if (same_device)
                grab_node = false;
            return same_device;
        }

        public void BeginRotateLamp(Figure fig)
        {
            this.fig = fig;

            rotate_lamp = true;
        }

        public bool WhileRotateLamp(int dx, int dy)
        {
            if (! rotate_lamp)
                return false;

            if (dx == 0 && dy == 0)
                return false;

            Quaternion rotation = Quaternion.RotationYawPitchRoll(dx * RotateNodeDelta, dy * RotateNodeDelta, 0.0f);

            Quaternion q = camera.RotationQuaternion;
            Quaternion q_1 = Quaternion.Conjugate(q);

            fig.LampRotation = Quaternion.Normalize(fig.LampRotation * q_1 * rotation * q);

            return true;
        }

        public void EndRotateLamp()
        {
            rotate_lamp = false;
        }

        public void BeginRotateNode(ManipulatorDeviceType device_type, TMONode selected_node)
        {
            this.device_type = device_type;
            this.selected_node = selected_node;

            rotate_node = true;
        }

        public bool WhileRotateNode(int dx, int dy)
        {
            if (! rotate_node)
                return false;

            if (dx == 0 && dy == 0)
                return false;

            Quaternion rotation = Quaternion.RotationYawPitchRoll(dx * RotateNodeDelta, dy * RotateNodeDelta, 0.0f);

            Quaternion world_rotation = Quaternion.Identity;
            TMONode parent_node = selected_node.parent;
            if (parent_node != null)
                world_rotation = parent_node.GetWorldRotation();

            Quaternion q = camera.RotationQuaternion * Quaternion.Conjugate(world_rotation);
            Quaternion q_1 = Quaternion.Conjugate(q);

            selected_node.Rotation = Quaternion.Normalize(selected_node.Rotation * q_1 * rotation * q);

            return true;
        }

        public bool WhileRotateNodeLocal(int dx, int dy, int dz)
        {
            if (! rotate_node)
                return false;

            if (dx == 0 && dy == 0 && dz == 0)
                return false;

            Quaternion rotation = Quaternion.RotationYawPitchRoll(dx * RotateNodeDelta, dy * RotateNodeDelta, dz * RotateNodeDelta);

            selected_node.Rotation = Quaternion.Normalize(selected_node.Rotation * rotation);

            return true;
        }

        public bool EndRotateNode(ManipulatorDeviceType device_type)
        {
            bool same_device = (device_type == this.device_type);
            if (same_device)
                rotate_node = false;
            return same_device;
        }

        public void BeginGrabCamera()
        {
            grab_camera = true;
        }

        public bool WhileGrabCamera(int dx, int dy)
        {
            if (! grab_camera)
                return false;

            if (dx == 0 && dy == 0)
                return false;

            camera.MoveView(-dx * GrabCameraDelta, dy * GrabCameraDelta, 0.0f);

            return true;
        }

        public bool WhileGrabCameraDepth(int dx, int dy)
        {
            if (! grab_camera)
                return false;

            if (dy == 0)
                return false;

            camera.MoveView(0.0f, 0.0f, -dy * GrabCameraDelta);

            return true;
        }

        public void EndGrabCamera()
        {
            grab_camera = false;
        }

        public void BeginRotateCamera()
        {
            rotate_camera = true;
        }

        public bool WhileRotateCamera(int dx, int dy)
        {
            if (! rotate_camera)
                return false;

            if (dx == 0 && dy == 0)
                return false;

            camera.Move(dx * RotateCameraDelta, -dy * RotateCameraDelta);

            return true;
        }

        public void EndRotateCamera()
        {
            rotate_camera = false;
        }
    }
}
