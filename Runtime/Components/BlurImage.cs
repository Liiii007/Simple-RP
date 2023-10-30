using UnityEngine;
using UnityEngine.UI;

namespace SimpleRP.Runtime.Components
{
    public class BlurImage : MonoBehaviour
    {
        private RawImage _image;
        private RenderTexture _rt;
        private Material _material;

        public Texture texture;
        public Shader shader;

        public bool refreshFlag = true;
        public int iterations;

        enum Pass
        {
            Copy,
            Horizontal,
            Vertical
        }

        private void Awake()
        {
            int width = 600;
            int height = 400;

            _rt = new RenderTexture(width, height, 0);
            _image = GetComponent<RawImage>();
            _image.texture = _rt;

            _material = new Material(shader);
            _material.hideFlags = HideFlags.HideAndDontSave;
        }

        private void Update()
        {
            if (!refreshFlag)
            {
                return;
            }

            refreshFlag = false;

            int width = 600;
            int height = 400;

            //Copy image texture to rt
            Draw(texture, _rt, Pass.Copy);

            if (iterations <= 0)
            {
                return;
            }

            var sourceRt = _rt;
            var tempRTs = new RenderTexture[iterations * 2];
            for (int i = 0; i < iterations; i++)
            {
                width /= 2;
                height /= 2;

                int horRt = i * 2;
                int verRt = i * 2 + 1;
                tempRTs[horRt] = RenderTexture.GetTemporary(width, height, 0);
                tempRTs[verRt] = RenderTexture.GetTemporary(width, height, 0);

                Draw(sourceRt, tempRTs[horRt], Pass.Horizontal);
                Draw(tempRTs[horRt], tempRTs[verRt], Pass.Vertical);
                sourceRt = tempRTs[verRt];
            }

            Draw(tempRTs[^1], _rt, Pass.Copy);

            for (int i = 0; i < iterations; i++)
            {
                RenderTexture.ReleaseTemporary(tempRTs[i * 2]);
                RenderTexture.ReleaseTemporary(tempRTs[i * 2 + 1]);
            }
        }

        private void Draw(Texture from, RenderTexture to, Pass pass)
        {
            Graphics.Blit(from, to, _material, (int)pass);
        }
    }
}