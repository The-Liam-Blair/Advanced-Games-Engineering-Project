using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using Unity.Jobs;
using UnityEditor.Experimental;
using UnityEngine;
using UnityEngine.AI;

/// <summary>
/// Rebakes the entire nav mesh. While not super inefficient, a more optimised solution will later be
/// implemented where only nav meshes with changed areas are rebaked. (Currently rebake pre-defined function rebakes ALL
/// nav mesh surfaces)
///
/// Script always executes first in the execution order: Guarantees updates from last frame are applied for agents in the next frame.
/// Also includes a shared bool that ensures that agents do not try to use the nav mesh during rebaking just in case to prevent errors.
/// </summary>
public class ReBake : MonoBehaviour
{
    private NavMeshSurface Rebaker;

    public bool
        ISNAVMESHBUILDING; // Shared with agents to make them wait to use the nav mesh if the nav mesh is being rebuilt.

    public void Start()
    {
        ISNAVMESHBUILDING = false;
        Rebaker = GetComponent<NavMeshSurface>();
    }

    public void Bake()
    {
        ISNAVMESHBUILDING = true;
        Rebaker.BuildNavMesh();
        ISNAVMESHBUILDING = false;
    }

    /// <summary>
    /// Increment area ID of a set of nav meshes and rebake. After a 3 second delay, decrement the ID to return to original state.
    /// Increment area ID increases area cost, while decrementing decreases area cost, represents the number of agents accessing these
    /// nav meshes to encourage spreading out.
    /// </summary>
    /// <param name="surfaces">List of nav meshes (their nav mesh modifier component) accessed by one agent.</param>
    public void IncrementSurfaceArea(List<NavMeshModifier> surfaces)
    {
        // Increment each surface first, then bake once for efficiency.
        foreach (NavMeshModifier surface in surfaces)
        {
            surface.area++;
        }
        Bake();

        // Perform opposite of the above: After 3 second delay, decrement each surface and bake once.
        StartCoroutine(DecrementAreaDelayCoroutine(surfaces));
    }

    IEnumerator DecrementAreaDelayCoroutine(List<NavMeshModifier> surfaces)
    {
        yield return new WaitForSeconds(3);

        foreach (NavMeshModifier surface in surfaces)
        {
            surface.area--;
        }
        Bake();

        yield return null;
    }
}
