using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace ControllerExperiment
{
    public class pc : MonoBehaviour
    {
        [Header("Rotation")]
        public AnimationCurve TorqueMultiplier;
        public float MaxTorque;
        public float Angle;
        public float AngleDifference;
        public float Torque;

        [Header("Horizontal Move")]
        public float WalkSpeed;
        Vector3 TargetWalkDir = new Vector3();

        [Header("Collision")]
        int DefaultLayerMask = 1 << 0;
        Vector3 GroundNormal = new Vector3();

        Rigidbody rbody;

        private void Start()
        {
            rbody = this.gameObject.GetComponent<Rigidbody>();
        }

        private void FixedUpdate()
        {
            RotateToTargetAngle();
            GetTargetWalkDir();
            WalkToTargetDir();

            CancelHorizontalVelocity();
            CancelVerticalVelocity();
        }

        void GetTargetWalkDir()
        {
            TargetWalkDir = Vector3.zero;

            if (Input.GetKey(KeyCode.W))
            {
                TargetWalkDir += this.transform.forward * WalkSpeed;
            }

            if (Input.GetKey(KeyCode.A))
            {
                TargetWalkDir -= this.transform.right * WalkSpeed;
            }

            if (Input.GetKey(KeyCode.S))
            {
                TargetWalkDir -= this.transform.forward * WalkSpeed;
            }

            if (Input.GetKey(KeyCode.D))
            {
                TargetWalkDir += this.transform.right * WalkSpeed;
            }

            if (Vector3.SqrMagnitude(TargetWalkDir) > 0.1f)
            {
                GroundNormal = GetGroundNormal();
                TargetWalkDir = Vector3.ProjectOnPlane(TargetWalkDir, GroundNormal);

                TargetWalkDir.Normalize();
                TargetWalkDir *= WalkSpeed;

                if (TargetWalkDir.y > 0f)
                {
                    TargetWalkDir -= Vector3.up * TargetWalkDir.y;
                    TargetWalkDir.Normalize();
                    TargetWalkDir *= WalkSpeed * 1.15f;
                }
                else if (TargetWalkDir.y < 0f)
                {
                    TargetWalkDir.Normalize();
                    TargetWalkDir *= WalkSpeed * 0.85f;
                }

                Debug.DrawLine(this.transform.position, this.transform.position + TargetWalkDir, Color.yellow, 1f);
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(new Vector3(TargetWalkDir.x, 0f, TargetWalkDir.z)), 10 * Time.deltaTime);

            }
        }

        void WalkToTargetDir()
        {
            if (TargetWalkDir.sqrMagnitude > 0.1f)
            {
                rbody.AddForce(TargetWalkDir, ForceMode.VelocityChange);
            }
        }

        void CancelHorizontalVelocity()
        {
            rbody.AddForce(Vector3.right * -rbody.velocity.x, ForceMode.VelocityChange);
            rbody.AddForce(Vector3.forward * -rbody.velocity.z, ForceMode.VelocityChange);
        }

        void CancelVerticalVelocity()
        {
            if (rbody.velocity.y > 0f)
            {
                rbody.AddForce(Vector3.up * -rbody.velocity.y, ForceMode.VelocityChange);
            }
        }

        void RotateToTargetAngle()
        {
            if (Mathf.Abs(AngleDifference) > 180f)
            {
                if (AngleDifference < 0f)
                {
                    AngleDifference = (360f + AngleDifference);
                }
                else if (AngleDifference > 0f)
                {
                    AngleDifference = (360f - AngleDifference) * -1f;
                }
            }

            rbody.maxAngularVelocity = MaxTorque;

            Torque = AngleDifference * TorqueMultiplier.Evaluate(Mathf.Abs(AngleDifference) / 180f) * 20f;
            rbody.AddTorque(Vector3.up * Torque, ForceMode.VelocityChange);
            rbody.AddTorque(Vector3.up * -rbody.angularVelocity.y, ForceMode.VelocityChange);
        }

        Vector3 GetGroundNormal()
        {
            Ray ray = new Ray(this.transform.position, Vector3.down);
            RaycastHit hit;
            if (Physics.Raycast(ray, out hit, 2f, DefaultLayerMask))
            {
                return hit.normal;
            }

            return Vector3.zero;
        }
    }
}