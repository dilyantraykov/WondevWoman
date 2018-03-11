using System;
using System.Linq;
using System.IO;
using System.Text;
using System.Collections;
using System.Collections.Generic;

/**
 * Auto-generated code below aims at helping you parse
 * the standard input according to the problem statement.
 **/
class Player
{
    static void Main(string[] args)
    {
        string[] inputs;
        int size = int.Parse(Console.ReadLine());
        int unitsPerPlayer = int.Parse(Console.ReadLine());
        var gameContext = new GameContext();

        // game loop
        while (true)
        {
            var myUnits = new List<Unit>();
            var enemyUnits = new List<Unit>();
            var availableActions = new List<Action>();
            var grid = new Cell[size, size];

            for (int i = 0; i < size; i++)
            {
                string row = Console.ReadLine();
                for (var j = 0; j < row.Length; j++)
                {
                    grid[i, j] = new Cell(row[j], j, i);
                }
            }
            for (int i = 0; i < unitsPerPlayer; i++)
            {
                inputs = Console.ReadLine().Split(' ');
                int unitX = int.Parse(inputs[0]);
                int unitY = int.Parse(inputs[1]);
                var unit = new Unit(i, grid[unitY, unitX].Level, unitX, unitY);
                myUnits.Add(unit);
            }
            for (int i = 0; i < unitsPerPlayer; i++)
            {
                inputs = Console.ReadLine().Split(' ');
                int otherX = int.Parse(inputs[0]);
                int otherY = int.Parse(inputs[1]);
                if (otherX != -1 && otherY != -1)
                {
                    var unit = new Unit(i, grid[otherY, otherX].Level, otherX, otherY);
                    enemyUnits.Add(unit);
                }
            }
            int legalActions = int.Parse(Console.ReadLine());
            for (int i = 0; i < legalActions; i++)
            {
                inputs = Console.ReadLine().Split(' ');
                string atype = inputs[0];
                int index = int.Parse(inputs[1]);
                string dir1 = inputs[2];
                string dir2 = inputs[3];
                var action = new Action(atype, myUnits[index], dir1, dir2, grid);
                availableActions.Add(action);
            }

            gameContext.MyUnits = myUnits;
            gameContext.EnemyUnits = enemyUnits;
            gameContext.Grid = grid;
            gameContext.AvailableActions = availableActions;
            gameContext.ProcessTurn();
        }
    }
}

public class GameContext
{
    public List<Unit> AllUnits
    {
        get { return this.MyUnits.Concat(this.EnemyUnits).ToList(); }
    }
    public List<Unit> MyUnits { get; set; }
    public List<Unit> EnemyUnits { get; set; }
    public Cell[,] Grid { get; set; }
    public List<Action> AvailableActions { get; set; }

    internal void ProcessTurn()
    {
        var h0 = new BlockEnemyHandler();
        var h1 = new PushEnemyHandler();
        var h2 = new BuildHandler();
        var h3 = new AvoidBlockHandler();
        var h4 = new BestMoveHandler();
        var h5 = new DefaultActionHandler();
        h0.SetSuccessor(h1);
        h1.SetSuccessor(h2);
        h2.SetSuccessor(h3);
        h3.SetSuccessor(h4);
        h4.SetSuccessor(h5);

        var finalAction = h0.HandleActions(this);

        if (finalAction == null)
        {
            Console.WriteLine(Constants.AcceptDefeatAction);
        }
        else
        {
            Console.WriteLine(finalAction);
        }
    }

    public int CalculateStreak(Cell cell, Cell[,] grid, int result)
    {
        if (Utils.NumberOfFreeAdjacentCells(cell, this.AllUnits, grid) == 0 || result > 30)
        {
            Console.Error.WriteLine($"{cell} Streak: " + result);
            return result;
        }

        foreach (var c in Utils.GetNeighbouringCells(cell.Point, this.Grid).Where(c => c.Level != Constants.HoleLevel && c.Level != Constants.RemovedCellLevel))
        {
            var newGrid = Utils.CloneGrid(grid);
            newGrid[cell.Point.Y, cell.Point.X].Level += 1;
            return CalculateStreak(c, newGrid, ++result);
        }

        return 1;
    }
}

