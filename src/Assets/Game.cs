using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum BlockType {
    Green,
    Blue,
    Red,
}

public enum MoveDirection {
    Left,
    Up,
    Right,
    Down,
}

public class CollisionData {
    public CollisionData(bool[,] field, Vector2 leftOfSawBlade, Vector2 rightOfSawBlade) {
        Field = field;
        LeftOfSawBlade = leftOfSawBlade;
        RightOfSawBlade = rightOfSawBlade;
    }
    public bool[,] Field { get; private set; }
    public Vector2 LeftOfSawBlade { get; private set; }
    public Vector2 RightOfSawBlade { get; private set; }
}

public class Game : MonoBehaviour {
    public static Game Instance { get; private set; }

    private Transform figures;

    // playable means within walls
    public int levelPlayableHeight;
    // total playable width = these four + 3 (1 for each color output)
    public int levelWidthBeforeGreen;
    public int levelWidthGreenToRed;
    public int levelWidthRedToBlue;
    public int levelWidthAfterBlue;
    // top left corner
    public int levelHoleSize;

    // Colors
    public Color green;
    public Color blue;
    public Color red;

    // only valid after Awake()
    public static int levelWidth { get; private set; }
    public static int levelHeight { get; private set; }

    // resources
    public static int maxFuel { get; private set; }
    private static int fuelField;
    public static int fuel {
        get {
            return fuelField;
        }
        set {
            int ok = value;
            if (ok < 0)
                ok = 0;
            if (ok > maxFuel)
                ok = maxFuel;
            UiStuff.setFuel(ok);
            fuelField = ok;
        }
    }

    public static int maxResearch { get; private set; }
    private static int researchField;
    public static int research {
        get {
            return researchField;
        }
        set {
            int ok = value;
            if (ok < 0)
                ok = 0;
            if (ok > maxResearch)
                ok = maxResearch;
            UiStuff.setResearch(ok);
            researchField = ok;
        }
    }

    public static Color TypeToColor(BlockType type) {
        return type == BlockType.Green ? Game.Instance.green
            : type == BlockType.Blue ? Game.Instance.blue
            : Game.Instance.red;
    }

    public static int cuttingCost(BlockType type) {
        if (type == BlockType.Green)
            return 0;
        if (type == BlockType.Blue)
            return 1;
        if (type == BlockType.Red)
            return 2;
        return 0;
    }

    public static int rotationCost { get; private set; }

    void Awake() {
        Instance = this;
        levelWidth = 3 + levelWidthBeforeGreen + levelWidthGreenToRed + levelWidthRedToBlue + levelWidthAfterBlue;
        levelHeight = levelPlayableHeight;

        rotationCost = 1;
        maxFuel = 100;
        fuel = 10;
        maxResearch = 10;
        research = 0;
    }

    void Start() {
        figures = GameObject.Find("Figures").transform;

        var figure = Figure.Create(FigureFactory.GetTemplate(), BlockType.Green);
        figure.transform.SetParent(figures);
        figure.transform.localPosition = new Vector2(0, levelHeight + 1);

        generateLevel();

        float yMin = -3.5f;
        float yMax = levelHeight + levelHoleSize + 1 + 0.5f;
        Camera.main.transform.position = new Vector3(levelWidth * 0.5f, 0.5f * (yMin + yMax), -10.0f);
        Camera.main.orthographicSize = 0.5f * (yMax - yMin);
    }

    void Update() {
        research = (int)(Time.time - 2);
    }

    public void OnBlockMouseDown(GameObject block) {
    }

