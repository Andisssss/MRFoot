using System;
using System.Collections.Generic;

public class ExerciseData
{
    public int ExerciseID { get; set; }
    public string Name { get; set; }
    public string LegsUsed { get; set; }
    public int Intro { get; set; }
    public int Demo { get; set; }
    public int PreparationCop { get; set; }
    public int TimingCop { get; set; }
    public int Release { get; set; }
    public int Switch { get; set; }
    public int Sets { get; private set; }
    public List<ZoneSequenceItem> ZoneSequence { get; set; }

    public ExerciseData(int exerciseID,
                        string name,
                        string legsUsed,
                        int intro,
                        int demo,
                        int preparationCop,
                        int timingCop,
                        int release,
                        int switchDelay,
                        int sets,
                        List<(int duration, (double, double) greenZoneX, (double, double) greenZoneY, (double, double) redZoneX, (double, double) redZoneY)> zoneSequence)
    {
        ExerciseID = exerciseID;
        Name = name;
        LegsUsed = legsUsed;
        Intro = intro;
        Demo = demo;
        PreparationCop = preparationCop;
        TimingCop = timingCop;
        Release = release;
        Switch = switchDelay;
        Sets = sets > 0 ? sets : 1;
        ZoneSequence = new List<ZoneSequenceItem>();
        foreach (var item in zoneSequence)
        {
            ZoneSequence.Add(new ZoneSequenceItem
            {
                Duration = item.duration,
                GreenZoneX = item.greenZoneX,
                GreenZoneY = item.greenZoneY,
                RedZoneX = item.redZoneX,
                RedZoneY = item.redZoneY
            });
        }
    }
}
public class ZoneSequenceItem
{
    public int Duration { get; set; }
    public (double, double) GreenZoneX { get; set; }
    public (double, double) GreenZoneY { get; set; }
    public (double, double) RedZoneX { get; set; }
    public (double, double) RedZoneY { get; set; }
}

