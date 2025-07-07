using AssetsTools.NET.Texture;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Formats.Tga;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Processing;
using System.IO;

namespace BA_MU.Texture2D;

public static class ImportExport
{
    public static byte[]? Import(string imagePath, TextureFormat format, out int width, out int height, ref int mips)
    {
        using var image = Image.Load<Rgba32>(imagePath);
        return Import(image, format, out width, out height, ref mips);
    }

    public static byte[]? Import(Image<Rgba32> image, TextureFormat format, out int width, out int height, ref int mips)
    {
        width = image.Width;
        height = image.Height;

        if (mips > 1 && (width != height || !Helper.IsPo2(width))) mips = 1;

        image.Mutate(i => i.Flip(FlipMode.Vertical));

        var rgbaBytes = new byte[width * height * 4];
        image.CopyPixelDataTo(rgbaBytes);

        var encodedData = TextureFile.EncodeManagedData(rgbaBytes, format, width, height, false);
        return encodedData;
    }

    public static bool Export(byte[] encData, string imagePath, int width, int height, TextureFormat format)
    {
        using var image = Export(encData, width, height, format);
        if (image == null)
            return false;

        SaveImageAtPath(image, imagePath);
        return true;
    }

    public static Image<Rgba32>? Export(byte[] encData, int width, int height, TextureFormat format)
    {
        var decodedData = TextureFile.DecodeManagedData(encData, format, width, height, false);
        if (decodedData == null)
            return null;

        var image = Image.LoadPixelData<Rgba32>(decodedData, width, height);

        image.Mutate(i => i.Flip(FlipMode.Vertical));

        return image;
    }

    private static void SaveImageAtPath(Image<Rgba32>? image, string path)
    {
        var ext = Path.GetExtension(path);

        switch (ext)
        {
            case ".png":
                image.SaveAsPng(path);
                break;
            case ".tga":
            {
                var encoder = new TgaEncoder { BitsPerPixel = TgaBitsPerPixel.Pixel32 };
                image.SaveAsTga(path, encoder);
                break;
            }
        }
    }
}