**Download the game at: *https://liam-blair.itch.io/shift*!**

# About
This university project, now named Shift, was implemented to investigate the effectiveness of the Goal-Oriented Action Planning (GOAP) planning strategy in use on enemy agents, in a maze escape scenario. The application was adapted from an existing Unity project which included a basic implementation of GOAP written by Brent Owens, which can be found here: *https://gamedevelopment.tutsplus.com/tutorials/goal-oriented-action-planning-for-a-smarter-ai--cms-20793*.

# Goal Oriented Action Planning
Goal-Oriented Action Planning is a game-specific implementation of STRIPS (Stanford Technology Research Insititue Problem Solver), which was a general-purpose design of this planning strategy. Agents utilising GOAP each have a knowledge base known as the world state; a set of facts about every relevant piece of information about the current state of the world from each agent's point of view as booleans. For example, "weaponLoaded, true" could be considered a fact about the state of an agent's held weapon.

## Actions and Goals
Agents have actions which allows it to interact with the world and change it's state. To run an action however, it's preconditions must be satisfied; a list of required world state facts that must be satisfied before the action can be executed. For example, to fire a gun, the agent will need to be holding a gun and for it to be loaded. After an action has been fully executed, the world state will be modified through it's effects list; the changes to the world state caused by this single action. For example, firing a gun could result in the gun running out of ammo.

Finally, agents have goals; desired world states which represent victory conditions for the enemy. This can include attacking the player or collecting items. Agents can have multiple goals, and so goal prioritisation is calculated using insistence, which is the measure of how important the agent perceives a given goal at this current moment. For example, if the agent sees an item it could collect, it may perceive the goal of collecting it higher than looking for the player. Goal insistence calculations allows the agent to dynamically select goals it should follow at run-time, producing more complex enemy behaviour.

## Planning
Once a goal is selected, the enemy agent will begin planning to satisfy that goal from the current world state. The A* graph traversal algorithm is employed to develop plans, with nodes representing world states and edges representing actions. From the initial (current) world state, the A* algorithm expands the graph by testing what actions can be run from this world state by looking at each action's preconditions and seeing if the current world state satisfies the preconditions of each potential action. The use of actions is then simulated, which results in the creation of new nodes, which is the current world state plus the changes brought by the execution of the action, which is the action's effects. Since the goal is a desired world state, the graph can be expanded until a node is found which matches the goal state, and from there the path from the starting node to the end node can be found, which is the set of actions which the agent can run to satisfy it's current goal.

To produce efficient plans actions will also have costs, which can be either pre-defined values or determined at runtime. An action's cost normally represents the time or resource cost to use said action. This allows the A* algorithm to produce weighted graphs, which is important as the lowest cost plan can be calculated, elminating redundant actions that have little to no use in satisfying the goal state.

GOAP plans are often very small: For example, in this project, the most complex plan that could be built consists of 5 seperate actions, and the average plan size ranging between 2 to 3. This is to allievate one major drawback of GOAP: The assumption made by the planner that the agent is the only object in the game that is able to modify the world state, and so no other agents or objects (In the planner's view) are unable to modify the world state. The longer a plan takes to fully execute, the more likely another agent or object could modify the world state, potentially resulting in the current plan being obsolete or impossible to complete, forcing the agent to compute a new plan. Shorter plans minimises the time required to execute, and so minimises the times it may be interrupted, though in return the agent needs to plan often. As such, goals often do not represent high-level, overarching objectives (Such as defeating the player), but instead are short-term goals which indirectly fulfill the enemy's main objective, such as taking cover or flanking the player.

# Additional Reading
**Introduction to GOAP along with a sample Unity project showcasing an example game utilising GOAP agents.**
<br>*https://gamedevelopment.tutsplus.com/tutorials/goal-oriented-action-planning-for-a-smarter-ai--cms-20793*

**FEAR (First Encounter Assault Recon) decompiled source code, a game which employed GOAP for enemies and NPCs.**
<br>(Classes which handle actions and goals can be found inside the "ObjectDLL" folder)
<br>*https://github.com/xfw5/Fear-SDK-1.08/tree/master/Game*
