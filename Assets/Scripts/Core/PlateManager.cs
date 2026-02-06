using System.Collections.Generic;
using UnityEngine;

public class PlateManager : MonoBehaviour
{
    [SerializeField] private List<Plate> plates;

    public void Initialize(List<PlateData> plateDataList)
    {
        for (int i = 0; i < plates.Count; i++)
        {
            if (i < plateDataList.Count)
            {
                plates[i].gameObject.SetActive(true);
                plates[i].Initialize(plateDataList[i].ActiveTypes, plateDataList[i].Layers);
            }
            else
            {
                plates[i].gameObject.SetActive(false);
            }
        }
    }

    public bool CanMoveSushi(Plate from, Plate to)
    {
        return from.ActiveCount > 0 && !to.IsFull;
    }

    public void MoveSushi(Plate from, Plate to, Sushi sushi)
    {
        if (!CanMoveSushi(from, to)) return;

        if (from.RemoveSpecificSushi(sushi))
        {
            to.AddSushi(sushi);
        }
    }

    public bool AreAllPlatesEmpty()
    {
        foreach (var plate in plates)
        {
            if (plate.gameObject.activeSelf && !plate.IsEmpty) return false;
        }
        return true;
    }

    public List<Plate> GetAllPlates()
    {
        return plates;
    }
}