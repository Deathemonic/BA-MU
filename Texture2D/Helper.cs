using AssetsTools.NET.Extra;
using AssetsTools.NET.Texture;

namespace BA_MU.Texture2D;

public static class Helper
{
    public static void GetResSTexture(TextureFile texFile, AssetsFileInstance fileInst)
    {
        if (fileInst.parentBundle == null || string.IsNullOrEmpty(texFile.m_StreamData.path)) return;

        texFile.SetPictureDataFromBundle(fileInst.parentBundle);
    }

    public static byte[]? GetRawTextureBytes(TextureFile texFile, AssetsFileInstance inst)
    {
        return texFile.FillPictureData(inst);
    }

    public static bool IsPo2(int n)
    {
        return n > 0 && (n & (n - 1)) == 0;
    }
}