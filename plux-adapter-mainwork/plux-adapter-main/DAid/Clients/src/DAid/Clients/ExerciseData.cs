using System;
using System.Collections.Generic;

public class ExerciseData
{
    public int ExerciseID { get; set; } // Added Exercise Number
    public string Name { get; set; }
    public (double Min, double Max) GreenZoneX { get; private set; }
    public (double Min, double Max) GreenZoneY { get; private set; }
    public ((double Min, double Max) Range1, (double Min, double Max) Range2) RedZoneX { get; private set; }
    public ((double Min, double Max) Range1, (double Min, double Max) Range2) RedZoneY { get; private set; }
    public int Timing { get; set; }
    public string LegsUsed { get; set; }

    public ExerciseData(int exerciseID,
                        string name, 
                        (double Min, double Max) greenZoneX, 
                        (double Min, double Max) greenZoneY,
                        ((double Min, double Max) Range1, (double Min, double Max) Range2) redZoneX, 
                        ((double Min, double Max) Range1, (double Min, double Max) Range2) redZoneY,
                        int timing, 
                        string legsUsed)
    {
        ExerciseID = exerciseID;
        Name = name;
        GreenZoneX = greenZoneX;
        GreenZoneY = greenZoneY;
        RedZoneX = redZoneX;
        RedZoneY = redZoneY;
        Timing = timing;
        LegsUsed = legsUsed;
    }

public bool IsInGreenZone(double x, double y)
{

    return x >= GreenZoneX.Min && x <= GreenZoneX.Max &&
           y >= GreenZoneY.Min && y <= GreenZoneY.Max;
}

public bool IsInRedZone(double x, double y)
{
    bool isInRedX = 
        (x >= RedZoneX.Range1.Min && x <= RedZoneX.Range1.Max) || 
        (RedZoneX.Range2.Min != 0 && x >= RedZoneX.Range2.Min && x <= RedZoneX.Range2.Max);

    bool isInRedY = 
        (y >= RedZoneY.Range1.Min && y <= RedZoneY.Range1.Max) || 
        (RedZoneY.Range2.Min != 0 && y >= RedZoneY.Range2.Min && y <= RedZoneY.Range2.Max);

    return isInRedX || isInRedY;
}
}



// List of Exercises
public static class ExerciseList
{
    public static List<ExerciseData> Exercises = new List<ExerciseData>
    {
        new ExerciseData(
            exerciseID: 1,
            name: "Single-Leg Stance - Right Leg",
            greenZoneX: (-1.0, 1.0),
            greenZoneY: (-1.0, 1.0),
            redZoneX: ((-2.0, -1.0), (1.0, 2.0)), 
            redZoneY: ((-6.0, -1.5), (1.5, 6.0)),  
            timing: 30,
            legsUsed: "right"
        ),
        new ExerciseData(
            exerciseID: 2,
            name: "Single-Leg Stance - Left Leg",
            greenZoneX: (-1.0, 1.0),
            greenZoneY: (-1.0, 1.0),
            redZoneX: ((0.0, 2.0),  (0.0, 0.0)),
            redZoneY: ((-6.0, -1.5), (1.5, 6.0)),  
            timing: 30,
            legsUsed: "left"
        ),
        new ExerciseData(
            exerciseID: 3,
            name: "Squats With Toe Rise",
            greenZoneX: (-1.0, 1.0),
            greenZoneY: (-1.0, 1.0),
            redZoneX: ((0.0, 2.0),  (0.0, 0.0)),
            redZoneY: ((-6.0, -1.5), (1.5, 6.0)),  
            timing: 30,
            legsUsed: "both"
        ),
        new ExerciseData(
            exerciseID: 4,
            name: "Vertical Jumps",
            greenZoneX: (-1.0, 1.0),
            greenZoneY: (-1.0, 1.0),
            redZoneX: ((0.0, 2.0),  (0.0, 0.0)),
            redZoneY: ((-6.0, -1.5), (1.5, 6.0)),  
            timing: 30,
            legsUsed: "both"
        ),
        new ExerciseData(
            exerciseID: 5,
            name: "Squats Walking Lunges",
            greenZoneX: (-1.0, 1.0),
            greenZoneY: (-1.0, 1.0),
            redZoneX: ((0.0, 2.0),  (0.0, 0.0)),
            redZoneY: ((-6.0, -1.5), (1.5, 6.0)),  
            timing: 60,
            legsUsed: "right"
        ),
         new ExerciseData(
            exerciseID: 6,
            name: "Squats Walking Lunges",
            greenZoneX: (-1.0, 1.0),
            greenZoneY: (-1.0, 1.0),
            redZoneX: ((0.0, 2.0),  (0.0, 0.0)),
            redZoneY: ((-6.0, -1.5), (1.5, 6.0)),  
            timing: 60,
            legsUsed: "left"
        ),
         new ExerciseData(
            exerciseID: 7,
            name: "Jumping - Lateral Jumps",
            greenZoneX: (-1.0, 1.0),
            greenZoneY: (-1.0, 1.0),
            redZoneX: ((0.0, 2.0),  (0.0, 0.0)),
            redZoneY: ((-6.0, -1.5), (1.5, 6.0)),  
            timing: 60,
            legsUsed: "right"
        ),
         new ExerciseData(
            exerciseID: 8,
            name: "Jumping - Lateral Jumps",
            greenZoneX: (-1.0, 1.0),
            greenZoneY: (-1.0, 1.0),
            redZoneX: ((0.0, 2.0),  (0.0, 0.0)),
            redZoneY: ((-6.0, -1.5), (1.5, 6.0)),  
            timing: 60,
            legsUsed: "left"
        ),
        new ExerciseData(
            exerciseID: 9,
            name: "Squats - One-leg Squats",
            greenZoneX: (-1.0, 1.0),
            greenZoneY: (-1.0, 1.0),
            redZoneX:((0.0, 2.0),  (0.0, 0.0)),
            redZoneY: ((-6.0, -1.5), (1.5, 6.0)),  
            timing: 60,
            legsUsed: "right"
        ),
        new ExerciseData(
            exerciseID: 10,
            name: "Squats - One-leg Squats",
            greenZoneX: (-1.0, 1.0),
            greenZoneY: (-1.0, 1.0),
            redZoneX:((0.0, 2.0),  (0.0, 0.0)),
            redZoneY: ((-6.0, -1.5), (1.5, 6.0)),  
            timing: 60,
            legsUsed: "left"
        ),
        new ExerciseData(
            exerciseID: 11,
            name: "Jumping - Box Jumps",
            greenZoneX: (-1.0, 1.0),
            greenZoneY: (-1.0, 1.0),
            redZoneX: ((0.0, 2.0),  (0.0, 0.0)),
            redZoneY: ((-6.0, -1.5), (1.5, 6.0)),  
            timing: 30,
            legsUsed: "both"
        ),
         new ExerciseData(
            exerciseID: 11,
            name: "Jumping - Box Jumps, 2nd set",
            greenZoneX: (-1.0, 1.0),
            greenZoneY: (-1.0, 1.0),
            redZoneX: ((0.0, 2.0),  (0.0, 0.0)),
            redZoneY: ((-6.0, -1.5), (1.5, 6.0)),  
            timing: 30,
            legsUsed: "both"
        ),

    };
}
