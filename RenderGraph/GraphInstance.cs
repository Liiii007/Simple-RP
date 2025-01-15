using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace Plugins.SimpleRP.RenderGraph
{
    public class GraphInstance
    {
        public class GraphBuilder
        {
            private readonly GraphInstance _graphInstance;

            public GraphBuilder(GraphInstance graphInstance)
            {
                _graphInstance = graphInstance;
            }

            public void WriteTexture(VirtualTexture texture)
            {
                _graphInstance.WriteTexture(texture);
            }

            public void ReadTexture(VirtualTexture texture)
            {
                _graphInstance.ReadTexture(texture);
            }
        }

        public struct SetupContext
        {
            public VirtualTexture CameraColorBuffer;
        }

        public struct RenderContext
        {
            public ScriptableRenderContext Context;
            public VirtualTexture CameraColorBuffer;
            public CommandBuffer cmd;
        }

        public struct PassContext
        {
            public int Index;
            public Action<GraphBuilder, SetupContext> SetupFunc;
            public Action<RenderContext> DrawFunc;
            public string Name;
        }

        private List<PassContext> Passes = new List<PassContext>();
        private SetupContext _setupContext;
        private RenderContext _renderContext;
        private GraphBuilder _builder;

        public GraphInstance()
        {
            _builder = new(this);
        }

        public void StartNewFrame(SetupContext setupContext, RenderContext renderContext)
        {
            Passes.Clear();
            _renderContext = renderContext;
            _setupContext = setupContext;
        }

        public void AddPass(Action<GraphBuilder, SetupContext> setup, Action<RenderContext> draw,
            bool canCulling = true, string name = "Pass")
        {
            Passes.Add(new PassContext()
            {
                SetupFunc = setup,
                DrawFunc = draw,
                Index = Passes.Count,
                Name = name
            });
            
        }

        private Dictionary<VirtualTexture, int> _firstRead = new Dictionary<VirtualTexture, int>();
        private Dictionary<VirtualTexture, int> _firstWrite = new Dictionary<VirtualTexture, int>();
        private Dictionary<VirtualTexture, int> _lastRead = new Dictionary<VirtualTexture, int>();
        private Dictionary<VirtualTexture, int> _lastWrite = new Dictionary<VirtualTexture, int>();

        private Dictionary<int, List<VirtualTexture>> _allocTextures = new();
        private Dictionary<int, List<VirtualTexture>> _releaseTextures = new();

        private HashSet<VirtualTexture> _allocTextureSet = new();

        private int _currentPassIndex = -1;

        public void Build()
        {
            return;
            _firstRead.Clear();
            _firstWrite.Clear();
            _lastRead.Clear();
            _lastWrite.Clear();

            _allocTextures.Clear();
            _allocTextureSet.Clear();
            _releaseTextures.Clear();

            for (int i = 0; i < Passes.Count; ++i)
            {
                _currentPassIndex = i;
                Passes[i].SetupFunc(_builder, _setupContext);
            }

            foreach (var (rt, passIndex) in _firstWrite)
            {
                if (rt.IsImported)
                {
                    continue;
                }

                if (!_allocTextures.TryGetValue(passIndex, out var textures))
                {
                    textures = new List<VirtualTexture>();
                    _allocTextures[passIndex] = textures;
                }

                textures.Add(rt);
                _allocTextureSet.Add(rt);

                if (!_lastRead.ContainsKey(rt))
                {
                    // TODO:Culling
                    Debug.LogWarning($"TODO:RT in {passIndex} not read at all");
                }
            }

            foreach (var (rt, passIndex) in _firstRead)
            {
                if (rt.IsImported)
                {
                    continue;
                }

                if (!_allocTextures.TryGetValue(passIndex, out var textures))
                {
                    textures = new List<VirtualTexture>();
                    _allocTextures[passIndex] = textures;
                }

                textures.Add(rt);
                _allocTextureSet.Add(rt);

                if (!_firstWrite.TryGetValue(rt, out var firstWritePassIndex) || firstWritePassIndex > passIndex)
                {
                    Debug.LogError("Build Failed:RT在写入之前就已经读取");
                }
            }

            foreach (var (rt, passIndex) in _lastRead)
            {
                if (rt.IsImported)
                {
                    continue;
                }

                if (!_releaseTextures.TryGetValue(passIndex, out var textures))
                {
                    textures = new List<VirtualTexture>();
                    _releaseTextures[passIndex] = textures;
                }

                // 将释放时机放到最后一次写入rt之后
                if (!_lastWrite.TryGetValue(rt, out var lastWritePassIndex) ||  lastWritePassIndex > passIndex)
                {
                    Debug.LogError($"RT Write after last read:{passIndex}, {lastWritePassIndex}");

                    if (!_releaseTextures.TryGetValue(lastWritePassIndex, out textures))
                    {
                        textures = new List<VirtualTexture>();
                        _releaseTextures[passIndex] = textures;
                    }
                }

                textures.Add(rt);
                _allocTextureSet.Remove(rt);
            }
        }

        public void WriteTexture(VirtualTexture texture)
        {
            if (_currentPassIndex == -1)
            {
                throw new InvalidOperationException("Cannot write texture to a non-existing pass");
            }

            _firstWrite.TryAdd(texture, _currentPassIndex);
            _lastWrite[texture] = _currentPassIndex;
        }

        public void ReadTexture(VirtualTexture texture)
        {
            _firstRead.TryAdd(texture, _currentPassIndex);
            _lastRead[texture] = _currentPassIndex;
        }

        public void OnPrePass(int index)
        {
            _renderContext.cmd = CommandBufferPool.Get(Passes[index].Name);  
            _renderContext.cmd.Clear();
            if (_allocTextures.TryGetValue(index, out var textures))
            {
                foreach (var texture in textures)
                {
                    texture.Create();
                }
            }
        }

        public void OnPostPass(int index)
        {
            if (_releaseTextures.TryGetValue(index, out var textures))
            {
                foreach (var texture in textures)
                {
                    texture.ReleaseAndInvalid();
                }
            }

            _renderContext.Context.ExecuteCommandBuffer(_renderContext.cmd);
            CommandBufferPool.Release(_renderContext.cmd);
            _renderContext.cmd = null;
        }

        public void Draw()
        {
            for (int i = 0; i < Passes.Count; i++)
            {
                OnPrePass(i);
                Passes[i].DrawFunc(_renderContext);
                OnPostPass(i);
            }

            // foreach (var allocTexture in _allocTextureSet)
            // {
            //     allocTexture.ReleaseAndInvalid();
            // }
            //
            // _allocTextureSet.Clear();
            //
            // _renderContext.Context.ExecuteCommandBuffer(_renderContext.cmd);
            // _renderContext.cmd.Clear();
            // CommandBufferPool.Release(_renderContext.cmd);
            // _renderContext.cmd = null;
            // _renderContext.Context.Submit();
            
        }
    }
}