using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace FrameGraph
{
    public class VirtualRenderTarget
    {
        private static readonly PriorityQueue<int>           NameIDPool   = new();
        public static readonly  HashSet<int>                 UsedIDs      = new();
        public static readonly  HashSet<VirtualRenderTarget> AllocatedRTs = new();
        public                  RenderTargetIdentifier       RT        { get; private set; } = default;
        public                  bool                         IsCreated { get; private set; } = false;
        public                  bool                         IsTempRT  { get; private set; } = true;
        public                  RenderTextureDescriptor      desc;
        public                  int                          NameID { get; private set; } = -1;

        public VirtualRenderTarget() { }

        public VirtualRenderTarget(RenderTargetIdentifier rt, RenderTextureDescriptor desc, bool isTempRT = false)
        {
            IsCreated = true;
            IsTempRT  = isTempRT;
            RT        = rt;
            this.desc = desc;
        }

        public RenderTargetIdentifier GetRT()
        {
            if (!IsCreated)
            {
                throw new NullReferenceException("RT is not created");
            }

            return RT;
        }

        public void OnAlloc(CommandBuffer cmd)
        {
            if (!IsTempRT)
            {
                Debug.LogError("Cannot alloc a persistent rt");
                return;
            }

            NameID = GetNewNameID();
            cmd.GetTemporaryRT(NameID, desc);
            RT        = NameID;
            IsCreated = true;
            if (!AllocatedRTs.Add(this))
            {
                Debug.LogWarning("Same VirtualRenderTarget alloc twice!");
            }
        }

        public void OnRelease(CommandBuffer cmd)
        {
            if (!IsTempRT)
            {
                Debug.LogError("Cannot release a persistent rt");
                return;
            }

            cmd.ReleaseTemporaryRT(NameID);
            ReturnNameID(NameID);
            NameID    = -1;
            RT        = default;
            IsCreated = false;
            if (!AllocatedRTs.Remove(this))
            {
                Debug.LogWarning("Allocated VirtualRenderTarget not in record set!");
            }
        }

        private static int TotalNameID = 0;

        private static int GetNewNameID()
        {
            if (NameIDPool.Count == 0)
            {
                for (int i = 0; i < 32; i++)
                {
                    NameIDPool.Enqueue(Shader.PropertyToID($"_FrameGraphTempRT{TotalNameID++}"));
                }

                Debug.Log("Get new nameID batch");
            }

            int id = NameIDPool.Dequeue();
            UsedIDs.Add(id);
            return id;
        }

        private static void ReturnNameID(int id)
        {
            UsedIDs.Remove(id);
            NameIDPool.Enqueue(id);
        }
    }
}