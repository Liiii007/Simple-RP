using System;
using UnityEngine;
using UnityEngine.Rendering;

namespace Plugins.SimpleRP.RenderGraph
{
    public class VirtualTexture
    {
        private RTHandle _handle;
        private RenderTextureDescriptor _desc;
        public RenderTargetIdentifier id { get; private set; }

        public bool IsValid { get; private set; }
        public bool IsImported { get; private set; }
        
        public string Name { get; private set; }

        public VirtualTexture(BuiltinRenderTextureType type)
        {
            id = new(type);
            IsValid = true;
            IsImported = true;
            Name = type.ToString();
        }
        
        public VirtualTexture(RenderTextureDescriptor desc, string name = "RT")
        {
            _desc = desc;
            IsImported = false;
            Name = name;
        }

        public void Create()
        {
            if (_handle != null)
            {
                Debug.LogWarning("先前的RT未释放");
            }

            _handle = RTHandles.Alloc(_desc, name:Name);
            id = _handle.nameID;
            IsValid = true;
        }

        public void ReleaseAndInvalid()
        {
            if (_handle == null)
            {
                throw new NullReferenceException("错误调用了尚未初始化的RT");
            }

            if (IsImported)
            {
                throw new ArgumentException("不能释放默认RT");
            }

            RTHandles.Release(_handle);
            _handle = null;
            MarkAsInvalid();
        }

        private void MarkAsInvalid()
        {
            if (_handle != null)
            {
                Debug.LogError("RT尚未释放，但被标记为无效,可能存在泄漏");
            }

            IsValid = false;
            IsImported = false;
        }
    }
}