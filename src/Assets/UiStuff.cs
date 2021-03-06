using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UiStuff : MonoBehaviour
{
    public static UiStuff Instance { get; private set; }

    RectTransform fuelCanvas;
    RectTransform fuelBar;
    Image fuelFlash;
    Text fuelText;

    RectTransform researchCanvas;
    RectTransform researchBar;
    Text researchText;
    GameObject upgradeButton;

    Transform draggedBlock;

    GameObject upgradeModal;
    Button upgradeSawCost;
    Button upgradeRotatorCost;
    Button upgradeRotatorSize;
    Button upgradeTransmuter1;
    Button upgradeTransmuter2;
    Button upgradeTransmuter3;

    void Awake() {
        Instance = this;
        draggedBlock = null;

        fuelCanvas = GameObject.Find("FuelCanvas").GetComponent<RectTransform>();
        fuelText = GameObject.Find("FuelText").GetComponent<Text>();
        fuelBar = GameObject.Find("FuelBar").GetComponent<RectTransform>();
        fuelFlash = GameObject.Find("FuelFlash").GetComponent<Image>();

        researchCanvas = GameObject.Find("ResearchCanvas").GetComponent<RectTransform>();
        researchText = GameObject.Find("ResearchText").GetComponent<Text>();
        researchBar = GameObject.Find("ResearchBar").GetComponent<RectTransform>();
        upgradeButton = GameObject.Find("UpgradeButton");
        upgradeButton.SetActive(false);

        upgradeModal = GameObject.Find("UpgradeModal");
        upgradeSawCost = GameObject.Find("UpgradeSawCost").GetComponent<Button>();
        upgradeRotatorCost = GameObject.Find("UpgradeRotatorCost").GetComponent<Button>();
        upgradeRotatorSize = GameObject.Find("UpgradeRotatorSize").GetComponent<Button>();
        upgradeTransmuter1 = GameObject.Find("UpgradeTransmuter1").GetComponent<Button>();
        upgradeTransmuter2 = GameObject.Find("UpgradeTransmuter2").GetComponent<Button>();
        upgradeTransmuter3 = GameObject.Find("UpgradeTransmuter3").GetComponent<Button>();
        upgradeModal.SetActive(false);
    }

    void Start() {
        int fuelWidth = Game.levelWidth - Game.Instance.levelWidthAfterBlue - 2;
        fuelCanvas.localPosition = new Vector2(fuelWidth * 0.5f, -2.0f);
        fuelCanvas.sizeDelta = new Vector2(fuelWidth * 100, 100);

        int researchWidth = Game.Instance.levelWidthAfterBlue + 1;
        researchCanvas.localPosition = new Vector2(fuelWidth + 1 + researchWidth * 0.5f, -2.0f);
        researchCanvas.sizeDelta = new Vector2(researchWidth * 100, 100);

        setFuel(Game.fuel);
        setResearch(Game.research);
    }
    
    void Update() {
        UpdateDragging();
    }

    void UpdateDragging() {
        Vector3 mouseWorld = Camera.main.ScreenToWorldPoint(Input.mousePosition);
        mouseWorld.z = 0;

        // true for one frame only
        if (Input.GetMouseButtonUp(0)) {
            if (draggedBlock) {
                var t = draggedBlock.parent;
                t.position = new Vector3(Mathf.Round(t.position.x), Mathf.Round(t.position.y));
                draggedBlock = null;
            }
        }

        if (draggedBlock) {
            Vector2 targetPos = (Vector2)mouseWorld;
            Vector2 currentPos = (Vector2)draggedBlock.position;
            Vector2 snappedPos = new Vector2(Mathf.Round(currentPos.x), Mathf.Round(currentPos.y));

            var figure = draggedBlock.parent.GetComponent<Figure>();
            if (figure) {
                bool left, top, right, bottom;
                figure.GetAllowedMoves(out left, out top, out right, out bottom);
                if (!left)
                    targetPos.x = Mathf.Max(snappedPos.x, targetPos.x);
                if (!right)
                    targetPos.x = Mathf.Min(snappedPos.x, targetPos.x);
                if (!bottom)
                    targetPos.y = Mathf.Max(snappedPos.y, targetPos.y);
                if (!top)
                    targetPos.y = Mathf.Min(snappedPos.y, targetPos.y);
            }

            Vector2 posToTarget = targetPos - currentPos;
            Vector2 gridToTarget = targetPos - snappedPos;
            Vector2 absDelta = new Vector2(Mathf.Abs(gridToTarget.x), Mathf.Abs(gridToTarget.y));
            Vector3 outputMove = Vector3.zero;

            if (absDelta.x >= 1.0f || absDelta.y >= 1.0f) {
                // long drag: move along the major axis, snap to the minor one
                float major = Mathf.Max(absDelta.x, absDelta.y);
                float minor = Mathf.Min(absDelta.x, absDelta.y);
                float snappiness = Mathf.Clamp01(major / 100 * Mathf.Max(0.1f, minor));

                if (absDelta.y >= absDelta.x) {
                    // snap to x, move along y
                    outputMove.x = snappiness * (Mathf.Round(currentPos.x) - currentPos.x);
                    outputMove.y = posToTarget.y;
                }
                else {
                    // move along x, snap to y
                    outputMove.x = posToTarget.x;
                    outputMove.y = snappiness * (Mathf.Round(currentPos.y) - currentPos.y);
                }
            }
            else {
                // short drag: allow free movement within the vicinity of the nearest gridpoint
                float d = 0.50f;  // [0, 1] default 0.5, higher is looser (how far away the circle is from the gridpoint)
                float r = 0.45f;  // [0, 1] default 0.5, higher is tigher (circle radius)
                Vector2 circleCenter = new Vector2(gridToTarget.x >= 0 ? d : (-d), gridToTarget.y >= 0 ? d : (-d));
                float t = RaycastVectorCircle(gridToTarget, circleCenter, r);
                Vector2 gridToEdge = t * gridToTarget;
                Vector2 onEdge = snappedPos + gridToEdge;
                Vector2 edgeToCircle = circleCenter - onEdge;
                Vector2 edgeToTarget = (1 - t) * gridToTarget;
                Vector2 intoTheCircle = edgeToTarget * Vector2.Dot(edgeToTarget, edgeToCircle.normalized);
                Vector2 alongTheCircle = edgeToTarget - intoTheCircle;
                outputMove = (snappedPos + gridToEdge + 0.2f * alongTheCircle) - currentPos;
            }

            // move the figure, not the block
            outputMove.x = Mathf.Clamp(outputMove.x, -0.75f, 0.75f);
            outputMove.y = Mathf.Clamp(outputMove.y, -0.75f, 0.75f);
            draggedBlock.parent.position += outputMove;
            return;
        }

        // true for one frame only
        if (Input.GetMouseButtonDown(0)) {
            Collider2D collider = Physics2D.OverlapPoint(mouseWorld);
            if (!collider) {
                return;
            }
            var block = collider.GetComponent<Block>();
            if (block) {
                draggedBlock = block.transform;
                return;
            }
            // todo: drag buildings
        }
    }

    public static float RaycastVectorCircle(Vector2 v, Vector2 center, float r) {
        // WARNING: this doesn't work in the general case
        // some assumptions:
        //   v is anchored at (0, 0)
        //   v is bounded by center - (r, r) and center + (r, r)
        // returns a float close to
        //   0 if (0, 0) is within the circle
        //   1 if v doesn't intersect the circle at all
        //   t such that v*t is the intersection point
        float a = 0;
        float b = 1;
        float rr = r * r;
        for (int i = 0; i < 12; ++i) {
            float s = 0.5f * (a + b);
            if ((v * s - center).sqrMagnitude < rr)
                b = s;
            else
                a = s;
        }
        return a;
    }

    public static void setFuel(int value) {
        var self = Instance;

        if (self && self.fuelText)
            self.fuelText.text = string.Format("{0}/{1}", value, Game.maxFuel);

        if (self && self.fuelBar && self.fuelCanvas) {
            Vector2 size = self.fuelCanvas.sizeDelta;
            size.x = size.x * (value / (float)Game.maxFuel);
            self.fuelBar.sizeDelta = size;
        }
    }

    public static void setResearch(int value) {
        var self = Instance;

        if (self && self.researchText)
            self.researchText.text = string.Format("{0}/{1}", value, Game.maxResearch);

        if (self && self.researchBar && self.researchCanvas) {
            Vector2 size = self.researchCanvas.sizeDelta;
            size.x = size.x * (value / (float)Game.maxResearch);
            self.researchBar.sizeDelta = size;
        }
    }

    private Coroutine runningOutOfFuel;

    public void flashOutOfFuel() {
        if (runningOutOfFuel != null)
            StopCoroutine(runningOutOfFuel);
        runningOutOfFuel = StartCoroutine(animateOutOfFuel());
    }

    IEnumerator animateOutOfFuel() {
        bool enabled = true;

        for (int i = 0; i < 6; ++i) {
            fuelFlash.enabled = enabled;
            enabled = !enabled;

            float next = Time.time + 0.3f;
            while (Time.time < next)
                yield return null;
        }
        fuelFlash.enabled = false;
    }

    public void EnableUpgradeButton() {
        upgradeButton.SetActive(true);
    }

    public void UpgradeButtonClicked() {
        upgradeButton.SetActive(false);
        upgradeModal.SetActive(true);
        upgradeSawCost.interactable = !Game.sawCostUpgraded;
        upgradeRotatorCost.interactable = !Game.rotatorCostUpgraded;
        upgradeRotatorSize.interactable = Game.rotatorSize == 2;
        upgradeTransmuter1.interactable = Game.transmuterSize == 0;
        upgradeTransmuter2.interactable = Game.transmuterSize == 1;
        upgradeTransmuter3.interactable = Game.transmuterSize == 2;
    }

    public void ChooseUpgrade(string what) {
        if (what == "sawCost") {
            Game.sawCostUpgraded = true;
        }
        else if (what == "rotatorCost") {
            Game.rotatorCostUpgraded = true;
            Game.rotationCost = 0;
        }
        else if (what == "rotatorSize") {
            Game.rotatorSize = 3;
            GameObject.Find("Rotator").GetComponent<Rotator>().UpgradeSize();
        }
        else if (what == "transmuter1") {
            Game.transmuterSize = 1;
            GameObject.Find("Transmuter").GetComponent<Transmuter>().UpgradeSize(Game.transmuterSize);
        }
        else if (what == "transmuter2") {
            Game.transmuterSize = 2;
            Game.transmutationCost = 2;
            GameObject.Find("Transmuter").GetComponent<Transmuter>().UpgradeSize(Game.transmuterSize);
        }
        else if (what == "transmuter3") {
            Game.transmuterSize = 3;
            Game.transmutationCost = 3;
            GameObject.Find("Transmuter").GetComponent<Transmuter>().UpgradeSize(Game.transmuterSize);
        }
        upgradeModal.SetActive(false);
    }
}