internal class BlockEnemyHandler : ActionsHandler
{
    public override Action GetBestAction(GameContext context)
    {
        Console.Error.WriteLine("BlockEnemy");
        foreach (var unit in context.EnemyUnits)
        {
            var freeAdjacentCellsForUnit = Utils.NumberOfFreeAdjacentCells(Utils.GetCellFromPoint(unit.Point, context.Grid), context.AllUnits, context.Grid);
            if (freeAdjacentCellsForUnit == 1 && !context.MyUnits.Any(u => Utils.AreAdjacent(u.Point, unit.Point)))
            {
                var cellToBlock = Utils.GetNeighbouringCells(unit.Point, context.Grid).First(c => c.IsAvailableForMove(unit.Level));
                return context.AvailableActions.FirstOrDefault(a => a.BuildCell.Point.Equals(cellToBlock.Point));
            }
            else if (freeAdjacentCellsForUnit == 0)
            {
                var blockingUnit = context.MyUnits
                    .Where(u => u.Level >= unit.Level)
                    .FirstOrDefault(u => Utils.AreAdjacent(u.Point, unit.Point));
                if (blockingUnit != null)
                {
                    var cellToBlock = Utils.GetCellFromPoint(blockingUnit.Point, context.Grid);
                    return context.AvailableActions.FirstOrDefault(a => a.BuildCell.Point.Equals(cellToBlock.Point));
                }
            }

            var neighBouringCells = Utils.GetNeighbouringCells(unit.Point, context.Grid);
            foreach (var cell in neighBouringCells)
            {
                var otherUnits = context.EnemyUnits.Where(u => u.Id != unit.Id).Concat(context.MyUnits);
                if (Utils.NumberOfFreeAdjacentCells(cell, otherUnits, context.Grid) == 1)
                {
                    return context.AvailableActions
                        .Where(a => a.Type == ActionType.PushAndBuild)
                        .FirstOrDefault(a => a.BuildCell.Point.Equals(cell.Point));
                }
            }
        }

        return null;
    }
}

abstract class ActionsHandler
{
    protected ActionsHandler successor;

    public void SetSuccessor(ActionsHandler successor)
    {
        this.successor = successor;
    }

    public Action HandleActions(GameContext context)
    {
        var action = this.GetBestAction(context);
        if (action != null)
        {
            return action;
        }
        else if (successor != null)
        {
            return successor.HandleActions(context);
        }

        return null;
    }

    public abstract Action GetBestAction(GameContext context);
}


internal class DefaultActionHandler : ActionsHandler
{
    public override Action GetBestAction(GameContext context)
    {
        Console.Error.WriteLine("Default");
        return context.AvailableActions.FirstOrDefault();
    }
}

internal class BestMoveHandler : ActionsHandler
{
    public override Action GetBestAction(GameContext context)
    {
        Console.Error.WriteLine("BestMove");
        return context.AvailableActions
                .Where(a => a.Type == ActionType.MoveAndBuild)
                .OrderByDescending(a => Utils.NumberOfFreeAdjacentCells(a.MoveCell, context.AllUnits, context.Grid))
                .ThenByDescending(a => a.MoveCell.Level)
                .ThenByDescending(a => a.BuildCell.Level)
                .FirstOrDefault();
    }
}

internal class AvoidBlockHandler : ActionsHandler
{
    public override Action GetBestAction(GameContext context)
    {
        Console.Error.WriteLine("AvoidBlock");

        var actions = new List<Action>();
        foreach (var unit in context.MyUnits)
        {
            var cell = context.Grid[unit.Point.Y, unit.Point.X];
            if (Utils.NumberOfFreeAdjacentCells(cell, context.AllUnits, context.Grid) == 1)
            {
                actions.Add(context.AvailableActions
                    .Where(a => a.Unit.Id == unit.Id)
                    .Where(a => a.Type == ActionType.MoveAndBuild)
                    .Where(a => a.BuildDirection == Utils.GetOppositeDirection(a.MoveDirection))
                    .FirstOrDefault());
            }

            if (Utils.GetNeighbouringCells(cell.Point, context.Grid)
                .Where(c => c.Level != Constants.HoleLevel)
                .All(c => c.Level >= Constants.TargetLevel))
            {
                actions.Add(context.AvailableActions
                    .Where(a => a.Unit.Id == unit.Id)
                    .Where(a => a.Type == ActionType.MoveAndBuild)
                    .OrderBy(a => Utils.NumberOfFreeAdjacentCells(a.BuildCell, context.AllUnits, context.Grid))
                    .ThenByDescending(a => Utils.NumberOfFreeAdjacentCells(a.MoveCell, context.AllUnits, context.Grid))
                    .ThenBy(a => Utils.GetCellPotential(a.BuildCell, context.Grid))
                    .ThenBy(a => Utils.GetCellPotential(a.MoveCell, context.Grid))
                    .ThenByDescending(a => a.BuildCell.Level)
                    .FirstOrDefault());
            }
        }

        return actions.FirstOrDefault();
    }
}