    private void generateLevel() {
        var level = GameObject.Find("Level").transform;
        var tools = GameObject.Find("Tools").transform;
        var pfWall = Resources.Load<GameObject>("Prefabs/Wall");
        var pfPipeEnd = Resources.Load<GameObject>("Prefabs/PipeEnd");
        var pfPipe = Resources.Load<GameObject>("Prefabs/Pipe");
        var pfInputArea = Resources.Load<GameObject>("Prefabs/InputArea");
        var pfDownArrow = Resources.Load<GameObject>("Prefabs/DownArrow");
        var pfFloor = Resources.Load<GameObject>("Prefabs/Floor");
        var pfOutput = Resources.Load<GameObject>("Prefabs/Output");
        var pfSaw = Resources.Load<GameObject>("Prefabs/Saw");
        var pfRotator = Resources.Load<GameObject>("Prefabs/Rotator");

        int emptyX1 = levelWidthBeforeGreen;
        int emptyX2 = emptyX1 + 1 + levelWidthGreenToRed;
        int emptyX3 = emptyX2 + 1 + levelWidthRedToBlue;

        var wallPositions = new List<Vector2>();
        var outputPositions = new List<Vector2>();

        for (int x = -1; x <= levelWidth; x += 1) {
            // the topmost layer is full
            wallPositions.Add(new Vector2(x, levelHeight + levelHoleSize + 1));

            // the top/middle layer has a gap
            if (x < 0 || x >= levelHoleSize)
                wallPositions.Add(new Vector2(x, levelHeight));

            // the bottom layer needs gaps for color outputs
            if (x != emptyX1 && x != emptyX2 && x != emptyX3)
                wallPositions.Add(new Vector2(x, -1));
            else
                outputPositions.Add(new Vector2(x, -1));

            // the very bottom layer is full
            wallPositions.Add(new Vector2(x, -3));
        }

        for (int y = 0; y <= levelHeight + levelHoleSize; ++y) {
            wallPositions.Add(new Vector2(-1, y));
            if (y < levelHeight)
                wallPositions.Add(new Vector2(levelWidth, y));
        }

        // extra regular walls
        wallPositions.Add(new Vector2(-1, -2));
        wallPositions.Add(new Vector2(emptyX3 - 1, -2));
        wallPositions.Add(new Vector2(levelWidth, -2));

        foreach (Vector2 pos in wallPositions) {
            var wall = Instantiate(pfWall);
            wall.transform.SetParent(level);
            wall.transform.localPosition = (Vector3)pos;
        }

        int inputAreaX = 0;
        for (int inputIdx = 0; inputIdx < levelWidth / levelHoleSize; inputIdx++) {
            inputAreaX = inputIdx * levelHoleSize;
            float modifier = inputIdx % 2 == 0 ? 1.1f : 1.0f;
            for (int x = 0; x < levelHoleSize; x++) {
                for (int y = 0; y < levelHoleSize; y++) {
                    var inputArea = Instantiate(pfInputArea);
                    inputArea.transform.SetParent(level);
                    inputArea.transform.localPosition = new Vector2(inputAreaX + x, levelHeight + y + 1);
                    inputArea.GetComponent<SpriteRenderer>().color *= modifier;
                }
            }
        }

        for (int x = inputAreaX + levelHoleSize; x < levelWidth + 1; x++) {
            for (int y = levelHeight + 1; y < levelHeight + levelHoleSize + 1; y++) {
                var pipe = Instantiate(x == (inputAreaX + levelHoleSize) ? pfPipeEnd : pfPipe);
                pipe.transform.SetParent(level);
                pipe.transform.localPosition = new Vector2(x, y);
            }
        }

        for (int x = 0; x < levelHoleSize; x++) {
            var arrow = Instantiate(pfDownArrow);
            arrow.transform.SetParent(level);
            arrow.transform.localPosition = new Vector2(x, levelHeight);
        }

        for (int x = 0; x < levelWidth; x++) {
            for (int y = 0; y < levelHeight; y++) {
                var floor = Instantiate(pfFloor);
                floor.transform.SetParent(level);
                floor.transform.localPosition = new Vector2(x, y);
            }
        }

        var types = new[] { BlockType.Green, BlockType.Red, BlockType.Blue };
        for (int i = 0; i < 3; i++) {
            var floor = Instantiate(pfFloor);
            floor.transform.SetParent(level);
            floor.transform.localPosition = (Vector3)outputPositions[i];

            var output = Instantiate(pfOutput);
            output.name = string.Format("Output ({0})", types[i]);
            output.GetComponent<Output>().Type = types[i];
            output.GetComponent<SpriteRenderer>().color = TypeToColor(types[i]);
            output.transform.SetParent(tools);
            output.transform.localPosition = (Vector3)outputPositions[i];
        }

        var saw = Instantiate(pfSaw);
        saw.name = "Saw";
        saw.transform.SetParent(tools);
        saw.transform.localPosition = new Vector3(4.0f, 2.0f, 0.0f);

        var rotator = Instantiate(pfRotator);
        rotator.name = "Rotator";
        rotator.transform.SetParent(tools);
        rotator.transform.localPosition = new Vector3(8.0f, 2.0f, 0.0f);
    }

    public CollisionData GetCollisionData(HashSet<GameObject> exclude) {
        var field = new bool[levelWidth, levelHeight];

        var activeObjects = GameObject.FindGameObjectsWithTag("Figure")
            .Concat(GameObject.FindGameObjectsWithTag("Tool"))
            .Where(o => !exclude.Contains(o));

        Vector2 leftOfSawBlade = new Vector2(0, 0); // to make compiler happy
        foreach (var obj in activeObjects) {
            int ox = (int)Math.Round(obj.transform.position.x);
            int oy = (int)Math.Round(obj.transform.position.y);
            foreach (var pos in obj.GetComponent<IMovable>().EnumerateAllFilledBlocks()) {
                field[ox + (int)pos.x, oy + (int)pos.y] = true;
            }
            var saw = obj.GetComponent<Saw>();
            if (saw != null) {
                leftOfSawBlade = saw.LeftOfSawBlade;
            }
        }

        return new CollisionData(field, leftOfSawBlade, leftOfSawBlade + new Vector2(1, 0));
    }

    public bool IsMoveAllowed(CollisionData collisionData, int x, int y, MoveDirection direction) {
        int output1 = levelWidthBeforeGreen;
        int output2 = output1 + 1 + levelWidthGreenToRed;
        int output3 = output2 + 1 + levelWidthRedToBlue;

        switch (direction) {
            case MoveDirection.Left: x -= 1; break;
            case MoveDirection.Up: y += 1; break;
            case MoveDirection.Right: x += 1; break;
            case MoveDirection.Down: y -= 1; break;
            default: throw new InvalidOperationException();
        }

        bool isInInputArea = y >= levelHeight && x < levelHoleSize && direction == MoveDirection.Down;
        bool isInOutputArea = y == -1 && (x == output1 || x == output2 || x == output3);
        bool isClippingThroughBlocks = x < 0 || x >= levelWidth || y < 0 || y >= levelHeight || collisionData.Field[x, y];
        bool isClippingThroughSawBlade = y == (int)collisionData.LeftOfSawBlade.y &&
            (direction == MoveDirection.Left && x == (int)collisionData.LeftOfSawBlade.x ||
             direction == MoveDirection.Right && x == (int)collisionData.RightOfSawBlade.x);
        return isInInputArea || isInOutputArea || !(isClippingThroughBlocks || isClippingThroughSawBlade);
    }

    public bool TryOutput(BlockType type) {
        switch (type) {
            case BlockType.Green:
                fuel += 1;
                return true;
            case BlockType.Blue:
                return true;
            case BlockType.Red:
                if (fuel == 0) {
                    return false;
                }
                fuel -= 1;
                return true;
            default: throw new InvalidOperationException();
        }
    }
}
