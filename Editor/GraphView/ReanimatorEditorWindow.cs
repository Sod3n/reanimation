namespace Aarthificial.Reanimation.Editor.GraphView
{
    using System.Collections;
    using System.Collections.Generic;
    using Aarthificial.Reanimation.Nodes;
    using UnityEngine;
    using UnityEditor;
    using UnityEngine.UIElements;
    using UnityEditor.UIElements;
    using UnityEditor.Experimental.GraphView;

    public class ReanimatorEditorWindow : EditorWindow
    {
        private ReanimatorGraphView graphView;
        private VisualElement helpMenu;
        private Button addReanimatorButton;
        private VisualElement toolbar;

        private SwitchNode rootNode;
        private bool isAnimationNodesHidden = false;
        VisualElement root => rootVisualElement;

        [MenuItem("Window/Reanimator/Reanimator Graph")]
        public static void OpenReanimatorGraph()
        {
            var window = GetWindow<ReanimatorEditorWindow>();
            window.titleContent = new GUIContent(text: "Reanimator Graph");
        }

        public void CreateGUI()
        {
            var visualTree = Resources.Load<VisualTreeAsset>("GraphEditorWindow");
            visualTree.CloneTree(root);

            GetGraphView();
            GenerateToolbar();
            OnSelectionChange();
        }
        private void Show(VisualElement visualElement)
        {
            visualElement.style.display = DisplayStyle.Flex;
        }
        private void HideAll()
        {
            helpMenu.style.display = DisplayStyle.None;
            graphView.style.display = DisplayStyle.None;
        }

        private void GenerateToolbar()
        {
            var toggleCollapse = new Button(clickEvent: ToggleHideAnimationNodes);
            toggleCollapse.text = "Hide Animation Nodes";
            toggleCollapse.name = "toggle-button";
            toolbar.Add(toggleCollapse);
        }
        private void AddReanimator()
        {
            GameObject gameObject = Selection.activeGameObject;
            if (gameObject == null) return;
            if(gameObject.GetComponent<Reanimator>() != null) return;
            Reanimator reanimator = gameObject.AddComponent<Reanimator>();
            reanimator.root = CreateRootNode();
            OpenSelectedObject();
        }
        private SwitchNode CreateRootNode()
        {
            var node = ScriptableObject.CreateInstance<SwitchNode>();
            node.name = "RootSwitch";
            RGVIOUtility.SaveNode<SwitchNode>(node);
            return node;
        }
        private void ToggleHideAnimationNodes()
        {
            isAnimationNodesHidden = !isAnimationNodesHidden;
            root.Q<Button>("toggle-button").text = isAnimationNodesHidden
                ? "Hide Animation Nodes"
                : "Show Animation Nodes";
            //graphView.ToggleAnimationNodes();
        }

        private void OnSelectionChange()
        {
            OpenSelectedObject();
            if (graphView.SelectedReanimator != null)
            {
                Show(graphView);
            }
        }

        private void OpenSelectedObject()
        {
            if (!Selection.activeGameObject) return;
            HideAll();
            Reanimator reanimator = Selection.activeGameObject.GetComponent<Reanimator>();
            graphView.RemoveAllGraphElements();
            if (reanimator != null)
            {
                graphView.SelectedReanimator = reanimator;
                graphView.Generate();
                Show(graphView);
            }
            else
            {
                graphView.SelectedReanimator = null;
                Show(helpMenu);
            }
        }

        void HandleNodeSelect(ReanimatorNodeView nodeView)
        {
            Selection.activeObject = nodeView.Node;
        }
        void HandleNodeUnSelect(ReanimatorNodeView nodeView)
        {
            if (Selection.activeObject == nodeView.Node)
            {
                Selection.activeObject = null;
            }
        }

        GraphViewChange HandleGraphViewChange(GraphViewChange graphViewChange)
        {
            if (graphViewChange.movedElements != null)
            {
                foreach (var graphElement in graphViewChange.movedElements)
                {
                    var nodeView = graphElement as ReanimatorNodeView;
                    if (nodeView == null)
                        continue;
                    var node = nodeView.Node;
                    node.Position = nodeView.GetPosition().position;
                }
            }
            AssetDatabase.SaveAssets();
            return graphViewChange;
        }

        void GetGraphView()
        {
            graphView = root.Q<ReanimatorGraphView>();
            helpMenu = root.Q("HelpMenu");
            toolbar = root.Q<Toolbar>();
            addReanimatorButton = root.Q<Button>("HelpButton");
            Subscribe();
        }

        private void OnPlayModeStateChanged(PlayModeStateChange state)
        {
            // Re-establish Graph View to work in Edit Mode
            if (state == PlayModeStateChange.EnteredEditMode)
            {
                GetGraphView();
            }
        }

        private void OnEnable()
        {
            GetGraphView();
            if (graphView != null)
            {
                graphView.Generate(rootNode);
            }
        }

        void OnDisable()
        {
            Unsubscribe();
        }

        public void Subscribe()
        {
            // Play it safe!
            Unsubscribe();

            // Nobody else than us should listen to these evens (therefor =)
            if (graphView != null)
            {
                graphView.OnNodeSelected = HandleNodeSelect;
                graphView.OnNodeUnSelected = HandleNodeUnSelect;
                graphView.graphViewChanged = HandleGraphViewChange;
            }
            if(helpMenu != null)
            {
                addReanimatorButton.clicked += AddReanimator;
            }
            EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        }

        // Unsubcribe from event
        public void Unsubscribe()
        {
            if (graphView != null)
            {
                graphView.OnNodeSelected -= HandleNodeSelect;
                graphView.OnNodeUnSelected -= HandleNodeUnSelect;
                graphView.graphViewChanged -= HandleGraphViewChange;
            }
            if (helpMenu != null)
            {
                addReanimatorButton.clicked -= AddReanimator;
            }
            EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        }
    }
}