internal class BuildHandler : ActionsHandler
{
    public override Action GetBestAction(GameContext context)
    {
        Console.Error.WriteLine("Build");

        return context.AvailableActions
            .Where(a => a.Type == ActionType.MoveAndBuild)
            .Where(a => a.BuildCell.Level <= a.MoveCell.Level && Math.Abs(a.MoveCell.Level - a.Unit.Level) <= 1)
            .OrderByDescending(a => a.MoveCell.Level)
            .ThenByDescending(a => Utils.GetCellPotential(a.MoveCell, context.Grid))
            .ThenBy(a => Utils.NumberOfNeighbouringCellsToBePushedOn(a.MoveCell, context.Grid))
            .ThenBy(a => Utils.NumberOfFreeAdjacentCells(a.BuildCell, context.AllUnits, context.Grid))
            .ThenByDescending(a => a.BuildCell.Level)
            .FirstOrDefault(a => a.BuildCell.Level != Constants.TargetLevel);
    }
}

internal class PushEnemyHandler : ActionsHandler
{

    public override Action GetBestAction(GameContext context)
    {
        Console.Error.WriteLine("PushEnemy");

        return context.AvailableActions
            .Where(a => a.Type == ActionType.PushAndBuild)
            .Where(a => Utils.NumberOfFreeAdjacentCells(Utils.GetCellFromPoint(a.Unit.Point, context.Grid), context.AllUnits, context.Grid) != 1)
            .OrderBy(a => a.BuildCell.Level)
            .Where(a => (a.MoveCell.Level > 1 && !Utils.IsCellOccupied(a.BuildCell, context.AllUnits)) ||
            (a.Unit.Level >= Constants.TargetLevel - 1 && Utils.IsCellOccupied(a.MoveCell, context.EnemyUnits)))
            .FirstOrDefault();
    }
}

public class Cell
{
    public Cell() { }

    public Cell(char v, int x, int y)
    {
        this.Level = v == '.' ? Constants.HoleLevel : int.Parse(v.ToString());
        this.Point = new Point(x, y);
    }

    public Point Point { get; set; }
    public int Level { get; set; }

    public bool IsAvailableForMove(int level)
    {
        return this.Level != Constants.HoleLevel && this.Level != Constants.RemovedCellLevel &&
                this.Level <= level + Constants.UpwardsReachRange;
    }

    internal bool IsAvailableForBuild(int level)
    {
        return this.Level != Constants.HoleLevel && this.Level != Constants.RemovedCellLevel;
    }

    public override string ToString()
    {
        return $"{this.Level} {this.Point}";
    }

    public Cell Clone()
    {
        return new Cell()
        {
            Level = this.Level,
            Point = this.Point
        };
    }
}

public class Action
{
    public Action(string type, Unit unit, string moveDirection, string buildDirection, Cell[,] grid)
    {
        this.Type = this.GetActionFromString(type);
        this.Unit = unit;
        this.MoveDirection = (Direction)Enum.Parse(typeof(Direction), moveDirection);
        this.BuildDirection = (Direction)Enum.Parse(typeof(Direction), buildDirection);
        this.Grid = grid;
    }

    public ActionType Type { get; set; }
    public Unit Unit { get; set; }
    public Direction MoveDirection { get; set; }
    public Direction BuildDirection { get; set; }
    public Cell[,] Grid { get; private set; }

    public Cell MoveCell
    {
        get { return this.GetMoveCell(this.Unit, this.MoveDirection); }
    }

