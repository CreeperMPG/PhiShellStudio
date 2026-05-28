using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Media.Imaging;
using ZXing.Common;
using ZXing.QrCode;

namespace PhigrosShellGUI.Services;

/// <summary>QR 码生成工具</summary>
public static class QrCodeHelper
{
    /// <summary>将文本生成为 QR 码 Bitmap</summary>
    public static Bitmap GenerateQrBitmap(string text, int size = 256)
    {
        // 直接用 QRCodeWriter 生成 BitMatrix，避免 BarcodeWriter<T> 的 Renderer 问题
        var writer = new QRCodeWriter();
        var hints = new System.Collections.Generic.Dictionary<ZXing.EncodeHintType, object>
        {
            { ZXing.EncodeHintType.MARGIN, 2 }
        };
        var matrix = writer.encode(text, ZXing.BarcodeFormat.QR_CODE, size, size, hints);

        var bitmap = new WriteableBitmap(
            new PixelSize(size, size),
            new Vector(96, 96),
            Avalonia.Platform.PixelFormat.Bgra8888);

        // 安全代码：用 byte[] + Marshal.Copy 写入帧缓冲
        var bytes = new byte[size * size * 4];

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                int idx = (y * size + x) * 4;
                if (matrix[x, y])
                {
                    // 黑色
                    bytes[idx + 0] = 0;     // B
                    bytes[idx + 1] = 0;     // G
                    bytes[idx + 2] = 0;     // R
                    bytes[idx + 3] = 255;   // A
                }
                else
                {
                    // 白色
                    bytes[idx + 0] = 255;   // B
                    bytes[idx + 1] = 255;   // G
                    bytes[idx + 2] = 255;   // R
                    bytes[idx + 3] = 255;   // A
                }
            }
        }

        using var fb = bitmap.Lock();
        Marshal.Copy(bytes, 0, fb.Address, bytes.Length);

        return bitmap;
    }
}
