using System.Collections.Generic;
using BitRunner.Core;
using UnityEngine;
using UnityEngine.UI;

namespace BitRunner.Game
{
    public sealed class BagPrototypeBootstrap : MonoBehaviour
    {
        private static readonly Color BackgroundColor = new Color32(18, 21, 26, 255);
        private static readonly Color PanelColor = new Color32(27, 31, 38, 255);
        private static readonly Color TextColor = new Color32(224, 230, 240, 255);
        private static readonly Color MutedColor = new Color32(130, 140, 158, 255);
        private static readonly Color HintColor = new Color32(96, 104, 120, 255);

        private static Font _font;
        private BagInventory _inventory;

        private void Start()
        {
            _inventory = new BagInventory(6, 5);

            Canvas canvas = CreateCanvas();
            RectTransform bagRect = CreateBagPanel(canvas.transform);
            Text info = CreateSidePanel(canvas.transform);

            var view = gameObject.AddComponent<BagView>();
            view.Initialize(_inventory, bagRect, BuildPalette());

            var interaction = gameObject.AddComponent<BagInteraction>();
            interaction.Initialize(_inventory, view, bagRect, info);

            SeedItems();
            view.Refresh();
        }

        private Canvas CreateCanvas()
        {
            var go = new GameObject("BagCanvas", typeof(Canvas), typeof(CanvasScaler));
            var canvas = go.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;

            var scaler = go.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(640f, 360f);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.Expand;
            scaler.matchWidthOrHeight = 0.5f;

            var background = new GameObject("Background", typeof(RectTransform), typeof(Image));
            background.transform.SetParent(go.transform, false);
            Stretch((RectTransform)background.transform);
            background.GetComponent<Image>().color = BackgroundColor;

            return canvas;
        }

        private static RectTransform CreateBagPanel(Transform parent)
        {
            var panel = new GameObject("BagPanel", typeof(RectTransform), typeof(Image), typeof(RectMask2D));
            panel.transform.SetParent(parent, false);

            var panelRect = (RectTransform)panel.transform;
            panelRect.anchorMin = new Vector2(0.5f, 0.5f);
            panelRect.anchorMax = new Vector2(0.5f, 0.5f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.sizeDelta = new Vector2(6f * BagView.CellSize + 8f, 5f * BagView.CellSize + 8f);
            panelRect.anchoredPosition = new Vector2(-155f, 0f);
            panel.GetComponent<Image>().color = PanelColor;

            var bag = new GameObject("Bag", typeof(RectTransform));
            bag.transform.SetParent(panel.transform, false);

            var bagRect = (RectTransform)bag.transform;
            bagRect.anchorMin = new Vector2(0.5f, 0.5f);
            bagRect.anchorMax = new Vector2(0.5f, 0.5f);
            bagRect.pivot = new Vector2(0.5f, 0.5f);
            bagRect.sizeDelta = new Vector2(6f * BagView.CellSize, 5f * BagView.CellSize);
            bagRect.anchoredPosition = Vector2.zero;

            return bagRect;
        }

        private static Text CreateSidePanel(Transform parent)
        {
            var panel = new GameObject("SidePanel", typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);

            var rect = (RectTransform)panel.transform;
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(250f, 5f * BagView.CellSize + 8f);
            rect.anchoredPosition = new Vector2(150f, 0f);
            panel.GetComponent<Image>().color = PanelColor;

            CreateText(panel.transform, "Title", "BITRUNNER // BAG TEST", 16, TextColor, new Vector2(12f, -10f), new Vector2(226f, 22f));
            Text info = CreateText(panel.transform, "Info", "Hover an item", 12, MutedColor, new Vector2(12f, -40f), new Vector2(226f, 72f));
            CreateText(panel.transform, "Hints", "Left drag: move\nRight click: rotate\nGold = loot, no combat value", 11, HintColor, new Vector2(12f, -118f), new Vector2(226f, 46f));

            return info;
        }

        private static Text CreateText(Transform parent, string name, string content, int size, Color color, Vector2 position, Vector2 sizeDelta)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Text));
            go.transform.SetParent(parent, false);

            var rect = (RectTransform)go.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = position;
            rect.sizeDelta = sizeDelta;

            var text = go.GetComponent<Text>();
            text.font = GetFont();
            text.fontSize = size;
            text.color = color;
            text.text = content;
            text.alignment = TextAnchor.UpperLeft;
            text.horizontalOverflow = HorizontalWrapMode.Wrap;
            text.verticalOverflow = VerticalWrapMode.Overflow;
            text.raycastTarget = false;
            return text;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static Font GetFont()
        {
            return GameFonts.Ui;
        }

        private static Dictionary<string, Color> BuildPalette()
        {
            return new Dictionary<string, Color>
            {
                { "Blade", new Color32(232, 110, 64, 255) },
                { "Rod", new Color32(232, 150, 64, 255) },
                { "Plate", new Color32(80, 140, 220, 255) },
                { "Idol", new Color32(220, 180, 70, 255) },
                { "Coin", new Color32(230, 205, 105, 255) },
                { "Chip", new Color32(90, 200, 130, 255) },
                { "Gem", new Color32(90, 200, 220, 255) }
            };
        }

        private void SeedItems()
        {
            Add("Blade", ItemShape.Parse("##", "#.", "#."), new Cell(0, 0));
            Add("Rod", ItemShape.Parse("#", "#", "#"), new Cell(2, 0));
            Add("Plate", ItemShape.Parse("##", "##"), new Cell(3, 0));
            Add("Idol", ItemShape.Parse("###", ".#."), new Cell(0, 3));
            Add("Coin", ItemShape.Parse("#"), new Cell(3, 2));
            Add("Gem", ItemShape.Parse("#"), new Cell(4, 2));
            Add("Chip", ItemShape.Parse("#"), new Cell(5, 2));
        }

        private void Add(string definitionId, ItemShape shape, Cell origin)
        {
            PlacedItem item;
            string error;
            if (!_inventory.TryAddAt(definitionId, shape, origin, 0, true, out item, out error))
            {
                _inventory.TryAdd(definitionId, shape, out item);
            }
        }
    }
}
