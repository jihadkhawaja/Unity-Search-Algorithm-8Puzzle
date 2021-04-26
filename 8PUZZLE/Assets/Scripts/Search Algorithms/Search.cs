using System;
using System.Text.RegularExpressions;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Search : MonoBehaviour
{
    [Header("UI")]
    public Button m_SearchButton;
    public Text m_MessageText;

    public GameObject m_StartPanel;
    public GameObject m_SolutionPanel;
    public GameObject m_ExitPanel;

    public Transform m_SolutionPathParent;
    public GameObject pathToSolution;
    
    [Header("Timing (For Presentation)")]
    [Range(0.000000001f, 0.25f)]
    [Tooltip("A delay for tree expandation")]
    public float m_SearchDelayTime = 0.000000001f;

    [Header("Puzzle")]
    public Puzzle m_StartState;
    public Puzzle m_GoalState;

    [Header("Tree")]
    [Tooltip("It must be existed in current scene")]
    public Node m_RootNode;
    [Tooltip("The latest node is the goal state")]
    public List<Node> m_PathToSolution;

    //Search State Track
    private bool isSearching;
    //Timer
    private float targetTime = 0.000f;
    public Text timerText;
    //Total Moves
    public Text totalMovesText;

    #region UI Management methods
    private void Awake()
    {
        m_ExitPanel.SetActive(false);

        // clearing message text for reuse
        ClearLog();

        SetUIActivity(true);
    }

    public void StartSearching()
    {
        //clear solution
        foreach(Transform t in m_SolutionPathParent)
        {
            Destroy(t.gameObject);
        }

        timerText.text = "";
        targetTime = 0;

        totalMovesText.text = "";

        isSearching = true;

        SetUIActivity(false);
        Log("Searching...", Color.white);

        // destroying previous children of root node
        DestroyChilrenNodes();

        Init();

        StartCoroutine(BreadthFirstSearch(m_RootNode));
    }

    //Timer
    private void Update()
    {
        if(isSearching)
        {
            targetTime += Time.deltaTime;
            timerText.text = $"{targetTime.ToString("0.000")} s";
        }
    }

    private void DestroyChilrenNodes()
    {
        m_RootNode.m_Children.Clear();
        m_PathToSolution.Clear();

        for (int i = 0; i < m_RootNode.transform.childCount; i++)
            Destroy(m_RootNode.transform.GetChild(i).gameObject);
    }

    private void SetUIActivity(bool active)
    {
        m_SearchButton.interactable = active;

        if(active)
            m_StartPanel.transform.Find("Lock").SetAsFirstSibling();
        else
            m_StartPanel.transform.Find("Lock").SetAsLastSibling();

    }

    private void Log(string message, Color logColor)
    {
        m_MessageText.color = logColor;
        m_MessageText.text = message;
    }

    private void ClearLog()
    {
        totalMovesText.text = "";
        timerText.text = "";
        m_MessageText.text = "";
    }
    #endregion

    // start and goal state coordination
    private void Init()
    {
        // setting init state for root node
        m_RootNode.m_Puzzle.SetFrom(m_StartState.m_Puzzle);
        
        // setting goal state for Utilities.s_GoalState[,]
        Utilities.s_GoalState = new int[Utilities.s_PuzzleDimension, Utilities.s_PuzzleDimension];
        Utilities.s_GoalState.SetFrom(m_GoalState.m_Puzzle);
    }

    // The searching algorithm
    //IEnumerator used because of rendering order/queue problems
    public IEnumerator BreadthFirstSearch(Node root)
    {
        if(root.IsGoalState())
        {
            Log("Please modify Initial or Desired State", Color.white);

            isSearching = false;

            SetUIActivity(true);
            // filling m_PathSolution with root node
            PathTrace(root, m_PathToSolution);  

            yield break;
        }
        // not searched
        List<Node> openList = new List<Node>();
        // searched
        List<Node> closedList = new List<Node>();  

        openList.Add(root);
        bool isGoalFound = false;

        while (openList.Count > 0 && !isGoalFound)
        {
            Node currentNode = openList[0];
            closedList.Add(currentNode);
            openList.RemoveAt(0);

            currentNode.ExpandNode();

            // checking children of currentNode to finding goal state
            for (int i = 0; i < currentNode.m_Children.Count; i++)
            {
                Node currentChild = currentNode.m_Children[i];

                if (currentChild.IsGoalState())
                {
                    Log("Solved!", Color.green);

                    isGoalFound = true;

                    // trace path to root node
                    PathTrace(currentChild, m_PathToSolution); 
                }

                if (!Contains(openList, currentChild) && !Contains(closedList, currentChild))
                    openList.Add(currentChild);
            }

            yield return new WaitForSeconds(m_SearchDelayTime);
        }

        //finished searching
        isSearching = false;

        //algorithm moves & solution path info
        totalMovesText.text = $"Moves:{m_PathToSolution.Count}";

        //add root node
        GameObject r = pathToSolution;
        int row = 0;
        for (int j = 0; j < m_RootNode.m_Puzzle.GetLength(1); j++)
        {
            for (int i = 0; i < m_RootNode.m_Puzzle.GetLength(0); i++)
            {
                if (m_RootNode.m_Puzzle[j, i] != 0)
                {
                    r.transform.GetChild(i + row).GetComponent<Text>().text = m_RootNode.m_Puzzle[j, i].ToString();
                }
                else
                {
                    r.transform.GetChild(i + row).GetComponent<Text>().text = "";
                }
            }
            row += 3;
        }

        r.GetComponent<Image>().color = Color.red;
        Instantiate(r, m_SolutionPathParent);

        int pathIndex = 0;
        //path nodes
        foreach (Node n in m_PathToSolution)
        {
            
            GameObject g = pathToSolution;
            g.GetComponent<Image>().color = Color.white;
            int rowp = 0;
            for (int j = 0; j < n.m_Puzzle.GetLength(1); j++)
            {
                for (int i = 0; i < n.m_Puzzle.GetLength(0); i++)
                {
                    if (n.m_Puzzle[j, i] != 0)
                    {
                        g.transform.GetChild(i + rowp).GetComponent<Text>().text = n.m_Puzzle[j, i].ToString();
                    }
                    else
                    {
                        g.transform.GetChild(i + rowp).GetComponent<Text>().text = "";
                    }
                    
                    //var msg = "[" + i.ToString() + ", " + j.ToString() + "] = " + n.m_Puzzle[i, j].ToString();
                    //Debug.Log(msg);
                }
                rowp += 3;
            }
            
            if (pathIndex == m_PathToSolution.Count - 1)
            {
                g.GetComponent<Image>().color = Color.green;
            }
            Instantiate(g, m_SolutionPathParent);
            pathIndex++;
        }

        //Display Solution Path
        m_SolutionPanel.gameObject.SetActive(true);

        SetUIActivity(true);
    }

    /// <summary>
    /// tracing path from a child node to root node (init state)
    /// </summary>
    /// <param name="pathToRootNode">An empty list for finding path</param>
    /// <param name="node">The current node for path finding from parents</param>
    private void PathTrace(Node node, List<Node> pathToRootNode)
    {
        Node current = node;

        pathToRootNode.Add(current);

        // root node has no parent
        while (current.m_Parent != null)
        {
            current = current.m_Parent;
            pathToRootNode.Add(current);
        }

        pathToRootNode.Reverse(); // reversing the order of path from root node to child nodes
    }

    private static bool Contains(List<Node> list, Node node)
    {
        bool contains = false;

        for (int i = 0; i < list.Count; i++)
        {
            if (list[i].IsSamePuzzle(node.m_Puzzle))
                contains = true;
        }

        return contains;
    }
}