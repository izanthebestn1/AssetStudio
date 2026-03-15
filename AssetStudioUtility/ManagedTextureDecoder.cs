using System;

namespace AssetStudio
{
    internal static class ManagedTextureDecoder
    {
        private struct Bgra32
        {
            public byte B;
            public byte G;
            public byte R;
            public byte A;
        }

        public static bool DecodeDXT1(byte[] data, int width, int height, byte[] output)
        {
            int blockCountX = (width + 3) >> 2;
            int blockCountY = (height + 3) >> 2;
            int requiredOutput = width * height * 4;
            if (output == null || output.Length < requiredOutput)
            {
                return false;
            }

            for (int by = 0; by < blockCountY; by++)
            {
                for (int bx = 0; bx < blockCountX; bx++)
                {
                    int offset = (by * blockCountX + bx) * 8;
                    if (offset + 8 > data.Length)
                    {
                        return false;
                    }

                    ushort c0 = (ushort)(data[offset] | (data[offset + 1] << 8));
                    ushort c1 = (ushort)(data[offset + 2] | (data[offset + 3] << 8));
                    uint indices = (uint)(data[offset + 4]
                        | (data[offset + 5] << 8)
                        | (data[offset + 6] << 16)
                        | (data[offset + 7] << 24));

                    Bgra32[] colors = BuildDxt1Palette(c0, c1);

                    for (int py = 0; py < 4; py++)
                    {
                        int y = (by << 2) + py;
                        if (y >= height)
                        {
                            continue;
                        }
                        for (int px = 0; px < 4; px++)
                        {
                            int x = (bx << 2) + px;
                            if (x >= width)
                            {
                                continue;
                            }

                            int pixelIndex = py * 4 + px;
                            int colorIndex = (int)((indices >> (pixelIndex * 2)) & 0x3);
                            Bgra32 c = colors[colorIndex];

                            int o = (y * width + x) * 4;
                            output[o] = c.B;
                            output[o + 1] = c.G;
                            output[o + 2] = c.R;
                            output[o + 3] = c.A;
                        }
                    }
                }
            }

            return true;
        }

        public static bool DecodeDXT3(byte[] data, int width, int height, byte[] output)
        {
            int blockCountX = (width + 3) >> 2;
            int blockCountY = (height + 3) >> 2;
            int requiredOutput = width * height * 4;
            if (output == null || output.Length < requiredOutput)
            {
                return false;
            }

            for (int by = 0; by < blockCountY; by++)
            {
                for (int bx = 0; bx < blockCountX; bx++)
                {
                    int offset = (by * blockCountX + bx) * 16;
                    if (offset + 16 > data.Length)
                    {
                        return false;
                    }

                    // 4-bit alpha values packed into 64 bits (little endian)
                    ulong alphaBits = 0;
                    for (int i = 0; i < 8; i++)
                    {
                        alphaBits |= (ulong)data[offset + i] << (8 * i);
                    }

                    ushort c0 = (ushort)(data[offset + 8] | (data[offset + 9] << 8));
                    ushort c1 = (ushort)(data[offset + 10] | (data[offset + 11] << 8));
                    uint indices = (uint)(data[offset + 12]
                        | (data[offset + 13] << 8)
                        | (data[offset + 14] << 16)
                        | (data[offset + 15] << 24));

                    Bgra32[] colors = BuildBc3ColorPalette(c0, c1);

                    for (int py = 0; py < 4; py++)
                    {
                        int y = (by << 2) + py;
                        if (y >= height)
                        {
                            continue;
                        }
                        for (int px = 0; px < 4; px++)
                        {
                            int x = (bx << 2) + px;
                            if (x >= width)
                            {
                                continue;
                            }

                            int pixelIndex = py * 4 + px;
                            int colorIndex = (int)((indices >> (pixelIndex * 2)) & 0x3);
                            int alpha4 = (int)((alphaBits >> (pixelIndex * 4)) & 0xF);

                            Bgra32 c = colors[colorIndex];
                            c.A = (byte)((alpha4 << 4) | alpha4);

                            int o = (y * width + x) * 4;
                            output[o] = c.B;
                            output[o + 1] = c.G;
                            output[o + 2] = c.R;
                            output[o + 3] = c.A;
                        }
                    }
                }
            }

            return true;
        }