    public Cell BuildCell
    {
        get { return this.GetBuildCell(this.Unit, this.MoveDirection, this.BuildDirection); }
    }

    private Cell GetBuildCell(Unit unit, Direction moveDirection, Direction buildDirection)
    {
        var cell = this.GetMoveCell(unit, moveDirection);
        var cellX = cell.Point.X + Utils.GetDeltaXFromDirection(buildDirection);
        var cellY = cell.Point.Y + Utils.GetDeltaYFromDirection(buildDirection);
        //Console.Error.WriteLine($"{cellY}, {cellX}");
        var buildCell = Grid[cellY, cellX];
        return buildCell;
    }

    private Cell GetMoveCell(Unit unit, Direction moveDirection)
    {
        var cellX = unit.Point.X + Utils.GetDeltaXFromDirection(moveDirection);
        var cellY = unit.Point.Y + Utils.GetDeltaYFromDirection(moveDirection);
        //Console.Error.WriteLine($"{cellY}, {cellX}");
        var cell = Grid[cellY, cellX];
        return cell;
    }

    private ActionType GetActionFromString(string type)
    {
        switch (type)
        {
            case Constants.MoveAndBuildAction:
                return ActionType.MoveAndBuild;
            case Constants.PushAndBuildAction:
                return ActionType.PushAndBuild;
        }

        return ActionType.MoveAndBuild;
    }

    public override string ToString()
    {
        return $"{this.Type.ToFriendlyString()} {this.Unit.Id} {this.MoveDirection} {this.BuildDirection}";
    }
}

public enum ActionType
{
    MoveAndBuild = 0,
    PushAndBuild = 1,
}

public struct Point
{
    public Point(int x, int y)
    {
        this.X = x;
        this.Y = y;
    }

    public int X { get; set; }
    public int Y { get; set; }

    public override string ToString()
    {
        return $"{X} {Y}";
    }

    public override bool Equals(object obj)
    {
        if (!(obj is Point))
        {
            return false;
        }

        var otherPoint = (Point)obj;
        return this.X == otherPoint.X && this.Y == otherPoint.Y;
    }

    public override int GetHashCode()
    {
        return 23 * this.X.GetHashCode() * this.Y.GetHashCode();
    }
}

public class Unit
{
    public Unit(int id, int level, int x, int y)
    {
        this.Id = id;
        this.Point = new Point(x, y);
        this.Level = level;
    }

    public int Level { get; set; }

    public int Id { get; set; }
    public Point Point { get; set; }

    public override bool Equals(object obj)
    {
        if (obj is Unit)
        {
            var otherUnit = obj as Unit;
            return this.Id == otherUnit.Id && this.Point.Equals(otherUnit.Point);
        }

        return false;
    }

    public override int GetHashCode()
    {
        return 31 * 31 * this.Point.X * this.Point.Y * this.Id;
    }
}

public enum Direction
{
    N = 0,
    NE = 1,
    E = 2,
    SE = 3,
    S = 4,
    SW = 5,
    W = 6,
    NW = 7
}

public static class Constants
{
    public const int HoleLevel = -1;
    public const int TargetLevel = 3;
    public const int RemovedCellLevel = 4;
    public const int UpwardsReachRange = 1;
    public const string HoleCharacter = ".";
    public const string MoveAndBuildAction = "MOVE&BUILD";
    public const string PushAndBuildAction = "PUSH&BUILD";
    public const string AcceptDefeatAction = "ACCEPT-DEFEAT Well done!";
}

public static class ActonTypeExtensions
{
    public static string ToFriendlyString(this ActionType type)
    {
        switch (type)
        {
            case ActionType.MoveAndBuild:
                return Constants.MoveAndBuildAction;
            case ActionType.PushAndBuild:
                return Constants.PushAndBuildAction;
            default:
                return string.Empty;
        }
    }
}

public static class Utils
{
    public static Direction GetOppositeDirection(Direction moveDirection)
    {
        var intValue = (int)moveDirection;
        var direction = (Direction)Math.Abs((intValue + 4) % 8);
        Console.Error.WriteLine(direction);
        return direction;
    }

