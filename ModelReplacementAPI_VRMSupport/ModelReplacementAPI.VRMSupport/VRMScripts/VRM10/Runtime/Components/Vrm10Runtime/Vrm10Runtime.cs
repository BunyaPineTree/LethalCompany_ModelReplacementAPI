using System;
using System.Collections.Generic;
using System.Linq;
using UniGLTF;
using UniGLTF.Utils;
using UnityEngine;
using UniVRM10.FastSpringBones.Blittables;
using UniVRM10.FastSpringBones.System;

namespace UniVRM10
{
    /// <summary>
    /// VRM モデルインスタンスを、状態をもって、元の状態から操作・変更するためのクラス。
    /// また、仕様に従ってその操作を行う。
    ///
    /// 操作対象としては以下が挙げられる。
    /// - ControlRig
    /// - Constraint
    /// - LookAt
    /// - Expression
    /// </summary>
    public class Vrm10Runtime : IDisposable
    {
        private readonly Vrm10Instance m_instance;
        private readonly Transform m_head;
        private readonly FastSpringBoneService m_fastSpringBoneService;
        private readonly IReadOnlyDictionary<Transform, TransformState> m_defaultTransformStates;

        private FastSpringBoneBuffer m_fastSpringBoneBuffer;
        private BlittableExternalData m_externalData;

        /// <summary>
        /// Control Rig may be null.
        /// Control Rig is generated at loading runtime only.
        /// </summary>
        public Vrm10RuntimeControlRig ControlRig { get; }

        public IVrm10Constraint[] Constraints { get; }
        public Vrm10RuntimeExpression Expression { get; }
        public Vrm10RuntimeLookAt LookAt { get; }

        public IVrm10Animation VrmAnimation { get; set; }

        public Vector3 ExternalForce
        {
            get => m_externalData.ExternalForce;
            set
            {
                m_externalData.ExternalForce = value;
                m_fastSpringBoneBuffer.ExternalData = m_externalData;
            }
        }

        public Vrm10Runtime(Vrm10Instance instance, bool useControlRig)
        {
            if (!Application.isPlaying)
            {
                Debug.LogWarning($"{nameof(Vrm10Runtime)} expects runtime behaviour.");
            }

            m_instance = instance;

            if (!instance.TryGetBoneTransform(HumanBodyBones.Head, out m_head))
            {
                throw new Exception();
            }

            if (useControlRig)
            {
                ControlRig = new Vrm10RuntimeControlRig(instance.Humanoid, m_instance.transform);
            }
            Constraints = instance.GetComponentsInChildren<IVrm10Constraint>();
            LookAt = new Vrm10RuntimeLookAt(instance.Vrm.LookAt, instance.Humanoid, ControlRig);
            Expression = new Vrm10RuntimeExpression(instance, LookAt.EyeDirectionApplicable);

            var gltfInstance = instance.GetComponent<RuntimeGltfInstance>();
            if (gltfInstance != null)
            {
                // ランタイムインポートならここに到達してゼロコストになる
                m_defaultTransformStates = gltfInstance.InitialTransformStates;
            }
            else
            {
                // エディタでプレハブ配置してる奴ならこっちに到達して収集する
                m_defaultTransformStates = instance.GetComponentsInChildren<Transform>()
                    .ToDictionary(tf => tf, tf => new TransformState(tf));
            }

            // NOTE: FastSpringBoneService は UnitTest などでは動作しない
            if (Application.isPlaying)
            {
                Console.WriteLine("Is playing ");
                m_fastSpringBoneService = FastSpringBoneService.Instance;
                m_fastSpringBoneBuffer = CreateFastSpringBoneBuffer(m_instance.SpringBone);
                m_fastSpringBoneService.BufferCombiner.Register(m_fastSpringBoneBuffer);
            }
        }

        public void Dispose()
        {
            ControlRig?.Dispose();
            m_fastSpringBoneService.BufferCombiner.Unregister(m_fastSpringBoneBuffer);
            m_fastSpringBoneBuffer.Dispose();
        }

