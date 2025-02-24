using System;
using System.Collections.Generic;

public class ExerciseData
{
    public int RepetitionID { get; set; }
    public string Name { get; set; }
    public string LegsUsed { get; set; }
    public int Intro { get; set; }
    public int Demo { get; set; }
    public int PreparationCop { get; set; }
    public int TimingCop { get; set; }
    public int Release { get; set; }
    public int Sets { get; private set; }
    public List<ZoneSequenceItem> ZoneSequence { get; set; }

    public ExerciseData(int repetitionID,
                        string name,
                        string legsUsed,
                        int intro,
                        int demo,
                        int preparationCop,
                        int timingCop,
                        int release,
                        int sets,
                        List<(int duration, (double, double) greenZoneX, (double, double) greenZoneY, (double, double) redZoneX, (double, double) redZoneY)> zoneSequence)
    {
        RepetitionID = repetitionID;
        Name = name;
        LegsUsed = legsUsed;
        Intro = intro;
        Demo = demo;
        PreparationCop = preparationCop;
        TimingCop = timingCop;
        Release = release;
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
            repetitionID: 1, //repetitions
            name: "Single-Leg Stance - Right Leg",
            legsUsed: "right",
            intro: 4,
            demo: 4,
            preparationCop: 3,
            timingCop: 30,
            release: 3,
            sets: 1,
            zoneSequence: new List<(int, (double, double), (double, double), (double, double), (double, double))>
            {
                (30, (-1.0, 1.0), (-1.0, 1.0), (-2.0, -1.0), (-6.0, -1.1))
            }
        ),
        new ExerciseData( 
            repetitionID: 2,
            name: "Single-Leg Stance - Left Leg",
            legsUsed: "left",
            intro: 0,
            demo: 0,
            preparationCop: 3,
            timingCop: 30,
            release: 3,
            sets: 1,
            zoneSequence: new List<(int, (double, double), (double, double), (double, double), (double, double))>
            {
                (30, (-1.0, 1.0), (-1.0, 1.0), (1.0, 2.0), (-6.0, -1.1))
            }
        ),
        new ExerciseData( 
            repetitionID: 3,
            name: "Squats With Toe Rise",
            legsUsed: "both",
            intro: 0,
            demo: 6,
            preparationCop: 6,
            timingCop: 30,
            release: 2,
            sets: 2,
            zoneSequence: new List<(int, (double, double), (double, double), (double, double), (double, double))>
            {
                (3, (0.0, 1.0), (-4.0, 4.0), (-1.5, 2.0), (-6.0, 6.0)),
                (1, (-1.0, 1.0), (-1.0, 1.0), (-2.0, 2.0), (-4.0, 2.0)),
                (1, (0.5, 1.9), (1.0, 5.9), (-2.0, 2.0), (0.0, 6.0)),
                (1, (-1.0, 1.0), (-1.0, 1.0), (-2.0, 2.0), (-4.0, 2.0))
            }
        ),
         new ExerciseData( 
            repetitionID: 4,
            name: "Vertical Jumps",
            legsUsed: "both",
            intro: 1,
            demo: 3,
            preparationCop: 3,
            timingCop: 30,
            release: 2,
            sets: 2,
            zoneSequence: new List<(int, (double, double), (double, double), (double, double), (double, double))>
            {
                (2, (-1.0, 1.0), (-1.0, 1.0), (-2.0, 2.0), (-4.0, 4.0)),
                (2, (-1.5, 1.0), (-2.0, 2.0), (-2.0, 1.5), (-5.0, 5.0)),
                (2, (-1.5, 1.5), (1.0, 2.0), (-2.0, 2.0), (0.0, 1.0)) //not checking cop here, during jump
            }
        ),
        new ExerciseData( 
            repetitionID: 5,
            name: "Squats Walking Lunges - Both Leg",
            legsUsed: "both",
            intro: 1,
            demo: 3,
            preparationCop: 3,
            timingCop: 50,
            release: 2,
            sets: 1,
            zoneSequence: new List<(int, (double, double), (double, double), (double, double), (double, double))>
            {
                (1, (-1.0, 1.0), (-1.0, 1.0), (-2.0, 2.0), (-4.0, 4.0)),
                (2, (-1.5, 1.5), (-3.0, 3.0), (-1.9, 1.9), (-5.0, 5.0)), //right leg,  left leg (2, (-1.5, 1.5), (0.3, 5.5), (-2.0, 2.0), (0.0, 6.0))
                (2, (-1.5, 1.5), (0.3, 5.5), (-2.0, 2.0), (0.0, 6.0)), // right leg, left leg (2, (-1.5, 1.5), (-3.0, 3.0), (-1.9, 1.9), (-5.0, 5.0))
                (8, (-1.5, 1.5), (1.0, 2.0), (-2.0, 2.0), (0.0, 1.0)) //no COP check
            }
        ),
        new ExerciseData( 
            repetitionID: 6,
            name: "Jumping - Lateral Jumps Both",
            legsUsed: "both",
            intro: 1,
            demo: 3,
            preparationCop: 3,
            timingCop: 60,
            release: 2,
            sets: 2,
            zoneSequence: new List<(int, (double, double), (double, double), (double, double), (double, double))>
            {
                (1, (-1.0, 1.0), (-1.0, 1.0), (-2.0, 2.0), (-4.0, 4.0)),
                (2, (-1.5, 1.5), (0.5, 1.9), (-2.0, 2.0), (0.0, 6.0)), //right leg, left leg (2, (-1.0, 1.0), (-5.0, 5.0), (-1.5, 1.5), (-6.0, 6.0))
                (2, (-1.0, 1.0), (-5.0, 5.0), (-1.5, 1.5), (-6.0, 6.0)) // right leg, left leg (2, (-1.5, 1.5), (0.5, 1.9), (-2.0, 2.0), (0.0, 6.0))
            }
        ),
        new ExerciseData( 
            repetitionID: 7,
            name: "Squats - One-leg Squats Right",
            legsUsed: "right",
            intro: 1,
            demo: 3,
            preparationCop: 3,
            timingCop: 60,
            release: 2,
            sets: 1,
            zoneSequence: new List<(int, (double, double), (double, double), (double, double), (double, double))>
            {
                (1, (-1.0, 1.0), (-1.0, 1.0), (-2.0, 2.0), (-4.0, 4.0)),
                (2, (-1.5, 1.0), (-3.5, 3.5), (-2.0, 1.5), (-6.0, 6.0)),
                (2, (-1.0, 1.0), (-1.0, 1.0), (-2.0, -1.5), (-6.0, 6.0))
            }
        ),
        new ExerciseData( 
            repetitionID: 8,
            name: "Squats - One-leg Squats Left",
            legsUsed: "left",
            intro: 0,
            demo: 0,
            preparationCop: 0,
            timingCop: 60,
            release: 2,
            sets: 1,
            zoneSequence: new List<(int, (double, double), (double, double), (double, double), (double, double))>
            {
                (1, (-1.0, 1.0), (-1.0, 1.0), (-2.0, 2.0), (-4.0, 4.0)),
                (2, (-1.5, 1.0), (-3.5, 3.5), (-2.0, 1.5), (-6.0, 6.0)),
                (2, (-1.0, 1.0), (-1.0, 1.0), (-2.0, -1.5), (-6.0, 6.0))
            }
        ),
    new ExerciseData( 
            repetitionID: 9,
            name: "Jumping - Box Jumps",
            legsUsed: "both",
            intro: 1,
            demo: 3,
            preparationCop: 3,
            timingCop: 30,
            release: 2,
            sets: 2,
            zoneSequence: new List<(int, (double, double), (double, double), (double, double), (double, double))>
            {
                (2, (-1.0, 1.0), (-1.0, 1.0), (-2.0, 2.0), (-4.0, 4.0)),
                (28, (-1.5, 1.5), (-3.0, 3.0), (-2.0, 2.0), (-4.5, 4.5))
            }
        )
    };
}