using ModelReplacement.Scripts.Player;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace ModelReplacement.Scripts
{
    public static class MeshHelper
    {

        public static int[] ConvertByteArrayToIntArray(byte[] bytes, int stride, int count)
        {
            int[] ints = new int[count];
            int j = 0;
            for (int i = 0; i < count; i++)
            {

                int newInt = 0;
                for (int k = 0; k < stride; k++)
                {
                    newInt += bytes[j + k] << (8 * k);
                }

                ints[i] = newInt;

                j += stride;
            }
            return ints;
        }

        public static byte[] ConvertIntArrayToByteArray(int[] ints, int stride, int count)
        {
            List<byte> bytes = new List<byte>();

            for (int i = 0; i < count; i++)
            {
                for (int k = 0; k < stride; k++)
                {
                    bytes.Add((byte)(ints[i] >> (8 * k)));
                }
            }
            return bytes.ToArray();
        }

        public static GameObject ConvertModelToViewModel(GameObject TempReplacementViewModel)
        {
            Animator animator = TempReplacementViewModel.GetComponentInChildren<Animator>();

            //Get all arm bones
            HashSet<Transform> armBones = new HashSet<Transform>();
            foreach (HumanBodyBones humanBone in ViewModelUpdater.ViewModelHumanBones)
            {
                Transform boneTransform = animator.GetBoneTransform(humanBone);
                if (boneTransform != null)
                {
                    armBones.Add(boneTransform);
                    foreach (Transform childBone in boneTransform.GetComponentsInChildren<Transform>())
                    {
                        armBones.Add(childBone);
                    }
                }
            }

            //For each skinnedMeshRenderer, get all triangles in each submesh that only have weightpainting in arm bones. Delete all other triangles.  
            foreach (SkinnedMeshRenderer skinnedMeshRenderer in TempReplacementViewModel.GetComponentsInChildren<SkinnedMeshRenderer>())
            {
                skinnedMeshRenderer.sharedMesh = UnityEngine.Object.Instantiate<Mesh>(skinnedMeshRenderer.sharedMesh);
                Mesh mesh = skinnedMeshRenderer.sharedMesh;

                //Specify if a bone index is an arm bone
                bool[] IsArmBone = new bool[skinnedMeshRenderer.bones.Length];
                for (int i = 0; i < skinnedMeshRenderer.bones.Length; i++)
                {
                    IsArmBone[i] = armBones.Contains(skinnedMeshRenderer.bones[i]);
                }

                // Get the number of bone weights per vertex 
                // Get all the bone weights, in vertex index order
                // Keep track of where we are in the array of BoneWeights, as we iterate over the vertices
                Unity.Collections.NativeArray<byte> bonesPerVertex = mesh.GetBonesPerVertex();
                Unity.Collections.NativeArray<BoneWeight1> boneWeights = mesh.GetAllBoneWeights();
                int boneWeightIndex = 0;

                // Iterate over the vertices
                bool[] VertexIsArm = new bool[mesh.vertexCount];
                for (int vertIndex = 0; vertIndex < mesh.vertexCount; vertIndex++)
                {
                    // Set vertexIsArm true if all of its bones are arm bones
                    VertexIsArm[vertIndex] = true;
                    for (int i = 0; i < bonesPerVertex[vertIndex]; i++)
                    {
                        // Bone is not an arm bone, set vertexIsArm[vertex] = false. If vertexIsArm[vertex] is already false, skip to avoid unnecessary allocation
                        if (VertexIsArm[vertIndex] && !IsArmBone[boneWeights[boneWeightIndex].boneIndex])
                        {
                            VertexIsArm[vertIndex] = false;
                        }
                        boneWeightIndex++;
                    }
                }

                // Get triangles from GraphicsBuffer
                GraphicsBuffer indexesBuffer = mesh.GetIndexBuffer();
                int tot = indexesBuffer.stride * indexesBuffer.count;
                byte[] indexesData = new byte[tot];
                indexesBuffer.GetData(indexesData);
                int[] triangles = MeshHelper.ConvertByteArrayToIntArray(indexesData, indexesBuffer.stride, indexesBuffer.count);

                // Sort through triangles for armtriangles, keep track of the start and count of tries for each submesh
                Dictionary<int, int> subMeshToIndexStart = new Dictionary<int, int>();
                Dictionary<int, int> subMeshToIndexCount = new Dictionary<int, int>();
                List<int> armtriangles = new List<int>();

                for (int i = 0; i < mesh.subMeshCount; i++)
                {
                    subMeshToIndexStart.Add(i, armtriangles.Count);
                    subMeshToIndexCount.Add(i, 0);
                    SubMeshDescriptor subMeshDesc = mesh.GetSubMesh(i);

                    int indexStart = subMeshDesc.indexStart;
                    int indexCount = subMeshDesc.indexCount;

                    for (int j = indexStart; j < indexStart + indexCount; j += 3)
                    {
                        if (VertexIsArm[triangles[j]] && VertexIsArm[triangles[j + 1]] && VertexIsArm[triangles[j + 2]])
                        {
                            armtriangles.Add(triangles[j]);
                            armtriangles.Add(triangles[j + 1]);
                            armtriangles.Add(triangles[j + 2]);
                            subMeshToIndexCount[i] += 3;
                        }
                    }
                }

                // Convert triangles to byte[], write data to GPU, input new submesh ranges
                byte[] armIndicesData = MeshHelper.ConvertIntArrayToByteArray(armtriangles.ToArray(), indexesBuffer.stride, armtriangles.Count);
                mesh.SetIndexBufferParams(armtriangles.Count, mesh.indexFormat);
                indexesBuffer.SetData(armIndicesData);
                for (int i = 0; i < mesh.subMeshCount; i++)
                {
                    mesh.SetSubMesh(i, new SubMeshDescriptor(subMeshToIndexStart[i], subMeshToIndexCount[i]));
                }

                // Release GraphicsBuffer
                indexesBuffer.Release();

            }
            return TempReplacementViewModel; //Return generated viewmodel
        }



    }
}