        /// <summary>
        /// このVRMに紐づくSpringBone関連のバッファを再構築する
        /// ランタイム実行時にSpringBoneに対して変更を行いたいときは、このメソッドを明示的に呼ぶ必要がある
        /// </summary>
        public void ReconstructSpringBone()
        {
            m_fastSpringBoneService.BufferCombiner.Unregister(m_fastSpringBoneBuffer);

            m_fastSpringBoneBuffer.Dispose();
            m_fastSpringBoneBuffer = CreateFastSpringBoneBuffer(m_instance.SpringBone);

            m_fastSpringBoneService.BufferCombiner.Register(m_fastSpringBoneBuffer);
        }

        private FastSpringBoneBuffer CreateFastSpringBoneBuffer(Vrm10InstanceSpringBone springBone)
        {
            return new FastSpringBoneBuffer(
                springBone.Springs.Select(spring => new FastSpringBoneSpring
                {
                    center = spring.Center,
                    colliders = spring.ColliderGroups
                    .SelectMany(group => group.Colliders)
                    .Select(collider => new FastSpringBoneCollider
                    {
                        Transform = collider.transform,
                        Collider = new BlittableCollider
                        {
                            offset = collider.Offset,
                            radius = collider.Radius,
                            tail = collider.Tail,
                            colliderType = TranslateColliderType(collider.ColliderType)
                        }
                    }).ToArray(),
                    joints = spring.Joints
                    .Select(joint => new FastSpringBoneJoint
                    {
                        Transform = joint.transform,
                        Joint = new BlittableJoint
                        {
                            radius = joint.m_jointRadius,
                            dragForce = joint.m_dragForce,
                            gravityDir = joint.m_gravityDir,
                            gravityPower = joint.m_gravityPower,
                            stiffnessForce = joint.m_stiffnessForce
                        },
                        DefaultLocalRotation = GetOrAddDefaultTransformState(joint.transform).LocalRotation,
                    }).ToArray(),
                }).ToArray(),
                m_externalData);
        }

        private TransformState GetOrAddDefaultTransformState(Transform tf)
        {
            if (m_defaultTransformStates.TryGetValue(tf, out var defaultTransformState))
            {
                return defaultTransformState;
            }

            Debug.LogWarning($"{tf.name} does not exist on load.");
            return new TransformState(null);
        }

        private static BlittableColliderType TranslateColliderType(VRM10SpringBoneColliderTypes colliderType)
        {
            switch (colliderType)
            {
                case VRM10SpringBoneColliderTypes.Sphere:
                    return BlittableColliderType.Sphere;
                case VRM10SpringBoneColliderTypes.Capsule:
                    return BlittableColliderType.Capsule;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        /// <summary>
        /// 毎フレーム関連コンポーネントを解決する
        ///
        /// * Update from VrmAnimation
        /// * Constraint
        /// * Spring
        /// * LookAt
        /// * Expression
        ///
        /// </summary>
        public void Process()
        {
            // 1. Update From VrmAnimation
            if (VrmAnimation != null)
            {
                // copy pose
                {
                    Vrm10Retarget.Retarget(VrmAnimation.ControlRig, (ControlRig, ControlRig));
                }

                // update expressions
                foreach (var (k, v) in VrmAnimation.ExpressionMap)
                {
                    Expression.SetWeight(k, v());
                }

                // look at
                if (VrmAnimation.LookAt.HasValue)
                {
                    LookAt.LookAtInput = VrmAnimation.LookAt.Value;
                }
            }

            // 2. Control Rig
            ControlRig?.Process();

            // 3. Constraints
            foreach (var constraint in Constraints)
            {
                constraint.Process();
            }

            if (m_instance.LookAtTargetType == VRM10ObjectLookAt.LookAtTargetTypes.SpecifiedTransform
            && m_instance.LookAtTarget != null)
            {
                // Transform 追跡で視線を生成する。
                // 値を上書きします。
                LookAt.LookAtInput = new LookAtInput { WorldPosition = m_instance.LookAtTarget.position };
            }

            // 4. Gaze control
            var eyeDirection = LookAt.Process();

            // 5. Apply Expression
            // LookAt の角度制限などはこちらで処理されます。
            Expression.Process(eyeDirection);
        }
    }
}
