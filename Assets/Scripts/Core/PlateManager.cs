using System.Collections.Generic;
using UnityEngine;

public class PlateManager : MonoBehaviour
{
    [SerializeField] private Plate platePrefab;
    [SerializeField] private Transform platesContainer;

    private List<Plate> plates = new List<Plate>();

    public void Initialize(List<PlateData> plateDataList)
    {
        ClearPlates();

        int rows = Mathf.CeilToInt(Mathf.Sqrt(plateDataList.Count));
        int cols = Mathf.CeilToInt((float)plateDataList.Count / rows);
        float spacing = 2f;

        for (int i = 0; i < plateDataList.Count; i++)
        {
            var plate = Instantiate(platePrefab, platesContainer);
            int row = i / cols;
            int col = i % cols;
            plate.transform.position = new Vector3(col * spacing, -row * spacing, 0);
            plate.Initialize(plateDataList[i].ActiveTypes, plateDataList[i].Layers);
            plates.Add(plate);
        }
    }

    public bool CanMoveSushi(Plate from, Plate to)
    {
        return from.ActiveCount > 0 && !to.IsFull;
    }

    public void MoveSushi(Plate from, Plate to)
    {
        if (!CanMoveSushi(from, to)) return;

        var sushi = from.RemoveTop();
        to.AddSushi(sushi);
    }

    public bool AreAllPlatesEmpty()
    {
        foreach (var plate in plates)
        {
            if (!plate.IsEmpty) return false;
        }
        return true;
    }

    private void ClearPlates()
    {
        foreach (var plate in plates)
        {
            Destroy(plate.gameObject);
        }
        plates.Clear();
    }
}
