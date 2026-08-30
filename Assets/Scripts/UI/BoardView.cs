using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Wuziqi.Core;
using Wuziqi.Game;

namespace Wuziqi.UI
{
    /// <summary>棋盘渲染与交互：网格绘制、落子弹跳、悬停预览、胜利连线。</summary>
    [RequireComponent(typeof(Image))]
    public class BoardView : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("引用")]
        [SerializeField] private GameManager gameManager;
        [SerializeField] private Sprite blackStoneSprite;
        [SerializeField] private Sprite whiteStoneSprite;
        [SerializeField] private GameObject winEffectPrefab;

        [Header("样式")]
        [SerializeField] private Color lineColor = new Color(0.23f, 0.22f, 0.20f, 0.92f);
        [SerializeField] private Color lastMarkerColor = new Color(0.76f, 0.15f, 0.16f, 0.95f);
        [SerializeField] private Color winLineColor = new Color(0.76f, 0.15f, 0.16f, 0.5f);
        [SerializeField, Range(0.02f, 0.15f)] private float boardPaddingRatio = 0.055f;
        [SerializeField, Range(0.6f, 1f)] private float stoneSizeRatio = 0.92f;
        [Header("图像网格校准")]
        [SerializeField] private bool autoCalibrateGrid = true;

        private RectTransform boardRect;
        private Canvas canvasRoot;
        private RectTransform gridLayer;
        private RectTransform stoneLayer;
        private RectTransform fxLayer;
        private readonly Dictionary<Vector2Int, Image> stones = new Dictionary<Vector2Int, Image>();
        private Image hoverPreview;
        private Image lastMarker;
        private Image winLine;
        private Sprite circleSprite;
        private float cellSize;
        private float padding;
        private float boardSide;
        private bool pointerInside;
        private float[] gridX; // 校准后的15条竖线局部X（null=未校准）
        private float[] gridY;

        private const int LineThickness = 2;
        private static readonly Vector2Int[] StarPoints =
        {
            new Vector2Int(3, 3), new Vector2Int(11, 3), new Vector2Int(7, 7),
            new Vector2Int(3, 11), new Vector2Int(11, 11),
        };

        private void Awake()
        {
            boardRect = (RectTransform)transform;
            canvasRoot = GetComponentInParent<Canvas>();
            circleSprite = MakeCircleSprite();
            BuildLayers();
        }

        private void Start()
        {
            CacheMetrics();
            if (autoCalibrateGrid) CalibrateFromSprite();
            BuildGrid();
            CreateOverlays();

            if (gameManager == null) gameManager = GameManager.Instance;
            if (gameManager != null)
            {
                gameManager.StonePlaced += OnStonePlaced;
                gameManager.StoneRemoved += OnStoneRemoved;
                gameManager.BoardReset += OnBoardReset;
                gameManager.GameEnded += OnGameEnded;
            }
            ShowPreview(false);
        }

        private void OnDestroy()
        {
            if (gameManager == null) return;
            gameManager.StonePlaced -= OnStonePlaced;
            gameManager.StoneRemoved -= OnStoneRemoved;
            gameManager.BoardReset -= OnBoardReset;
            gameManager.GameEnded -= OnGameEnded;
        }

        private void CacheMetrics()
        {
            boardSide = Mathf.Min(boardRect.rect.width, boardRect.rect.height);
            padding = boardSide * boardPaddingRatio;
            cellSize = (boardSide - 2f * padding) / (GomokuBoard.Size - 1);
        }

        // ---------- 图像网格校准：检测棋盘贴图里的真实网格线位置 ----------

        /// <summary>读取棋盘Image贴图像素，检测15条网格线的实际位置，落子与图中线条精准对齐。</summary>
        private void CalibrateFromSprite()
        {
            var img = GetComponent<Image>();
            if (img == null || img.sprite == null || img.sprite.texture == null) return;
            Texture2D tex = img.sprite.texture;
            if (!tex.isReadable) { Debug.LogWarning("[BoardView] 棋盘贴图未开启Read/Write，跳过校准"); return; }

            int w = tex.width, h = tex.height;
            var px = tex.GetPixels32();

            var vLines = DetectLines(w, h, px, vertical: true);
            var hLines = DetectLines(w, h, px, vertical: false);
            gridX = ToGridPositions(vLines, w, boardSide, "vertical");
            gridY = ToGridPositions(hLines, h, boardSide, "horizontal");

            if (gridX != null && gridY != null)
            {
                // 用实际间距更新 cellSize（供棋子尺寸/胜利连线参考）
                float sumX = 0f;
                for (int i = 1; i < gridX.Length; i++) sumX += gridX[i] - gridX[i - 1];
                float sumY = 0f;
                for (int i = 1; i < gridY.Length; i++) sumY += gridY[i] - gridY[i - 1];
                cellSize = ((sumX / (gridX.Length - 1)) + (sumY / (gridY.Length - 1))) * 0.5f;
                padding = gridX[0] + boardSide * 0.5f;
                Debug.Log($"[BoardView] 图像网格校准成功 v={vLines.Count}线 h={hLines.Count}线 cellSize={cellSize:F1}");
            }
            else
            {
                gridX = gridY = null;
                Debug.LogWarning("[BoardView] 网格线检测失败，回退均匀网格（代码绘制）");
            }
        }

        /// <summary>扫描暗像素密度找网格线中心位置（像素坐标）。</summary>
        private static List<int> DetectLines(int w, int h, Color32[] px, bool vertical)
        {
            var raw = new List<int>();
            if (vertical)
            {
                int y0 = (int)(h * 0.35f), y1 = (int)(h * 0.65f);
                for (int x = 0; x < w; x++)
                {
                    int dark = 0, n = 0;
                    for (int y = y0; y < y1; y += 3) { n++; if (px[y * w + x].r < 115) dark++; }
                    if (dark / (float)n > 0.55f) raw.Add(x);
                }
            }
            else
            {
                int x0 = (int)(w * 0.35f), x1 = (int)(w * 0.65f);
                for (int y = 0; y < h; y++)
                {
                    int dark = 0, n = 0;
                    for (int x = x0; x < x1; x += 3) { n++; if (px[y * w + x].r < 115) dark++; }
                    if (dark / (float)n > 0.55f) raw.Add(y);
                }
            }

            // 聚类：相邻≤6px归为一条线
            var lines = new List<int>();
            int i = 0;
            while (i < raw.Count)
            {
                int j = i;
                while (j + 1 < raw.Count && raw[j + 1] - raw[j] <= 6) j++;
                lines.Add((raw[i] + raw[j]) / 2);
                i = j + 1;
            }
            // 合并疑似重复线（间距<25px）
            for (int k = lines.Count - 2; k >= 0; k--)
                if (lines[k + 1] - lines[k] < 25) { lines[k] = (lines[k] + lines[k + 1]) / 2; lines.RemoveAt(k + 1); }
            // 补漏线：淡线漏检时，把≈2倍中位数的空档从中点补齐
            while (lines.Count >= 11 && lines.Count < 15)
            {
                var gaps = new List<float>();
                for (int k = 1; k < lines.Count; k++) gaps.Add(lines[k] - lines[k - 1]);
                gaps.Sort();
                float median = gaps[gaps.Count / 2];
                int bigIdx = -1; float bigGap = 0f;
                for (int k = 1; k < lines.Count; k++)
                {
                    float g = lines[k] - lines[k - 1];
                    if (g > bigGap) { bigGap = g; bigIdx = k - 1; }
                }
                if (bigGap < median * 1.6f) break; // 没有显著空档，无法补
                lines.Insert(bigIdx + 1, lines[bigIdx] + Mathf.RoundToInt(bigGap * 0.5f));
            }
            return lines;
        }

        /// <summary>从检测到的线集合取中央15条，转成棋盘局部坐标；失败返回null。</summary>
        private static float[] ToGridPositions(List<int> lines, int texSize, float boardSide, string axis)
        {
            if (lines == null || lines.Count < 15) return null;
            int start = (lines.Count - 15) / 2; // 15线取全部；19线取中央15；17线取中央15
            var res = new float[15];
            for (int i = 0; i < 15; i++)
                res[i] = (lines[start + i] / (float)texSize - 0.5f) * boardSide;
            // 合理性检查：首尾跨度需占棋盘60%以上
            float span = Mathf.Abs(res[14] - res[0]);
            if (span < boardSide * 0.6f) return null;
            return res;
        }

        private Vector2 CellToLocal(int x, int y)
        {
            if (gridX != null && gridY != null)
                return new Vector2(gridX[x], gridY[y]);
            float o = -boardSide * 0.5f + padding;
            return new Vector2(o + x * cellSize, o + y * cellSize);
        }

        private void BuildLayers()
        {
            gridLayer = CreateLayer("Grid");
            stoneLayer = CreateLayer("Stones");
            fxLayer = CreateLayer("FX");
        }

        private RectTransform CreateLayer(string name)
        {
            GameObject go = new GameObject(name);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.SetParent(boardRect, false);
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
            return rt;
        }

        private void BuildGrid()
        {
            if (gridX != null && gridY != null) return; // 图像自带网格线，无需代码绘制
            float span = cellSize * (GomokuBoard.Size - 1);
            for (int i = 0; i < GomokuBoard.Size; i++)
            {
                bool edge = i == 0 || i == GomokuBoard.Size - 1;
                float t = edge ? LineThickness + 1.5f : LineThickness;
                CreateRect(gridLayer, $"V{i}", CellToLocal(i, 0).x, 0f, new Vector2(t, span), lineColor);
                CreateRect(gridLayer, $"H{i}", 0f, CellToLocal(0, i).y, new Vector2(span, t), lineColor);
            }
            foreach (Vector2Int sp in StarPoints)
                CreateRect(gridLayer, $"Star{sp.x}_{sp.y}", CellToLocal(sp.x, sp.y), new Vector2(9, 9), lineColor, circleSprite);
        }

        private void CreateOverlays()
        {
            hoverPreview = CreateRect(fxLayer, "HoverPreview", Vector2.zero, StonePixelSize(), Color.clear);
            hoverPreview.gameObject.SetActive(false);
            lastMarker = CreateRect(fxLayer, "LastMarker", Vector2.zero, Vector2.one * (cellSize * 0.22f), lastMarkerColor, circleSprite);
            lastMarker.gameObject.SetActive(false);
            winLine = CreateRect(fxLayer, "WinLine", Vector2.zero, Vector2.zero, winLineColor, circleSprite);
            winLine.gameObject.SetActive(false);
        }

        private Vector2 StonePixelSize() => Vector2.one * (cellSize * stoneSizeRatio);

        private Image CreateRect(Transform parent, string name, Vector2 pos, Vector2 size, Color color, Sprite sprite = null)
        {
            return CreateRect(parent, name, pos.x, pos.y, size, color, sprite);
        }

        private Image CreateRect(Transform parent, string name, float x, float y, Vector2 size, Color color, Sprite sprite = null)
        {
            GameObject go = new GameObject(name);
            RectTransform rt = go.AddComponent<RectTransform>();
            rt.SetParent(parent, false);
            rt.anchoredPosition = new Vector2(x, y);
            rt.sizeDelta = size;
            Image img = go.AddComponent<Image>();
            img.sprite = sprite;
            img.color = color;
            img.raycastTarget = false;
            return img;
        }

        private Sprite GetStoneSprite(StoneColor color)
        {
            Sprite s = color == StoneColor.Black ? blackStoneSprite : whiteStoneSprite;
            return s != null ? s : circleSprite;
        }

        // ---------- 事件处理 ----------

        private void OnStonePlaced(Vector2Int cell, StoneColor color)
        {
            Image stone = CreateStone(cell, color);
            StartCoroutine(PopIn(stone.rectTransform));
            lastMarker.rectTransform.anchoredPosition = CellToLocal(cell.x, cell.y);
            lastMarker.gameObject.SetActive(true);
            ShowPreview(false);
        }

        private Image CreateStone(Vector2Int cell, StoneColor color)
        {
            Image img = CreateRect(stoneLayer, $"Stone_{cell.x}_{cell.y}", CellToLocal(cell.x, cell.y), StonePixelSize(), Color.white);
            Sprite s = GetStoneSprite(color);
            img.sprite = s;
            if (s == circleSprite)
                img.color = color == StoneColor.Black
                    ? new Color(0.13f, 0.12f, 0.11f)
                    : new Color(0.97f, 0.96f, 0.93f);
            stones[cell] = img;
            return img;
        }

        private void OnStoneRemoved(Vector2Int cell)
        {
            if (stones.TryGetValue(cell, out Image img))
            {
                Destroy(img.gameObject);
                stones.Remove(cell);
            }
            winLine.gameObject.SetActive(false);

            var h = gameManager.Board.History;
            if (h.Count > 0)
            {
                var last = h[h.Count - 1];
                lastMarker.rectTransform.anchoredPosition = CellToLocal(last.X, last.Y);
                lastMarker.gameObject.SetActive(true);
            }
            else lastMarker.gameObject.SetActive(false);
        }

        private void OnBoardReset()
        {
            foreach (Image img in stones.Values)
                if (img != null) Destroy(img.gameObject);
            stones.Clear();
            winLine.gameObject.SetActive(false);
            lastMarker.gameObject.SetActive(false);
        }

        private void OnGameEnded(GameResult result, IReadOnlyList<Vector2Int> line)
        {
            ShowPreview(false);
            if (line != null && line.Count >= 5)
            {
                DrawWinLine(line);
                StartCoroutine(PulseStones(line));
            }

            bool playerWon = (result == GameResult.BlackWin && gameManager.playerColor == StoneColor.Black)
                          || (result == GameResult.WhiteWin && gameManager.playerColor == StoneColor.White);
            if (playerWon) PlayWinEffect();
        }

        /// <summary>在世界空间棋盘上方播撒纸花（画布为 Camera 模式，特效放在画布前方）。</summary>
        private void PlayWinEffect()
        {
            if (winEffectPrefab == null) return;
            var cam = canvasRoot != null && canvasRoot.worldCamera != null ? canvasRoot.worldCamera : Camera.main;
            if (cam == null) return;

            Vector3 boardWorld;
            Vector3[] corners = new Vector3[4];
            boardRect.GetWorldCorners(corners);
            boardWorld = (corners[0] + corners[2]) * 0.5f;

            // 画布平面距相机 planeDistance；把特效放到画布前方 3 个单位处
            Vector3 camPos = cam.transform.position;
            Vector3 dir = cam.transform.forward;
            float distToBoard = Vector3.Dot(boardWorld - camPos, dir);
            Vector3 pos = camPos + dir * (distToBoard - 3f);
            pos += Vector3.up * 1.2f; // 纸花从棋盘上方开始飘落

            GameObject fx = Instantiate(winEffectPrefab, pos, Quaternion.LookRotation(dir));
            fx.transform.localScale = Vector3.one * 2f;
            var ps = fx.GetComponent<ParticleSystem>();
            if (ps != null)
            {
                ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = ps.main;
                main.loop = false;
                main.duration = 2.5f;
                main.playOnAwake = true;
                main.stopAction = ParticleSystemStopAction.Destroy;
                ps.Play();
            }
            Destroy(fx, 5f);
        }

        // ---------- 动画 ----------

        private static IEnumerator PopIn(RectTransform rt)
        {
            const float dur = 0.14f;
            float t = 0f;
            while (t < dur && rt != null)
            {
                t += Time.deltaTime;
                float k = 1f - Mathf.Clamp01(t / dur);
                float s = 1f + 0.28f * k * k;
                rt.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            if (rt != null) rt.localScale = Vector3.one;
        }

        private IEnumerator PulseStones(IReadOnlyList<Vector2Int> line)
        {
            var targets = new List<RectTransform>();
            foreach (Vector2Int p in line)
                if (stones.TryGetValue(p, out Image img) && img != null) targets.Add(img.rectTransform);

            float t = 0f;
            while (t < 2.2f)
            {
                t += Time.deltaTime;
                float s = 1f + 0.07f * Mathf.Abs(Mathf.Sin(t * 6f));
                foreach (RectTransform rt in targets)
                    if (rt != null) rt.localScale = new Vector3(s, s, 1f);
                yield return null;
            }
            foreach (RectTransform rt in targets)
                if (rt != null) rt.localScale = Vector3.one;
        }

        private void DrawWinLine(IReadOnlyList<Vector2Int> line)
        {
            Vector2 a = CellToLocal(line[0].x, line[0].y);
            Vector2 b = CellToLocal(line[line.Count - 1].x, line[line.Count - 1].y);
            Vector2 mid = (a + b) * 0.5f;
            float length = Vector2.Distance(a, b) + cellSize * 0.45f;
            float angle = Mathf.Atan2(b.y - a.y, b.x - a.x) * Mathf.Rad2Deg;
            winLine.rectTransform.anchoredPosition = mid;
            winLine.rectTransform.sizeDelta = new Vector2(length, cellSize * 0.38f);
            winLine.rectTransform.localEulerAngles = new Vector3(0f, 0f, angle);
            winLine.gameObject.SetActive(true);
        }

        // ---------- 输入 ----------

        public void OnPointerClick(PointerEventData eventData)
        {
            if (gameManager == null || !gameManager.CanPlayerPlaceNow) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(boardRect, eventData.position, eventData.pressEventCamera, out Vector2 local)) return;
            if (!TryCellFromLocal(local, out Vector2Int cell)) return;
            gameManager.TryPlayerPlace(cell.x, cell.y);
        }

        public void OnPointerEnter(PointerEventData eventData) => pointerInside = true;

        public void OnPointerExit(PointerEventData eventData)
        {
            pointerInside = false;
            ShowPreview(false);
        }

        private void Update()
        {
            if (!pointerInside || gameManager == null || !gameManager.CanPlayerPlaceNow) return;
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(boardRect, Input.mousePosition, null, out Vector2 local)) return;
            if (!TryCellFromLocal(local, out Vector2Int cell) || !gameManager.Board.IsEmpty(cell.x, cell.y))
            {
                ShowPreview(false);
                return;
            }
            hoverPreview.rectTransform.anchoredPosition = CellToLocal(cell.x, cell.y);
            ShowPreview(true);
        }

        private bool TryCellFromLocal(Vector2 local, out Vector2Int cell)
        {
            if (gridX != null && gridY != null)
            {
                int cx = NearestIndex(gridX, local.x);
                int cy = NearestIndex(gridY, local.y);
                cell = new Vector2Int(cx, cy);
                float tolX = MaxGap(gridX) * 0.55f;
                float tolY = MaxGap(gridY) * 0.55f;
                return Mathf.Abs(local.x - gridX[cx]) <= tolX
                    && Mathf.Abs(local.y - gridY[cy]) <= tolY;
            }
            float o = -boardSide * 0.5f + padding;
            int ux = Mathf.RoundToInt((local.x - o) / cellSize);
            int uy = Mathf.RoundToInt((local.y - o) / cellSize);
            cell = new Vector2Int(ux, uy);
            return ux >= 0 && ux < GomokuBoard.Size && uy >= 0 && uy < GomokuBoard.Size;
        }

        private static int NearestIndex(float[] arr, float v)
        {
            int best = 0;
            float bd = Mathf.Abs(v - arr[0]);
            for (int i = 1; i < arr.Length; i++)
            {
                float d = Mathf.Abs(v - arr[i]);
                if (d < bd) { bd = d; best = i; }
            }
            return best;
        }

        private static float MaxGap(float[] arr)
        {
            float m = 0f;
            for (int i = 1; i < arr.Length; i++) m = Mathf.Max(m, arr[i] - arr[i - 1]);
            return m;
        }

        private void ShowPreview(bool show)
        {
            if (show && gameManager != null)
            {
                StoneColor c = gameManager.playerColor;
                Sprite s = GetStoneSprite(c);
                hoverPreview.sprite = s;
                hoverPreview.color = s == circleSprite
                    ? (c == StoneColor.Black
                        ? new Color(0.13f, 0.12f, 0.11f, 0.45f)
                        : new Color(0.97f, 0.96f, 0.93f, 0.6f))
                    : new Color(1f, 1f, 1f, 0.45f);
            }
            hoverPreview.gameObject.SetActive(show);
        }

        
        // ========== Replay API ==========

        /// <summary>
        /// Place a stone directly (for replay).
        /// </summary>
        public void PlaceStoneDirect(int x, int y, StoneColor color)
        {
            Vector2Int cell = new Vector2Int(x, y);
            if (stones.ContainsKey(cell)) return;
            OnStonePlaced(cell, color);
        }

        /// <summary>
        /// Remove the last stone placed (for replay step back).
        /// </summary>
        public void RemoveLastStone()
        {
            if (stones.Count == 0) return;
            KeyValuePair<Vector2Int, Image> last = default;
            foreach (var kvp in stones) last = kvp;
            if (last.Value != null) Destroy(last.Value.gameObject);
            stones.Remove(last.Key);
            UpdateLastMarker();
        }

        /// <summary>
        /// Clear all stones (for replay reset).
        /// </summary>
        public void ClearAllStones()
        {
            foreach (var kvp in stones)
                if (kvp.Value != null) Destroy(kvp.Value.gameObject);
            stones.Clear();
            if (lastMarker != null) lastMarker.gameObject.SetActive(false);
            if (winLine != null) winLine.gameObject.SetActive(false);
        }

        private void UpdateLastMarker()
        {
            if (lastMarker == null) return;
            if (stones.Count == 0) { lastMarker.gameObject.SetActive(false); return; }
            KeyValuePair<Vector2Int, Image> last = default;
            foreach (var kvp in stones) last = kvp;
            lastMarker.rectTransform.anchoredPosition = CellToLocal(last.Key.x, last.Key.y);
            lastMarker.gameObject.SetActive(true);
        }
        // ---------- 工具 ----------

        private static Sprite MakeCircleSprite()
        {
            const int size = 64;
            Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
            tex.wrapMode = TextureWrapMode.Clamp;
            float c = (size - 1) * 0.5f;
            float r = c - 1.5f;
            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float d = Mathf.Sqrt((x - c) * (x - c) + (y - c) * (y - c));
                    float a = d <= r - 1.5f ? 1f : (d <= r ? 1f - (d - (r - 1.5f)) / 1.5f : 0f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, a));
                }
            }
            tex.Apply();
            return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
        }
    }
}