    public static int GetDeltaYFromDirection(Direction moveDirection)
    {
        switch (moveDirection)
        {
            case Direction.E:
            case Direction.W:
                return 0;
            case Direction.NE:
            case Direction.N:
            case Direction.NW:
                return -1;
            case Direction.SW:
            case Direction.SE:
            case Direction.S:
                return 1;
            default:
                return 0;
        }
    }

    public static int GetDeltaXFromDirection(Direction moveDirection)
    {
        switch (moveDirection)
        {
            case Direction.N:
            case Direction.S:
                return 0;
            case Direction.NE:
            case Direction.E:
            case Direction.SE:
                return 1;
            case Direction.SW:
            case Direction.W:
            case Direction.NW:
                return -1;
            default:
                return 0;
        }
    }

    public static bool AreAdjacent(Point a, Point b)
    {
        var deltaX = Math.Abs(a.X - b.X);
        var deltaY = Math.Abs(a.Y - b.Y);

        if (deltaX <= 1 && deltaY <= 1)
        {
            return true;
        }

        return false;
    }

    public static List<Cell> GetNeighbouringCells(Point point, Cell[,] grid)
    {
        var cells = new List<Cell>();
        for (int i = -1; i <= 1; i++)
        {
            for (int j = -1; j <= 1; j++)
            {
                if (i == 0 && j == 0)
                {
                    continue;
                }

                var cellX = point.X + j;
                var cellY = point.Y + i;

                if (cellY < 0 || cellX < 0 || cellX >= grid.GetLength(1) || cellY >= grid.GetLength(0))
                {
                    continue;
                }

                cells.Add(grid[cellY, cellX]);
            }
        }

        return cells;
    }

    public static int NumberOfFreeAdjacentCells(Cell cell, IEnumerable<Unit> units, Cell[,] grid)
    {
        var cells = Utils.GetNeighbouringCells(cell.Point, grid);
        return cells.Where(c => c.IsAvailableForMove(cell.Level) && !IsCellOccupied(c, units)).Count();
    }

    public static bool IsCellOccupied(Cell cell, IEnumerable<Unit> units)
    {
        return units.Any(u => u.Point.Equals(cell.Point));
    }

    public static Cell[,] CloneGrid(Cell[,] grid)
    {
        var newGrid = new Cell[grid.GetLength(0), grid.GetLength(1)];
        for (var i = 0; i < grid.GetLength(0); i++)
        {
            for (var j = 0; j < grid.GetLength(0); j++)
            {
                newGrid[i, j] = grid[i, j].Clone();
            }
        }

        return newGrid;
    }

    public static List<Direction> GetAdjacentDirections(Direction direction)
    {
        switch (direction)
        {
            case Direction.N:
                return new List<Direction>() { direction, Direction.NE, Direction.NW };
            case Direction.NW:
                return new List<Direction>() { direction, Direction.N, Direction.W };
            default:
                return new List<Direction>() { direction, (Direction)((int)direction - 1), (Direction)((int)direction + 1) };
        }
    }

    public static Direction GetDirection(Point point1, Point point2)
    {
        string direction = string.Empty;
        switch (point1.Y - point2.Y)
        {
            case -1:
                direction += 'S';
                break;
            case 1:
                direction += 'N';
                break;
            default:
                break;
        }

        switch (point1.X - point2.X)
        {
            case -1:
                direction += 'E';
                break;
            case 1:
                direction += 'W';
                break;
            default:
                break;
        }

        return (Direction)Enum.Parse(typeof(Direction), direction);
    }

    public static int NumberOfNeighbouringCellsToBePushedOn(Cell cell, Cell[,] grid)
    {
        var result = Utils.GetNeighbouringCells(cell.Point, grid)
            .Where(c => c.IsAvailableForMove(cell.Level))
            .Where(c => c.Level < cell.Level)
            .Count();

        return result;
    }

    public static int GetCellPotential(Cell cell, Cell[,] grid)
    {
        var result = 0;
        foreach (var c in Utils.GetNeighbouringCells(cell.Point, grid).Where(c => c.IsAvailableForMove(cell.Level)))
        {
            result += c.Level;
        }

        return result;
    }

    public static Cell GetCellFromPoint(Point point, Cell[,] grid)
    {
        return grid[point.Y, point.X];
    }
}