public static class ExerciseList
{
    public static List<ExerciseData> Exercises = new List<ExerciseData>
    {
        new ExerciseData( 
            exerciseID: 1,
            name: "Single-Leg Stance - Right Leg",
            legsUsed: "right",
            intro: 1,
            demo: 3,
            preparationCop: 3,
            timingCop: 30,
            release: 3,
            switchDelay: 3,
            sets: 1,
            zoneSequence: new List<(int, (double, double), (double, double), (double, double), (double, double))>
            {
                (30, (-1.0, 1.0), (-1.0, 1.0), (-2.0, -1.0), (-6.0, -1.1))
            }
        ),
        new ExerciseData( 
            exerciseID: 2,
            name: "Single-Leg Stance - Left Leg",
            legsUsed: "left",
            intro: 0,
            demo: 0,
            preparationCop: 0,
            timingCop: 30,
            release: 3,
            switchDelay: 3,
            sets: 1,
            zoneSequence: new List<(int, (double, double), (double, double), (double, double), (double, double))>
            {
                (30, (-1.0, 1.0), (-1.0, 1.0), (1.0, 2.0), (-6.0, -1.1))
            }
        ),
        new ExerciseData( 
            exerciseID: 3,
            name: "Squats With Toe Rise",
            legsUsed: "both",
            intro: 1,
            demo: 3,
            preparationCop: 3,
            timingCop: 30,
            release: 2,
            switchDelay: 3,
            sets: 2,
            zoneSequence: new List<(int, (double, double), (double, double), (double, double), (double, double))>
            {
                (1, (-1.0, 1.0), (-1.0, 1.0), (-2.0, -1.0), (-4.0, -2.0)), 
                (1, (-1.0, 1.0), (-1.0, 1.0), (1.0, 2.0), (1.0, 4.0)),
                (2, (0.0, 1.5), (-4.5, 4.5), (-1.0, 0.0), (-6.0, -4.5)),
                (2, (0.0, 1.5), (-4.5, 4.5), (1.5, 2.0), (4.5, 6.0))
            }
        ),
         new ExerciseData( 
            exerciseID: 4,
            name: "Vertical Jumps",
            legsUsed: "both",
            intro: 1,
            demo: 3,
            preparationCop: 3,
            timingCop: 30,
            release: 2,
            switchDelay: 3,
            sets: 2,
            zoneSequence: new List<(int, (double, double), (double, double), (double, double), (double, double))>
            {
                (1, (-1.0, 1.0), (-1.0, 1.0), (-2.0, 2.0), (-4.0, 4.0)),
                (2, (-1.5, 1.0), (-5.0, 5.0), (-2.0, 1.5), (-6.0, 6.0)),
                (2, (-1.5, 1.5), (1.0, 2.0), (-2.0, 2.0), (0.0, 1.0))
            }
        ),
        new ExerciseData( 
            exerciseID: 5,
            name: "Squats Walking Lunges - Right Leg",
            legsUsed: "right",
            intro: 1,
            demo: 3,
            preparationCop: 3,
            timingCop: 50,
            release: 2,
            switchDelay: 3,
            sets: 1,
            zoneSequence: new List<(int, (double, double), (double, double), (double, double), (double, double))>
            {
                (1, (-1.0, 1.0), (-1.0, 1.0), (-2.0, 2.0), (-4.0, 4.0)),
                (2, (-1.0, 1.0), (-4.0, 4.0), (-1.0, 1.5), (-5.0, 5.0)),
                (8, (-1.5, 1.5), (1.0, 2.0), (-2.0, 2.0), (0.0, 1.0))
            }
        ),
        new ExerciseData( 
            exerciseID: 6,
            name: "Squats Walking Lunges - Left Leg",
            legsUsed: "left",
            intro: 0,
            demo: 0,
            preparationCop: 0,
            timingCop: 50,
            release: 2,
            switchDelay: 3,
            sets: 1,
            zoneSequence: new List<(int, (double, double), (double, double), (double, double), (double, double))>
            {
               (1, (-1.0, 1.0), (-1.0, 1.0), (-2.0, 2.0), (-4.0, 4.0)),
                (2, (1.0, 2.0), (-1.5, 1.5), (0.0, 1.0), (-2.0, 2.0)),
                (8, (-1.5, 1.5), (1.0, 2.0), (-2.0, 2.0), (0.0, 1.0))
            }
        ),
        new ExerciseData( 
            exerciseID: 7,
            name: "Jumping - Lateral Jumps Right",
            legsUsed: "right",
            intro: 1,
            demo: 3,
            preparationCop: 3,
            timingCop: 60,
            release: 2,
            switchDelay: 3,
            sets: 1,
            zoneSequence: new List<(int, (double, double), (double, double), (double, double), (double, double))>
            {
                (1, (-1.0, 1.0), (-1.0, 1.0), (-2.0, 2.0), (-4.0, 4.0)),
                (2, (-1.0, 1.0), (-1.0, 1.0), (-1.8, 1.2), (-6.0, 2.0)),
                (2, (-1.0, 1.0), (-1.0, 1.0), (-1.2, 1.8), (-6.0, 2.0))
            }
        ),
        new ExerciseData( 
            exerciseID: 8,
            name: "Jumping - Lateral Jumps Left",
            legsUsed: "left",
            intro: 0,
            demo: 0,
            preparationCop: 0,
            timingCop: 60,
            release: 2,
            switchDelay: 3,
            sets: 1,
            zoneSequence: new List<(int, (double, double), (double, double), (double, double), (double, double))>
            {
                (1, (-1.0, 1.0), (-1.0, 1.0), (-2.0, 2.0), (-4.0, 4.0)),
                (2, (-1.5, 1.5), (1.0, 2.0), (-2.0, 2.0), (0.0, 1.0)),
                (2, (-1.5, 1.0), (-5.0, 5.0), (-2.0, 1.5), (-6.0, 6.0))
            }
        ),
        new ExerciseData( 
            exerciseID: 9,
            name: "Squats - One-leg Squats Right",
            legsUsed: "right",
            intro: 1,
            demo: 3,
            preparationCop: 3,
            timingCop: 60,
            release: 2,
            switchDelay: 3,
            sets: 1,
            zoneSequence: new List<(int, (double, double), (double, double), (double, double), (double, double))>
            {
                (1, (-1.0, 1.0), (-1.0, 1.0), (-2.0, 2.0), (-4.0, 4.0)),
                (2, (-1.5, 1.0), (-3.5, 3.5), (-2.0, 1.5), (-6.0, 6.0)),
                (2, (-1.0, 1.0), (-1.0, 1.0), (-2.0, -1.5), (-6.0, 6.0))
            }
        ),
        new ExerciseData( 
            exerciseID: 10,
            name: "Squats - One-leg Squats Left",
            legsUsed: "left",
            intro: 0,
            demo: 0,
            preparationCop: 0,
            timingCop: 60,
            release: 2,
            switchDelay: 3,
            sets: 1,
            zoneSequence: new List<(int, (double, double), (double, double), (double, double), (double, double))>
            {
                (1, (-1.0, 1.0), (-1.0, 1.0), (-2.0, 2.0), (-4.0, 4.0)),
                (2, (-1.5, 1.0), (-3.5, 3.5), (-2.0, 1.5), (-6.0, 6.0)),
                (2, (-1.0, 1.0), (-1.0, 1.0), (-2.0, -1.5), (-6.0, 6.0))
            }
        ),
    new ExerciseData( 
            exerciseID: 11,
            name: "Jumping - Box Jumps",
            legsUsed: "both",
            intro: 1,
            demo: 3,
            preparationCop: 3,
            timingCop: 30,
            release: 2,
            switchDelay: 3,
            sets: 2,
            zoneSequence: new List<(int, (double, double), (double, double), (double, double), (double, double))>
            {
                (2, (-1.0, 1.0), (-1.0, 1.0), (-2.0, 2.0), (-4.0, 4.0)),
                (28, (-1.5, 1.5), (-3.0, 3.0), (-2.0, 2.0), (-4.5, 4.5))
            }
        )
    };
}