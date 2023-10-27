using System;
using UnityEngine;
using UnityEngine.UI;

public class BlurImage : MonoBehaviour
{
    private RawImage image;
    private RenderTexture rt;
    private Material material;

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

        rt = new RenderTexture(width, height, 0);
        image = GetComponent<RawImage>();
        image.texture = rt;

        material = new Material(shader);
        material.hideFlags = HideFlags.HideAndDontSave;
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
        Draw(texture, rt, Pass.Copy);

        if (iterations <= 0)
        {
            return;
        }

        var sourceRt = rt;
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

        Draw(tempRTs[^1], rt, Pass.Copy);

        for (int i = 0; i < iterations; i++)
        {
            RenderTexture.ReleaseTemporary(tempRTs[i * 2]);
            RenderTexture.ReleaseTemporary(tempRTs[i * 2 + 1]);
        }
    }

    private void Draw(Texture from, RenderTexture to, Pass pass)
    {
        Graphics.Blit(from, to, material, (int)pass);
    }
}