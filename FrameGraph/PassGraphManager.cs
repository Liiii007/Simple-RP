using System.Collections.Generic;
using SimpleRP.Runtime;
using UnityEngine;
using UnityEngine.Rendering;

namespace FrameGraph
{
    public enum RTUsage
    {
        Create,
        Read,
        Write
    }

    public class PassInfo
    {
        public PassBase                                      pass;
        public List<(VirtualRenderTarget rt, RTUsage usage)> UsedRTs;
        public List<VirtualRenderTarget>                     AllocRTs;
        public List<VirtualRenderTarget>                     ReleaseRTs;

        public PassInfo()
        {
            UsedRTs    = new();
            AllocRTs   = new();
            ReleaseRTs = new();
        }

        public void Reset()
        {
            pass = null;
            UsedRTs.Clear();
            AllocRTs.Clear();
            ReleaseRTs.Clear();
        }
    }

    public class PassLifeCycle
    {
        public PassBase startPass;
        public PassBase endPass;
    }

    public class GraphRuntimeData
    {
        public Dictionary<VirtualRenderTarget, PassLifeCycle> PassLifeCycles = new();
        public List<PassInfo>                                 PassInfos      = new();

        public void Clear()
        {
            PassLifeCycles.Clear();
            PassInfos.Clear();
        }
    }

    public class PassGraphManager
    {
        public static Dictionary<PassGraph, GraphRuntimeData> PassInfos = new();

        public static PassGraph        CurrentGraph     { get; private set; }
        public static GraphRuntimeData CurrentGraphData { get; private set; }
        public static PassInfo         CurrentPassInfo  { get; private set; }
        public static PassBase         CurrentPass      { get; private set; }

        public static void EnterGraph(PassGraph graph, bool clear = false)
        {
            CurrentGraph = graph;

            if (!PassInfos.TryGetValue(graph, out var result))
            {
                result           = new GraphRuntimeData();
                PassInfos[graph] = result;
            }

            if (clear)
            {
                result.PassInfos.Clear();
                result.PassLifeCycles.Clear();
            }

            CurrentGraphData = result;
        }

        public static void SetupRT(PassBase pass, VirtualRenderTarget rt, RTUsage usage)
        {
            switch (usage)
            {
                case RTUsage.Read:
                    SetupReadRT(rt);
                    break;
                case RTUsage.Write:
                    SetupWriteRT(rt);
                    break;
            }
        }

        private static void SetupReadRT(VirtualRenderTarget rt)
        {
            //设置初次分配RT
            if (!CurrentGraphData.PassLifeCycles.TryGetValue(rt, out var cycle))
            {
                cycle                               = new PassLifeCycle();
                CurrentGraphData.PassLifeCycles[rt] = cycle;
                cycle.startPass                     = CurrentPass;
                if (rt.IsTempRT)
                {
                    CurrentPassInfo.AllocRTs.Add(rt);
                }
            }

            cycle.endPass = CurrentPass;
            CurrentPassInfo.UsedRTs.Add((rt, RTUsage.Read));
        }

        private static void SetupWriteRT(VirtualRenderTarget rt)
        {
            //设置初次分配RT
            if (!CurrentGraphData.PassLifeCycles.TryGetValue(rt, out var cycle))
            {
                cycle                               = new PassLifeCycle();
                CurrentGraphData.PassLifeCycles[rt] = cycle;
                cycle.startPass                     = CurrentPass;
                if (rt.IsTempRT)
                {
                    CurrentPassInfo.AllocRTs.Add(rt);
                }
            }

            cycle.endPass = CurrentPass;
            CurrentPassInfo.UsedRTs.Add((rt, RTUsage.Write));
        }

        public static void SetupPasses(PassGraph graph)
        {
            EnterGraph(graph, true);
            foreach (var pass in graph.Travel())
            {
                AddPass(pass);
            }
        }

        private static void AddPass(PassBase pass)
        {
            if (CurrentPass == pass)
            {
                return;
            }

            CurrentPass = pass;
            CurrentPassInfo = new PassInfo()
            {
                pass = pass
            };
            CurrentGraphData.PassInfos.Add(CurrentPassInfo);

            pass.Setup();
        }

        public static void ExecutePasses(PassGraph graph, RenderData data)
        {
            EnterGraph(graph);
            CalcReleaseRT();
            if (!PassInfos.TryGetValue(graph, out var passes))
            {
                Debug.LogError("Cannot find graph's pass");
                return;
            }

            for (int i = 0; i < passes.PassInfos.Count; i++)
            {
                var pass = passes.PassInfos[i].pass;

                AllocRTRuntime(passes.PassInfos[i].AllocRTs, data.cmd);
                pass.Execute(data.context, data.cmd);
                ReleaseRTRuntime(passes.PassInfos[i].ReleaseRTs, data.cmd);
            }
        }

        private static void CalcReleaseRT()
        {
            foreach (var passInfo in CurrentGraphData.PassInfos)
            {
                foreach (var (rt, _) in passInfo.UsedRTs)
                {
                    if (!rt.IsTempRT)
                    {
                        continue;
                    }

                    if (CurrentGraphData.PassLifeCycles[rt].endPass == passInfo.pass)
                    {
                        passInfo.ReleaseRTs.Add(rt);
                    }
                }
            }
        }

        private static void AllocRTRuntime(List<VirtualRenderTarget> vrts, CommandBuffer cmd)
        {
            foreach (var vrt in vrts)
            {
                if (vrt.IsCreated)
                {
                    Debug.LogWarning("Try to create same vrt twice, cancel");
                    return;
                }

                vrt.OnAlloc(cmd);
            }
        }

        private static void ReleaseRTRuntime(List<VirtualRenderTarget> vrts, CommandBuffer cmd)
        {
            foreach (var vrt in vrts)
            {
                if (!vrt.IsCreated)
                {
                    Debug.LogWarning("Try to release uncreated RT, cancel");
                    return;
                }

                if (!vrt.IsTempRT)
                {
                    Debug.LogWarning("Try to release persistent RT, cancel");
                    return;
                }

                vrt.OnRelease(cmd);
            }
        }
    }
}