using DSPAddPlanet.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace DSPAddPlanet
{
    /// <summary>
    /// 备注：
    /// left-panel 中的内容在每次加载完毕存档时动态创建
    /// right-panel 中的内容在用户点选 left-panel 中的列表项时动态销毁和重建
    /// </summary>
    class UIAddPlanet : ManualBehaviour
    {
        public Text UniqueStarId { get; set; }

        public Text ExtraPlanetsInfo { get; set; }

        public Text PlanetIndexSummary { get; set; }

        private GameObject goContentLeft = null;

        private GameObject goContentRight = null;

        private GameObject goStarPrefab = null;

        private UIStarmap uiStarmap = null;

        private ScrollRect rightScrollRect = null;

        private string uniqueStarIdXml = "";

        static public UIAddPlanet Create ()
        {
            GameObject goTutorialWindow = UIRoot.instance.transform.Find("Overlay Canvas/In Game/Windows/Tutorial Window").gameObject;
            GameObject goAddPlanet = Instantiate(goTutorialWindow, goTutorialWindow.transform.parent);
            goAddPlanet.name = "Add Planet";

            // 添加 UIAddPlanet 组件
            UIAddPlanet uiAddPlanet = goAddPlanet.AddComponent<UIAddPlanet>();
            uiAddPlanet._Create();

            // 销毁不需要的子对象
            Destroy(goAddPlanet.transform.Find("video-camera").gameObject);
            Destroy(goAddPlanet.transform.Find("video-player").gameObject);

            // 销毁左侧面板中所有的内容
            uiAddPlanet.goContentLeft = goAddPlanet.transform.Find("left-panel/ListView/Mask/Content Panel").gameObject;
            for (int i = 0; i < uiAddPlanet.goContentLeft.transform.childCount; ++i)
            {
                Destroy(uiAddPlanet.goContentLeft.transform.GetChild(i).gameObject);
            }
            GameObject goLeftListView = goAddPlanet.transform.Find("left-panel/ListView").gameObject;
            Destroy(goLeftListView.GetComponent<UIListView>());

            // 左侧显示滚动条
            goLeftListView.GetComponent<ScrollRect>().vertical = true;

            // 销毁右侧面板中所有的内容
            uiAddPlanet.goContentRight = goAddPlanet.transform.Find("right-panel/Scroll View/Viewport/Content").gameObject;
            for (int i = 0; i < uiAddPlanet.goContentRight.transform.childCount; ++i)
            {
                Destroy(uiAddPlanet.goContentRight.transform.GetChild(i).gameObject);
            }
            uiAddPlanet.rightScrollRect = goAddPlanet.transform.Find("right-panel/Scroll View").GetComponent<ScrollRect>();
            if (uiAddPlanet.rightScrollRect != null)
            {
                uiAddPlanet.rightScrollRect.horizontal = false;
                uiAddPlanet.rightScrollRect.vertical = true;
            }

            RectTransform rightContentRect = uiAddPlanet.goContentRight.GetComponent<RectTransform>();
            rightContentRect.Zeroize();
            rightContentRect.anchorMin = new Vector2(0, 1);
            rightContentRect.anchorMax = new Vector2(1, 1);
            rightContentRect.pivot = new Vector2(0.5f, 1);
            rightContentRect.offsetMin = new Vector2(0, -400);
            rightContentRect.offsetMax = Vector2.zero;

            // 删除原有的 UITutorialWindow
            Destroy(goAddPlanet.GetComponent<UITutorialWindow>());

            // 修改标题
            GameObject goTitleText = goAddPlanet.transform.Find("panel-bg/title-text").gameObject;
            Destroy(goTitleText.GetComponent<Localizer>());
            goTitleText.GetComponent<Text>().text = "Add Planet";

            // 添加窗口关闭事件
            Button cmpCloseBtn = goAddPlanet.transform.Find("panel-bg/close-btn").GetComponent<Button>();
            cmpCloseBtn.onClick.RemoveAllListeners();
            cmpCloseBtn.onClick.AddListener(uiAddPlanet._Close);

            // 预先通过 item-prefab 创建 star-prefab
            GameObject goItemPrefab = UIRoot.instance.transform.Find("Overlay Canvas/In Game/Windows/Tutorial Window/left-panel/ListView/Mask/Content Panel/item-prefab").gameObject;
            GameObject goStarPrefab = Instantiate(goItemPrefab, uiAddPlanet.goContentLeft.transform);
            goStarPrefab.name = "star-prefab";
            Destroy(goStarPrefab.GetComponent<UITutorialListEntry>());
            goStarPrefab.GetComponent<Button>().onClick.RemoveAllListeners();
            uiAddPlanet.goStarPrefab = goStarPrefab;

            // 获取 UIStarmap 的引用
            uiAddPlanet.uiStarmap = UIRoot.instance.transform.Find("Overlay Canvas/In Game/Starmap UIs").GetComponent<UIStarmap>();

            // 在右侧内容面板中创建文本组件
            GameObject goUniqueStarId = UIUtility.CreateText("unique-star-id", uiAddPlanet.goContentRight.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(10, -62), new Vector2(-104, -4));
            uiAddPlanet.UniqueStarId = goUniqueStarId.GetComponent<Text>();
            uiAddPlanet.UniqueStarId.alignment = TextAnchor.UpperLeft;
            uiAddPlanet.UniqueStarId.fontSize = 14;
            uiAddPlanet.UniqueStarId.horizontalOverflow = HorizontalWrapMode.Wrap;
            uiAddPlanet.UniqueStarId.verticalOverflow = VerticalWrapMode.Truncate;
            uiAddPlanet.UniqueStarId.resizeTextForBestFit = true;
            uiAddPlanet.UniqueStarId.resizeTextMinSize = 10;
            uiAddPlanet.UniqueStarId.resizeTextMaxSize = 14;

            GameObject goPlanetIndexSummary = UIUtility.CreateText("planet-index-summary", uiAddPlanet.goContentRight.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(10, -88), new Vector2(-10, -66));
            uiAddPlanet.PlanetIndexSummary = goPlanetIndexSummary.GetComponent<Text>();
            uiAddPlanet.PlanetIndexSummary.alignment = TextAnchor.UpperLeft;
            uiAddPlanet.PlanetIndexSummary.fontSize = 12;
            uiAddPlanet.PlanetIndexSummary.color = new Color(0.68f, 0.74f, 0.78f, 1f);

            GameObject goExtraPlanetsInfo = UIUtility.CreateText("extra-planets-info", uiAddPlanet.goContentRight.transform, new Vector2(0, 1), new Vector2(1, 1), new Vector2(10, -400), new Vector2(-10, -94));
            uiAddPlanet.ExtraPlanetsInfo = goExtraPlanetsInfo.GetComponent<Text>();
            uiAddPlanet.ExtraPlanetsInfo.alignment = TextAnchor.UpperLeft;
            uiAddPlanet.ExtraPlanetsInfo.fontSize = 13;
            uiAddPlanet.ExtraPlanetsInfo.lineSpacing = 1.08f;
            uiAddPlanet.ExtraPlanetsInfo.horizontalOverflow = HorizontalWrapMode.Wrap;
            uiAddPlanet.ExtraPlanetsInfo.verticalOverflow = VerticalWrapMode.Overflow;

            // 创建一个复制唯一恒星编号的按钮
            UIUtility.CreateTextButton(
                "Copy XML",
                () =>
                {
                    GUIUtility.systemCopyBuffer = uiAddPlanet.uniqueStarIdXml;
                },
                "copy-unique-star-id",
                uiAddPlanet.goContentRight.transform,
                new Vector2(1, 1),
                new Vector2(1, 1),
                new Vector2(-94, -32),
                new Vector2(-10, -4)
            );

            return uiAddPlanet;
        }

        private void SelectStar (StarData star)
        {
            if (star == null)
            {
                return;
            }

            string starName = string.IsNullOrWhiteSpace(star.name) ? "Unnamed star" : star.name;
            UniqueStarId.text = "Star ID: " + star.id + "\n<StarId>" + star.id + "</StarId>\nStar: " + starName;
            uniqueStarIdXml = BuildUniqueStarIdXml(star.id);

            int planetCount;
            int maxIndex;
            ExtraPlanetsInfo.text = BuildPlanetTreeText(star, out planetCount, out maxIndex);
            PlanetIndexSummary.text = "Planets: " + planetCount
                + "    Max used Index: " + maxIndex
                + "    Next Index: " + (maxIndex + 1);

            RefreshRightContentLayout();
        }

        private string BuildUniqueStarIdXml (int starId)
        {
            string clusterString = GameMain.data?.gameDesc?.clusterString ?? "";
            StringBuilder result = new StringBuilder();
            result.Append("<UniqueStarId>\r\n");
            if (!string.IsNullOrWhiteSpace(GameMain.gameName))
            {
                result.Append("    <GameName>")
                    .Append(EscapeXml(GameMain.gameName))
                    .Append("</GameName>\r\n");
            }
            result.Append("    <ClusterString>")
                .Append(EscapeXml(clusterString))
                .Append("</ClusterString>\r\n")
                .Append("    <StarId>")
                .Append(starId)
                .Append("</StarId>\r\n")
                .Append("</UniqueStarId>");
            return result.ToString();
        }

        private static string EscapeXml (string value)
        {
            return SecurityElement.Escape(value ?? "") ?? "";
        }

        private static string BuildPlanetTreeText (StarData star, out int planetCount, out int maxIndex)
        {
            List<PlanetData> planets = new List<PlanetData>();
            if (star.planets != null)
            {
                foreach (PlanetData planet in star.planets)
                {
                    if (planet != null)
                    {
                        planets.Add(planet);
                    }
                }
            }

            planetCount = planets.Count;
            maxIndex = -1;
            if (planetCount == 0)
            {
                return "No planets.";
            }

            HashSet<PlanetData> planetSet = new HashSet<PlanetData>(planets);
            Dictionary<PlanetData, List<PlanetData>> children = new Dictionary<PlanetData, List<PlanetData>>();
            List<PlanetData> roots = new List<PlanetData>();
            foreach (PlanetData planet in planets)
            {
                maxIndex = Math.Max(maxIndex, planet.index);
                PlanetData parent = planet.orbitAroundPlanet;
                if (parent == null || parent == planet || !planetSet.Contains(parent))
                {
                    roots.Add(planet);
                    continue;
                }

                if (!children.ContainsKey(parent))
                {
                    children[parent] = new List<PlanetData>();
                }
                children[parent].Add(planet);
            }

            roots.Sort(ComparePlanets);
            foreach (List<PlanetData> siblings in children.Values)
            {
                siblings.Sort(ComparePlanets);
            }

            StringBuilder result = new StringBuilder();
            HashSet<PlanetData> visited = new HashSet<PlanetData>();
            foreach (PlanetData root in roots)
            {
                AppendPlanetTree(result, root, children, visited, 0);
            }

            // A malformed cycle has no root. Keep those planets visible instead of silently dropping them.
            planets.Sort(ComparePlanets);
            foreach (PlanetData planet in planets)
            {
                if (!visited.Contains(planet))
                {
                    AppendPlanetTree(result, planet, children, visited, 0);
                }
            }
            return result.ToString().TrimEnd();
        }

        private static int ComparePlanets (PlanetData left, PlanetData right)
        {
            int comparison = left.orbitIndex.CompareTo(right.orbitIndex);
            if (comparison != 0)
            {
                return comparison;
            }
            comparison = left.number.CompareTo(right.number);
            return comparison != 0 ? comparison : left.index.CompareTo(right.index);
        }

        private static void AppendPlanetTree (
            StringBuilder result,
            PlanetData planet,
            Dictionary<PlanetData, List<PlanetData>> children,
            HashSet<PlanetData> visited,
            int depth
        )
        {
            if (!visited.Add(planet))
            {
                return;
            }

            string indent = new string(' ', Math.Min(depth, 10) * 4);
            string detailIndent = indent + "  ";
            string planetName = string.IsNullOrWhiteSpace(planet.name) ? "Planet " + planet.index : planet.name;
            string parentIndex = planet.orbitAroundPlanet == null ? "star" : planet.orbitAroundPlanet.index.ToString();
            result.Append(indent)
                .Append("- [Index=").Append(planet.index).Append("] ").Append(planetName).Append('\n')
                .Append(detailIndent)
                .Append("orbitAround=").Append(planet.orbitAround)
                .Append(" | parentIndex=").Append(parentIndex)
                .Append(" | orbitIndex=").Append(planet.orbitIndex)
                .Append(" | number=").Append(planet.number).Append('\n')
                .Append(detailIndent)
                .Append("gasGiant=").Append(planet.type == EPlanetType.Gas ? "true" : "false")
                .Append(" | infoSeed=").Append(planet.infoSeed)
                .Append(" | genSeed=").Append(planet.seed).Append('\n');

            if (!children.ContainsKey(planet))
            {
                return;
            }
            foreach (PlanetData child in children[planet])
            {
                AppendPlanetTree(result, child, children, visited, depth + 1);
            }
        }

        private void RefreshRightContentLayout ()
        {
            Canvas.ForceUpdateCanvases();
            float preferredHeight = Mathf.Max(ExtraPlanetsInfo.preferredHeight, 20f);
            const float textTop = 94f;
            float contentHeight = Mathf.Max(400f, textTop + preferredHeight + 16f);

            RectTransform extraInfoRect = ExtraPlanetsInfo.rectTransform;
            extraInfoRect.offsetMin = new Vector2(10, -(textTop + preferredHeight));
            extraInfoRect.offsetMax = new Vector2(-10, -textTop);

            RectTransform contentRect = goContentRight.GetComponent<RectTransform>();
            contentRect.offsetMin = new Vector2(0, -contentHeight);
            contentRect.offsetMax = Vector2.zero;
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);

            if (rightScrollRect != null)
            {
                rightScrollRect.StopMovement();
                rightScrollRect.verticalNormalizedPosition = 1f;
            }
        }

        private static void ConfigureStarListLabel (Text label, string text)
        {
            label.text = text;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 10;
            label.resizeTextMaxSize = 14;
        }

        /// <summary>
        /// 打开窗口时，动态生成左侧的恒星列表
        /// 
        /// 备注：左侧列表中列表项的大小和位置似乎不是在这里用 RectTransform 直接控制的
        /// </summary>
        protected override void _OnOpen ()
        {
            // 删除除了 star-prefab 之外的所有子对象
            for (int i = 0; i < goContentLeft.transform.childCount; ++i)
            {
                GameObject child = goContentLeft.transform.GetChild(i).gameObject;
                if (child.name != "star-prefab")
                {
                    Destroy(child);
                }
            }

            // 在最上方添加用户当前聚焦的恒星
            StarData viewStar = uiStarmap.viewStarSystem;
            if (viewStar != null)
            {
                GameObject go = Instantiate(goStarPrefab, goContentLeft.transform);
                go.name = viewStar.name + " (Current)";
                go.SetActive(true);

                ConfigureStarListLabel(
                    go.transform.Find("name-text").GetComponent<Text>(),
                    "[" + viewStar.id + "] " + viewStar.name + " (Current)"
                );

                // 设置列表项的位置
                RectTransform rect = go.GetComponent<RectTransform>();
                rect.Zeroize();
                rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
                rect.offsetMax = new Vector2(226, -4);
                rect.offsetMin = new Vector2(4, -28);

                // 点击事件
                go.GetComponent<Button>().onClick.AddListener(() =>
                {
                    SelectStar(viewStar);
                });
            }
            else
            {
                GameObject go = Instantiate(goStarPrefab, goContentLeft.transform);
                go.name = "N/A";
                go.SetActive(true);

                ConfigureStarListLabel(go.transform.Find("name-text").GetComponent<Text>(), "N/A (Current)");

                // 设置列表项的位置
                RectTransform rect = go.GetComponent<RectTransform>();
                rect.Zeroize();
                rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
                rect.offsetMax = new Vector2(226, -4);
                rect.offsetMin = new Vector2(4, -28);
            }

            // 列出所有恒星
            for (int i = 0; i < GameMain.galaxy.stars.Length; ++i)
            {
                StarData star = GameMain.galaxy.stars[i];

                GameObject go = Instantiate(goStarPrefab, goContentLeft.transform);
                go.name = star.name;
                go.SetActive(true);

                ConfigureStarListLabel(
                    go.transform.Find("name-text").GetComponent<Text>(),
                    "[" + star.id + "] " + star.name
                );

                // 设置列表项的位置
                RectTransform rect = go.GetComponent<RectTransform>();
                rect.Zeroize();
                rect.anchorMin = rect.anchorMax = new Vector2(0, 1);
                rect.offsetMax = new Vector2(226, -4 - (i + 1) * 26);
                rect.offsetMin = new Vector2(4, -28 - (i + 1) * 26);

                // 点击事件
                go.GetComponent<Button>().onClick.AddListener(() =>
                {
                    SelectStar(star);
                });
            }

            // 调整左侧内容区域的高度
            RectTransform rectLeft = goContentLeft.GetComponent<RectTransform>();
            rectLeft.Zeroize();
            rectLeft.anchorMin = rectLeft.anchorMax = new Vector2(0, 1);
            rectLeft.offsetMax = new Vector2(230, 0);
            rectLeft.offsetMin = new Vector2(0, -4 - (GameMain.galaxy.stars.Length + 1) * 26);

            if (viewStar != null)
            {
                SelectStar(viewStar);
            }
        }

        protected override bool _OnInit ()
        {
            return true;
        }
    }
}
