/*
 * COPYRIGHT:   See COPYING in the top level directory
 * PROJECT:     Pathfinding
 * FILE:        AgentProfile.cs
 * PURPOSE:     Your file purpose here
 * PROGRAMMER:  Peter Geinitz (Wayfarer)
 */

namespace FolkTales
{
    /// <summary>
    /// Defines physical dimensions, limits, and traversal capabilities for an NPC.
    /// </summary>
    public readonly record struct AgentProfile(
        float Radius = 0.4f,
        float MaxStepHeight = 0.5f,
        float MaxJumpHeight = 1.5f,
        bool CanClimbLadders = true,
        float JumpCostPenalty = 3.0f, // Higher cost so NPCs prefer walking around if possible
        int Id = 0
    );
}