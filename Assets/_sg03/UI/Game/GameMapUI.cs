using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace SG03.UI
{
    // Controls the simulated battlefield map overlay in scene 2-game.
    // Handles right-mouse-button drag panning, map node generation and selection,
    // synchronization with EnemyCodeNameInput, and toggle visibility from TopMenu.
    public class GameMapUI
    {
        private const float CanvasWidth = 2400f;
        private const float CanvasHeight = 1600f;
        private const float MinZoom = 0.5f;
        private const float MaxZoom = 1.8f;
        private const float DefaultZoom = 1.0f;

        private class MapNodeData
        {
            public string CodeName;
            public string DisplayName;
            public string Badge;
            public string Pin;
            public Vector2 Position;
        }

        private readonly List<MapNodeData> defaultNodes = new List<MapNodeData>
        {
            new MapNodeData
            {
                CodeName = "bastion_blood",
                DisplayName = "Bastion Blood",
                Badge = "Sector 01 • Outpost",
                Pin = "1",
                Position = new Vector2(460f, 820f)
            },
            new MapNodeData
            {
                CodeName = "the_bent_spoon_1",
                DisplayName = "The Bent Spoon",
                Badge = "Sector 02 • Crossroads",
                Pin = "2",
                Position = new Vector2(960f, 520f)
            },
            new MapNodeData
            {
                CodeName = "silas",
                DisplayName = "Silas",
                Badge = "Sector 03 • Shadow Lair",
                Pin = "3",
                Position = new Vector2(1480f, 920f)
            },
            new MapNodeData
            {
                CodeName = "goblin_shaman",
                DisplayName = "Goblin Shaman",
                Badge = "Sector 04 • High Peak",
                Pin = "4",
                Position = new Vector2(1920f, 560f)
            }
        };

        private VisualElement overlay;
        private VisualElement header;
        private VisualElement viewport;
        private VisualElement canvas;
        private VisualElement gridBackground;
        private VisualElement linesContainer;
        private VisualElement nodesContainer;

        private Button btnToggleMap;
        private Button btnCloseMap;
        private Button btnResetMapView;
        private DropdownField enemyCodeNameInput;

        private readonly Dictionary<string, VisualElement> nodeElements = new Dictionary<string, VisualElement>();
        private readonly Dictionary<string, Label> nodeStatusLabels = new Dictionary<string, Label>();

        private bool isPanning;
        private int panPointerId = -1;
        private Vector2 panStartPointer;
        private Vector2 panStartPosition;
        private Vector2 currentPan = new Vector2(-400f, -400f);
        private float currentZoom = DefaultZoom;
        private bool isVisible;
        private bool eventsRegistered;

        public bool IsVisible => this.isVisible;

        public void Bind(VisualElement root)
        {
            if (root == null) return;
            this.BindElements(root);
            this.ConfigurePickingModes();
            this.BuildSectorBackgrounds();
            this.BuildMapEdges();
            this.BuildMapNodes();
            this.RegisterEvents();
            this.SynchronizeInitialSelection();
            this.Show();
        }

        private void ConfigurePickingModes()
        {
            if (this.viewport != null) this.viewport.pickingMode = PickingMode.Position;
            if (this.canvas != null)
            {
                this.canvas.pickingMode = PickingMode.Position;
                this.canvas.style.transformOrigin = new TransformOrigin(0f, 0f);
            }
        }

        private void BindElements(VisualElement root)
        {
            this.btnToggleMap = root.Q<Button>("BtnToggleMap");
            this.overlay = root.Q("GameMapOverlay");
            this.header = root.Q("GameMapHeader");
            this.btnCloseMap = root.Q<Button>("BtnCloseMap");
            this.btnResetMapView = root.Q<Button>("BtnResetMapView");
            this.viewport = root.Q("GameMapViewport");
            this.canvas = root.Q("GameMapCanvas");
            this.gridBackground = root.Q("GameMapGridBackground");
            this.linesContainer = root.Q("GameMapLinesContainer");
            this.nodesContainer = root.Q("GameMapNodesContainer");
            this.enemyCodeNameInput = root.Q<DropdownField>("EnemyCodeNameInput");
        }

        private void RegisterEvents()
        {
            if (this.eventsRegistered) return;
            this.eventsRegistered = true;

            this.btnToggleMap?.RegisterCallback<ClickEvent>(this.OnToggleMapClicked);
            this.btnCloseMap?.RegisterCallback<ClickEvent>(this.OnCloseMapClicked);
            this.btnResetMapView?.RegisterCallback<ClickEvent>(this.OnResetMapViewClicked);

            this.RegisterViewportPanEvents();
            this.RegisterDropdownSyncEvents();
        }

        private void OnToggleMapClicked(ClickEvent evt)
        {
            this.Toggle();
            evt.StopPropagation();
        }

        private void OnCloseMapClicked(ClickEvent evt)
        {
            this.Hide();
            evt.StopPropagation();
        }

        private void RegisterViewportPanEvents()
        {
            if (this.viewport == null) return;
            this.viewport.RegisterCallback<PointerDownEvent>(this.OnPointerDown);
            this.viewport.RegisterCallback<PointerMoveEvent>(this.OnPointerMove);
            this.viewport.RegisterCallback<PointerUpEvent>(this.OnPointerUp);
            this.viewport.RegisterCallback<PointerCaptureOutEvent>(this.OnPointerCaptureOut);
            this.viewport.RegisterCallback<WheelEvent>(this.OnWheel);
        }

        private void RegisterDropdownSyncEvents()
        {
            if (this.enemyCodeNameInput == null) return;
            this.enemyCodeNameInput.RegisterValueChangedCallback(this.OnEnemyDropdownValueChanged);
        }

        public void Toggle()
        {
            if (this.isVisible)
            {
                this.Hide();
            }
            else
            {
                this.Show();
            }
        }

        public void Show()
        {
            if (this.overlay == null || this.isVisible) return;
            this.isVisible = true;
            this.overlay.style.display = DisplayStyle.Flex;
            this.btnToggleMap?.AddToClassList("game-top-menu__map-btn--active");
            this.CenterOnSelectedNode();
            this.viewport?.RegisterCallback<GeometryChangedEvent>(this.OnViewportInitialGeometryChanged);
        }

        private void OnViewportInitialGeometryChanged(GeometryChangedEvent evt)
        {
            this.viewport?.UnregisterCallback<GeometryChangedEvent>(this.OnViewportInitialGeometryChanged);
            if (this.isVisible)
            {
                this.CenterOnSelectedNode();
            }
        }

        public void Hide()
        {
            if (this.overlay == null || !this.isVisible) return;
            this.isVisible = false;
            this.overlay.style.display = DisplayStyle.None;
            this.btnToggleMap?.RemoveFromClassList("game-top-menu__map-btn--active");
            this.StopPanning();
        }

        private void BuildSectorBackgrounds()
        {
            if (this.gridBackground == null) return;
            this.gridBackground.Clear();

            this.AddSectorZone("SectorZone_1", 360f, 700f, 340f, 260f, "SECTOR 01: FRONTIER BASTION");
            this.AddSectorZone("SectorZone_2", 840f, 400f, 360f, 260f, "SECTOR 02: THE BENT CROSSROADS");
            this.AddSectorZone("SectorZone_3", 1360f, 780f, 360f, 280f, "SECTOR 03: SILAS SHADOW SANCTUM");
            this.AddSectorZone("SectorZone_4", 1800f, 440f, 360f, 260f, "SECTOR 04: GOBLIN SHAMAN PEAK");
        }

        private void AddSectorZone(string name, float x, float y, float width, float height, string label)
        {
            VisualElement zone = new VisualElement { name = name };
            zone.AddToClassList("game-map-sector-zone");
            zone.style.left = x;
            zone.style.top = y;
            zone.style.width = width;
            zone.style.height = height;

            Label zoneLabel = new Label(label) { name = name + "_Label" };
            zoneLabel.AddToClassList("game-map-sector-label");
            zone.Add(zoneLabel);

            this.gridBackground.Add(zone);
        }

        private void BuildMapEdges()
        {
            if (this.linesContainer == null) return;
            this.linesContainer.Clear();

            for (int i = 0; i < this.defaultNodes.Count - 1; i++)
            {
                MapNodeData from = this.defaultNodes[i];
                MapNodeData to = this.defaultNodes[i + 1];
                Vector2 fromCenter = from.Position + new Vector2(73f, 45f);
                Vector2 toCenter = to.Position + new Vector2(73f, 45f);
                this.AddEdge($"MapEdge_{from.CodeName}_{to.CodeName}", fromCenter, toCenter);
            }
        }

        private void AddEdge(string name, Vector2 from, Vector2 to)
        {
            VisualElement edge = new VisualElement { name = name };
            edge.AddToClassList("game-map-edge");
            edge.pickingMode = PickingMode.Ignore;

            float length = Vector2.Distance(from, to);
            float angle = Mathf.Atan2(to.y - from.y, to.x - from.x) * Mathf.Rad2Deg;

            edge.style.left = from.x;
            edge.style.top = from.y;
            edge.style.width = length;
            edge.style.height = 3f;
            edge.style.transformOrigin = new TransformOrigin(0f, 1.5f);
            edge.style.rotate = new Rotate(angle);

            this.linesContainer.Add(edge);
        }

        private void BuildMapNodes()
        {
            if (this.nodesContainer == null) return;
            this.nodesContainer.Clear();
            this.nodeElements.Clear();
            this.nodeStatusLabels.Clear();

            List<string> choices = this.enemyCodeNameInput?.choices;
            List<MapNodeData> activeNodeList = this.CollectNodesToDisplay(choices);

            for (int i = 0; i < activeNodeList.Count; i++)
            {
                MapNodeData data = activeNodeList[i];
                this.CreateMapNodeElement(data);
            }
        }

        private List<MapNodeData> CollectNodesToDisplay(List<string> choices)
        {
            List<MapNodeData> list = new List<MapNodeData>(this.defaultNodes);
            if (choices == null) return list;

            for (int i = 0; i < choices.Count; i++)
            {
                string code = choices[i];
                if (list.Exists(n => n.CodeName == code)) continue;

                list.Add(new MapNodeData
                {
                    CodeName = code,
                    DisplayName = this.FormatDisplayName(code),
                    Badge = $"Sector Extra • Lv.{list.Count + 1}",
                    Pin = (list.Count + 1).ToString(),
                    Position = new Vector2(400f + (list.Count * 320f), 700f)
                });
            }

            return list;
        }

        private void CreateMapNodeElement(MapNodeData data)
        {
            VisualElement node = new VisualElement { name = "MapNode_" + data.CodeName };
            node.AddToClassList("game-map-node");
            node.style.left = data.Position.x;
            node.style.top = data.Position.y;

            VisualElement pin = new VisualElement { name = "MapNodePin_" + data.CodeName };
            pin.AddToClassList("game-map-node__pin");

            Label pinIcon = new Label(data.Pin) { name = "MapNodePinIcon_" + data.CodeName };
            pinIcon.AddToClassList("game-map-node__pin-icon");
            pin.Add(pinIcon);
            node.Add(pin);

            Label title = new Label(data.DisplayName) { name = "MapNodeTitle_" + data.CodeName };
            title.AddToClassList("game-map-node__title");
            node.Add(title);

            Label badge = new Label(data.Badge) { name = "MapNodeBadge_" + data.CodeName };
            badge.AddToClassList("game-map-node__badge");
            node.Add(badge);

            Label status = new Label("TARGET SELECTED") { name = "MapNodeStatus_" + data.CodeName };
            status.AddToClassList("game-map-node__status");
            status.style.display = DisplayStyle.None;
            node.Add(status);

            string code = data.CodeName;
            node.RegisterCallback<ClickEvent>(_ => this.OnNodeClicked(code));

            this.nodesContainer.Add(node);
            this.nodeElements[code] = node;
            this.nodeStatusLabels[code] = status;
        }

        private void OnNodeClicked(string enemyCode)
        {
            this.SelectEnemy(enemyCode);
        }

        private void SelectEnemy(string enemyCode)
        {
            if (this.enemyCodeNameInput != null && this.enemyCodeNameInput.value != enemyCode)
            {
                this.enemyCodeNameInput.value = enemyCode;
            }

            this.UpdateSelectedVisual(enemyCode);
        }

        private void OnEnemyDropdownValueChanged(ChangeEvent<string> evt)
        {
            this.UpdateSelectedVisual(evt.newValue);
        }

        private void SynchronizeInitialSelection()
        {
            string current = this.enemyCodeNameInput != null ? this.enemyCodeNameInput.value : "bastion_blood";
            this.UpdateSelectedVisual(current);
        }

        private void UpdateSelectedVisual(string selectedCode)
        {
            foreach (KeyValuePair<string, VisualElement> entry in this.nodeElements)
            {
                bool isSelected = string.Equals(entry.Key, selectedCode, StringComparison.OrdinalIgnoreCase);
                if (isSelected)
                {
                    entry.Value.AddToClassList("game-map-node--selected");
                }
                else
                {
                    entry.Value.RemoveFromClassList("game-map-node--selected");
                }

                if (this.nodeStatusLabels.TryGetValue(entry.Key, out Label statusLabel) && statusLabel != null)
                {
                    statusLabel.style.display = isSelected ? DisplayStyle.Flex : DisplayStyle.None;
                }
            }
        }

        private void OnPointerDown(PointerDownEvent evt)
        {
            // Right mouse button only for panning
            if (evt.button != 1) return;

            this.isPanning = true;
            this.panPointerId = evt.pointerId;
            this.panStartPointer = evt.position;
            this.panStartPosition = this.currentPan;
            this.viewport.CapturePointer(evt.pointerId);
            evt.StopPropagation();
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!this.isPanning || evt.pointerId != this.panPointerId) return;

            Vector2 pointerPos = evt.position;
            Vector2 delta = pointerPos - this.panStartPointer;
            this.currentPan = this.panStartPosition + delta;
            this.ClampAndApplyPan();
            evt.StopPropagation();
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (!this.isPanning || evt.pointerId != this.panPointerId) return;
            if (evt.button != 1) return;

            this.StopPanning();
            evt.StopPropagation();
        }

        private void OnPointerCaptureOut(PointerCaptureOutEvent evt)
        {
            this.StopPanning();
        }

        private void StopPanning()
        {
            if (!this.isPanning) return;
            this.isPanning = false;
            if (this.panPointerId >= 0 && this.viewport != null && this.viewport.HasPointerCapture(this.panPointerId))
            {
                this.viewport.ReleasePointer(this.panPointerId);
            }
            this.panPointerId = -1;
        }

        private void OnWheel(WheelEvent evt)
        {
            Vector2 focalPoint = evt.localMousePosition;
            float zoomFactor = evt.delta.y > 0f ? 0.9f : 1.1f;
            this.ZoomAt(focalPoint, this.currentZoom * zoomFactor);
            evt.StopPropagation();
        }

        public void ZoomAt(Vector2 focalPoint, float targetZoom)
        {
            float newZoom = Mathf.Clamp(targetZoom, MinZoom, MaxZoom);
            if (Mathf.Approximately(newZoom, this.currentZoom)) return;

            Vector2 canvasPoint = (focalPoint - this.currentPan) / this.currentZoom;
            this.currentZoom = newZoom;
            this.currentPan = focalPoint - (canvasPoint * this.currentZoom);
            this.ClampAndApplyPan();
        }

        private void OnResetMapViewClicked(ClickEvent evt)
        {
            this.currentZoom = DefaultZoom;
            this.CenterOnSelectedNode();
            evt.StopPropagation();
        }

        public void CenterOnSelectedNode()
        {
            string selected = this.enemyCodeNameInput != null ? this.enemyCodeNameInput.value : "bastion_blood";
            Vector2 targetPos = new Vector2(CanvasWidth * 0.5f, CanvasHeight * 0.5f);

            MapNodeData found = this.defaultNodes.Find(n => n.CodeName == selected);
            if (found != null)
            {
                targetPos = found.Position + new Vector2(73f, 45f);
            }

            float viewW = this.viewport != null && this.viewport.layout.width > 0 ? this.viewport.layout.width : 1280f;
            float viewH = this.viewport != null && this.viewport.layout.height > 0 ? this.viewport.layout.height : 720f;

            this.currentPan = new Vector2((viewW * 0.5f) - (targetPos.x * this.currentZoom), (viewH * 0.5f) - (targetPos.y * this.currentZoom));
            this.ClampAndApplyPan();
        }

        private void ClampAndApplyPan()
        {
            if (this.canvas == null) return;

            float viewW = this.viewport != null && this.viewport.layout.width > 0 ? this.viewport.layout.width : 1280f;
            float viewH = this.viewport != null && this.viewport.layout.height > 0 ? this.viewport.layout.height : 720f;

            float effectiveW = CanvasWidth * this.currentZoom;
            float effectiveH = CanvasHeight * this.currentZoom;

            float minX = viewW - effectiveW;
            float minY = viewH - effectiveH;

            if (effectiveW < viewW)
            {
                this.currentPan.x = (viewW - effectiveW) * 0.5f;
            }
            else
            {
                this.currentPan.x = Mathf.Clamp(this.currentPan.x, minX, 0f);
            }

            if (effectiveH < viewH)
            {
                this.currentPan.y = (viewH - effectiveH) * 0.5f;
            }
            else
            {
                this.currentPan.y = Mathf.Clamp(this.currentPan.y, minY, 0f);
            }

            this.canvas.style.left = this.currentPan.x;
            this.canvas.style.top = this.currentPan.y;
            this.canvas.style.scale = new Scale(new Vector3(this.currentZoom, this.currentZoom, 1f));
        }

        private string FormatDisplayName(string code)
        {
            if (string.IsNullOrEmpty(code)) return "Enemy";
            string[] words = code.Split('_');
            for (int i = 0; i < words.Length; i++)
            {
                if (words[i].Length > 0)
                {
                    words[i] = char.ToUpperInvariant(words[i][0]) + words[i].Substring(1);
                }
            }
            return string.Join(" ", words);
        }
    }
}