        public static bool DecodeDXT5(byte[] data, int width, int height, byte[] output)
        {
            int blockCountX = (width + 3) >> 2;
            int blockCountY = (height + 3) >> 2;
            int requiredOutput = width * height * 4;
            if (output == null || output.Length < requiredOutput)
            {
                return false;
            }

            for (int by = 0; by < blockCountY; by++)
            {
                for (int bx = 0; bx < blockCountX; bx++)
                {
                    int offset = (by * blockCountX + bx) * 16;
                    if (offset + 16 > data.Length)
                    {
                        return false;
                    }

                    byte a0 = data[offset];
                    byte a1 = data[offset + 1];
                    ulong alphaBits = 0;
                    for (int i = 0; i < 6; i++)
                    {
                        alphaBits |= (ulong)data[offset + 2 + i] << (8 * i);
                    }

                    byte[] alphaPalette = BuildDxt5AlphaPalette(a0, a1);

                    ushort c0 = (ushort)(data[offset + 8] | (data[offset + 9] << 8));
                    ushort c1 = (ushort)(data[offset + 10] | (data[offset + 11] << 8));
                    uint colorBits = (uint)(data[offset + 12]
                        | (data[offset + 13] << 8)
                        | (data[offset + 14] << 16)
                        | (data[offset + 15] << 24));

                    Bgra32[] colors = BuildBc3ColorPalette(c0, c1);

                    for (int py = 0; py < 4; py++)
                    {
                        int y = (by << 2) + py;
                        if (y >= height)
                        {
                            continue;
                        }
                        for (int px = 0; px < 4; px++)
                        {
                            int x = (bx << 2) + px;
                            if (x >= width)
                            {
                                continue;
                            }

                            int pixelIndex = py * 4 + px;
                            int colorIndex = (int)((colorBits >> (pixelIndex * 2)) & 0x3);
                            int alphaIndex = (int)((alphaBits >> (pixelIndex * 3)) & 0x7);

                            Bgra32 c = colors[colorIndex];
                            c.A = alphaPalette[alphaIndex];

                            int o = (y * width + x) * 4;
                            output[o] = c.B;
                            output[o + 1] = c.G;
                            output[o + 2] = c.R;
                            output[o + 3] = c.A;
                        }
                    }
                }
            }

            return true;
        }

        public static bool DecodeBC4(byte[] data, int width, int height, byte[] output)
        {
            int blockCountX = (width + 3) >> 2;
            int blockCountY = (height + 3) >> 2;
            int requiredOutput = width * height * 4;
            if (output == null || output.Length < requiredOutput)
            {
                return false;
            }

            for (int by = 0; by < blockCountY; by++)
            {
                for (int bx = 0; bx < blockCountX; bx++)
                {
                    int offset = (by * blockCountX + bx) * 8;
                    if (offset + 8 > data.Length)
                    {
                        return false;
                    }

                    byte[] redPalette = BuildBc4Palette(data[offset], data[offset + 1]);
                    ulong redBits = 0;
                    for (int i = 0; i < 6; i++)
                    {
                        redBits |= (ulong)data[offset + 2 + i] << (8 * i);
                    }

                    for (int py = 0; py < 4; py++)
                    {
                        int y = (by << 2) + py;
                        if (y >= height)
                        {
                            continue;
                        }

                        for (int px = 0; px < 4; px++)
                        {
                            int x = (bx << 2) + px;
                            if (x >= width)
                            {
                                continue;
                            }

                            int pixelIndex = py * 4 + px;
                            int redIndex = (int)((redBits >> (pixelIndex * 3)) & 0x7);
                            byte red = redPalette[redIndex];

                            int o = (y * width + x) * 4;
                            output[o] = 0;
                            output[o + 1] = 0;
                            output[o + 2] = red;
                            output[o + 3] = 255;
                        }
                    }
                }
            }

            return true;
        }

        public static bool DecodeBC5(byte[] data, int width, int height, byte[] output)
        {
            int blockCountX = (width + 3) >> 2;
            int blockCountY = (height + 3) >> 2;
            int requiredOutput = width * height * 4;
            if (output == null || output.Length < requiredOutput)
            {
                return false;
            }

            for (int by = 0; by < blockCountY; by++)
            {
                for (int bx = 0; bx < blockCountX; bx++)
                {
                    int offset = (by * blockCountX + bx) * 16;
                    if (offset + 16 > data.Length)
                    {
                        return false;
                    }

                    byte[] redPalette = BuildBc4Palette(data[offset], data[offset + 1]);
                    ulong redBits = 0;
                    for (int i = 0; i < 6; i++)
                    {
                        redBits |= (ulong)data[offset + 2 + i] << (8 * i);
                    }

                    int greenOffset = offset + 8;
                    byte[] greenPalette = BuildBc4Palette(data[greenOffset], data[greenOffset + 1]);
                    ulong greenBits = 0;
                    for (int i = 0; i < 6; i++)
                    {
                        greenBits |= (ulong)data[greenOffset + 2 + i] << (8 * i);
                    }

                    for (int py = 0; py < 4; py++)
                    {
                        int y = (by << 2) + py;
                        if (y >= height)
                        {
                            continue;
                        }

                        for (int px = 0; px < 4; px++)
                        {
                            int x = (bx << 2) + px;
                            if (x >= width)
                            {
                                continue;
                            }

                            int pixelIndex = py * 4 + px;
                            int redIndex = (int)((redBits >> (pixelIndex * 3)) & 0x7);
                            int greenIndex = (int)((greenBits >> (pixelIndex * 3)) & 0x7);

                            byte red = redPalette[redIndex];
                            byte green = greenPalette[greenIndex];

                            int o = (y * width + x) * 4;
                            output[o] = 0;
                            output[o + 1] = green;
                            output[o + 2] = red;
                            output[o + 3] = 255;
                        }
                    }
                }
            }

            return true;
        }

