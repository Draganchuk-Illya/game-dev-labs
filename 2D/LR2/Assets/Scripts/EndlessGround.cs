using UnityEngine;
using UnityEngine.Tilemaps;

// Безкінечний рівень: коли герой наближається до краю намальованої землі,
// скрипт добудовує нові ділянки (земля, провалля, пагорби, платформи) в обидва боки.
[RequireComponent(typeof(Tilemap))]
public class EndlessGround : MonoBehaviour
{
    // порядок тайлів: верх (лівий край, середина A, середина B, правий край),
    // другий ряд (лівий край, середина A, середина B, правий край),
    // нижні ряди (лівий край, середина A, середина B, правий край), платформа (3 тайли)
    [SerializeField] private TileBase[] tiles = new TileBase[15];
    [SerializeField] private int rightEdge = 46;    // остання клітинка намальованого рівня праворуч
    [SerializeField] private int leftEdge = -14;    // остання клітинка намальованого рівня ліворуч
    [SerializeField] private int lookAhead = 30;    // на скільки клітинок наперед добудовувати
    [SerializeField] private int seed = 2026;

    private Tilemap map;
    private Transform player;
    private System.Random rndRight, rndLeft;
    private int cursorRight, cursorLeft;

    private void Awake()
    {
        Init();
    }

    private void Init()
    {
        map = GetComponent<Tilemap>();
        rndRight = new System.Random(seed);
        rndLeft = new System.Random(seed + 1);
        cursorRight = rightEdge + 1;
        cursorLeft = leftEdge - 1;
    }

    private void Start()
    {
        var go = GameObject.Find("MainCharacter");
        if (go != null) player = go.transform;
    }

    private void Update()
    {
        if (player != null) Generate(player.position.x);
    }

    public void Generate(float playerX)
    {
        if (map == null) Init();
        while (cursorRight < playerX + lookAhead) cursorRight = AddSegment(cursorRight, 1, rndRight);
        while (cursorLeft > playerX - lookAhead) cursorLeft = AddSegment(cursorLeft, -1, rndLeft);
    }

    // Додає провалля і ділянку землі, починаючи з клітинки start у напрямку dir. Повертає нову позицію курсора.
    private int AddSegment(int start, int dir, System.Random rnd)
    {
        int gap = rnd.Next(2, 4);            // 2-3 клітинки, герой перестрибує
        int length = rnd.Next(5, 11);        // 5-10 клітинок землі
        int a = start + dir * gap;
        int b = a + dir * (length - 1);
        int from = Mathf.Min(a, b), to = Mathf.Max(a, b);

        Ground(from, to);
        if (length >= 7 && rnd.Next(3) == 0) Hill(from + 2, to - 2, -3);
        if (rnd.Next(3) == 0) Plank(Mathf.Min(start, a - dir) - (gap == 2 ? 1 : 0), -3);
        return b + dir;
    }

    private void Put(int x, int y, int index)
    {
        map.SetTile(new Vector3Int(x, y, 0), tiles[index]);
    }

    private void Ground(int from, int to)
    {
        for (int x = from; x <= to; x++)
        {
            int k = x == from ? 0 : (x == to ? 3 : (Mathf.Abs(x) % 2 == 0 ? 1 : 2));
            Put(x, -5, k);
            Put(x, -6, 4 + k);
            Put(x, -7, 8 + k);
            Put(x, -8, 8 + k);
        }
    }

    private void Hill(int from, int to, int top)
    {
        for (int x = from; x <= to; x++)
        {
            int k = x == from ? 0 : (x == to ? 3 : (Mathf.Abs(x) % 2 == 0 ? 1 : 2));
            Put(x, top, k);
            Put(x, top - 1, 4 + k);
            Put(x, -5, 10);
        }
    }

    private void Plank(int x, int y)
    {
        Put(x, y, 12); Put(x + 1, y, 13); Put(x + 2, y, 14);
    }

    public void SetTiles(TileBase[] newTiles)
    {
        tiles = newTiles;
    }
}
