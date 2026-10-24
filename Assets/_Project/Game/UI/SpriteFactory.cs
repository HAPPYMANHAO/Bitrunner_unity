using BitRunner.Core;
using UnityEngine;

namespace BitRunner.Game
{
    public static class SpriteFactory
    {
        public const int CellPixels = 16;

        private static Sprite _cellBackground;
        private static Sprite _white;

        public static Sprite White
        {
            get
            {
                if (_white == null)
                {
                    var texture = new Texture2D(1, 1, TextureFormat.RGBA32, false);
                    texture.filterMode = FilterMode.Point;
                    texture.SetPixel(0, 0, Color.white);
                    texture.Apply();
                    _white = Sprite.Create(texture, new Rect(0, 0, 1, 1), new Vector2(0.5f, 0.5f), 1f, 0, SpriteMeshType.FullRect);
                }

                return _white;
            }
        }

        public static Sprite CellBackground
        {
            get
            {
                if (_cellBackground == null)
                {
                    _cellBackground = CreateCellBackground(CellPixels, new Color32(38, 42, 50, 255), new Color32(60, 66, 78, 255));
                }

                return _cellBackground;
            }
        }

        public static Sprite CreateCellBackground(int size, Color32 fill, Color32 border)
        {
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;

            var pixels = new Color32[size * size];
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    bool edge = x == 0 || y == 0 || x == size - 1 || y == size - 1;
                    pixels[y * size + x] = edge ? border : fill;
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), CellPixels, 0, SpriteMeshType.FullRect);
        }

        public static Sprite CreateShapeSprite(ItemShape shape, Color fill, Color border)
        {
            int width = shape.Width * CellPixels;
            int height = shape.Height * CellPixels;

            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false);
            texture.filterMode = FilterMode.Point;
            texture.wrapMode = TextureWrapMode.Clamp;

            var pixels = new Color32[width * height];
            var transparent = new Color32(0, 0, 0, 0);
            for (int i = 0; i < pixels.Length; i++)
            {
                pixels[i] = transparent;
            }

            Color32 fill32 = fill;
            Color32 border32 = border;

            for (int i = 0; i < shape.Cells.Count; i++)
            {
                Cell cell = shape.Cells[i];
                int startX = cell.X * CellPixels;
                int startY = height - (cell.Y + 1) * CellPixels;

                bool openLeft = !shape.Contains(new Cell(cell.X - 1, cell.Y));
                bool openRight = !shape.Contains(new Cell(cell.X + 1, cell.Y));
                bool openUp = !shape.Contains(new Cell(cell.X, cell.Y - 1));
                bool openDown = !shape.Contains(new Cell(cell.X, cell.Y + 1));

                for (int y = 0; y < CellPixels; y++)
                {
                    for (int x = 0; x < CellPixels; x++)
                    {
                        bool edge = (x == 0 && openLeft)
                                    || (x == CellPixels - 1 && openRight)
                                    || (y == 0 && openDown)
                                    || (y == CellPixels - 1 && openUp);

                        pixels[(startY + y) * width + (startX + x)] = edge ? border32 : fill32;
                    }
                }
            }

            texture.SetPixels32(pixels);
            texture.Apply();

            return Sprite.Create(texture, new Rect(0, 0, width, height), new Vector2(0f, 1f), CellPixels, 0, SpriteMeshType.FullRect);
        }
    }
}