        private static byte[] BuildBc4Palette(byte e0, byte e1)
        {
            var palette = new byte[8];
            palette[0] = e0;
            palette[1] = e1;

            if (e0 > e1)
            {
                palette[2] = (byte)((6 * e0 + 1 * e1) / 7);
                palette[3] = (byte)((5 * e0 + 2 * e1) / 7);
                palette[4] = (byte)((4 * e0 + 3 * e1) / 7);
                palette[5] = (byte)((3 * e0 + 4 * e1) / 7);
                palette[6] = (byte)((2 * e0 + 5 * e1) / 7);
                palette[7] = (byte)((1 * e0 + 6 * e1) / 7);
            }
            else
            {
                palette[2] = (byte)((4 * e0 + 1 * e1) / 5);
                palette[3] = (byte)((3 * e0 + 2 * e1) / 5);
                palette[4] = (byte)((2 * e0 + 3 * e1) / 5);
                palette[5] = (byte)((1 * e0 + 4 * e1) / 5);
                palette[6] = 0;
                palette[7] = 255;
            }

            return palette;
        }

        private static Bgra32[] BuildDxt1Palette(ushort c0, ushort c1)
        {
            var colors = new Bgra32[4];
            colors[0] = DecodeRgb565(c0);
            colors[1] = DecodeRgb565(c1);

            if (c0 > c1)
            {
                colors[2] = Lerp(colors[0], colors[1], 2, 1, 3);
                colors[3] = Lerp(colors[0], colors[1], 1, 2, 3);
            }
            else
            {
                colors[2] = Lerp(colors[0], colors[1], 1, 1, 2);
                colors[3] = new Bgra32 { B = 0, G = 0, R = 0, A = 0 };
            }

            return colors;
        }

        private static Bgra32[] BuildBc3ColorPalette(ushort c0, ushort c1)
        {
            var colors = new Bgra32[4];
            colors[0] = DecodeRgb565(c0);
            colors[1] = DecodeRgb565(c1);
            colors[2] = Lerp(colors[0], colors[1], 2, 1, 3);
            colors[3] = Lerp(colors[0], colors[1], 1, 2, 3);
            return colors;
        }

        private static byte[] BuildDxt5AlphaPalette(byte a0, byte a1)
        {
            var alpha = new byte[8];
            alpha[0] = a0;
            alpha[1] = a1;

            if (a0 > a1)
            {
                alpha[2] = (byte)((6 * a0 + 1 * a1) / 7);
                alpha[3] = (byte)((5 * a0 + 2 * a1) / 7);
                alpha[4] = (byte)((4 * a0 + 3 * a1) / 7);
                alpha[5] = (byte)((3 * a0 + 4 * a1) / 7);
                alpha[6] = (byte)((2 * a0 + 5 * a1) / 7);
                alpha[7] = (byte)((1 * a0 + 6 * a1) / 7);
            }
            else
            {
                alpha[2] = (byte)((4 * a0 + 1 * a1) / 5);
                alpha[3] = (byte)((3 * a0 + 2 * a1) / 5);
                alpha[4] = (byte)((2 * a0 + 3 * a1) / 5);
                alpha[5] = (byte)((1 * a0 + 4 * a1) / 5);
                alpha[6] = 0;
                alpha[7] = 255;
            }

            return alpha;
        }

        private static Bgra32 DecodeRgb565(ushort value)
        {
            int r = (value >> 11) & 0x1F;
            int g = (value >> 5) & 0x3F;
            int b = value & 0x1F;

            return new Bgra32
            {
                R = (byte)((r << 3) | (r >> 2)),
                G = (byte)((g << 2) | (g >> 4)),
                B = (byte)((b << 3) | (b >> 2)),
                A = 255
            };
        }

        private static Bgra32 Lerp(Bgra32 a, Bgra32 b, int wa, int wb, int div)
        {
            return new Bgra32
            {
                B = (byte)((a.B * wa + b.B * wb) / div),
                G = (byte)((a.G * wa + b.G * wb) / div),
                R = (byte)((a.R * wa + b.R * wb) / div),
                A = (byte)((a.A * wa + b.A * wb) / div),
            };
        }
    }
}
